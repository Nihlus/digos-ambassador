//
//  LevenshteinExtensions.cs
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
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Kinks.Extensions
{
    /// <summary>
    /// Extensions for levenshtein distances.
    /// </summary>
    [PublicAPI]
    public static class LevenshteinExtensions
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
        /// <param name="ct">The cancellation token in use.</param>
        /// <typeparam name="TSource">The source type of the enumerable.</typeparam>
        /// <typeparam name="TResult">The resulting type.</typeparam>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public static async Task<Result<TResult>> SelectFromBestLevenshteinMatchAsync<TSource, TResult>
        (
            this IQueryable<TSource> @this,
            Func<TSource, TResult> selector,
            Expression<Func<TSource, string>> stringSelector,
            string search,
            double tolerance = 0.25,
            CancellationToken ct = default
        )
            where TResult : class
            where TSource : class
        {
            var matchResult = (await @this.Select(stringSelector).ToListAsync(ct))
                .BestLevenshteinMatch(search, tolerance);

            if (!matchResult.IsSuccess)
            {
                return Result<TResult>.FromError(matchResult);
            }

            var selectedString = matchResult.Entity;

            var selectorFunc = stringSelector.Compile();

            var selectedObject = await @this.FirstOrDefaultAsync(i => selectorFunc(i) == selectedString, ct);
            if (selectedObject is null)
            {
                return new NotFoundError("No matching object for the selector found.");
            }

            var result = selector(selectedObject);

            return Result<TResult>.FromSuccess(result);
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
        [Pure]
        public static Result<TResult> SelectFromBestLevenshteinMatch<TSource, TResult>
        (
            this IEnumerable<TSource> @this,
            Func<TSource, TResult> selector,
            Func<TSource, string> stringSelector,
            string search,
            double tolerance = 0.25
        )
            where TResult : class
            where TSource : class
        {
            var enumerable = @this as IList<TSource> ?? @this.ToList();

            var matchResult = enumerable.Select(stringSelector).BestLevenshteinMatch(search, tolerance);
            if (!matchResult.IsSuccess)
            {
                return Result<TResult>.FromError(matchResult);
            }

            var selectedString = matchResult.Entity;

            var selectedObject = enumerable.FirstOrDefault(i => stringSelector(i) == selectedString);
            if (selectedObject is null)
            {
                return new NotFoundError("No matching object for the selector found.");
            }

            var result = selector(selectedObject);

            return Result<TResult>.FromSuccess(result);
        }
    }
}
