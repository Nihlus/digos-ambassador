//
//  AnyRoleplayAutocompleteProvider.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Autocomplete;

/// <summary>
/// Provides autocomplete suggestions for Roleplay names.
/// </summary>
public class AnyRoleplayAutocompleteProvider : IAutocompleteProvider
{
    private readonly InteractionContext _context;
    private readonly RoleplayingDatabaseContext _database;

    /// <inheritdoc />
    public string Identity => "roleplay::any";

    /// <summary>
    /// Initializes a new instance of the <see cref="AnyRoleplayAutocompleteProvider"/> class.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="database">The database context.</param>
    public AnyRoleplayAutocompleteProvider(InteractionContext context, RoleplayingDatabaseContext database)
    {
        _context = context;
        _database = database;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync
    (
        IReadOnlyList<IApplicationCommandInteractionDataOption> options,
        string userInput,
        CancellationToken ct = default
    )
    {
        _ = _context.TryGetGuildID(out var guildID);
        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var scopedRoleplays = guildID is not null
            ? _database.Roleplays
                .Where(r => r.Server.DiscordID == guildID.Value)
            : _database.Roleplays;

        var suggestedRoleplays = await scopedRoleplays
            .OrderByDescending(r => r.Owner.DiscordID == userID.Value)
            .ThenBy(r => EF.Functions.FuzzyStringMatchLevenshtein(r.Name, userInput))
            .Take(25)
            .Select(r => r.Name)
            .ToListAsync(ct);

        return suggestedRoleplays.Select
        (
            n => new ApplicationCommandOptionChoice(n, n)
        ).ToList();
    }
}
