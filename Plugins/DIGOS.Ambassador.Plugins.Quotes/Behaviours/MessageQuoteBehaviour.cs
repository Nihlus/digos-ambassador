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
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Plugins.Quotes.Services;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Discord.Behaviours;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Quotes.Behaviours
{
    /// <summary>
    /// Generates quotes from message links. Based on code from MODiX.
    /// </summary>
    public class MessageQuoteBehaviour : ClientEventBehaviour<MessageQuoteBehaviour>
    {
        private static readonly Regex Pattern = new Regex
        (
            @"https?://(?:(?:ptb|canary)\.)?discordapp\.com/channels/(?<GuildId>\d+)/(?<ChannelId>\d+)/(?<MessageId>\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        );

        private readonly QuoteService _quotes;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageQuoteBehaviour"/> class.
        /// </summary>
        /// <param name="client">The discord client.</param>
        /// <param name="serviceScope">The service scope in use.</param>
        /// <param name="logger">The logging instance for this type.</param>
        /// <param name="quotes">The feedback service.</param>
        public MessageQuoteBehaviour
        (
            DiscordSocketClient client,
            IServiceScope serviceScope,
            ILogger<MessageQuoteBehaviour> logger,
            QuoteService quotes
        )
            : base(client, serviceScope, logger)
        {
            _quotes = quotes;
        }

        /// <inheritdoc />
        protected override async Task<OperationResult> MessageUpdatedAsync
        (
            Cacheable<IMessage, ulong> oldMessage,
            SocketMessage? updatedMessage,
            ISocketMessageChannel messageChannel
        )
        {
            if (updatedMessage is null)
            {
                return OperationResult.FromError("The message was null.");
            }

            // Ignore all changes except text changes
            var isTextUpdate = updatedMessage.EditedTimestamp.HasValue &&
                               updatedMessage.EditedTimestamp.Value > DateTimeOffset.Now - 1.Minutes();

            if (!isTextUpdate)
            {
                return OperationResult.FromSuccess();
            }

            return await MessageReceivedAsync(updatedMessage);
        }

        /// <inheritdoc />
        protected override async Task<OperationResult> MessageReceivedAsync(SocketMessage arg)
        {
            if (!(arg is SocketUserMessage message))
            {
                return OperationResult.FromError("The message was not a user message.");
            }

            if (!(message.Author is SocketGuildUser guildUser))
            {
                return OperationResult.FromError("The author was not a guild user.");
            }

            if ((arg.Author.IsBot && !arg.Author.IsMe(this.Client)) || arg.Author.IsWebhook)
            {
                return OperationResult.FromSuccess();
            }

            var discard = 0;

            if (message.HasCharPrefix('!', ref discard))
            {
                return OperationResult.FromSuccess();
            }

            if (message.HasMentionPrefix(this.Client.CurrentUser, ref discard))
            {
                return OperationResult.FromSuccess();
            }

            if (IsQuote(message))
            {
                return OperationResult.FromSuccess();
            }

            var matches = Pattern.Matches(message.Content);
            foreach (Match? match in matches)
            {
                if (match is null)
                {
                    continue;
                }

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
                    return OperationResult.FromSuccess();
                }

                var embed = _quotes.CreateMessageQuote(quotedMessage, guildUser);
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

            if (!(message.Channel is IGuildChannel messageGuildChannel))
            {
                return OperationResult.FromSuccess();
            }

            var guildBotUser = await messageGuildChannel.Guild.GetUserAsync(this.Client.CurrentUser.Id);

            var messageCharacterCount = message.Content.Select(c => !char.IsWhiteSpace(c)).Count();
            var matchesCharacterCount = matches.Sum(m => m.Length);

            // If the message consists of just links (i.e, no other text outside of the links, not counting
            // whitespace), we'll delete it
            if (messageCharacterCount == matchesCharacterCount && guildBotUser.GuildPermissions.ManageMessages)
            {
                await message.DeleteAsync();
            }

            return OperationResult.FromSuccess();
        }

        /// <summary>
        /// Determines if the given message is a quoted message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>true if the message is a quoted message; otherwise, false.</returns>
        private bool IsQuote(IMessage message)
        {
            var hasQuoteField =
                message
                    .Embeds?
                    .SelectMany(d => d.Fields)
                    .Any(d => d.Name == "Quoted by");

            return hasQuoteField.HasValue && hasQuoteField.Value;
        }
    }
}
