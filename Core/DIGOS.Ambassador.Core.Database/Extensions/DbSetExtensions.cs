//
//  DbSetExtensions.cs
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Core.Database.Extensions;

/// <summary>
/// Defines extension methods for <see cref="DbSet{TEntity}"/>.
/// </summary>
public static class DbSetExtensions
{
    /// <summary>
    /// Performs a serverside query against the database set, fully materializing it after finishing.
    /// </summary>
    /// <param name="dbSet">The database set.</param>
    /// <param name="query">The query. This query runs serverside where possible. Any clientside operations must be
    /// performed on the resulting <see cref="IEnumerable{T}"/>.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <returns>The final query.</returns>
    public static async Task<IReadOnlyList<TOut>> ServersideQueryAsync<TEntity, TOut>
    (
        this DbSet<TEntity> dbSet,
        Func<IQueryable<TEntity>, IQueryable<TOut>> query,
        CancellationToken ct
    )
        where TEntity : class
    {
        return await query(dbSet).ToListAsync(ct);
    }

    /// <summary>
    /// Performs a unified query against the database set, including both local and database entities.
    /// </summary>
    /// <param name="dbSet">The database set.</param>
    /// <param name="query">The query. This query runs serverside where possible. Any clientside operations must be
    /// performed on the resulting <see cref="IEnumerable{T}"/>.</param>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TOut">The resulting type.</typeparam>
    /// <returns>The final query result.</returns>
    public static async Task<TOut> ServersideQueryAsync<TEntity, TOut>
    (
        this DbSet<TEntity> dbSet,
        Func<IQueryable<TEntity>, Task<TOut>> query
    )
        where TEntity : class
    {
        return await query(dbSet);
    }
}
