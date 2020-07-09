//
//  CoreDbSetExtensions.cs
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
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Plugins.Core.Extensions
{
    /// <summary>
    /// Defines extension methods for the <see cref="DbSet{TEntity}"/> class.
    /// </summary>
    public static class CoreDbSetExtensions
    {
        /// <summary>
        /// Performs a serverside query against the database set, fully materializing it after finishing.
        /// </summary>
        /// <param name="dbSet">The database set.</param>
        /// <param name="server">The server to scope the search to.</param>
        /// <param name="query">Additional query statements.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TOut">The output type.</typeparam>
        /// <returns>A queryable set of characters.</returns>
        public static Task<IReadOnlyList<TOut>> ServerScopedServersideQueryAsync<TEntity, TOut>
        (
            this DbSet<TEntity> dbSet,
            Server server,
            Func<IQueryable<TEntity>, IQueryable<TOut>> query,
            CancellationToken ct = default
        )
            where TEntity : class, IServerEntity
        {
            return dbSet.ServersideQueryAsync
            (
                q => query(q.Where(a => a.Server == server)),
                ct
            );
        }

        /// <summary>
        /// Performs a single-value query against the characters on the given server.
        /// </summary>
        /// <param name="dbSet">The database set.</param>
        /// <param name="server">The server to scope the search to.</param>
        /// <param name="query">Additional query statements.</param>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TOut">The output type.</typeparam>
        /// <returns>A queryable set of characters.</returns>
        public static Task<TOut> ServerScopedServersideQueryAsync<TEntity, TOut>
        (
            this DbSet<TEntity> dbSet,
            Server server,
            Func<IQueryable<TEntity>, Task<TOut>> query
        )
            where TEntity : class, IServerEntity
        {
            return dbSet.ServersideQueryAsync
            (
                q => query(q.Where(a => a.Server == server))
            );
        }

        /// <summary>
        /// Performs a serverside query against the database set, fully materializing it after finishing.
        /// </summary>
        /// <param name="dbSet">The database set.</param>
        /// <param name="user">The user to get the characters of.</param>
        /// <param name="server">The server to scope the search to.</param>
        /// <param name="query">Additional query statements.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TOut">The output type.</typeparam>
        /// <returns>A queryable list of characters belonging to the user.</returns>
        public static Task<IReadOnlyList<TOut>> UserScopedServersideQueryAsync<TEntity, TOut>
        (
            this DbSet<TEntity> dbSet,
            User user,
            Server server,
            Func<IQueryable<TEntity>, IQueryable<TOut>> query,
            CancellationToken ct = default
        )
            where TEntity : class, IServerEntity, IOwnedNamedEntity
        {
            return dbSet.ServerScopedServersideQueryAsync
            (
                server,
                q => query(q.Where(ch => ch.Owner == user)),
                ct
            );
        }

        /// <summary>
        /// Performs a single-value query against the characters belonging to the given user on the given server.
        /// </summary>
        /// <param name="dbSet">The database set.</param>
        /// <param name="user">The user to scope the search to.</param>
        /// <param name="server">The server to scope the search to.</param>
        /// <param name="query">Additional query statements.</param>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TOut">The output type.</typeparam>
        /// <returns>A queryable set of characters.</returns>
        public static Task<TOut> UserScopedServersideQueryAsync<TEntity, TOut>
        (
            this DbSet<TEntity> dbSet,
            User user,
            Server server,
            Func<IQueryable<TEntity>, Task<TOut>> query
        )
            where TEntity : class, IServerEntity, IOwnedEntity
        {
            return dbSet.ServerScopedServersideQueryAsync
            (
                server,
                q => query(q.Where(ch => ch.Owner == user))
            );
        }
    }
}
