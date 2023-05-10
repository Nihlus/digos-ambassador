//
//  UserBid.cs
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
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Auctions.Model.Data;

/// <summary>
/// Represents a bid made by a user.
/// </summary>
[Table("user_bids", Schema = "AuctionModule")]
public class UserBid : EFEntity
{
    /// <summary>
    /// Gets or sets the auction the bid is associated with.
    /// </summary>
    public virtual Auction Auction { get; protected set; }

    /// <summary>
    /// Gets or sets the user that placed the bid.
    /// </summary>
    public virtual User User { get; protected set; }

    /// <summary>
    /// Gets or sets the time at which the bid was placed.
    /// </summary>
    public DateTimeOffset Timestamp { get; protected set; }

    /// <summary>
    /// Gets or sets the bidding amount.
    /// </summary>
    public decimal Amount { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether the bid has been retracted.
    /// </summary>
    public bool IsRetracted { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserBid"/> class.
    /// </summary>
    [Obsolete("For internal EF Core use only", true)]
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor", Justification = "Required by EF Core.")]
    [UsedImplicitly]
    protected UserBid()
    {
        this.Auction = null!;
        this.User = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserBid"/> class.
    /// </summary>
    /// <param name="auction">The auction the bid is associated with.</param>
    /// <param name="user">The user that placed the bid.</param>
    /// <param name="timestamp">The time at which the bid was placed.</param>
    /// <param name="amount">The amount.</param>
    /// <param name="isRetracted">Indicates whether the bid has been retracted.</param>
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor", Justification = "Required by EF Core.")]
    public UserBid(Auction auction, User user, DateTimeOffset timestamp, decimal amount, bool isRetracted = false)
    {
        this.Auction = auction;
        this.User = user;
        this.Timestamp = timestamp;
        this.Amount = amount;
        this.IsRetracted = isRetracted;
    }

    /// <summary>
    /// Gets a displayable identity from the bid's associated user in relation to the auction's settings.
    /// </summary>
    /// <returns>
    /// An anonymized display string unique to the user/auction combination or a Discord mention string.
    /// </returns>
    public string GetDisplayIdentity()
    {
        string GetAnonymizedIdentity()
        {
            var combinedIdentity = (ulong)this.User.ID ^ this.User.DiscordID.Value ^ (ulong)this.Auction.ID ^ 23;
            return $"#{combinedIdentity.ToString("X8")[..7]}";
        }

        var isWinner = this.Auction.State is AuctionState.Concluded
                       && this.Auction.Bids.MaxBy(b => b.Amount)?.User.ID == this.User.ID;

        if (this.Auction.State is AuctionState.Concluded)
        {
            if (isWinner && this.Auction.Privacy.HasFlag(AuctionPrivacy.AnonymizeWinner))
            {
                return GetAnonymizedIdentity();
            }

            if (!isWinner && this.Auction.Privacy.HasFlag(AuctionPrivacy.AnonymizeBiddersAfterAuction))
            {
                return GetAnonymizedIdentity();
            }
        }

        if (this.Auction.Privacy.HasFlag(AuctionPrivacy.AnonymizeBiddersDuringAuction))
        {
            return GetAnonymizedIdentity();
        }

        return $"<@{this.User.DiscordID}>";
    }
}
