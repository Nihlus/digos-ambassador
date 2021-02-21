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
using DIGOS.Ambassador.Core.Utility;
using JetBrains.Annotations;
using MoreLinq.Extensions;
using Remora.Results;

namespace DIGOS.Ambassador.Core.Extensions
{
    /// <summary>
    /// Extensions for enumerables.
    /// </summary>
    [PublicAPI]
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Selects the closest match in the sequence using the levenshtein algorithm.
        /// </summary>
        /// <param name="this">The sequence to search.</param>
        /// <param name="search">The value to search for.</param>
        /// <param name="tolerance">The percentile distance tolerance for results. The distance must be below this value.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public static Result<string> BestLevenshteinMatch
        (
            this IEnumerable<string> @this,
            string search,
            double tolerance = 0.25
        )
        {
            var candidates = @this.Select
            (
                s =>
                {
                    var distance = LevenshteinDistance.Compute
                    (
                        s.ToLowerInvariant(),
                        search.ToLowerInvariant()
                    );

                    var maxDistance = Math.Max(s.Length, search.Length);
                    var percentile = distance / (float)maxDistance;

                    return (value: s, distance, percentile);
                }
            );

            var passing = candidates.Where(c => c.percentile <= tolerance).ToList();
            if (!passing.Any())
            {
                return new GenericError("No sufficiently close match found.");
            }

            var best = passing.MinBy(c => c.distance).First();
            return Result<string>.FromSuccess(best.value);
        }
    }
}
