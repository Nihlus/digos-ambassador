//
//  MessageReader.cs
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

using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Remora.Commands.Parsers;
using Remora.Commands.Results;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Discord.TypeReaders;

/// <summary>
/// Parses Discord message links and IDs into complete message objects.
/// </summary>
public class MessageReader : AbstractTypeParser<IMessage>
{
    private static readonly Regex Pattern = new
    (
        @"(?<!<)https?://(?:(?:ptb|canary)\.)?discord(?:app)?\.com/channels/(?<GuildId>\d+)/(?<ChannelId>\d+)/(?<MessageId>\d+)(?!>)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    );

    private readonly IDiscordRestChannelAPI _channelAPI;
    private readonly ICommandContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageReader"/> class.
    /// </summary>
    /// <param name="channelAPI">The chanel API.</param>
    /// <param name="context">The command context.</param>
    public MessageReader(IDiscordRestChannelAPI channelAPI, ICommandContext context)
    {
        _channelAPI = channelAPI;
        _context = context;
    }

    /// <inheritdoc />
    public override async ValueTask<Result<IMessage>> TryParseAsync(string value, CancellationToken ct = default)
    {
        value = value.Trim();

        var match = Pattern.Match(value);
        if (!match.Success)
        {
            if (!Snowflake.TryParse(value, out var parsedID))
            {
                return new ParsingError<IMessage>(value);
            }

            var getParsedMessage = await _channelAPI.GetChannelMessageAsync(_context.ChannelID, parsedID.Value, ct);
            return !getParsedMessage.IsSuccess
                ? getParsedMessage
                : Result<IMessage>.FromSuccess(getParsedMessage.Entity);
        }

        var rawChannelID = match.Groups["ChannelId"].Value;
        var rawMessageID = match.Groups["MessageId"].Value;

        if (!Snowflake.TryParse(rawChannelID, out var channelID) ||
            !Snowflake.TryParse(rawMessageID, out var messageID))
        {
            return new ParsingError<IMessage>(value);
        }

        var getMessage = await _channelAPI.GetChannelMessageAsync(channelID.Value, messageID.Value, ct);
        return !getMessage.IsSuccess
            ? getMessage
            : Result<IMessage>.FromSuccess(getMessage.Entity);
    }
}
