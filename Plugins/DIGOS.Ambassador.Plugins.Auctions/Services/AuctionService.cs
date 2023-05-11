//
//  AuctionService.cs
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Auctions.Model;
using DIGOS.Ambassador.Plugins.Auctions.Model.Data;
using Microsoft.Extensions.Logging;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Auctions.Services;

/// <summary>
/// Defines operations on auctions.
/// </summary>
public class AuctionService
{
    private readonly ILogger<AuctionService> _log;
    private readonly AuctionsDatabaseContext _database;
    private readonly FeedbackService _feedbackService;
    private readonly AuctionDisplayService _displayService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuctionService"/> class.
    /// </summary>
    /// <param name="log">The logging instance for this type.</param>
    /// <param name="database">The auction database.</param>
    /// <param name="feedbackService">The feedback service.</param>
    /// <param name="displayService">The auction display service.</param>
    public AuctionService
    (
        ILogger<AuctionService> log,
        AuctionsDatabaseContext database,
        FeedbackService feedbackService,
        AuctionDisplayService displayService
    )
    {
        _log = log;
        _database = database;
        _feedbackService = feedbackService;
        _displayService = displayService;
    }

    /// <summary>
    /// Concludes the given auction.
    /// </summary>
    /// <param name="auction">The auction.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> ConcludeAuctionAsync(Auction auction, CancellationToken ct = default)
    {
        auction.State = AuctionState.Concluded;

        // Clean up any bids that may have slipped through the net between the auction concluding and us reacting.
        auction.Bids.RemoveAll(b => b.Timestamp >= auction.EndTime);

        var notifyWinner = await NotifyWinnerAsync(auction, ct);
        if (!notifyWinner.IsSuccess)
        {
            _log.LogWarning("Failed to notify the winner of an auction: {Error}", notifyWinner.Error);
        }

        var notifyOwner = await NotifyOwnerAsync
        (
            auction,
            notifyWinner.IsSuccess,
            ct
        );

        if (!notifyOwner.IsSuccess)
        {
            _log.LogWarning("Failed to notify the owner of an auction: {Error}", notifyOwner.Error);
        }

        var updateDisplay = await _displayService.UpdateDisplaysAsync(auction, ct);
        if (!updateDisplay.IsSuccess)
        {
            _log.LogWarning("Failed to update an auction display: {Error}", updateDisplay.Error);
        }

        await _database.SaveChangesAsync(ct);
        return Result.FromSuccess();
    }

    private async Task<Result> NotifyWinnerAsync
    (
        Auction auction,
        CancellationToken ct = default
    )
    {
        var winner = auction.Bids.MaxBy(b => b.Amount);
        if (winner is null)
        {
            return Result.FromSuccess();
        }

        var winMessage = $"Congratulations! You're the winner of auction #{auction.ID} - {auction.Name}.\n\nPlease "
                         + $"contact <@{auction.Owner.DiscordID}> at your earliest convenience for details about the "
                         + $"sale.";

        return (Result)await _feedbackService.SendPrivateSuccessAsync
        (
            winner.User.DiscordID,
            winMessage,
            ct: ct
        );
    }

    private async Task<Result> NotifyOwnerAsync
    (
        Auction auction,
        bool wasWinnerNotified,
        CancellationToken ct = default
    )
    {
        var winner = auction.Bids.MaxBy(b => b.Amount);

        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine($"Auction #{auction.ID} ({auction.Name}) has now concluded.");
        messageBuilder.AppendLine();

        if (winner is null)
        {
            messageBuilder.AppendLine("Unfortunately, nobody placed any bids.");
        }
        else
        {
            messageBuilder.Append($"The winner is <@{winner.User.DiscordID}>. ");
            messageBuilder.AppendLine
            (
                wasWinnerNotified
                    ? "They have been notified and should contact you soon."
                    : "Unfortunately, I was not able to notify them. You should contact them directly at your "
                      + "earliest convenience."
            );
        }

        return (Result)await _feedbackService.SendPrivateSuccessAsync
        (
            auction.Owner.DiscordID,
            messageBuilder.ToString(),
            ct: ct
        );
    }
}
