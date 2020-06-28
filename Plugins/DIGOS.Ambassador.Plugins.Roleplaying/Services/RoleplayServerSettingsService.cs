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
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using Discord;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Services
{
    /// <summary>
    /// Business logic for server-specific roleplay settings.
    /// </summary>
    [PublicAPI]
    public class RoleplayServerSettingsService
    {
        private readonly RoleplayingDatabaseContext _database;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayServerSettingsService"/> class.
        /// </summary>
        /// <param name="database">The database context.</param>
        public RoleplayServerSettingsService(RoleplayingDatabaseContext database)
        {
            _database = database;
        }

        /// <summary>
        /// Gets or creates a set of server-specific roleplaying settings for the given server.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<ServerRoleplaySettings>> GetOrCreateServerRoleplaySettingsAsync
        (
            Server server
        )
        {
            var settings = await _database.ServerSettings.ServersideQueryAsync
            (
                q => q
                    .Where(s => s.Server == server)
                    .SingleOrDefaultAsync()
            );

            if (!(settings is null))
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
        /// <param name="server">The server.</param>
        /// <param name="channel">The channel to use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetArchiveChannelAsync
        (
            Server server,
            ITextChannel? channel
        )
        {
            var getSettingsResult = await GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettingsResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;

            long? channelId;
            if (channel?.Id is null)
            {
                channelId = null;
            }
            else
            {
                channelId = (long)channel.Id;
            }

            settings.ArchiveChannel = channelId;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the role to use as a default @everyone role.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="role">The role to use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetDefaultUserRoleAsync
        (
            Server server,
            IRole? role
        )
        {
            var getSettingsResult = await GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettingsResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;

            long? roleId;
            if (role?.Id is null)
            {
                roleId = null;
            }
            else
            {
                roleId = (long)role.Id;
            }

            if (settings.DefaultUserRole == roleId)
            {
                return ModifyEntityResult.FromError("That's already the default user role.");
            }

            settings.DefaultUserRole = roleId;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the channel category to use for dedicated roleplay channels.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="category">The category to use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetDedicatedChannelCategoryAsync
        (
            Server server,
            ICategoryChannel? category
        )
        {
            var getSettingsResult = await GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettingsResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;

            long? categoryId;
            if (category?.Id is null)
            {
                categoryId = null;
            }
            else
            {
                categoryId = (long)category.Id;
            }

            settings.DedicatedRoleplayChannelsCategory = categoryId;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }
    }
}
