//
//  AuctionExtensions.cs
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
using System.Drawing;
using System.Globalization;
using System.Linq;
using DIGOS.Ambassador.Plugins.Auctions.Model.Data;
using Remora.Discord.API.Objects;

namespace DIGOS.Ambassador.Plugins.Auctions.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="Auction"/> type.
/// </summary>
public static class AuctionExtensions
{
    /// <summary>
    /// Creates an embed with relevant information.
    /// </summary>
    /// <param name="auction">The auction display.</param>
    /// <returns>An embed.</returns>
    public static Embed CreateEmbed(this Auction auction)
    {
        var leadingBid = auction.Bids.MaxBy(b => b.Amount);

        var displayColour = auction.State switch
        {
            AuctionState.Closed => Color.SlateGray,
            AuctionState.Open => Color.MediumSeaGreen,
            AuctionState.Concluded => Color.DeepSkyBlue,
            _ => throw new ArgumentOutOfRangeException()
        };

        var fields = auction.State switch
        {
            AuctionState.Closed => new[]
            {
                new EmbedField("Starting Bid", auction.StartBid.ToString(CultureInfo.InvariantCulture), true),
                new EmbedField("Currency", auction.Currency)
            },
            AuctionState.Open => new[]
            {
                new EmbedField("Starting Bid", auction.StartBid.ToString(CultureInfo.InvariantCulture), true),
                new EmbedField("Currency", auction.Currency, true),
                new EmbedField("\u200b", "\u200b", true),
                new EmbedField("Leading Bid", leadingBid?.Amount.ToString(CultureInfo.InvariantCulture) ?? "-", true),
                new EmbedField("By", leadingBid?.GetDisplayIdentity() ?? "nobody", true),
                new EmbedField("\u200b", "\u200b", true),
            },
            AuctionState.Concluded => new[]
            {
                new EmbedField("Starting Bid", auction.StartBid.ToString(CultureInfo.InvariantCulture), true),
                new EmbedField("Currency", auction.Currency, true),
                new EmbedField("\u200b", "\u200b", true),
                new EmbedField("Winning Bid", leadingBid?.Amount.ToString(CultureInfo.InvariantCulture) ?? "-", true),
                new EmbedField("Winner", leadingBid?.GetDisplayIdentity() ?? "nobody", true),
                new EmbedField("\u200b", "\u200b", true),
            },
            _ => throw new ArgumentOutOfRangeException()
        };

        return new Embed
        (
            Title: auction.Name,
            Colour: displayColour,
            Fields: fields
        );
    }
}
