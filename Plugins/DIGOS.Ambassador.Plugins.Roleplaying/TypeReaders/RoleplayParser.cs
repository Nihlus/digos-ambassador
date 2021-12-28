//
//  RoleplayParser.cs
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
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using Remora.Commands.Parsers;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Roleplaying.TypeReaders;

/// <summary>
/// Reads owned roleplays as command arguments. The name "current" is reserved, and will retrieve the current
/// roleplay.
/// </summary>
public sealed class RoleplayParser : AbstractTypeParser<Roleplay>
{
    private readonly RoleplayDiscordService _roleplays;
    private readonly ICommandContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleplayParser"/> class.
    /// </summary>
    /// <param name="roleplays">The roleplaying service.</param>
    /// <param name="context">The command context.</param>
    public RoleplayParser(RoleplayDiscordService roleplays, ICommandContext context)
    {
        _roleplays = roleplays;
        _context = context;
    }

    /// <inheritdoc />
    public override async ValueTask<Result<Roleplay>> TryParseAsync(string value, CancellationToken ct = default)
    {
        value = value.Trim();

        if (!_context.GuildID.HasValue)
        {
            throw new InvalidOperationException();
        }

        if (string.Equals(value, "current", StringComparison.OrdinalIgnoreCase))
        {
            return await _roleplays.GetActiveRoleplayAsync(_context.ChannelID);
        }

        if (!value.Contains(':'))
        {
            return await _roleplays.GetBestMatchingRoleplayAsync
            (
                _context.ChannelID,
                _context.GuildID.Value,
                _context.User.ID,
                value
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
                "I couldn't parse whatever you gave me as a user-scoped roleplay search. Try again?"
            );
        }

        var rawName = parts[1];

        return await _roleplays.GetBestMatchingRoleplayAsync
        (
            _context.ChannelID,
            _context.GuildID.Value,
            parsedUser,
            rawName
        );
    }
}
