//
//  DbSetExtensions.cs
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Core.Database.Extensions
{
    /// <summary>
    /// Defines extension methods for <see cref="DbSet{TEntity}"/>.
    /// </summary>
    public static class DbSetExtensions
    {
        /// <summary>
        /// Performs a unified query against the database set, including both local and database entities.
        /// </summary>
        /// <param name="dbSet">The database set.</param>
        /// <param name="query">The query. This query runs serverside where possible. Any clientside operations must be
        /// performed on the resulting <see cref="IEnumerable{T}"/>.</param>
        /// <param name="ct">A cancellation token.</param>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TOut">The output entity type.</typeparam>
        /// <returns>The queried entity set.</returns>
        public static async Task<IEnumerable<TOut>> UnifiedQueryAsync<TEntity, TOut>
        (
            this DbSet<TEntity> dbSet,
            Func<IQueryable<TEntity>, CancellationToken, IQueryable<TOut>> query,
            CancellationToken ct
        )
            where TEntity : class
            where TOut : class
        {
            var localMatches = query(dbSet.Local.AsQueryable(), ct);
            var dbMatches = await query(dbSet, ct).ToListAsync(ct);

            return dbMatches.Union(localMatches);
        }

        /// <summary>
        /// Performs a unified query against the database set, including both local and database entities.
        /// </summary>
        /// <param name="dbSet">The database set.</param>
        /// <param name="query">The query. This query runs serverside where possible. Any clientside operations must be
        /// performed on the resulting <see cref="IEnumerable{T}"/>.</param>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TOut">The resulting entity type.</typeparam>
        /// <returns>The queried entity set.</returns>
        public static async Task<IEnumerable<TOut>> UnifiedQueryAsync<TEntity, TOut>
        (
            this DbSet<TEntity> dbSet,
            Func<IQueryable<TEntity>, IQueryable<TOut>> query
        )
            where TEntity : class
        {
            var localMatches = query(dbSet.Local.AsQueryable());
            var dbMatches = await query(dbSet).ToListAsync();

            return dbMatches.Union(localMatches);
        }
    }
}
