//
//  RoleplayServerSettingsService.cs
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

using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Services
{
    /// <summary>
    /// Business logic for server-specific roleplay settings.
    /// </summary>
    public class RoleplayServerSettingsService
    {
        private readonly RoleplayingDatabaseContext _database;
        private readonly ServerService _servers;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayServerSettingsService"/> class.
        /// </summary>
        /// <param name="database">The database context.</param>
        /// <param name="servers">The server service.</param>
        public RoleplayServerSettingsService(RoleplayingDatabaseContext database, ServerService servers)
        {
            _database = database;
            _servers = servers;
        }

        /// <summary>
        /// Gets or creates a set of server-specific roleplaying settings for the given server.
        /// </summary>
        /// <param name="serverID">The ID of the server.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<ServerRoleplaySettings>> GetOrCreateServerRoleplaySettingsAsync(Snowflake serverID)
        {
            var getServer = await _servers.GetOrRegisterServerAsync(serverID);
            if (!getServer.IsSuccess)
            {
                return Result<ServerRoleplaySettings>.FromError(getServer);
            }

            var server = getServer.Entity;

            var settings = await _database.ServerSettings.ServersideQueryAsync
            (
                q => q
                    .Where(s => s.Server == server)
                    .SingleOrDefaultAsync()
            );

            if (settings is not null)
            {
                return settings;
            }

            var newSettings = _database.CreateProxy<ServerRoleplaySettings>(server);
            _database.ServerSettings.Update(newSettings);

            await _database.SaveChangesAsync();

            return newSettings;
        }

        /// <summary>
        /// Sets the channel to use for archived roleplays.
        /// </summary>
        /// <param name="serverID">The server.</param>
        /// <param name="channelID">The channel to use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetArchiveChannelAsync
        (
            Snowflake serverID,
            Snowflake? channelID
        )
        {
            var getSettingsResult = await GetOrCreateServerRoleplaySettingsAsync(serverID);
            if (!getSettingsResult.IsSuccess)
            {
                return Result.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;
            settings.ArchiveChannel = channelID;

            await _database.SaveChangesAsync();

            return Result.FromSuccess();
        }

        /// <summary>
        /// Sets the role to use as a default @everyone role.
        /// </summary>
        /// <param name="serverID">The server.</param>
        /// <param name="roleID">The role to use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetDefaultUserRoleAsync
        (
            Snowflake serverID,
            Snowflake? roleID
        )
        {
            var getSettingsResult = await GetOrCreateServerRoleplaySettingsAsync(serverID);
            if (!getSettingsResult.IsSuccess)
            {
                return Result.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;

            if (settings.DefaultUserRole == roleID)
            {
                return new UserError("That's already the default user role.");
            }

            settings.DefaultUserRole = roleID;
            await _database.SaveChangesAsync();

            return Result.FromSuccess();
        }

        /// <summary>
        /// Sets the channel category to use for dedicated roleplay channels.
        /// </summary>
        /// <param name="serverID">The server.</param>
        /// <param name="categoryID">The category to use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetDedicatedChannelCategoryAsync
        (
            Snowflake serverID,
            Snowflake? categoryID
        )
        {
            var getSettingsResult = await GetOrCreateServerRoleplaySettingsAsync(serverID);
            if (!getSettingsResult.IsSuccess)
            {
                return Result.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;
            settings.DedicatedRoleplayChannelsCategory = categoryID;

            await _database.SaveChangesAsync();

            return Result.FromSuccess();
        }
    }
}
