//
//  EnumerableExtensions.cs
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
using System.Collections.Generic;
using System.Linq;

namespace DIGOS.Ambassador.Core.Extensions
{
    /// <summary>
    /// Contains LINQ-style enumerable extensions.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Performs a topological sort of the input, given an expression that produces a set of connected nodes.
        /// </summary>
        /// <param name="nodes">The input.</param>
        /// <param name="connected">The expression.</param>
        /// <typeparam name="T">The node type.</typeparam>
        /// <returns>The input nodes, sorted.</returns>
        /// <exception cref="ArgumentException">Thrown if a cyclic dependency is found.</exception>
        public static IEnumerable<T> TopologicalSort<T>
        (
            this IEnumerable<T> nodes,
            Func<T, IEnumerable<T>> connected
        )
        {
            var elems = nodes.ToDictionary(node => node, node => new HashSet<T>(connected(node)));
            while (elems.Count > 0)
            {
                var (key, _) = elems.FirstOrDefault(x => x.Value.Count == 0);

                if (key == null)
                {
                    throw new ArgumentException("Cyclic connections are not allowed");
                }

                elems.Remove(key);

                foreach (var selem in elems)
                {
                    selem.Value.Remove(key);
                }

                yield return key;
            }
        }
    }
}
