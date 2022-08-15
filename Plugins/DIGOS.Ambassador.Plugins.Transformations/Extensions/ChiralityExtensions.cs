//
//  ChiralityExtensions.cs
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
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Transformations.Extensions;

/// <summary>
/// Extension methods for the <see cref="Chirality"/> enum.
/// </summary>
[PublicAPI]
public static class ChiralityExtensions
{
    /// <summary>
    /// Gets the inverse chirality of the given chirality.
    /// </summary>
    /// <param name="chirality">The chirality.</param>
    /// <returns>The inverse chirality.</returns>
    [Pure]
    public static Chirality Opposite(this Chirality chirality)
    {
        return chirality switch
        {
            Chirality.Left => Chirality.Right,
            Chirality.Right => Chirality.Left,
            Chirality.Center => Chirality.Center,
            _ => throw new ArgumentOutOfRangeException(nameof(chirality), chirality, "Unknown chirality.")
        };
    }
}
