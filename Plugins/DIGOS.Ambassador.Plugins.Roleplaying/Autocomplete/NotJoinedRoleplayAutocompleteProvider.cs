//
//  NotJoinedRoleplayAutocompleteProvider.cs
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
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;
using Remora.Discord.Commands.Contexts;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Autocomplete
{
    /// <summary>
    /// Provides autocomplete suggestions for Roleplay names.
    /// </summary>
    public class NotJoinedRoleplayAutocompleteProvider : IAutocompleteProvider
    {
        private readonly InteractionContext _context;
        private readonly RoleplayingDatabaseContext _database;

        /// <inheritdoc />
        public string Identity => "roleplay::notjoined";

        /// <summary>
        /// Initializes a new instance of the <see cref="NotJoinedRoleplayAutocompleteProvider"/> class.
        /// </summary>
        /// <param name="context">The interaction context.</param>
        /// <param name="database">The database context.</param>
        public NotJoinedRoleplayAutocompleteProvider(InteractionContext context, RoleplayingDatabaseContext database)
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
            var scopedRoleplays = _context.GuildID.HasValue
                ? _database.Roleplays
                    .Where(r => r.Server.DiscordID == _context.GuildID.Value)
                    .Where
                    (
                        r => r.ParticipatingUsers
                            .Where(pu => pu.Status == ParticipantStatus.Joined)
                            .All(ju => ju.User.DiscordID != _context.User.ID)
                    )
                : _database.Roleplays
                    .Where
                    (
                        r => r.ParticipatingUsers
                            .Where(pu => pu.Status == ParticipantStatus.Joined)
                            .All(ju => ju.User.DiscordID != _context.User.ID)
                    );

            var suggestedRoleplays = await scopedRoleplays
                .OrderBy(r => EF.Functions.FuzzyStringMatchLevenshtein(r.Name, userInput))
                .Take(25)
                .Select(r => r.Name)
                .ToListAsync(ct);

            return suggestedRoleplays.Select
            (
                n => new ApplicationCommandOptionChoice(n, n)
            ).ToList();
        }
    }
}
