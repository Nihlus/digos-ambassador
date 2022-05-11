//
//  CharacterParser.cs
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
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services;
using Remora.Commands.Parsers;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Rest.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Characters.Parsers;

/// <summary>
/// Reads owned characters as command arguments.
/// </summary>
public sealed class CharacterParser : AbstractTypeParser<Character>
{
    private readonly CharacterDiscordService _characterService;
    private readonly ICommandContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterParser"/> class.
    /// </summary>
    /// <param name="characterService">The character service.</param>
    /// <param name="context">The command context.</param>
    public CharacterParser(CharacterDiscordService characterService, ICommandContext context)
    {
        _characterService = characterService;
        _context = context;
    }

    /// <inheritdoc />
    public override async ValueTask<Result<Character>> TryParseAsync(string value, CancellationToken ct = default)
    {
        value = value.Trim();

        if (!_context.GuildID.IsDefined(out var guildID))
        {
            return new UserError("Characters can only be parsed in the context of a guild.");
        }

        // Special case
        if (string.Equals(value, "current", StringComparison.OrdinalIgnoreCase))
        {
            return await _characterService.GetCurrentCharacterAsync(guildID, _context.User.ID, ct);
        }

        if (!value.Contains(':'))
        {
            return await _characterService.GetBestMatchingCharacterAsync
            (
                guildID,
                _context.User.ID,
                value,
                ct
            );
        }

        var parts = value.Split(':');
        if (parts.Length != 2)
        {
            return new UserError
            (
                "When searching a specific user, the name must be in the form \"@someone:name\"."
            );
        }

        var rawUser = parts[0];
        if (!Snowflake.TryParse(rawUser.Unmention(), out var parsedUser))
        {
            return new UserError
            (
                "I couldn't parse whatever you gave me as a user-scoped character search. Try again?"
            );
        }

        var rawName = parts[1];

        return await _characterService.GetBestMatchingCharacterAsync
        (
            _context.GuildID.Value,
            parsedUser,
            rawName,
            ct
        );
    }
}
