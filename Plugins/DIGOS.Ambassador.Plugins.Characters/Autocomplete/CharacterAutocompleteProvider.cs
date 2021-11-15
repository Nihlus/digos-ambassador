//
//  CharacterAutocompleteProvider.cs
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Characters.Model;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;
using Remora.Discord.Commands.Contexts;

namespace DIGOS.Ambassador.Plugins.Characters.Autocomplete
{
    /// <summary>
    /// Provides autocomplete suggestions for character names.
    /// </summary>
    public class CharacterAutocompleteProvider : IAutocompleteProvider<Character>
    {
        private readonly InteractionContext _context;
        private readonly CharactersDatabaseContext _database;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterAutocompleteProvider"/> class.
        /// </summary>
        /// <param name="context">The interaction context.</param>
        /// <param name="database">The database context.</param>
        public CharacterAutocompleteProvider(InteractionContext context, CharactersDatabaseContext database)
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
            var scopedCharacters = _context.GuildID.HasValue
                ? _database.Characters
                    .Where(c => c.Owner.DiscordID == _context.User.ID)
                    .Where(c => c.Server.DiscordID == _context.GuildID.Value)
                : _database.Characters
                    .Where(c => c.Owner.DiscordID == _context.User.ID);

            var suggestedCharacters = await scopedCharacters
                .OrderBy(c => EF.Functions.FuzzyStringMatchLevenshtein(c.Name, userInput))
                .Take(25)
                .Select(c => new { c.Nickname, c.Name })
                .ToListAsync(ct);

            return suggestedCharacters.Select
            (
                n => new ApplicationCommandOptionChoice(n.Nickname ?? n.Name, n.Name)
            ).ToList();
        }
    }
}
