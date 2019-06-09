//
//  DatabaseModuleBase.cs
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

using DIGOS.Ambassador.Database;
using Discord.Commands;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Modules.Base
{
    /// <summary>
    /// Represents a command module with a dependency on a database context.
    /// </summary>
    public abstract class DatabaseModuleBase : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Gets the database context.
        /// </summary>
        [NotNull, ProvidesContext]
        protected AmbyDatabaseContext Database { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseModuleBase"/> class.
        /// </summary>
        /// <param name="database">The database context.</param>
        protected DatabaseModuleBase([NotNull] AmbyDatabaseContext database)
        {
            this.Database = database;
        }
    }
}
