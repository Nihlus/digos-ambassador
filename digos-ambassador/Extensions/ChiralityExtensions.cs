//
//  ChiralityExtensions.cs
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

using System;
using DIGOS.Ambassador.Transformations;
using static DIGOS.Ambassador.Transformations.Chirality;

namespace DIGOS.Ambassador.Extensions
{
    /// <summary>
    /// Extension methods for the <see cref="Chirality"/> enum.
    /// </summary>
    public static class ChiralityExtensions
    {
        /// <summary>
        /// Gets the inverse chirality of the given chirality.
        /// </summary>
        /// <param name="this">The chirality.</param>
        /// <returns>The inverse chirality.</returns>
        public static Chirality Opposite(this Chirality @this)
        {
            switch (@this)
            {
                case Left: return Right;
                case Right: return Left;
                case Center: return Center;
                default: throw new ArgumentOutOfRangeException(nameof(@this), @this, "Unknown chirality.");
            }
        }
    }
}
