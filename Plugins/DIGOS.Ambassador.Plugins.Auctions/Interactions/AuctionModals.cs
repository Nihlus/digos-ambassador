//
//  AuctionModals.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Plugins.Auctions.Model;
using DIGOS.Ambassador.Plugins.Auctions.Model.Data;
using DIGOS.Ambassador.Plugins.Auctions.Services;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Auctions.Interactions;

/// <summary>
/// Handles modal interactions related to auctions.
/// </summary>
public class AuctionModals : InteractionGroup
{
    private readonly IInteractionContext _context;
    private readonly AuctionsDatabaseContext _database;
    private readonly FeedbackService _feedbackService;
    private readonly UserService _userService;
    private readonly AuctionDisplayService _auctionDisplay;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuctionModals"/> class.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="database">The database.</param>
    /// <param name="feedbackService">The feedback service.</param>
    /// <param name="userService">The user service.</param>
    /// <param name="auctionDisplay">The auction display service.</param>
    public AuctionModals
    (
        IInteractionContext context,
        AuctionsDatabaseContext database,
        FeedbackService feedbackService,
        UserService userService,
        AuctionDisplayService auctionDisplay
    )
    {
        _context = context;
        _database = database;
        _feedbackService = feedbackService;
        _userService = userService;
        _auctionDisplay = auctionDisplay;
    }

    /// <summary>
    /// Handles a submitted bid.
    /// </summary>
    /// <param name="state">The state information.</param>
    /// <param name="bidAmount">The amount the user has bid.</param>
    /// <param name="bidConfirmation">The confirmation string submitted by the user.</param>
    /// <returns>An asynchronous result representing the operation.</returns>
    [Modal("auction-bid")]
    public async Task<Result> HandleBidModalAsync(Auction state, decimal bidAmount, string bidConfirmation)
    {
        var bidValidation = ValidateBid(state, bidAmount, bidConfirmation);
        if (!bidValidation.IsSuccess)
        {
            var sendError = await _feedbackService.SendContextualErrorAsync
            (
                bidValidation.Error.Message,
                options: new FeedbackMessageOptions(MessageFlags: MessageFlags.Ephemeral),
                ct: this.CancellationToken
            );

            if (!sendError.IsSuccess)
            {
                return (Result)sendError;
            }

            return Result.FromSuccess();
        }

        _ = _context.TryGetUserID(out var userID);
        var getUser = await _userService.GetOrRegisterUserAsync(userID!.Value, this.CancellationToken);
        if (!getUser.IsDefined(out var user))
        {
            return (Result)getUser;
        }

        user = _database.NormalizeReference(user);

        state.Bids.Add(new UserBid(state, user, DateTimeOffset.UtcNow, bidAmount));
        await _database.SaveChangesAsync(this.CancellationToken);

        var updateDisplays = await _auctionDisplay.UpdateDisplaysAsync(state, this.CancellationToken);
        if (!updateDisplays.IsSuccess)
        {
            var warnUser = await _feedbackService.SendContextualWarningAsync
            (
                "Failed to update auction displays. Some information may be out of date.",
                options: new FeedbackMessageOptions(MessageFlags: MessageFlags.Ephemeral),
                ct: this.CancellationToken
            );

            if (!warnUser.IsSuccess)
            {
                return (Result)warnUser;
            }
        }

        return (Result)await _feedbackService.SendContextualSuccessAsync
        (
            "Bid confirmed - good luck!",
            options: new FeedbackMessageOptions(MessageFlags: MessageFlags.Ephemeral),
            ct: this.CancellationToken
        );
    }

    private Result ValidateBid(Auction auction, decimal bidAmount, string bidConfirmation)
    {
        if (auction.AreBidsBinding && bidConfirmation != "I UNDERSTAND")
        {
            return new UserError("Please type the confirmation message exactly as is written.");
        }

        var highestBid = auction.GetHighestBid();
        if (bidAmount <= highestBid)
        {
            return new UserError($"Your bid is too low. The current leading bid is {highestBid} {auction.Currency}.");
        }

        if (bidAmount == auction.Buyout)
        {
            // buyouts bypass the min/max restrictions
            return Result.FromSuccess();
        }

        if (bidAmount < auction.MinimumBid)
        {
            return new UserError($"Your bid is too low. Please bit at least {auction.MinimumBid} {auction.Currency}.");
        }

        if (bidAmount > auction.MaximumBid)
        {
            return new UserError($"Your bid is too high. Please bit at most {auction.MaximumBid} {auction.Currency}.");
        }

        return Result.FromSuccess();
    }
}
