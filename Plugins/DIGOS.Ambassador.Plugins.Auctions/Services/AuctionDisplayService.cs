//
//  AuctionDisplayService.cs
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Auctions.Extensions;
using DIGOS.Ambassador.Plugins.Auctions.Model;
using DIGOS.Ambassador.Plugins.Auctions.Model.Data;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Abstractions.Results;
using Remora.Rest.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Auctions.Services;

/// <summary>
/// Handles display of active auctions.
/// </summary>
public class AuctionDisplayService
{
    private readonly AuctionsDatabaseContext _database;
    private readonly IDiscordRestChannelAPI _channelAPI;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuctionDisplayService"/> class.
    /// </summary>
    /// <param name="database">The auction database.</param>
    /// <param name="channelAPI">The channel API.</param>
    public AuctionDisplayService(AuctionsDatabaseContext database, IDiscordRestChannelAPI channelAPI)
    {
        _database = database;
        _channelAPI = channelAPI;
    }

    /// <summary>
    /// Refreshes all known auction displays.
    /// </summary>
    /// <remarks>
    /// Should be called sparingly, since refreshing an auction display involves several network and database
    /// round-trips.
    /// </remarks>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>An asynchronous result representing the operation.</returns>
    public async Task<Result> RefreshDisplaysAsync(CancellationToken ct = default)
    {
        var auctionDisplays = await _database.AuctionDisplays.ToArrayAsync(ct);
        foreach (var auctionDisplay in auctionDisplays)
        {
            var updateDisplay = await UpdateDisplayAsync(auctionDisplay, ct);
            if (updateDisplay.IsSuccess)
            {
                continue;
            }

            if (updateDisplay.Error is not NotFoundError)
            {
                continue;
            }

            _database.AuctionDisplays.Attach(auctionDisplay);
            _database.AuctionDisplays.Remove(auctionDisplay);
        }

        await _database.SaveChangesAsync(ct);
        return Result.FromSuccess();
    }

    /// <summary>
    /// Updates the displays associated with the given auction.
    /// </summary>
    /// <param name="auction">The auction.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>An asynchronous result representing the operation.</returns>
    public async Task<Result> UpdateDisplaysAsync(Auction auction, CancellationToken ct = default)
    {
        var auctionDisplays = await _database.AuctionDisplays.Where(d => d.Auction.ID == auction.ID).ToArrayAsync(ct);
        foreach (var auctionDisplay in auctionDisplays)
        {
            var updateDisplay = await UpdateDisplayAsync(auctionDisplay, ct);
            if (updateDisplay.IsSuccess)
            {
                continue;
            }

            if (updateDisplay.Error is not NotFoundError)
            {
                continue;
            }

            _database.AuctionDisplays.Attach(auctionDisplay);
            _database.AuctionDisplays.Remove(auctionDisplay);
        }

        await _database.SaveChangesAsync(ct);
        return Result.FromSuccess();
    }

    /// <summary>
    /// Displays the auction in the given channel.
    /// </summary>
    /// <param name="auction">The auction.</param>
    /// <param name="channelID">The ID of the channel to display the auction in.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>An asynchronous result representing the operation.</returns>
    public async Task<Result> DisplayAuctionAsync
    (
        Auction auction,
        Snowflake channelID,
        CancellationToken ct = default
    )
    {
        var alreadyExists = await _database.AuctionDisplays
            .Where(d => d.Auction.ID == auction.ID)
            .Where(d => d.Channel == channelID)
            .AnyAsync(ct);

        if (alreadyExists)
        {
            return new InvalidOperationError("The auction is already being displayed in that channel.");
        }

        var createMessage = await _channelAPI.CreateMessageAsync
        (
            channelID,
            embeds: new[] { auction.CreateEmbed() },
            ct: ct
        );

        if (!createMessage.IsDefined(out var message))
        {
            return (Result)createMessage;
        }

        var display = new AuctionDisplay(auction, channelID, message.ID);

        _database.AuctionDisplays.Add(display);
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <summary>
    /// Hides the auction in the given channel.
    /// </summary>
    /// <param name="auction">The auction.</param>
    /// <param name="channelID">The ID of the channel to hide the auction in, or null for every channel.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>An asynchronous result representing the operation.</returns>
    public async Task<Result> HideAuctionAsync
    (
        Auction auction,
        Snowflake? channelID = null,
        CancellationToken ct = default
    )
    {
        var query = _database.AuctionDisplays.Where(d => d.Auction.ID == auction.ID);
        if (channelID is not null)
        {
            query = query.Where(d => d.Channel == channelID);
        }

        var errors = new List<IResult>();
        await foreach (var auctionDisplay in query.AsAsyncEnumerable().WithCancellation(ct))
        {
            var deleteMessage = await _channelAPI.DeleteMessageAsync
            (
                auctionDisplay.Channel,
                auctionDisplay.Message,
                ct: ct
            );

            if (deleteMessage.IsSuccess || deleteMessage.FailedBecauseOf(DiscordError.UnknownMessage))
            {
                _database.AuctionDisplays.Remove(auctionDisplay);
                continue;
            }

            errors.Add(deleteMessage);
        }

        return errors.Count > 0
            ? new AggregateError(errors)
            : Result.FromSuccess();
    }

    /// <summary>
    /// Updates the specified display.
    /// </summary>
    /// <param name="display">The display.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>An asynchronous result representing the operation.</returns>
    private async Task<Result> UpdateDisplayAsync(AuctionDisplay display, CancellationToken ct = default)
    {
        var editMessage = await _channelAPI.EditMessageAsync
        (
            display.Channel,
            display.Message,
            embeds: new[] { display.Auction.CreateEmbed() },
            ct: ct
        );

        if (editMessage.FailedBecauseOf(DiscordError.UnknownMessage))
        {
            _database.AuctionDisplays.Remove(display);
            await _database.SaveChangesAsync(ct);

            return Result.FromSuccess();
        }

        if (!editMessage.IsSuccess)
        {
            return (Result)editMessage;
        }

        return Result.FromSuccess();
    }
}
