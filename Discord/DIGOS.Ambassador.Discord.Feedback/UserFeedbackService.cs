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
using System.Threading.Tasks;
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
    [PublicAPI]
    public class UserFeedbackService
    {
        private IDiscordRestChannelAPI _channelAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserFeedbackService"/> class.
        /// </summary>
        /// <param name="channelAPI">The channel API.</param>
        public UserFeedbackService(IDiscordRestChannelAPI channelAPI)
        {
            _channelAPI = channelAPI;
        }

        /// <summary>
        /// Send a positive confirmation message.
        /// </summary>
        /// <param name="channel">The channel to send the message to.</param>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<IReadOnlyList<Result<IMessage>>> SendConfirmationAsync
        (
            Snowflake channel,
            Snowflake? target,
            string contents
        )
            => SendEmbedAsync(channel, target, Color.Purple, contents);

        /// <summary>
        /// Send a negative error message.
        /// </summary>
        /// <param name="channel">The channel to send the message to.</param>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<IReadOnlyList<Result<IMessage>>> SendErrorAsync
        (
            Snowflake channel,
            Snowflake? target,
            string contents
        )
            => SendEmbedAsync(channel, target, Color.Red, contents);

        /// <summary>
        /// Send an alerting warning message.
        /// </summary>
        /// <param name="channel">The channel to send the message to.</param>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<IReadOnlyList<Result<IMessage>>> SendWarningAsync
        (
            Snowflake channel,
            Snowflake? target,
            string contents
        )
            => SendEmbedAsync(channel, target, Color.Orange, contents);

        /// <summary>
        /// Send an informational message.
        /// </summary>
        /// <param name="channel">The channel to send the message to.</param>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<IReadOnlyList<Result<IMessage>>> SendInfoAsync
        (
            Snowflake channel,
            Snowflake? target,
            string contents
        )
            => SendEmbedAsync(channel, target, Color.Blue, contents);

        /// <summary>
        /// Sends the given string as one or more sequential embeds, chunked into sets of 1024 characters.
        /// </summary>
        /// <param name="channel">The channel to send the embed to.</param>
        /// <param name="target">The target user to mention, if any.</param>
        /// <param name="color">The embed colour.</param>
        /// <param name="contents">The contents to send.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<IReadOnlyList<Result<IMessage>>> SendEmbedAsync
        (
            Snowflake channel,
            Snowflake? target,
            Color color,
            string contents
        )
        {
            var sendResults = new List<Result<IMessage>>();

            // Sometimes the content is > 2048 in length. We'll chunk it into embeds of 1024 here.
            if (contents.Length < 1024)
            {
                var eb = CreateFeedbackEmbed(target, color, contents);
                sendResults.Add(await _channelAPI.CreateMessageAsync(channel, embed: eb));

                return sendResults;
            }

            var words = contents.Split(' ');
            var messageBuilder = new StringBuilder();
            foreach (var word in words)
            {
                if (messageBuilder.Length >= 1024)
                {
                    var eb = CreateFeedbackEmbed(target, color, messageBuilder.ToString());
                    sendResults.Add(await _channelAPI.CreateMessageAsync(channel, embed: eb));

                    messageBuilder.Clear();
                }

                messageBuilder.Append(word);
                messageBuilder.Append(" ");
            }

            if (messageBuilder.Length > 0)
            {
                var eb = CreateFeedbackEmbed(target, color, messageBuilder.ToString());
                sendResults.Add(await _channelAPI.CreateMessageAsync(channel, embed: eb));
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
            color ??= Color.Purple;

            var eb = new Embed { Colour = color.Value };
            return eb;
        }
    }
}
