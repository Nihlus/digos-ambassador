//
//  ModerationService.cs
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

using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Moderation.Services
{
    /// <summary>
    /// Acts as an interface for accessing and modifying moderation actions.
    /// </summary>
    [PublicAPI]
    public sealed class ModerationService
    {
        [NotNull] private readonly ModerationDatabaseContext _database;
        [NotNull] private readonly ServerService _servers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModerationService"/> class.
        /// </summary>
        /// <param name="database">The database context.</param>
        /// <param name="servers">The server service.</param>
        public ModerationService
        (
            [NotNull] ModerationDatabaseContext database,
            [NotNull] ServerService servers
        )
        {
            _database = database;
            _servers = servers;
        }
    }
}
