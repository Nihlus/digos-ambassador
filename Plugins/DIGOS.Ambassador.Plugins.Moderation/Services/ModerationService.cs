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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Moderation.Services
{
    /// <summary>
    /// Acts as an interface for accessing and modifying moderation settings.
    /// </summary>
    public sealed class ModerationService
    {
        private readonly ModerationDatabaseContext _database;
        private readonly ServerService _servers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModerationService"/> class.
        /// </summary>
        /// <param name="database">The database context.</param>
        /// <param name="servers">The server service.</param>
        public ModerationService
        (
            ModerationDatabaseContext database,
            ServerService servers
        )
        {
            _database = database;
            _servers = servers;
        }

        /// <summary>
        /// Gets or creates the settings entity for the given Discord guild.
        /// </summary>
        /// <param name="guildID">The ID of the guild.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<ServerModerationSettings>> GetOrCreateServerSettingsAsync
        (
            Snowflake guildID,
            CancellationToken ct = default
        )
        {
            var getExistingEntry = await GetServerSettingsAsync(guildID, ct);
            if (getExistingEntry.IsSuccess)
            {
                return getExistingEntry.Entity;
            }

            var createSettings = await CreateServerSettingsAsync(guildID, ct);

            return !createSettings.IsSuccess
                ? Result<ServerModerationSettings>.FromError(createSettings)
                : createSettings.Entity;
        }

        /// <summary>
        /// Gets the settings for the given Discord guild.
        /// </summary>
        /// <param name="guildID">The ID of the guild.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<ServerModerationSettings>> GetServerSettingsAsync
        (
            Snowflake guildID,
            CancellationToken ct = default
        )
        {
            var settings = await _database.ServerSettings.ServersideQueryAsync
            (
                q => q
                    .Where(s => s.Server.DiscordID == guildID)
                    .SingleOrDefaultAsync(ct)
            );

            if (!(settings is null))
            {
                return settings;
            }

            return new GenericError
            (
                "The server doesn't have any settings."
            );
        }

        /// <summary>
        /// Creates the settings for the given Discord guild.
        /// </summary>
        /// <param name="guildID">The ID of the guild.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<Result<ServerModerationSettings>> CreateServerSettingsAsync
        (
            Snowflake guildID,
            CancellationToken ct = default
        )
        {
            var existingEntity = await GetServerSettingsAsync(guildID, ct);
            if (existingEntity.IsSuccess)
            {
                return new GenericError("That server already has settings.");
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result<ServerModerationSettings>.FromError(getServer);
            }

            var server = getServer.Entity;

            var settings = _database.CreateProxy<ServerModerationSettings>(server);
            _database.ServerSettings.Update(settings);

            await _database.SaveChangesAsync(ct);

            return settings;
        }

        /// <summary>
        /// Sets the moderation log channel for the given server.
        /// </summary>
        /// <param name="guildID">The ID of the guild.</param>
        /// <param name="channelID">The ID of the moderation log channel.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetModerationLogChannelAsync
        (
            Snowflake guildID,
            Snowflake channelID,
            CancellationToken ct = default
        )
        {
            var getSettings = await GetOrCreateServerSettingsAsync(guildID, ct);
            if (!getSettings.IsSuccess)
            {
                return Result.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            if (settings.ModerationLogChannel == channelID)
            {
                return new GenericError("That's already the moderation log channel.");
            }

            settings.ModerationLogChannel = channelID;
            await _database.SaveChangesAsync(ct);

            return Result.FromSuccess();
        }

        /// <summary>
        /// Sets the monitoring channel for the given server.
        /// </summary>
        /// <param name="guildID">The ID of the guild.</param>
        /// <param name="channelID">The ID of the monitoring channel.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetMonitoringChannelAsync
        (
            Snowflake guildID,
            Snowflake channelID,
            CancellationToken ct = default
        )
        {
            var getSettings = await GetOrCreateServerSettingsAsync(guildID, ct);
            if (!getSettings.IsSuccess)
            {
                return Result.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            if (settings.MonitoringChannel == channelID)
            {
                return new GenericError("That's already the monitoring channel.");
            }

            settings.MonitoringChannel = channelID;
            await _database.SaveChangesAsync(ct);

            return Result.FromSuccess();
        }

        /// <summary>
        /// Sets the warning threshold for the given server.
        /// </summary>
        /// <param name="guildID">The ID of the guild.</param>
        /// <param name="warningThreshold">The warning threshold.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetWarningThresholdAsync
        (
            Snowflake guildID,
            int warningThreshold,
            CancellationToken ct = default
        )
        {
            var getSettings = await GetOrCreateServerSettingsAsync(guildID, ct);
            if (!getSettings.IsSuccess)
            {
                return Result.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            if (settings.WarningThreshold == warningThreshold)
            {
                return new GenericError($"The warning threshold is already {warningThreshold}.");
            }

            settings.WarningThreshold = warningThreshold;
            await _database.SaveChangesAsync(ct);

            return Result.FromSuccess();
        }
    }
}
