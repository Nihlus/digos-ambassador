//
//  MessageQuoteResponder.cs
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
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Quotes.Services;
using Humanizer;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Core;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Quotes.Responders
{
    /// <summary>
    /// Generates quotes from message links. Based on code from MODiX.
    /// </summary>
    public class MessageQuoteResponder : IResponder<IMessageCreate>, IResponder<IMessageUpdate>
    {
        private static readonly Regex Pattern = new
        (
            @"(?<!<)https?://(?:(?:ptb|canary)\.)?discord(?:app)?\.com/channels/(?<GuildId>\d+)/(?<ChannelId>\d+)/(?<MessageId>\d+)(?!>)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        );

        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly QuoteService _quotes;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageQuoteResponder"/> class.
        /// </summary>
        /// <param name="quotes">The feedback service.</param>
        /// <param name="channelAPI">The Discord channel API.</param>
        public MessageQuoteResponder(QuoteService quotes, IDiscordRestChannelAPI channelAPI)
        {
            _quotes = quotes;
            _channelAPI = channelAPI;
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
        {
            var author = gatewayEvent.Author;
            if ((author.IsBot.HasValue && author.IsBot.Value) || (author.IsSystem.HasValue && author.IsSystem.Value))
            {
                return Result.FromSuccess();
            }

            if (IsQuote(gatewayEvent))
            {
                return Result.FromSuccess();
            }

            return await CreateQuotesAsync(gatewayEvent.ChannelID, gatewayEvent.Content, gatewayEvent.Author.ID, ct);
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IMessageUpdate gatewayEvent, CancellationToken ct = default)
        {
            // Ignore all changes except text changes
            var isTextUpdate = gatewayEvent.EditedTimestamp.HasValue &&
                               gatewayEvent.EditedTimestamp.Value > DateTimeOffset.Now - 1.Minutes();

            if (!isTextUpdate)
            {
                return Result.FromSuccess();
            }

            if (IsQuote(gatewayEvent))
            {
                return Result.FromSuccess();
            }

            if (!gatewayEvent.Content.HasValue)
            {
                return Result.FromSuccess();
            }

            if (!gatewayEvent.Author.HasValue)
            {
                return Result.FromSuccess();
            }

            if (!gatewayEvent.ChannelID.HasValue)
            {
                return Result.FromSuccess();
            }

            return await CreateQuotesAsync
            (
                gatewayEvent.ChannelID.Value,
                gatewayEvent.Content.Value,
                gatewayEvent.Author.Value!.ID,
                ct
            );
        }

        private async Task<Result> CreateQuotesAsync
        (
            Snowflake channelID,
            string content,
            Snowflake quoterID,
            CancellationToken ct = default
        )
        {
            var matches = Pattern.Matches(content);
            if (matches.Count == 0)
            {
                return Result.FromSuccess();
            }

            List<IMessage> quotedMessages = new();
            foreach (Match? match in matches)
            {
                if (match is null)
                {
                    continue;
                }

                if (!Snowflake.TryParse(match.Groups["GuildId"].Value, out _) ||
                    !Snowflake.TryParse(match.Groups["ChannelId"].Value, out var channelId) ||
                    !Snowflake.TryParse(match.Groups["MessageId"].Value, out var messageId))
                {
                    continue;
                }

                var getMessage = await _channelAPI.GetChannelMessageAsync(channelId.Value, messageId.Value, ct);
                if (!getMessage.IsSuccess)
                {
                    // Maybe it's been deleted, maybe it never existed in the first place
                    continue;
                }

                var quotedMessage = getMessage.Entity;
                if (IsQuote(quotedMessage))
                {
                    // No quote recursion please
                    continue;
                }

                quotedMessages.Add(quotedMessage);
            }

            var embeds = quotedMessages.Select
            (
                m => _quotes.CreateMessageQuote(m, quoterID) with { Timestamp = m.Timestamp }
            );

            foreach (var embed in embeds)
            {
                var sendQuote = await _channelAPI.CreateMessageAsync(channelID, embeds: new[] { embed }, ct: ct);
                if (!sendQuote.IsSuccess)
                {
                    return Result.FromError(sendQuote);
                }
            }

            return Result.FromSuccess();
        }

        /// <summary>
        /// Determines if the given message is a quoted message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>true if the message is a quoted message; otherwise, false.</returns>
        private bool IsQuote(IMessage message)
        {
            foreach (var embed in message.Embeds)
            {
                if (!embed.Fields.HasValue)
                {
                    continue;
                }

                var fields = embed.Fields.Value;
                if (fields!.Any(f => f.Name.Contains("Quoted by")))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if the given message is a quoted message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>true if the message is a quoted message; otherwise, false.</returns>
        private bool IsQuote(IPartialMessage message)
        {
            if (!message.Embeds.HasValue)
            {
                return false;
            }

            foreach (var embed in message.Embeds.Value!)
            {
                if (!embed.Fields.HasValue)
                {
                    continue;
                }

                var fields = embed.Fields.Value;
                if (fields!.Any(f => f.Name.Contains("Quoted by")))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
