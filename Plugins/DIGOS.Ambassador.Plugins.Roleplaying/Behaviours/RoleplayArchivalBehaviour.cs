//
//  RoleplayArchivalBehaviour.cs
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

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Roleplaying.Extensions;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using DIGOS.Ambassador.Plugins.Roleplaying.Services.Exporters;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Discord.Behaviours;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Behaviours
{
    /// <summary>
    /// Continuously archives old roleplays.
    /// </summary>
    [UsedImplicitly]
    public class RoleplayArchivalBehaviour : ContinuousDiscordBehaviour<RoleplayArchivalBehaviour>
    {
        private readonly UserFeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayArchivalBehaviour"/> class.
        /// </summary>
        /// <param name="client">The Discord client.</param>
        /// <param name="serviceScope">The service scope in use.</param>
        /// <param name="logger">The logging instance for this type.</param>
        /// <param name="feedback">The feedback service.</param>
        public RoleplayArchivalBehaviour
        (
            DiscordSocketClient client,
            IServiceScope serviceScope,
            ILogger<RoleplayArchivalBehaviour> logger,
            UserFeedbackService feedback
        )
            : base(client, serviceScope, logger)
        {
            _feedback = feedback;
        }

        /// <inheritdoc />
        protected override async Task OnTickAsync(CancellationToken ct)
        {
            if (this.Client.ConnectionState != ConnectionState.Connected)
            {
                // Give the client some time to start up
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                return;
            }

            using var tickScope = this.Services.CreateScope();
            var roleplayService = tickScope.ServiceProvider.GetRequiredService<RoleplayDiscordService>();
            var serverService = tickScope.ServiceProvider.GetRequiredService<ServerService>();
            var serverSettings = tickScope.ServiceProvider.GetRequiredService<RoleplayServerSettingsService>();
            var dedicatedChannels = tickScope.ServiceProvider.GetRequiredService<DedicatedChannelService>();

            foreach (var guild in this.Client.Guilds)
            {
                var getGuildRoleplays = await roleplayService.GetRoleplaysAsync(guild);
                if (!getGuildRoleplays.IsSuccess)
                {
                    continue;
                }

                var guildRoleplays = getGuildRoleplays.Entity;

                var roleplays = await guildRoleplays
                    .Where(r => r.DedicatedChannelID.HasValue)
                    .Where(r => r.LastUpdated.HasValue)
                    .ToListAsync(ct);

                foreach (var roleplay in roleplays)
                {
                    // ReSharper disable once PossibleInvalidOperationException
                    var timeSinceLastActivity = DateTime.Now - roleplay.LastUpdated!.Value;
                    if (timeSinceLastActivity <= TimeSpan.FromDays(28))
                    {
                        continue;
                    }

                    await ArchiveRoleplayAsync
                    (
                        guild,
                        serverService,
                        roleplayService,
                        dedicatedChannels,
                        serverSettings,
                        roleplay
                    );

                    await NotifyOwnerAsync(roleplay);
                }
            }

            await Task.Delay(TimeSpan.FromHours(1), ct);
        }

        private async Task ArchiveRoleplayAsync
        (
            SocketGuild guild,
            ServerService serverService,
            RoleplayDiscordService roleplayService,
            DedicatedChannelService dedicatedChannels,
            RoleplayServerSettingsService serverSettings,
            Roleplay roleplay
        )
        {
            if (roleplay.DedicatedChannelID is null)
            {
                return;
            }

            if (roleplay.IsPublic)
            {
                await PostArchivedRoleplayAsync(guild, serverService, serverSettings, roleplay);
            }

            var dedicatedChannel = guild.GetTextChannel((ulong)roleplay.DedicatedChannelID);
            if (dedicatedChannel is null)
            {
                return;
            }

            // Ensure the messages are all caught up
            foreach (var message in await dedicatedChannel.GetMessagesAsync().FlattenAsync())
            {
                if (!(message is IUserMessage userMessage))
                {
                    continue;
                }

                // We don't care about the results here.
                await roleplayService.ConsumeMessageAsync(userMessage);
            }

            await dedicatedChannels.DeleteChannelAsync(guild, roleplay);
        }

        /// <summary>
        /// Posts the archived roleplay to the guild's archive channel, if it has one.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <param name="serverService">The server service.</param>
        /// <param name="serverSettings">The server settings service.</param>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task PostArchivedRoleplayAsync
        (
            SocketGuild guild,
            ServerService serverService,
            RoleplayServerSettingsService serverSettings,
            Roleplay roleplay
        )
        {
            var getServer = await serverService.GetOrRegisterServerAsync(guild);
            if (!getServer.IsSuccess)
            {
                this.Log.LogWarning("Failed to get the server for the current guild. Bug?");
                return;
            }

            var server = getServer.Entity;

            var getSettings = await serverSettings.GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettings.IsSuccess)
            {
                this.Log.LogWarning("Failed to get the server settings for the current guild. Bug?");
                return;
            }

            var settings = getSettings.Entity;

            if (settings.ArchiveChannel is null)
            {
                return;
            }

            var archiveChannel = guild.GetTextChannel((ulong)settings.ArchiveChannel);
            if (archiveChannel is null)
            {
                this.Log.LogWarning("Failed to get the archive channel for the current guild. Deleted?");
                return;
            }

            var exporter = new PDFRoleplayExporter(guild);
            using var exportedRoleplay = await exporter.ExportAsync(roleplay);

            var eb = _feedback.CreateEmbedBase();
            eb.WithTitle($"{exportedRoleplay.Title} - Archived");
            eb.WithDescription(roleplay.Summary);
            eb.WithFooter($"Archived on {DateTime.Now:d}.");

            await archiveChannel.SendFileAsync
            (
                exportedRoleplay.Data,
                $"{exportedRoleplay.Title}.{exportedRoleplay.Format.GetFileExtension()}",
                string.Empty,
                embed: eb.Build()
            );
        }

        /// <summary>
        /// Notifies the owner of the roleplay that it was stopped because it timed out.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task NotifyOwnerAsync(Roleplay roleplay)
        {
            var owner = this.Client.GetUser((ulong)roleplay.Owner.DiscordID);
            if (owner is null)
            {
                return;
            }

            var notification = _feedback.CreateEmbedBase();
            notification.WithDescription
            (
                $"Your roleplay \"{roleplay.Name}\" has been inactive for more than 28 days, and has been " +
                $"archived.\n" +
                $"\n" +
                $"This means that the dedicated channel that the roleplay had has been deleted. All messages in the " +
                $"roleplay have been saved, and can be exported or replayed as normal."
            );

            notification.WithFooter
            (
                $"You can export it by running !rp export \"{roleplay.Name}\"."
            );

            try
            {
                await owner.SendMessageAsync(string.Empty, embed: notification.Build());
            }
            catch (HttpException hex) when (hex.WasCausedByDMsNotAccepted())
            {
                // Nom nom nom
            }
        }
    }
}
