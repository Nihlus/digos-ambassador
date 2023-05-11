//
//  Auction.cs
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
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Auctions.Model.Data;

/// <summary>
/// Represents an auction.
/// </summary>
[Table("auctions", Schema = "AuctionModule")]
public class Auction : EFEntity
{
    /// <summary>
    /// Gets or sets the server the auction is in.
    /// </summary>
    public virtual Server Server { get; protected set; }

    /// <summary>
    /// Gets or sets the user that owns the auction.
    /// </summary>
    public virtual User Owner { get; protected set; }

    /// <summary>
    /// Gets the human-readable and searchable name of the auction.
    /// </summary>
    public string Name { get; internal set; }

    /// <summary>
    /// Gets the starting bid.
    /// </summary>
    public decimal StartBid { get; internal set; }

    /// <summary>
    /// Gets the time at which the auction ends.
    /// </summary>
    public DateTimeOffset EndTime { get; internal set; }

    /// <summary>
    /// Gets the currency used for the auction.
    /// </summary>
    public string Currency { get; internal set; }

    /// <summary>
    /// Gets or sets the placed bids.
    /// </summary>
    public virtual List<UserBid> Bids { get; protected set; }

    /// <summary>
    /// Gets the current state of the auction.
    /// </summary>
    public AuctionState State { get; internal set; }

    /// <summary>
    /// Gets the time with which the auction is extended when a bid is placed.
    /// </summary>
    public TimeSpan? TimeExtension { get; internal set; }

    /// <summary>
    /// Gets the minimum bidding amount.
    /// </summary>
    public decimal? MinimumBid { get; internal set; }

    /// <summary>
    /// Gets the maximum bidding amount.
    /// </summary>
    public decimal? MaximumBid { get; internal set; }

    /// <summary>
    /// Gets the bidding cap at which the auction automatically closes.
    /// </summary>
    public decimal? BidCap { get; internal set; }

    /// <summary>
    /// Gets the buyout amount.
    /// </summary>
    public decimal? Buyout { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether bids are binding.
    /// </summary>
    public bool AreBidsBinding { get; internal set; }

    /// <summary>
    /// Gets a set of values indicating privacy options.
    /// </summary>
    public AuctionPrivacy Privacy { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Auction"/> class.
    /// </summary>
    [Obsolete("For internal EF Core use only", true)]
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor", Justification = "Required by EF Core.")]
    [UsedImplicitly]
    protected Auction()
    {
        this.Server = null!;
        this.Owner = null!;
        this.Name = null!;
        this.Currency = null!;
        this.Bids = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Auction"/> class.
    /// </summary>
    /// <remarks>
    /// Auctions start off closed when first created.
    /// </remarks>
    /// <param name="server">The server the auction is in.</param>
    /// <param name="owner">The auction's owner.</param>
    /// <param name="name">The name of the auction.</param>
    /// <param name="startBid">The starting bid.</param>
    /// <param name="endTime">The time at which the auction ends.</param>
    /// <param name="currency">The currency used when bidding.</param>
    /// <param name="timeExtension">The time with which the time is extended when a bid is made.</param>
    /// <param name="minimumBid">The minimum bidding amount.</param>
    /// <param name="maximumBid">The maximum bidding amount.</param>
    /// <param name="bidCap">The bidding cap at which the auction automatically closes.</param>
    /// <param name="buyout">The buyout amount.</param>
    /// <param name="areBidsBinding">Indicates whether bids are binding.</param>
    /// <param name="privacy">The privacy options.</param>
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor", Justification = "Required by EF Core.")]
    public Auction
    (
        Server server,
        User owner,
        string name,
        decimal startBid,
        DateTimeOffset endTime,
        string currency,
        TimeSpan? timeExtension,
        decimal? minimumBid,
        decimal? maximumBid,
        decimal? bidCap,
        decimal? buyout,
        bool areBidsBinding,
        AuctionPrivacy privacy
    )
    {
        this.Server = server;
        this.Owner = owner;
        this.Name = name;
        this.EndTime = endTime.ToUniversalTime();
        this.TimeExtension = timeExtension;
        this.Currency = currency;
        this.Bids = new List<UserBid>();
        this.State = AuctionState.Closed;
        this.StartBid = startBid;
        this.MinimumBid = minimumBid;
        this.MaximumBid = maximumBid;
        this.BidCap = bidCap;
        this.Buyout = buyout;
        this.AreBidsBinding = areBidsBinding;
        this.Privacy = privacy;
    }

    /// <summary>
    /// Gets the highest bid in the auction.
    /// </summary>
    /// <returns>The highest bid.</returns>
    public UserBid? GetHighestBid()
    {
        var highestBid = this.Bids.MaxBy(b => b.Amount);
        return highestBid;
    }

    /// <summary>
    /// Gets a suggestion for the next bid.
    /// </summary>
    /// <returns>The next bid.</returns>
    public decimal GetNextBidSuggestion()
    {
        var highestBid = GetHighestBid();
        if (highestBid is null)
        {
            return this.StartBid;
        }

        return highestBid.Amount + (this.MinimumBid ?? 1);
    }
}
