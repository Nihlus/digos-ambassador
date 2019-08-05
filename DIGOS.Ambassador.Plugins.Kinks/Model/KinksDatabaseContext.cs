//
//  KinksDatabaseContext.cs
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

using DIGOS.Ambassador.Database.Abstractions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Plugins.Kinks.Model
{
    /// <summary>
    /// Represents the database model of the dossier plugin.
    /// </summary>
    public class KinksDatabaseContext : SchemaAwareDbContext
    {
        private const string SchemaName = "KinkModule";

        /// <summary>
        /// Gets or sets the table where kinks are stored.
        /// </summary>
        public DbSet<Kink> Kinks
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where user kinks are stored.
        /// </summary>
        public DbSet<UserKink> UserKinks
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KinksDatabaseContext"/> class.
        /// </summary>
        /// <param name="contextOptions">The context options.</param>
        public KinksDatabaseContext(DbContextOptions<KinksDatabaseContext> contextOptions)
            : base(SchemaName, contextOptions)
        {
        }
    }
}
