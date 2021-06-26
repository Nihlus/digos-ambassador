//
//  PaginatedMessageResponder.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Interactivity.Responders;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DIGOS.Ambassador.Discord.Pagination.Responders
{
    /// <summary>
    /// Responds to events required for interactivity.
    /// </summary>
    public class PaginatedMessageResponder :
        InteractivityResponder,
        IResponder<IInteractionCreate>
    {
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly IDiscordRestInteractionAPI _interactionAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaginatedMessageResponder"/> class.
        /// </summary>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="channelAPI">The channel API.</param>
        /// <param name="interactionAPI">The interaction API.</param>
        public PaginatedMessageResponder
        (
            InteractivityService interactivity,
            IDiscordRestChannelAPI channelAPI,
            IDiscordRestInteractionAPI interactionAPI
        )
            : base(interactivity)
        {
            _channelAPI = channelAPI;
            _interactionAPI = interactionAPI;
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IInteractionCreate gatewayEvent, CancellationToken ct = default)
        {
            if (gatewayEvent.Type != InteractionType.MessageComponent)
            {
                return Result.FromSuccess();
            }

            var data = gatewayEvent.Data.Value ?? throw new InvalidOperationException();
            var type = data.ComponentType.Value;

            if (type != ComponentType.Button)
            {
                return Result.FromSuccess();
            }

            var interactedMessage = gatewayEvent.Message.Value ?? throw new InvalidOperationException();
            var messageID = interactedMessage.ID.ToString();

            if (!this.Interactivity.TryGetInteractiveEntity<PaginatedMessage>(messageID, out var message))
            {
                return Result.FromSuccess();
            }

            // This is something we're supposed to handle
            var respondDeferred = await _interactionAPI.CreateInteractionResponseAsync
            (
                gatewayEvent.ID,
                gatewayEvent.Token,
                new InteractionResponse(InteractionCallbackType.DeferredUpdateMessage),
                ct
            );

            if (!respondDeferred.IsSuccess)
            {
                return respondDeferred;
            }

            var user = gatewayEvent.User.HasValue
                ? gatewayEvent.User.Value
                : gatewayEvent.Member.HasValue
                    ? gatewayEvent.Member.Value.User.HasValue
                        ? gatewayEvent.Member.Value.User.Value
                        : null
                    : null;

            if (user is null)
            {
                return Result.FromSuccess();
            }

            var userID = user.ID;

            var buttonNonce = data.CustomID.Value ?? throw new InvalidOperationException();

            try
            {
                await message.Semaphore.WaitAsync(ct);

                if (userID != message.SourceUserID)
                {
                    // We handled it, but we won't respond
                    return Result.FromSuccess();
                }

                var button = message.Buttons.FirstOrDefault
                (
                    b => b.CustomID.HasValue && b.CustomID.Value == buttonNonce
                );

                if (button is null)
                {
                    // This isn't a button we react to
                    return Result.FromSuccess();
                }

                // Special actions
                if (button == message.Appearance.Close)
                {
                    return await _channelAPI.DeleteMessageAsync(message.ChannelID, message.MessageID, ct);
                }

                if (button == message.Appearance.Help)
                {
                    var embed = new Embed { Colour = Color.Cyan, Description = message.Appearance.HelpText };
                    var sendHelp = await _channelAPI.CreateMessageAsync
                    (
                        message.ChannelID,
                        embeds: new[] { embed },
                        ct: ct
                    );

                    return !sendHelp.IsSuccess
                        ? Result.FromError(sendHelp)
                        : Result.FromSuccess();
                }

                // Page movement actions
                var didPageUpdate = false;
                if (button == message.Appearance.First)
                {
                    didPageUpdate = message.MoveFirst();
                }

                if (button == message.Appearance.Back)
                {
                    didPageUpdate = message.MovePrevious();
                }

                if (button == message.Appearance.Next)
                {
                    didPageUpdate = message.MoveNext();
                }

                if (button == message.Appearance.Last)
                {
                    didPageUpdate = message.MoveLast();
                }

                return didPageUpdate
                    ? await UpdateAsync(message, ct)
                    : Result.FromSuccess();
            }
            finally
            {
                message.Semaphore.Release();
            }
        }

        /// <inheritdoc />
        public override async Task<Result> OnCreateAsync(string nonce, CancellationToken ct = default)
        {
            if (!this.Interactivity.TryGetInteractiveEntity<PaginatedMessage>(nonce, out var message))
            {
                return Result.FromSuccess();
            }

            try
            {
                await message.Semaphore.WaitAsync(ct);
                return await UpdateAsync(message, ct);
            }
            finally
            {
                message.Semaphore.Release();
            }
        }

        private IReadOnlyList<IMessageComponent> GetCurrentPageComponents(PaginatedMessage message)
        {
            return new List<IMessageComponent>
            {
                new ActionRowComponent
                (
                    new[]
                    {
                        message.Appearance.First,
                        message.Appearance.Back,
                        message.Appearance.Next,
                        message.Appearance.Last,
                    }
                ),
                new ActionRowComponent
                (
                    new[]
                    {
                        message.Appearance.Close,
                        message.Appearance.Help
                    }
                )
            };
        }

        /// <summary>
        /// Updates the contents of the interactive message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        private async Task<Result> UpdateAsync(PaginatedMessage message, CancellationToken ct = default)
        {
            var page = message.GetCurrentPage();

            var modifyMessage = await _channelAPI.EditMessageAsync
            (
                message.ChannelID,
                message.MessageID,
                embeds: new[] { page },
                components: new Optional<IReadOnlyList<IMessageComponent>>(GetCurrentPageComponents(message)),
                ct: ct
            );

            return modifyMessage.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(modifyMessage);
        }
    }
}
