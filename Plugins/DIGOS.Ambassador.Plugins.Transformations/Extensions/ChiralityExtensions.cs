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
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Transformations.Extensions
{
    /// <summary>
    /// Extension methods for the <see cref="Chirality"/> enum.
    /// </summary>
    [PublicAPI]
    public static class ChiralityExtensions
    {
        /// <summary>
        /// Gets the inverse chirality of the given chirality.
        /// </summary>
        /// <param name="this">The chirality.</param>
        /// <returns>The inverse chirality.</returns>
        [Pure]
        public static Chirality Opposite(this Chirality @this)
        {
            switch (@this)
            {
                case Chirality.Left: return Chirality.Right;
                case Chirality.Right: return Chirality.Left;
                case Chirality.Center: return Chirality.Center;
                default: throw new ArgumentOutOfRangeException(nameof(@this), @this, "Unknown chirality.");
            }
        }
    }
}
