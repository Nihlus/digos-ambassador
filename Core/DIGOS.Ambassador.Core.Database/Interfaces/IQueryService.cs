//
//  IQueryService.cs
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
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Core.Database.Interfaces;

/// <summary>
/// Represents the public interface of a service that can perform single- and multi-entity queries against a
/// specialized portion of the database.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IQueryService<TEntity>
{
    /// <summary>
    /// Performs a multi-entity query against the database.
    /// </summary>
    /// <param name="query">Additional query statements.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A list of materialized results.</returns>
    [Pure]
    public Task<IReadOnlyList<TEntity>> QueryDatabaseAsync
    (
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? query = default,
        CancellationToken ct = default
    );

    /// <summary>
    /// Performs a single-entity query against the database.
    /// </summary>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="query">Additional query statements.</param>
    /// <returns>A single result from the query.</returns>
    [Pure]
    public Task<TOut> QueryDatabaseAsync<TOut>
    (
        Func<IQueryable<TEntity>, Task<TOut>> query
    );
}
