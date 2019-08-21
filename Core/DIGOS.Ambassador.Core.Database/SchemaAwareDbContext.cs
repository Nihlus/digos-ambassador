//
//  SchemaAwareDbContext.cs
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
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Core.Database
{
    /// <summary>
    /// Acts as a base class for EF Core database contexts that take the schema of the model into account.
    /// </summary>
    public abstract class SchemaAwareDbContext : DbContext
    {
        /// <summary>
        /// Gets the schema of the database.
        /// </summary>
        public string Schema { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaAwareDbContext"/> class.
        /// </summary>
        /// <param name="schema">The schema.</param>
        /// <param name="contextOptions">The context options.</param>
        protected SchemaAwareDbContext(string schema, DbContextOptions contextOptions)
            : base(contextOptions)
        {
            this.Schema = schema;
        }

        /// <inheritdoc />
        protected override void OnConfiguring([NotNull] DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                throw new InvalidOperationException("Configure the context before use.");
            }
        }

        /// <inheritdoc />
        protected override void OnModelCreating([NotNull] ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema(this.Schema);
        }
    }
}
