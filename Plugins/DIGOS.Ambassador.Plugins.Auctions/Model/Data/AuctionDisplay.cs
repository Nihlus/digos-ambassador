//
//  AuctionDisplay.cs
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
using DIGOS.Ambassador.Core.Database.Entities;
using JetBrains.Annotations;
using Remora.Rest.Core;

namespace DIGOS.Ambassador.Plugins.Auctions.Model.Data;

/// <summary>
/// Represents information about an automatically updating auction display.
/// </summary>
[Table("auction_displays", Schema = "AuctionModule")]
public class AuctionDisplay : EFEntity
{
    /// <summary>
    /// Gets or sets the displayed auction.
    /// </summary>
    public virtual Auction Auction { get; protected set; }

    /// <summary>
    /// Gets or sets the channel the display is in.
    /// </summary>
    public Snowflake Channel { get; protected set; }

    /// <summary>
    /// Gets or sets the message used for displaying the auction.
    /// </summary>
    public Snowflake Message { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuctionDisplay"/> class.
    /// </summary>
    [Obsolete("For internal EF Core use only", true)]
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor", Justification = "Required by EF Core.")]
    [UsedImplicitly]
    protected AuctionDisplay()
    {
        this.Auction = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuctionDisplay"/> class.
    /// </summary>
    /// <param name="auction">The auction.</param>
    /// <param name="channel">The channel the auction is displayed in.</param>
    /// <param name="message">The message used for displaying the auction.</param>
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor", Justification = "Required by EF Core.")]
    public AuctionDisplay(Auction auction, Snowflake channel, Snowflake message)
    {
        this.Auction = auction;
        this.Channel = channel;
        this.Message = message;
    }
}
