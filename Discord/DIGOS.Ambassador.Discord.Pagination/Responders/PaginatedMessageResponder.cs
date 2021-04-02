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

using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Interactivity.Responders;
using DIGOS.Ambassador.Discord.Pagination.Extensions;
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
        IResponder<IMessageReactionAdd>,
        IResponder<IMessageReactionRemove>
    {
        private readonly IDiscordRestChannelAPI _channelAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaginatedMessageResponder"/> class.
        /// </summary>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="channelAPI">The channel API.</param>
        public PaginatedMessageResponder(InteractivityService interactivity, IDiscordRestChannelAPI channelAPI)
            : base(interactivity)
        {
            _channelAPI = channelAPI;
        }

        /// <inheritdoc />
        public Task<Result> RespondAsync(IMessageReactionAdd gatewayEvent, CancellationToken ct = default)
            => OnReactionAsync(gatewayEvent.UserID, gatewayEvent.MessageID, gatewayEvent.Emoji, ct);

        /// <inheritdoc />
        public Task<Result> RespondAsync(IMessageReactionRemove gatewayEvent, CancellationToken ct = default)
            => OnReactionAsync
            (
                gatewayEvent.UserID,
                gatewayEvent.MessageID,
                gatewayEvent.Emoji,
                ct
            );

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

        /// <summary>
        /// Handles an added reaction.
        /// </summary>
        /// <param name="userID">The ID of the reacting user.</param>
        /// <param name="messageID">The ID of the message.</param>
        /// <param name="emoji">The emoji used.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        private async Task<Result> OnReactionAsync
        (
            Snowflake userID,
            Snowflake messageID,
            IPartialEmoji emoji,
            CancellationToken ct = default
        )
        {
            if (!this.Interactivity.TryGetInteractiveEntity<PaginatedMessage>(messageID.ToString(), out var message))
            {
                return Result.FromSuccess();
            }

            try
            {
                await message.Semaphore.WaitAsync(ct);

                if (userID != message.SourceUserID)
                {
                    // We handled it, but we won't react
                    return Result.FromSuccess();
                }

                var reactionName = emoji.GetEmojiName();
                if (!message.ReactionNames.TryGetValue(reactionName, out var knownEmoji))
                {
                    // This isn't an emoji we react to
                    return Result.FromSuccess();
                }

                // Special actions
                if (knownEmoji.Equals(message.Appearance.Close))
                {
                    return await _channelAPI.DeleteMessageAsync(message.ChannelID, message.MessageID, ct);
                }

                if (knownEmoji.Equals(message.Appearance.Help))
                {
                    var embed = new Embed { Colour = Color.Cyan, Description = message.Appearance.HelpText };
                    var sendHelp = await _channelAPI.CreateMessageAsync(message.ChannelID, embed: embed, ct: ct);
                    return !sendHelp.IsSuccess
                        ? Result.FromError(sendHelp)
                        : Result.FromSuccess();
                }

                // Page movement actions
                var didPageUpdate = false;
                if (knownEmoji.Equals(message.Appearance.First))
                {
                    didPageUpdate = message.MoveFirst();
                }

                if (knownEmoji.Equals(message.Appearance.Back))
                {
                    didPageUpdate = message.MovePrevious();
                }

                if (knownEmoji.Equals(message.Appearance.Next))
                {
                    didPageUpdate = message.MoveNext();
                }

                if (knownEmoji.Equals(message.Appearance.Last))
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

        /// <summary>
        /// Updates the contents of the interactive message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        private async Task<Result> UpdateAsync(PaginatedMessage message, CancellationToken ct = default)
        {
            var page = message.GetCurrentPage();

            var updateButtons = await UpdateReactionButtonsAsync(message, ct);
            if (!updateButtons.IsSuccess)
            {
                return updateButtons;
            }

            var modifyMessage = await _channelAPI.EditMessageAsync
            (
                message.ChannelID,
                message.MessageID,
                embed: page,
                ct: ct
            );

            return modifyMessage.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(modifyMessage);
        }

        /// <summary>
        /// Updates the displayed buttons on the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        private async Task<Result> UpdateReactionButtonsAsync(PaginatedMessage message, CancellationToken ct = default)
        {
            var getDiscordMessage = await _channelAPI.GetChannelMessageAsync(message.ChannelID, message.MessageID, ct);
            if (!getDiscordMessage.IsSuccess)
            {
                return Result.FromError(getDiscordMessage);
            }

            var discordMessage = getDiscordMessage.Entity;
            var existingReactions = discordMessage.Reactions;

            var reactions = new[]
            {
                message.Appearance.First.GetEmojiName(),
                message.Appearance.Back.GetEmojiName(),
                message.Appearance.Next.GetEmojiName(),
                message.Appearance.Last.GetEmojiName(),
                message.Appearance.Close.GetEmojiName(),
                message.Appearance.Help.GetEmojiName()
            };

            foreach (var reaction in reactions)
            {
                if (existingReactions.HasValue)
                {
                    if (existingReactions.Value!.Any(r => r.Emoji.GetEmojiName() == reaction))
                    {
                        // This one is already added; skip it
                        continue;
                    }
                }

                var addReaction = await _channelAPI.CreateReactionAsync
                (
                    message.ChannelID,
                    message.MessageID,
                    reaction,
                    ct
                );

                if (!addReaction.IsSuccess)
                {
                    return addReaction;
                }
            }

            return Result.FromSuccess();
        }
    }
}
