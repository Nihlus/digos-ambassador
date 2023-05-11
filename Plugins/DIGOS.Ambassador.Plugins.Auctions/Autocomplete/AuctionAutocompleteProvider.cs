//
//  AuctionAutocompleteProvider.cs
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
using DIGOS.Ambassador.Plugins.Auctions.Model;
using DIGOS.Ambassador.Plugins.Auctions.Model.Data;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;

namespace DIGOS.Ambassador.Plugins.Auctions.Autocomplete;

/// <summary>
/// Provides suggestions for auctions.
/// </summary>
public class AuctionAutocompleteProvider : IAutocompleteProvider<Auction>
{
    private readonly IOperationContext _context;
    private readonly AuctionsDatabaseContext _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuctionAutocompleteProvider"/> class.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="database">The database.</param>
    public AuctionAutocompleteProvider(IOperationContext context, AuctionsDatabaseContext database)
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
        if (!_context.TryGetGuildID(out var guildID))
        {
            return ArraySegment<IApplicationCommandOptionChoice>.Empty;
        }

        _ = _context.TryGetUserID(out var userID);

        var auctions = await _database.Auctions
            .AsNoTracking()
            .Where(a => a.Server.DiscordID == guildID)
            .OrderBy(c => EF.Functions.FuzzyStringMatchLevenshtein(c.Name, userInput))
            .ThenBy(a => a.Owner.DiscordID == userID)
            .ThenBy(a => a.State == AuctionState.Open)
            .Take(25)
            .Select(a => new { a.ID, a.Name })
            .ToListAsync(ct);

        return auctions.Select
        (
            a => new ApplicationCommandOptionChoice($"Auction #{a.ID}: {a.Name}", a.Name)
        ).ToArray();
    }
}
