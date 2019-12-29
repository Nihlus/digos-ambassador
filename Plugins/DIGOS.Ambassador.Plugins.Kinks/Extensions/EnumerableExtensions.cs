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
using System.Linq.Expressions;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Utility;
using DIGOS.Ambassador.Plugins.Kinks.Utility;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MoreLinq;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Kinks.Extensions
{
    /// <summary>
    /// Extensions for enumerables.
    /// </summary>
    [PublicAPI]
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Selects an object from the queryable by the best Levenshtein match.
        /// </summary>
        /// <param name="this">The sequence.</param>
        /// <param name="selector">A function which selects the object to return.</param>
        /// <param name="stringSelector">A function which selects the string field to search.</param>
        /// <param name="search">The pattern to search for.</param>
        /// <param name="tolerance">
        /// The percentile distance tolerance for results. The distance must be below this value.
        /// </param>
        /// <typeparam name="TSource">The source type of the enumerable.</typeparam>
        /// <typeparam name="TResult">The resulting type.</typeparam>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull, ItemNotNull]
        public static async Task<RetrieveEntityResult<TResult>> SelectFromBestLevenshteinMatchAsync<TSource, TResult>
        (
            [NotNull] this IQueryable<TSource> @this,
            [NotNull] Func<TSource, TResult> selector,
            [NotNull] Expression<Func<TSource, string>> stringSelector,
            [NotNull] string search,
            double tolerance = 0.25
        )
            where TResult : class
            where TSource : class
        {
            var matchResult = await @this.Select(stringSelector).BestLevenshteinMatchAsync(search, tolerance);
            if (!matchResult.IsSuccess)
            {
                return RetrieveEntityResult<TResult>.FromError(matchResult);
            }

            var selectedString = matchResult.Entity;

            var selectorFunc = stringSelector.Compile();

            var selectedObject = await @this.FirstOrDefaultAsync(i => selectorFunc(i) == selectedString);
            if (selectedObject is null)
            {
                return RetrieveEntityResult<TResult>.FromError("No matching object for the selector found.");
            }

            var result = selector(selectedObject);

            return RetrieveEntityResult<TResult>.FromSuccess(result);
        }

        /// <summary>
        /// Selects an object from the enumerable by the best Levenshtein match.
        /// </summary>
        /// <param name="this">The sequence.</param>
        /// <param name="selector">A function which selects the object to return.</param>
        /// <param name="stringSelector">A function which selects the string field to search.</param>
        /// <param name="search">The pattern to search for.</param>
        /// <param name="tolerance">The percentile distance tolerance for results. The distance must be below this value.</param>
        /// <typeparam name="TSource">The source type of the enumerable.</typeparam>
        /// <typeparam name="TResult">The resulting type.</typeparam>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull]
        public static RetrieveEntityResult<TResult> SelectFromBestLevenshteinMatch<TSource, TResult>
        (
            [NotNull, ItemNotNull] this IEnumerable<TSource> @this,
            [NotNull] Func<TSource, TResult> selector,
            [NotNull] Func<TSource, string> stringSelector,
            [NotNull] string search,
            double tolerance = 0.25
        )
            where TResult : class
            where TSource : class
        {
            var enumerable = @this as IList<TSource> ?? @this.ToList();

            var matchResult = enumerable.Select(stringSelector).BestLevenshteinMatch(search, tolerance);
            if (!matchResult.IsSuccess)
            {
                return RetrieveEntityResult<TResult>.FromError(matchResult);
            }

            var selectedString = matchResult.Entity;

            var selectedObject = enumerable.FirstOrDefault(i => stringSelector(i) == selectedString);
            if (selectedObject is null)
            {
                return RetrieveEntityResult<TResult>.FromError("No matching object for the selector found.");
            }

            var result = selector(selectedObject);

            return RetrieveEntityResult<TResult>.FromSuccess(result);
        }

        /// <summary>
        /// Selects the closest match in the sequence using the levenshtein algorithm.
        /// </summary>
        /// <param name="this">The sequence to search.</param>
        /// <param name="search">The value to search for.</param>
        /// <param name="tolerance">The percentile distance tolerance for results. The distance must be below this value.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull, ItemNotNull]
        public static async Task<RetrieveEntityResult<string>> BestLevenshteinMatchAsync
        (
            [NotNull, ItemNotNull] this IQueryable<string> @this,
            [NotNull] string search,
            double tolerance = 0.25
        )
        {
            var candidates =
                from candidate in @this
                    let distance = LevenshteinDistance.Compute
                    (
                        candidate.ToLowerInvariant(),
                        search.ToLowerInvariant()
                    )
                    let maxDistance = Math.Max(candidate.Length, search.Length)
                    let percentile = distance / (float)maxDistance
                select new Tuple<string, int, double>(candidate, distance, percentile);

            var hasAnyPassing = await candidates.Where(c => c.Item3 <= tolerance).AnyAsync();
            if (!hasAnyPassing)
            {
                return RetrieveEntityResult<string>.FromError("No sufficiently close match found.");
            }

            var best = await candidates.OrderBy(x => x.Item2).FirstAsync();
            return RetrieveEntityResult<string>.FromSuccess(best.Item1);
        }

        /// <summary>
        /// Selects the closest match in the sequence using the levenshtein algorithm.
        /// </summary>
        /// <param name="this">The sequence to search.</param>
        /// <param name="search">The value to search for.</param>
        /// <param name="tolerance">The percentile distance tolerance for results. The distance must be below this value.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull]
        public static RetrieveEntityResult<string> BestLevenshteinMatch
        (
            [NotNull, ItemNotNull] this IEnumerable<string> @this,
            [NotNull] string search,
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
                return RetrieveEntityResult<string>.FromError("No sufficiently close match found.");
            }

            var best = passing.MinBy(c => c.distance).First();
            return RetrieveEntityResult<string>.FromSuccess(best.value);
        }
    }
}
