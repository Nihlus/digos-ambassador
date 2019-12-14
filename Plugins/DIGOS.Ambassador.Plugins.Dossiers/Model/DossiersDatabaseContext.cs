//
//  DossiersDatabaseContext.cs
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

namespace DIGOS.Ambassador.Plugins.Dossiers.Model
{
    /// <summary>
    /// Represents the database model of the dossier plugin.
    /// </summary>
    [PublicAPI]
    public class DossiersDatabaseContext : SchemaAwareDbContext
    {
        private const string SchemaName = "DossierModule";

        /// <summary>
        /// Gets or sets the table where dossiers are stored.
        /// </summary>
        public DbSet<Dossier> Dossiers
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DossiersDatabaseContext"/> class.
        /// </summary>
        /// <param name="contextOptions">The context options.</param>
        public DossiersDatabaseContext(DbContextOptions contextOptions)
            : base(SchemaName, contextOptions)
        {
        }
    }
}
