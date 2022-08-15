﻿//
//  ListExtensions.cs
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
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Core.Extensions;

/// <summary>
/// Extension methods for lists.
/// </summary>
public static class ListExtensions
{
    /// <summary>
    /// Holds an entropy source for this extension set.
    /// </summary>
    private static readonly Random _random = new();

    /// <summary>
    /// Holds a locking object for the entropy source.
    /// </summary>
    private static readonly object _randomLock = new();

    /// <summary>
    /// Picks a random value from the list.
    /// </summary>
    /// <param name="list">The list to pick from.</param>
    /// <typeparam name="T">The type contained in the list.</typeparam>
    /// <returns>A random value.</returns>
    [Pure]
    public static T PickRandom<T>(this IReadOnlyList<T> list)
    {
        lock (_randomLock)
        {
            return list[_random.Next(0, list.Count)];
        }
    }
}
