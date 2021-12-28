//
//  EmojiTypeReader.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Remora.Commands.Parsers;
using Remora.Commands.Results;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Discord.TypeReaders;

/// <summary>
/// Parses server-supported emojis from user input.
/// </summary>
public class EmojiTypeReader : AbstractTypeParser<IEmoji>
{
    private static readonly Regex EmojiRegex = new
    (
        @"\uD83C[\uDDE6-\uDDFF]\uD83C[\uDDE6-\uDDFF]|\uD83C[\uDC04\uDCCF\uDD70\uDD71\uDD7E\uDD7F\uDD8E\uDD91-\uDD9A\uDDE6-\uDDFF\uDE01\uDE02\uDE1A\uDE2F\uDE32-\uDE3A\uDE50\uDE51\uDF00-\uDF21\uDF24-\uDF93\uDF96\uDF97\uDF99-\uDF9B\uDF9E-\uDFF0\uDFF3-\uDFF5\uDFF7-\uDFFF]|\uD83D[\uDC00-\uDCFD\uDCFF-\uDD3D\uDD49-\uDD4E\uDD50-\uDD67\uDD6F\uDD70\uDD73-\uDD7A\uDD87\uDD8A-\uDD8D\uDD90\uDD95\uDD96\uDDA4\uDDA5\uDDA8\uDDB1\uDDB2\uDDBC\uDDC2-\uDDC4\uDDD1-\uDDD3\uDDDC-\uDDDE\uDDE1\uDDE3\uDDE8\uDDEF\uDDF3\uDDFA-\uDE4F\uDE80-\uDEC5\uDECB-\uDED2\uDED5-\uDED7\uDEE0-\uDEE5\uDEE9\uDEEB\uDEEC\uDEF0\uDEF3-\uDEFC\uDFE0-\uDFEB]|\uD83E[\uDD0C-\uDD3A\uDD3C-\uDD45\uDD47-\uDD78\uDD7A-\uDDCB\uDDCD-\uDDFF\uDE70-\uDE74\uDE78-\uDE7A\uDE80-\uDE86\uDE90-\uDEA8\uDEB0-\uDEB6\uDEC0-\uDEC2\uDED0-\uDED6]|[\#\*0-9\u00A9\u00AE\u203C\u2049\u2122\u2139\u2194-\u2199\u21A9\u21AA\u231A\u231B\u2328\u23CF\u23E9-\u23F3\u23F8-\u23FA\u24C2\u25AA\u25AB\u25B6\u25C0\u25FB-\u25FE\u2600-\u2604\u260E\u2611\u2614\u2615\u2618\u261D\u2620\u2622\u2623\u2626\u262A\u262E\u262F\u2638-\u263A\u2640\u2642\u2648-\u2653\u265F\u2660\u2663\u2665\u2666\u2668\u267B\u267E\u267F\u2692-\u2697\u2699\u269B\u269C\u26A0\u26A1\u26A7\u26AA\u26AB\u26B0\u26B1\u26BD\u26BE\u26C4\u26C5\u26C8\u26CE\u26CF\u26D1\u26D3\u26D4\u26E9\u26EA\u26F0-\u26F5\u26F7-\u26FA\u26FD\u2702\u2705\u2708-\u270D\u270F\u2712\u2714\u2716\u271D\u2721\u2728\u2733\u2734\u2744\u2747\u274C\u274E\u2753-\u2755\u2757\u2763\u2764\u2795-\u2797\u27A1\u27B0\u27BF\u2934\u2935\u2B05-\u2B07\u2B1B\u2B1C\u2B50\u2B55\u3030\u303D\u3297\u3299](?:\uD83C[\uDFFB-\uDFFF]|\uFE0F\u20E3?|(?:\uDB40[\uDC20-\uDC7E])+\uDB40\uDC7F)?(?:\u200D\uD83C[\uDC04\uDCCF\uDD70\uDD71\uDD7E\uDD7F\uDD8E\uDD91-\uDD9A\uDDE6-\uDDFF\uDE01\uDE02\uDE1A\uDE2F\uDE32-\uDE3A\uDE50\uDE51\uDF00-\uDF21\uDF24-\uDF93\uDF96\uDF97\uDF99-\uDF9B\uDF9E-\uDFF0\uDFF3-\uDFF5\uDFF7-\uDFFF]|\uD83D[\uDC00-\uDCFD\uDCFF-\uDD3D\uDD49-\uDD4E\uDD50-\uDD67\uDD6F\uDD70\uDD73-\uDD7A\uDD87\uDD8A-\uDD8D\uDD90\uDD95\uDD96\uDDA4\uDDA5\uDDA8\uDDB1\uDDB2\uDDBC\uDDC2-\uDDC4\uDDD1-\uDDD3\uDDDC-\uDDDE\uDDE1\uDDE3\uDDE8\uDDEF\uDDF3\uDDFA-\uDE4F\uDE80-\uDEC5\uDECB-\uDED2\uDED5-\uDED7\uDEE0-\uDEE5\uDEE9\uDEEB\uDEEC\uDEF0\uDEF3-\uDEFC\uDFE0-\uDFEB]|\uD83E[\uDD0C-\uDD3A\uDD3C-\uDD45\uDD47-\uDD78\uDD7A-\uDDCB\uDDCD-\uDDFF\uDE70-\uDE74\uDE78-\uDE7A\uDE80-\uDE86\uDE90-\uDEA8\uDEB0-\uDEB6\uDEC0-\uDEC2\uDED0-\uDED6]|[\#\*0-9\u00A9\u00AE\u203C\u2049\u2122\u2139\u2194-\u2199\u21A9\u21AA\u231A\u231B\u2328\u23CF\u23E9-\u23F3\u23F8-\u23FA\u24C2\u25AA\u25AB\u25B6\u25C0\u25FB-\u25FE\u2600-\u2604\u260E\u2611\u2614\u2615\u2618\u261D\u2620\u2622\u2623\u2626\u262A\u262E\u262F\u2638-\u263A\u2640\u2642\u2648-\u2653\u265F\u2660\u2663\u2665\u2666\u2668\u267B\u267E\u267F\u2692-\u2697\u2699\u269B\u269C\u26A0\u26A1\u26A7\u26AA\u26AB\u26B0\u26B1\u26BD\u26BE\u26C4\u26C5\u26C8\u26CE\u26CF\u26D1\u26D3\u26D4\u26E9\u26EA\u26F0-\u26F5\u26F7-\u26FA\u26FD\u2702\u2705\u2708-\u270D\u270F\u2712\u2714\u2716\u271D\u2721\u2728\u2733\u2734\u2744\u2747\u274C\u274E\u2753-\u2755\u2757\u2763\u2764\u2795-\u2797\u27A1\u27B0\u27BF\u2934\u2935\u2B05-\u2B07\u2B1B\u2B1C\u2B50\u2B55\u3030\u303D\u3297\u3299](?:\uD83C[\uDFFB-\uDFFF]|\uFE0F\u20E3?|(?:\uDB40[\uDC20-\uDC7E])+\uDB40\uDC7F)?)*",
        RegexOptions.Compiled
    );

