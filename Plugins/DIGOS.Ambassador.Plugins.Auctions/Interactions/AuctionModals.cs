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

using System.Drawing;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Plugins.Auctions.Model.Data;
using DIGOS.Ambassador.Plugins.Auctions.Services;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Interactivity;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Auctions.Interactions;

/// <summary>
/// Handles modal interactions related to auctions.
/// </summary>
public class AuctionModals : InteractionGroup
{
    private readonly IInteractionContext _context;
    private readonly AuctionService _auctionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuctionModals"/> class.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="auctionService">The auction service.</param>
    public AuctionModals
    (
        IInteractionContext context,
        AuctionService auctionService
    )
    {
        _context = context;
        _auctionService = auctionService;
    }

    /// <summary>
    /// Handles a submitted bid.
    /// </summary>
    /// <param name="state">The state information.</param>
    /// <param name="bidAmount">The amount the user has bid.</param>
    /// <param name="bidConfirmation">The confirmation string submitted by the user.</param>
    /// <returns>An asynchronous result representing the operation.</returns>
    [Modal("auction-bid")]
    [Ephemeral]
    public async Task<Result<FeedbackMessage>> HandleBidModalAsync
    (
        Auction state,
        decimal bidAmount,
        string bidConfirmation
    )
    {
        if (state.AreBidsBinding && bidConfirmation != "I UNDERSTAND")
        {
            return new UserError("Please type the confirmation message exactly as is written.");
        }

        _ = _context.TryGetUserID(out var userID);

        var placeBid = await _auctionService.PlaceBidAsync(state, userID!.Value, bidAmount, this.CancellationToken);

        return placeBid.IsSuccess
            ? new FeedbackMessage("Bid confirmed - good luck!", Color.MediumSeaGreen)
            : Result<FeedbackMessage>.FromError(placeBid);
    }
}
