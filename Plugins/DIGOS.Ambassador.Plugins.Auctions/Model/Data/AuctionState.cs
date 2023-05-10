//
//  AuctionState.cs
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

using System.ComponentModel;

namespace DIGOS.Ambassador.Plugins.Auctions.Model.Data;

/// <summary>
/// Enumerates various states an auction can be in.
/// </summary>
public enum AuctionState
{
    /// <summary>
    /// The auction is currently closed and is not accepting bids.
    /// </summary>
    [Description("The auction is currently closed and is not accepting bids.")]
    Closed,

    /// <summary>
    /// The auction is open and accepting bids.
    /// </summary>
    [Description("The auction is open and accepting bids.")]
    Open,

    /// <summary>
    /// The auction has concluded.
    /// </summary>
    [Description("The auction has concluded.")]
    Concluded
}
