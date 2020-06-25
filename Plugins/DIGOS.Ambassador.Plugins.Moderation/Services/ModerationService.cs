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

using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Services.TransientState;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using Discord;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Moderation.Services
{
    /// <summary>
    /// Acts as an interface for accessing and modifying moderation settings.
    /// </summary>
    [PublicAPI]
    public sealed class ModerationService : AbstractTransientStateService
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
            : base(servers)
        {
            _database = database;
            _servers = servers;
        }

        /// <summary>
        /// Gets or creates the settings entity for the given Discord guild.
        /// </summary>
        /// <param name="discordServer">The server.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<ServerModerationSettings>> GetOrCreateServerSettingsAsync
        (
            IGuild discordServer
        )
        {
            var getExistingEntry = await GetServerSettingsAsync(discordServer);
            if (getExistingEntry.IsSuccess)
            {
                return getExistingEntry.Entity;
            }

            var createSettings = await CreateServerSettingsAsync(discordServer);
            if (!createSettings.IsSuccess)
            {
                return RetrieveEntityResult<ServerModerationSettings>.FromError(createSettings);
            }

            return createSettings.Entity;
        }

        /// <summary>
        /// Gets the settings for the given Discord guild.
        /// </summary>
        /// <param name="discordServer">The server.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<ServerModerationSettings>> GetServerSettingsAsync
        (
            IGuild discordServer
        )
        {
            var entity = await _database.ServerSettings.FirstOrDefaultAsync
            (
                s => s.Server.DiscordID == (long)discordServer.Id
            );

            if (entity is null)
            {
                return RetrieveEntityResult<ServerModerationSettings>.FromError
                (
                    "The server doesn't have any settings."
                );
            }

            return entity;
        }

        /// <summary>
        /// Creates the settings for the given Discord guild.
        /// </summary>
        /// <param name="discordServer">The server.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<ServerModerationSettings>> CreateServerSettingsAsync
        (
            IGuild discordServer
        )
        {
            var existingEntity = await GetServerSettingsAsync(discordServer);
            if (existingEntity.IsSuccess)
            {
                return CreateEntityResult<ServerModerationSettings>.FromError("That server already has settings.");
            }

            var getServer = await _servers.GetOrRegisterServerAsync(discordServer);
            if (!getServer.IsSuccess)
            {
                return CreateEntityResult<ServerModerationSettings>.FromError(getServer);
            }

            var server = getServer.Entity;
            var settings = new ServerModerationSettings(server);

            _database.ServerSettings.Update(settings);

            return settings;
        }

        /// <summary>
        /// Sets the moderation log channel for the given server.
        /// </summary>
        /// <param name="guild">The server.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetModerationLogChannelAsync
        (
            IGuild guild,
            ITextChannel channel
        )
        {
            var getSettings = await GetOrCreateServerSettingsAsync(guild);
            if (!getSettings.IsSuccess)
            {
                return ModifyEntityResult.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            if (settings.ModerationLogChannel == (long)channel.Id)
            {
                return ModifyEntityResult.FromError("That's already the moderation log channel.");
            }

            settings.ModerationLogChannel = (long)channel.Id;
            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the monitoring channel for the given server.
        /// </summary>
        /// <param name="guild">The server.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetMonitoringChannelAsync
        (
            IGuild guild,
            ITextChannel channel
        )
        {
            var getSettings = await GetOrCreateServerSettingsAsync(guild);
            if (!getSettings.IsSuccess)
            {
                return ModifyEntityResult.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            if (settings.MonitoringChannel == (long)channel.Id)
            {
                return ModifyEntityResult.FromError("That's already the monitoring channel.");
            }

            settings.MonitoringChannel = (long)channel.Id;
            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the warning threshold for the given server.
        /// </summary>
        /// <param name="guild">The server.</param>
        /// <param name="warningThreshold">The warning threshold.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetWarningThresholdAsync
        (
            IGuild guild,
            int warningThreshold
        )
        {
            var getSettings = await GetOrCreateServerSettingsAsync(guild);
            if (!getSettings.IsSuccess)
            {
                return ModifyEntityResult.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            if (settings.WarningThreshold == warningThreshold)
            {
                return ModifyEntityResult.FromError($"The warning threshold is already {warningThreshold}.");
            }

            settings.WarningThreshold = warningThreshold;

            return ModifyEntityResult.FromSuccess();
        }

        /// <inheritdoc/>
        protected override void OnSavingChanges()
        {
            _database.SaveChanges();
        }

        /// <inheritdoc/>
        protected override async ValueTask OnSavingChangesAsync(CancellationToken ct = default)
        {
            await _database.SaveChangesAsync(ct);
        }
    }
}
