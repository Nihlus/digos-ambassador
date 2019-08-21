//
//  KinkVisibility.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
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

namespace DIGOS.Ambassador.Plugins.Kinks.Services
{
    /// <summary>
    /// Holds options for the visibility of a user's kink list.
    /// </summary>
    public enum KinkVisibility
    {
        /// <summary>
        /// Anyone can view your kinks.
        /// </summary>
        All,

        /// <summary>
        /// Only your friends can view your kinks.
        /// </summary>
        Friends,

        /// <summary>
        /// Only people on your whitelist can view your kinks.
        /// </summary>
        Whitelist
    }
}
