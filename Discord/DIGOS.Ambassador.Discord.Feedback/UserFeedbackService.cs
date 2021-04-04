//
//  UserFeedbackService.cs
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback.Errors;
using DIGOS.Ambassador.Discord.Feedback.Results;
using DIGOS.Ambassador.Discord.Feedback.Services;
using JetBrains.Annotations;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Discord.Feedback
{
    /// <summary>
    /// Handles sending formatted messages to the users.
    /// </summary>
    public class UserFeedbackService
    {
        private readonly ContextInjectionService _contextInjection;
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly IDiscordRestUserAPI _userAPI;
        private readonly IDiscordRestWebhookAPI _webhookAPI;
        private readonly IdentityInformationService _identity;

        /// <summary>
        /// Holds a value indicating whether the original interaction message (if any) has been edited.
        /// </summary>
        private bool _hasEditedOriginalInteraction;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserFeedbackService"/> class.
        /// </summary>
        /// <param name="channelAPI">The channel API.</param>
        /// <param name="userAPI">The user API.</param>
        /// <param name="contextInjection">The context injection service.</param>
        /// <param name="webhookAPI">The webhook API.</param>
        /// <param name="identity">The identity service.</param>
        public UserFeedbackService
        (
            IDiscordRestChannelAPI channelAPI,
            IDiscordRestUserAPI userAPI,
            ContextInjectionService contextInjection,
            IDiscordRestWebhookAPI webhookAPI,
            IdentityInformationService identity
        )
        {
            _channelAPI = channelAPI;
            _userAPI = userAPI;
            _contextInjection = contextInjection;
            _webhookAPI = webhookAPI;
            _identity = identity;
        }

        /// <summary>
        /// Send a positive confirmation message.
        /// </summary>
        /// <param name="channel">The channel to send the message to.</param>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<Result<IReadOnlyList<IMessage>>> SendConfirmationAsync
        (
            Snowflake channel,
            Snowflake? target,
            string contents,
            CancellationToken ct = default
        )
            => SendMessageAsync(channel, target, new ConfirmationMessage(contents), ct);

        /// <summary>
        /// Send a positive confirmation message.
        /// </summary>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<Result<IReadOnlyList<IMessage>>> SendContextualConfirmationAsync
        (
            Snowflake? target,
            string contents,
            CancellationToken ct = default
        )
            => SendContextualMessageAsync(target, new ConfirmationMessage(contents), ct);

        /// <summary>
        /// Send a negative error message.
        /// </summary>
        /// <param name="channel">The channel to send the message to.</param>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<Result<IReadOnlyList<IMessage>>> SendErrorAsync
        (
            Snowflake channel,
            Snowflake? target,
            string contents,
            CancellationToken ct = default
        )
            => SendContentAsync(channel, target, Color.OrangeRed, contents, ct);

        /// <summary>
        /// Send a negative error message.
        /// </summary>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<Result<IReadOnlyList<IMessage>>> SendContextualErrorAsync
        (
            Snowflake? target,
            string contents,
            CancellationToken ct = default
        )
            => SendContextualContentAsync(target, Color.OrangeRed, contents, ct);

        /// <summary>
        /// Send an alerting warning message.
        /// </summary>
        /// <param name="channel">The channel to send the message to.</param>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<Result<IReadOnlyList<IMessage>>> SendWarningAsync
        (
            Snowflake channel,
            Snowflake? target,
            string contents,
            CancellationToken ct = default
        )
            => SendMessageAsync(channel, target, new WarningMessage(contents), ct);

        /// <summary>
        /// Send an alerting warning message.
        /// </summary>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<Result<IReadOnlyList<IMessage>>> SendContextualWarningAsync
        (
            Snowflake? target,
            string contents,
            CancellationToken ct = default
        )
            => SendContextualMessageAsync(target, new WarningMessage(contents), ct);

        /// <summary>
        /// Send an informational message.
        /// </summary>
        /// <param name="channel">The channel to send the message to.</param>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<Result<IReadOnlyList<IMessage>>> SendInfoAsync
        (
            Snowflake channel,
            Snowflake? target,
            string contents,
            CancellationToken ct = default
        )
            => SendMessageAsync(channel, target, new InfoMessage(contents), ct);

        /// <summary>
        /// Send an informational message.
        /// </summary>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<Result<IReadOnlyList<IMessage>>> SendContextualInfoAsync
        (
            Snowflake? target,
            string contents,
            CancellationToken ct = default
        )
            => SendContextualMessageAsync(target, new InfoMessage(contents), ct);

        /// <summary>
        /// Send a message.
        /// </summary>
        /// <param name="channel">The channel to send the message to.</param>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<Result<IReadOnlyList<IMessage>>> SendMessageAsync
        (
            Snowflake channel,
            Snowflake? target,
            UserMessage message,
            CancellationToken ct = default
        )
            => SendContentAsync(channel, target, message.Colour, message.Message, ct);

        /// <summary>
        /// Send a contextual message.
        /// </summary>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<Result<IReadOnlyList<IMessage>>> SendContextualMessageAsync
        (
            Snowflake? target,
            UserMessage message,
            CancellationToken ct = default
        )
            => SendContextualContentAsync(target, message.Colour, message.Message, ct);

        /// <summary>
        /// Sends the given embed to the given channel.
        /// </summary>
        /// <param name="channel">The channel to send the embed to.</param>
        /// <param name="embed">The embed.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<Result<IMessage>> SendEmbedAsync
        (
            Snowflake channel,
            Embed embed,
            CancellationToken ct = default
        )
        {
            return _channelAPI.CreateMessageAsync(channel, embed: embed, ct: ct);
        }

        /// <summary>
        /// Sends the given embed to current context.
        /// </summary>
        /// <param name="embed">The embed.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<Result<IMessage>> SendContextualEmbedAsync
        (
            Embed embed,
            CancellationToken ct = default
        )
        {
            if (_contextInjection.Context is null)
            {
                return new UserError("Contextual sends require a context to be available.");
            }

            switch (_contextInjection.Context)
            {
                case MessageContext messageContext:
                {
                    return await _channelAPI.CreateMessageAsync(messageContext.ChannelID, embed: embed, ct: ct);
                }
                case InteractionContext interactionContext:
                {
                    if (_hasEditedOriginalInteraction)
                    {
                        return await _webhookAPI.CreateFollowupMessageAsync
                        (
                            _identity.ApplicationID,
                            interactionContext.Token,
                            embeds: new[] { embed },
                            ct: ct
                        );
                    }

                    var edit = await _webhookAPI.EditOriginalInteractionResponseAsync
                    (
                        _identity.ApplicationID,
                        interactionContext.Token,
                        embeds: new[] { embed },
                        ct: ct
                    );

                    if (edit.IsSuccess)
                    {
                        _hasEditedOriginalInteraction = true;
                    }

                    return edit;
                }
                default:
                {
                    throw new InvalidOperationException();
                }
            }
        }

        /// <summary>
        /// Sends the given embed to the given user in their private DM channel.
        /// </summary>
        /// <param name="user">The ID of the user to send the embed to.</param>
        /// <param name="embed">The embed.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<Result<IMessage>> SendPrivateEmbedAsync
        (
            Snowflake user,
            Embed embed,
            CancellationToken ct = default
        )
        {
            var getUserDM = await _userAPI.CreateDMAsync(user, ct);
            if (!getUserDM.IsSuccess)
            {
                return Result<IMessage>.FromError(getUserDM);
            }

            var dm = getUserDM.Entity;

            return await SendEmbedAsync(dm.ID, embed, ct);
        }

        /// <summary>
        /// Creates a feedback embed.
        /// </summary>
        /// <param name="target">The invoking mentionable.</param>
        /// <param name="color">The colour of the embed.</param>
        /// <param name="contents">The contents of the embed.</param>
        /// <returns>A feedback embed.</returns>
        [Pure]
        public Embed CreateFeedbackEmbed(Snowflake? target, Color color, string contents)
        {
            if (target is null)
            {
                return CreateEmbedBase(color) with { Description = contents };
            }

            return CreateEmbedBase(color) with { Description = $"<@{target}> | {contents}" };
        }

        /// <summary>
        /// Creates a base embed.
        /// </summary>
        /// <param name="color">The colour of the embed. Optional.</param>
        /// <returns>A basic embed.</returns>
        [Pure]
        public Embed CreateEmbedBase(Color? color = null)
        {
            color ??= Color.MediumPurple;

            var eb = new Embed { Colour = color.Value };
            return eb;
        }

        /// <summary>
        /// Sends the given string as one or more sequential embeds, chunked into sets of 1024 characters.
        /// </summary>
        /// <param name="channel">The channel to send the embed to.</param>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="color">The embed colour.</param>
        /// <param name="contents">The contents to send.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<Result<IReadOnlyList<IMessage>>> SendContentAsync
        (
            Snowflake channel,
            Snowflake? target,
            Color color,
            string contents,
            CancellationToken ct = default
        )
        {
            var sendResults = new List<IMessage>();
            foreach (var chunk in CreateContentChunks(target, color, contents))
            {
                var send = await SendEmbedAsync(channel, chunk, ct);
                if (!send.IsSuccess)
                {
                    return Result<IReadOnlyList<IMessage>>.FromError(send);
                }

                sendResults.Add(send.Entity);
            }

            return sendResults;
        }

        /// <summary>
        /// Sends the given string as one or more sequential embeds, chunked into sets of 1024 characters.
        /// </summary>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="color">The embed colour.</param>
        /// <param name="contents">The contents to send.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<Result<IReadOnlyList<IMessage>>> SendContextualContentAsync
        (
            Snowflake? target,
            Color color,
            string contents,
            CancellationToken ct = default
        )
        {
            var sendResults = new List<IMessage>();
            foreach (var chunk in CreateContentChunks(target, color, contents))
            {
                var send = await SendContextualEmbedAsync(chunk, ct);
                if (!send.IsSuccess)
                {
                    return Result<IReadOnlyList<IMessage>>.FromError(send);
                }

                sendResults.Add(send.Entity);
            }

            return sendResults;
        }

        /// <summary>
        /// Chunks an input string into one or more embeds. Discord places an internal limit on embed lengths of 2048
        /// characters, and we collapse that into 1024 for readability's sake.
        /// </summary>
        /// <param name="target">The target user, if any.</param>
        /// <param name="color">The color of the embed.</param>
        /// <param name="contents">The complete contents of the message.</param>
        /// <returns>The chunked embeds.</returns>
        [Pure]
        private IEnumerable<Embed> CreateContentChunks(Snowflake? target, Color color, string contents)
        {
            // Sometimes the content is > 2048 in length. We'll chunk it into embeds of 1024 here.
            if (contents.Length < 1024)
            {
                yield return CreateFeedbackEmbed(target, color, contents.Trim());
                yield break;
            }

            var words = contents.Split(' ');
            var messageBuilder = new StringBuilder();
            foreach (var word in words)
            {
                if (messageBuilder.Length >= 1024)
                {
                    yield return CreateFeedbackEmbed(target, color, messageBuilder.ToString().Trim());
                    messageBuilder.Clear();
                }

                messageBuilder.Append(word);
                messageBuilder.Append(' ');
            }

            if (messageBuilder.Length > 0)
            {
                yield return CreateFeedbackEmbed(target, color, messageBuilder.ToString().Trim());
            }
        }
    }
}
