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

using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback.Results;
using JetBrains.Annotations;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Discord.Feedback
{
    /// <summary>
    /// Handles sending formatted messages to the users.
    /// </summary>
    public class UserFeedbackService
    {
        private IDiscordRestChannelAPI _channelAPI;
        private IDiscordRestUserAPI _userAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserFeedbackService"/> class.
        /// </summary>
        /// <param name="channelAPI">The channel API.</param>
        /// <param name="userAPI">The user API.</param>
        public UserFeedbackService(IDiscordRestChannelAPI channelAPI, IDiscordRestUserAPI userAPI)
        {
            _channelAPI = channelAPI;
            _userAPI = userAPI;
        }

        /// <summary>
        /// Send a positive confirmation message.
        /// </summary>
        /// <param name="channel">The channel to send the message to.</param>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<IReadOnlyList<Result<IMessage>>> SendConfirmationAsync
        (
            Snowflake channel,
            Snowflake? target,
            string contents,
            CancellationToken ct = default
        )
            => SendMessageAsync(channel, target, new ConfirmationMessage(contents), ct);

        /// <summary>
        /// Send a negative error message.
        /// </summary>
        /// <param name="channel">The channel to send the message to.</param>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<IReadOnlyList<Result<IMessage>>> SendErrorAsync
        (
            Snowflake channel,
            Snowflake? target,
            string contents,
            CancellationToken ct = default
        )
            => SendEmbedAsync(channel, target, Color.OrangeRed, contents, ct);

        /// <summary>
        /// Send an alerting warning message.
        /// </summary>
        /// <param name="channel">The channel to send the message to.</param>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<IReadOnlyList<Result<IMessage>>> SendWarningAsync
        (
            Snowflake channel,
            Snowflake? target,
            string contents,
            CancellationToken ct = default
        )
            => SendMessageAsync(channel, target, new WarningMessage(contents), ct);

        /// <summary>
        /// Send an informational message.
        /// </summary>
        /// <param name="channel">The channel to send the message to.</param>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<IReadOnlyList<Result<IMessage>>> SendInfoAsync
        (
            Snowflake channel,
            Snowflake? target,
            string contents,
            CancellationToken ct = default
        )
            => SendMessageAsync(channel, target, new InfoMessage(contents), ct);

        /// <summary>
        /// Send a message.
        /// </summary>
        /// <param name="channel">The channel to send the message to.</param>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<IReadOnlyList<Result<IMessage>>> SendMessageAsync
        (
            Snowflake channel,
            Snowflake? target,
            UserMessage message,
            CancellationToken ct = default
        )
            => SendEmbedAsync(channel, target, message.Colour, message.Message, ct);

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
        /// Sends the given string as one or more sequential embeds, chunked into sets of 1024 characters.
        /// </summary>
        /// <param name="channel">The channel to send the embed to.</param>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="color">The embed colour.</param>
        /// <param name="contents">The contents to send.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<IReadOnlyList<Result<IMessage>>> SendEmbedAsync
        (
            Snowflake channel,
            Snowflake? target,
            Color color,
            string contents,
            CancellationToken ct = default
        )
        {
            var sendResults = new List<Result<IMessage>>();

            // Sometimes the content is > 2048 in length. We'll chunk it into embeds of 1024 here.
            if (contents.Length < 1024)
            {
                var eb = CreateFeedbackEmbed(target, color, contents);
                sendResults.Add(await _channelAPI.CreateMessageAsync(channel, embed: eb, ct: ct));

                return sendResults;
            }

            var words = contents.Split(' ');
            var messageBuilder = new StringBuilder();
            foreach (var word in words)
            {
                if (messageBuilder.Length >= 1024)
                {
                    var eb = CreateFeedbackEmbed(target, color, messageBuilder.ToString());
                    sendResults.Add(await _channelAPI.CreateMessageAsync(channel, embed: eb, ct: ct));

                    messageBuilder.Clear();
                }

                messageBuilder.Append(word);
                messageBuilder.Append(" ");
            }

            if (messageBuilder.Length > 0)
            {
                var eb = CreateFeedbackEmbed(target, color, messageBuilder.ToString());
                sendResults.Add(await _channelAPI.CreateMessageAsync(channel, embed: eb, ct: ct));
            }

            return sendResults;
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
    }
}
