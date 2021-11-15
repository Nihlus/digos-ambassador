//
//  AmbassadorDbContext.cs
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

using DIGOS.Ambassador.Core.Database.Converters;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.Core;

namespace DIGOS.Ambassador.Core.Database.Context
{
    /// <summary>
    /// The base context for all ambassador contexts.
    /// </summary>
    public abstract class AmbassadorDbContext : DbContext
    {
        /// <summary>
        /// Gets the schema of the database.
        /// </summary>
        public string Schema { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbassadorDbContext"/> class.
        /// </summary>
        /// <param name="schema">The schema managed by the context.</param>
        /// <param name="contextOptions">The context options.</param>
        public AmbassadorDbContext(string schema, DbContextOptions contextOptions)
            : base(contextOptions)
        {
            this.Schema = schema;
        }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema(this.Schema);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.GetSchema() == this.Schema)
                {
                    continue;
                }

                entityType.SetIsTableExcludedFromMigrations(true);
            }

            modelBuilder.HasPostgresExtension("fuzzystrmatch");
        }

        /// <inheritdoc />
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            configurationBuilder.Properties<Snowflake>().HaveConversion(typeof(SnowflakeConverter));
        }
    }
}
