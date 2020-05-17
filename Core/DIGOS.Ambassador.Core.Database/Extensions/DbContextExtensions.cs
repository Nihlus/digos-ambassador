//
//  DbContextExtensions.cs
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

using System.Linq;
using DIGOS.Ambassador.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Core.Database.Extensions
{
    /// <summary>
    /// Extensions for database contexts.
    /// </summary>
    public static class DbContextExtensions
    {
        /// <summary>
        /// Normalizes an entity reference, replacing it with an already-tracked instance if one exists.
        /// </summary>
        /// <param name="context">The context to normalize from.</param>
        /// <param name="entity">The potentially foreign entity.</param>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <returns>The entity, or another entity from the context with the same ID.</returns>
        public static TEntity NormalizeReference<TEntity>(this DbContext context, TEntity entity)
            where TEntity : class, IEFEntity
        {
            var existingEntityEntry = context.ChangeTracker.Entries<TEntity>().FirstOrDefault
            (
                e => e.Entity.ID == entity.ID
            );

            return existingEntityEntry is null
                ? entity
                : existingEntityEntry.Entity;
        }
    }
}
