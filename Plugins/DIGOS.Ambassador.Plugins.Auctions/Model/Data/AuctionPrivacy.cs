//
//  AuctionPrivacy.cs
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
using System.ComponentModel;

namespace DIGOS.Ambassador.Plugins.Auctions.Model.Data;

/// <summary>
/// Enumerates various options related to privacy for an auction.
/// </summary>
[Flags]
public enum AuctionPrivacy
{
    /// <summary>
    /// No anonymization takes place.
    /// </summary>
    [Description("The identities of all bidders are visible to anyone.")]
    None = 0,

    /// <summary>
    /// Anonymize bidders during the auction.
    /// </summary>
    [Description("The bidders are anonymized during the auction.")]
    AnonymizeBiddersDuringAuction = 1 << 1,

    /// <summary>
    /// Anonymize bidders after the auction.
    /// </summary>
    [Description("The bidders are anonymized after the auction.")]
    AnonymizeBiddersAfterAuction = 1 << 2,

    /// <summary>
    /// Anonymize the winder of the auction.
    /// </summary>
    [Description("The winner is anonymized after the auction.")]
    AnonymizeWinner = 1 << 3,

    /// <summary>
    /// Anonymize bidders during and after the auction.
    /// </summary>
    [Description("The bidders are always anonymized.")]
    AnonymizeBidders = AnonymizeBiddersDuringAuction | AnonymizeBiddersAfterAuction
}
