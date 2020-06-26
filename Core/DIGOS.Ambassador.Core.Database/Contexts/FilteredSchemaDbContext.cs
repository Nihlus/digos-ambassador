//
//  FilteredSchemaDbContext.cs
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

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Remora.EntityFrameworkCore.Modular;

namespace DIGOS.Ambassador.Core.Database.Contexts
{
    /// <summary>
    /// Represents a schema-aware db context that filters out any entities that doesn't belong to its schema on save.
    /// </summary>
    public abstract class FilteredSchemaDbContext : SchemaAwareDbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilteredSchemaDbContext"/> class.
        /// </summary>
        /// <param name="schema">The name of the schema.</param>
        /// <param name="contextOptions">The configured context options.</param>
        public FilteredSchemaDbContext
        (
            string schema,
            DbContextOptions contextOptions
        )
            : base(schema, contextOptions)
        {
        }

        /// <inheritdoc />
        public override int SaveChanges()
        {
            foreach (var entry in this.ChangeTracker.Entries())
            {
                // Detach any entities that don't belong to us
                if (entry.Metadata.GetSchema() != this.Schema)
                {
                    entry.State = EntityState.Detached;
                }
            }

            return base.SaveChanges();
        }

        /// <inheritdoc />
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            foreach (var entry in this.ChangeTracker.Entries())
            {
                // Detach any entities that don't belong to us
                if (entry.Metadata.GetSchema() != this.Schema)
                {
                    entry.State = EntityState.Detached;
                }
            }

            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        /// <inheritdoc />
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            foreach (var entry in this.ChangeTracker.Entries())
            {
                // Detach any entities that don't belong to us
                if (entry.Metadata.GetSchema() != this.Schema)
                {
                    entry.State = EntityState.Detached;
                }
            }

            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <inheritdoc />
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in this.ChangeTracker.Entries())
            {
                // Detach any entities that don't belong to us
                if (entry.Metadata.GetSchema() != this.Schema)
                {
                    entry.State = EntityState.Detached;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
