//
//  ModelBuilderExtensions.cs
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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DIGOS.Ambassador.Core.Database.Extensions
{
    /// <summary>
    /// Defines extension methods for the <see cref="ModelBuilder"/> class.
    /// </summary>
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Excludes the given entity type from migrations.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <returns>The model builder, with the type excluded.</returns>
        public static ModelBuilder ExcludeEntityFromMigrations<TEntity>(this ModelBuilder modelBuilder)
            where TEntity : class
        {
            modelBuilder.Entity<TEntity>().ToTable("placeholder", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<TEntity>().Metadata.RemoveAnnotation(RelationalAnnotationNames.TableName);

            return modelBuilder;
        }
    }
}
