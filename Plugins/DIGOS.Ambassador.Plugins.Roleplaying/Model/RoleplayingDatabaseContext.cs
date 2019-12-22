//
//  RoleplayingDatabaseContext.cs
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

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.EntityFrameworkCore.Modular;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Roleplaying.Model
{
    /// <summary>
    /// Represents the database model of the dossier plugin.
    /// </summary>
    [PublicAPI]
    public class RoleplayingDatabaseContext : SchemaAwareDbContext
    {
        private const string SchemaName = "RoleplayModule";

        /// <summary>
        /// Gets or sets the table where roleplays are stored.
        /// </summary>
        public DbSet<Roleplay> Roleplays { get; [UsedImplicitly] set; } = null!;

        /// <summary>
        /// Gets or sets the table where server settings are stored.
        /// </summary>
        public DbSet<ServerRoleplaySettings> ServerSettings { get; [UsedImplicitly] set; } = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayingDatabaseContext"/> class.
        /// </summary>
        /// <param name="contextOptions">The context options.</param>
        public RoleplayingDatabaseContext(DbContextOptions<RoleplayingDatabaseContext> contextOptions)
            : base(SchemaName, contextOptions)
        {
        }
    }
}