    private readonly IDiscordRestChannelAPI _channelAPI;
    private readonly IDiscordRestGuildAPI _guildAPI;
    private readonly ICommandContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmojiTypeReader"/> class.
    /// </summary>
    /// <param name="channelAPI">The Discord channel API.</param>
    /// <param name="guildAPI">The Discord guild API.</param>
    /// <param name="context">The command context.</param>
    public EmojiTypeReader
    (
        IDiscordRestChannelAPI channelAPI,
        IDiscordRestGuildAPI guildAPI,
        ICommandContext context
    )
    {
        _channelAPI = channelAPI;
        _guildAPI = guildAPI;
        _context = context;
    }

    /// <inheritdoc />
    public override async ValueTask<Result<IEmoji>> TryParseAsync(string value, CancellationToken ct = default)
    {
        value = value.Trim();

        var regexMatches = EmojiRegex.Matches(value);
        if (regexMatches.Count != 0)
        {
            return regexMatches.Count switch
            {
                < 1 => new ParsingError<IEmoji>(value, "No matching emoji found."),
                > 1 => new ParsingError<IEmoji>(value, "Multiple matching emoji found."),
                _ => new Emoji(null, regexMatches.First().Value)
            };
        }

        var getChannel = await _channelAPI.GetChannelAsync(_context.ChannelID, ct);
        if (!getChannel.IsSuccess)
        {
            return Result<IEmoji>.FromError(getChannel);
        }

        var channel = getChannel.Entity;
        if (!channel.GuildID.HasValue)
        {
            return new ParsingError<IEmoji>(value, "No matching emoji found.");
        }

        var getGuild = await _guildAPI.GetGuildAsync(channel.GuildID.Value, ct: ct);
        if (!getGuild.IsSuccess)
        {
            return Result<IEmoji>.FromError(getGuild);
        }

        var guild = getGuild.Entity;

        IEmoji? guildEmoji;
        if (TryParseEmoji(value, out var parsedEmoji))
        {
            guildEmoji = guild.Emojis.FirstOrDefault
            (
                e => e.Name == parsedEmoji.Name && e.ID == parsedEmoji.ID
            );
        }
        else
        {
            guildEmoji = guild.Emojis.FirstOrDefault
            (
                e => e.Name == value
            );
        }

        return guildEmoji is not null
            ? Result<IEmoji>.FromSuccess(guildEmoji)
            : new ParsingError<IEmoji>(value, "No matching emoji found.");
    }

    private bool TryParseEmoji(string input, [NotNullWhen(true)] out IEmoji? emoji)
    {
        emoji = null;

        if (input.Length < 2)
        {
            return false;
        }

        input = input[1..^1];
        var inputParts = input.Split(':', StringSplitOptions.RemoveEmptyEntries);

        if (inputParts.Length is < 2 or > 3)
        {
            return false;
        }

        if (!Snowflake.TryParse(inputParts[^1], out var emojiID))
        {
            return false;
        }

        emoji = new Emoji(emojiID, inputParts[^2], IsAnimated: inputParts.Length == 3 && inputParts[0] == "a");
        return true;
    }
}
