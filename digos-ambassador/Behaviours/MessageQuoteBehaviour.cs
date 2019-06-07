//
//  MessageQuoteBehaviour.cs
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
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Services;

using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;

using Humanizer;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Behaviours
{
    /// <summary>
    /// Generates quotes from message links. Based on code from MODiX.
    /// </summary>
    public class MessageQuoteBehaviour : BehaviourBase
    {
        private static readonly Regex Pattern = new Regex
        (
            @"https?://(?:(?:ptb|canary)\.)?discordapp\.com/channels/(?<GuildId>\d+)/(?<ChannelId>\d+)/(?<MessageId>\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        );

        private readonly UserFeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageQuoteBehaviour"/> class.
        /// </summary>
        /// <param name="client">The discord client.</param>
        /// <param name="feedback">The feedback service.</param>
        public MessageQuoteBehaviour(DiscordSocketClient client, UserFeedbackService feedback)
            : base(client)
        {
            this._feedback = feedback;
        }

        /// <inheritdoc />
        protected override Task OnStartingAsync()
        {
            this.Client.MessageReceived += OnMessageReceived;
            this.Client.MessageUpdated += OnMessageUpdated;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task OnStoppingAsync()
        {
            this.Client.MessageReceived -= OnMessageReceived;
            this.Client.MessageUpdated -= OnMessageUpdated;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles incoming messages, passing them to the command context handler.
        /// </summary>
        /// <param name="arg">The message coming in from the socket client.</param>
        /// <returns>A task representing the message handling.</returns>
        private async Task OnMessageReceived(SocketMessage arg)
        {
            if (!(arg is SocketUserMessage message))
            {
                return;
            }

            if (!(message.Author is SocketGuildUser guildUser))
            {
                return;
            }

            if (arg.Author.IsBot || arg.Author.IsWebhook)
            {
                return;
            }

            int discard = 0;

            if (message.HasCharPrefix('!', ref discard))
            {
                return;
            }

            if (message.HasMentionPrefix(this.Client.CurrentUser, ref discard))
            {
                return;
            }

            foreach (Match match in Pattern.Matches(message.Content))
            {
                if (!ulong.TryParse(match.Groups["GuildId"].Value, out _) ||
                    !ulong.TryParse(match.Groups["ChannelId"].Value, out var channelId) ||
                    !ulong.TryParse(match.Groups["MessageId"].Value, out var messageId))
                {
                    continue;
                }

                var quotedChannel = this.Client.GetChannel(channelId);

                if (!(quotedChannel is IGuildChannel) ||
                    !(quotedChannel is ISocketMessageChannel quotedMessageChannel))
                {
                    continue;
                }

                var quotedMessage = await quotedMessageChannel.GetMessageAsync(messageId);
                if (quotedMessage == null || IsQuote(quotedMessage))
                {
                    return;
                }

                if (message.Channel is IGuildChannel messageGuildChannel)
                {
                    var guildBotUser = await messageGuildChannel.Guild.GetUserAsync(this.Client.CurrentUser.Id);

                    // It's just a single quote link, so we'll delete it
                    if (message.Content == match.Value && guildBotUser.GuildPermissions.ManageMessages)
                    {
                        await message.DeleteAsync();
                    }
                }

                var embed = this._feedback.CreateMessageQuote(quotedMessage, guildUser);
                embed.WithTimestamp(quotedMessage.Timestamp);

                try
                {
                    await message.Channel.SendMessageAsync(string.Empty, embed: embed.Build());
                }
                catch (HttpException hex)
                {
                    if (!hex.WasCausedByMissingPermission())
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Determines if the given message is a quoted message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>true if the message is a quoted message; otherwise, false.</returns>
        private bool IsQuote([NotNull] IMessage message)
        {
            var hasQuoteField =
                message
                    .Embeds?
                    .SelectMany(d => d.Fields)
                    .Any(d => d.Name == "Quoted by");

            return hasQuoteField.HasValue && hasQuoteField.Value;
        }

        /// <summary>
        /// Handles reparsing of edited messages.
        /// </summary>
        /// <param name="oldMessage">The old message.</param>
        /// <param name="updatedMessage">The new message.</param>
        /// <param name="messageChannel">The channel of the message.</param>
        private async Task OnMessageUpdated
        (
            Cacheable<IMessage, ulong> oldMessage,
            [CanBeNull] SocketMessage updatedMessage,
            ISocketMessageChannel messageChannel
        )
        {
            if (updatedMessage is null)
            {
                return;
            }

            // Ignore all changes except text changes
            bool isTextUpdate = updatedMessage.EditedTimestamp.HasValue && (updatedMessage.EditedTimestamp.Value > DateTimeOffset.Now - 1.Minutes());
            if (!isTextUpdate)
            {
                return;
            }

            await OnMessageReceived(updatedMessage);
        }
    }
}
