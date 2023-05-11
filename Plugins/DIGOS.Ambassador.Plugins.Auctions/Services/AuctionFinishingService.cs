//
//  AuctionFinishingService.cs
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database;
using DIGOS.Ambassador.Plugins.Auctions.Model;
using DIGOS.Ambassador.Plugins.Auctions.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DIGOS.Ambassador.Plugins.Auctions.Services;

/// <summary>
/// Waits for auctions to finish and then announces or alerts winners.
/// </summary>
public class AuctionFinishingService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AuctionFinishingService> _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuctionFinishingService"/> class.
    /// </summary>
    /// <param name="services">The available services.</param>
    /// <param name="log">The logging instance for this type.</param>
    public AuctionFinishingService(IServiceProvider services, ILogger<AuctionFinishingService> log)
    {
        _services = services;
        _log = log;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            {
                using var serviceScope = _services.CreateScope();

                await using var database = serviceScope.ServiceProvider.GetRequiredService<AuctionsDatabaseContext>();
                var auctionService = serviceScope.ServiceProvider.GetRequiredService<AuctionService>();

                var now = DateTimeOffset.UtcNow;
                var finishedAuctions = await database.Auctions
                    .Where(a => a.State == AuctionState.Open)
                    .Where(a => a.EndTime <= now)
                    .ToListAsync(ct);

                foreach (var finishedAuction in finishedAuctions)
                {
                    try
                    {
                        using var transaction = TransactionFactory.Create();

                        var concludeAuction = await auctionService.ConcludeAuctionAsync(finishedAuction, ct);
                        if (!concludeAuction.IsSuccess)
                        {
                            continue;
                        }

                        transaction.Complete();
                    }
                    catch (Exception e)
                    {
                        _log.LogError(e, "Failed to conclude auction #{AuctionID}", finishedAuction.ID);
                    }
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1), ct);
        }
    }
}
