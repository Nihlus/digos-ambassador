//
//  AnyCharacterAutocompleteProvider.cs
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
using DIGOS.Ambassador.Plugins.Characters.Model;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;

namespace DIGOS.Ambassador.Plugins.Characters.Autocomplete;

/// <summary>
/// Provides autocomplete suggestions for character names.
/// </summary>
public class AnyCharacterAutocompleteProvider : IAutocompleteProvider
{
    private readonly IInteractionContext _context;
    private readonly CharactersDatabaseContext _database;

    /// <inheritdoc />
    public string Identity => "character::any";

    /// <summary>
    /// Initializes a new instance of the <see cref="AnyCharacterAutocompleteProvider"/> class.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="database">The database context.</param>
    public AnyCharacterAutocompleteProvider(IInteractionContext context, CharactersDatabaseContext database)
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
        if (!_context.TryGetUserID(out var userID))
        {
            throw new NotSupportedException();
        }

        var scopedCharacters = _context.TryGetGuildID(out var guildID)
            ? _database.Characters
                .Where(c => c.Server.DiscordID == guildID)
            : _database.Characters;

        var suggestedCharacters = await scopedCharacters
            .OrderByDescending(r => r.Owner.DiscordID == userID)
            .ThenBy(c => EF.Functions.FuzzyStringMatchLevenshtein(c.Name, userInput))
            .Take(25)
            .Select(c => new { c.Nickname, c.Name })
            .ToListAsync(ct);

        return suggestedCharacters.Select
        (
            n => new ApplicationCommandOptionChoice(n.Nickname ?? n.Name, n.Name)
        ).ToList();
    }
}
