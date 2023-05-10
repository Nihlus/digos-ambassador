//
//  AuctionParser.cs
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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Auctions.Model;
using DIGOS.Ambassador.Plugins.Auctions.Model.Data;
using Microsoft.EntityFrameworkCore;
using Remora.Commands.Parsers;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Auctions.Parsers;

/// <summary>
/// Parses auctions.
/// </summary>
public class AuctionParser : AbstractTypeParser<Auction>
{
    private readonly IOperationContext _context;
    private readonly AuctionsDatabaseContext _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuctionParser"/> class.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="database">The database.</param>
    public AuctionParser(IOperationContext context, AuctionsDatabaseContext database)
    {
        _context = context;
        _database = database;
    }

    /// <inheritdoc />
    public override async ValueTask<Result<Auction>> TryParseAsync(string token, CancellationToken ct = default)
    {
        if (token.IsNullOrWhitespace())
        {
            return new ArgumentInvalidError(nameof(token), "The auction identifier must not be null or whitespace.");
        }

        if (!_context.TryGetGuildID(out var guildID))
        {
            return new InvalidOperationError("Auctions can only be parsed in guilds.");
        }

        if (long.TryParse(token, out var auctionID))
        {
            var auctionById = await _database.Auctions
                .Where(a => a.Server.DiscordID == guildID)
                .SingleOrDefaultAsync(a => a.ID == auctionID, ct);

            if (auctionById is not null)
            {
                return auctionById;
            }
        }

        var auctionByName = await _database.Auctions
            .Where(a => a.Server.DiscordID == guildID)
            .SingleOrDefaultAsync(a => a.Name == token, ct);

        if (auctionByName is null)
        {
            return new NotFoundError("No auction with the given ID or name could be found.");
        }

        return auctionByName;
    }
}
