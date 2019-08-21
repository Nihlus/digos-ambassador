//
//  TransformationsDatabaseContext.cs
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

using DIGOS.Ambassador.Core.Database;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Plugins.Transformations.Model
{
    /// <summary>
    /// Represents the database model of the dossier plugin.
    /// </summary>
    public class TransformationsDatabaseContext : SchemaAwareDbContext
    {
        private const string SchemaName = "TransformationModule";

        /// <summary>
        /// Gets or sets the table where transformation species are stored.
        /// </summary>
        public DbSet<Species> Species
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where transformations are stored.
        /// </summary>
        public DbSet<Transformation> Transformations
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where global transformation protections are stored.
        /// </summary>
        public DbSet<GlobalUserProtection> GlobalUserProtections
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where server-specific transformation protections are stored.
        /// </summary>
        public DbSet<ServerUserProtection> ServerUserProtections
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where appearance configurations are stored.
        /// </summary>
        public DbSet<AppearanceConfiguration> AppearanceConfigurations
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationsDatabaseContext"/> class.
        /// </summary>
        /// <param name="contextOptions">The context options.</param>
        public TransformationsDatabaseContext(DbContextOptions<TransformationsDatabaseContext> contextOptions)
            : base(SchemaName, contextOptions)
        {
        }
    }
}
