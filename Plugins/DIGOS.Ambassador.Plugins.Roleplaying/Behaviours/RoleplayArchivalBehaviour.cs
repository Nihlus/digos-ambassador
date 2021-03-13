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
using System.Transactions;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Roleplaying.Extensions;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using DIGOS.Ambassador.Plugins.Roleplaying.Services.Exporters;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Behaviours.Bases;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Behaviours
{
    /// <summary>
    /// Continuously archives old roleplays.
    /// </summary>
    [UsedImplicitly]
    public class RoleplayArchivalBehaviour : ContinuousBehaviour<RoleplayArchivalBehaviour>
    {
        private readonly UserFeedbackService _feedback;

        /// <inheritdoc />
        protected override TimeSpan TickDelay => TimeSpan.FromMinutes(1);

        /// <inheritdoc />
        protected override bool UseTransaction => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayArchivalBehaviour"/> class.
        /// </summary>
        /// <param name="client">The Discord client.</param>
        /// <param name="services">The services.</param>
        /// <param name="logger">The logging instance for this type.</param>
        /// <param name="feedback">The feedback service.</param>
        public RoleplayArchivalBehaviour
        (
            IServiceProvider services,
            ILogger<RoleplayArchivalBehaviour> logger,
            UserFeedbackService feedback
        )
            : base(services, logger)
        {
            _feedback = feedback;
        }

        /// <inheritdoc />
        protected override async Task<Result> OnTickAsync(CancellationToken ct, IServiceProvider tickServices)
        {
            var roleplayService = tickServices.GetRequiredService<RoleplayDiscordService>();
            var serverService = tickServices.GetRequiredService<ServerService>();
            var serverSettings = tickServices.GetRequiredService<RoleplayServerSettingsService>();
            var dedicatedChannels = tickServices.GetRequiredService<DedicatedChannelService>();

            foreach (var guild in this.Client.Guilds)
            {
                if (ct.IsCancellationRequested)
                {
                    return new UserError("Operation was cancelled.");
                }

                var getGuildRoleplays = await roleplayService.GetRoleplaysAsync(guild);
                if (!getGuildRoleplays.IsSuccess)
                {
                    continue;
                }

                var guildRoleplays = getGuildRoleplays.Entity;

                var roleplays = guildRoleplays
                    .Where(r => r.DedicatedChannelID.HasValue)
                    .Where(r => r.LastUpdated.HasValue)
                    .Where(r => DateTime.Now - r.LastUpdated > TimeSpan.FromDays(28))
                    .ToList();

                foreach (var roleplay in roleplays)
                {
                    if (ct.IsCancellationRequested)
                    {
                        return new UserError("Operation was cancelled.");
                    }

                    // We'll use a transaction per warning to avoid timeouts
                    using var archivalTransaction = new TransactionScope
                    (
                        TransactionScopeOption.Required,
                        this.TransactionOptions,
                        TransactionScopeAsyncFlowOption.Enabled
                    );

                    var archiveResult = await ArchiveRoleplayAsync
                    (
                        guild,
                        serverService,
                        roleplayService,
                        dedicatedChannels,
                        serverSettings,
                        roleplay
                    );

                    if (!archiveResult.IsSuccess)
                    {
                        return Result.FromError(archiveResult);
                    }

                    var notifyResult = await NotifyOwnerAsync(roleplay);
                    if (!notifyResult.IsSuccess)
                    {
                        return Result.FromError(notifyResult);
                    }

                    archivalTransaction.Complete();
                }
            }

            return Result.FromSuccess();
        }

        private async Task<Result> ArchiveRoleplayAsync
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
                return new UserError("The roleplay doesn't have a dedicated channel.");
            }

            if (roleplay.IsPublic)
            {
                var postResult = await PostArchivedRoleplayAsync(guild, serverService, serverSettings, roleplay);
                if (!postResult.IsSuccess)
                {
                    return Result.FromError(postResult);
                }
            }

            var dedicatedChannel = guild.GetTextChannel((ulong)roleplay.DedicatedChannelID);
            if (dedicatedChannel is null)
            {
                // Something's gone wrong in the database. Who the fuck knows why. We'll do an extra delete to be
                // on the safe side.
                await dedicatedChannels.DeleteChannelAsync(guild, roleplay);

                return Result.FromSuccess();
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
            return Result.FromSuccess();
        }

        /// <summary>
        /// Posts the archived roleplay to the guild's archive channel, if it has one.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <param name="serverService">The server service.</param>
        /// <param name="serverSettings">The server settings service.</param>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<Result> PostArchivedRoleplayAsync
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
                return Result.FromError(getServer);
            }

            var server = getServer.Entity;

            var getSettings = await serverSettings.GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettings.IsSuccess)
            {
                return Result.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            if (settings.ArchiveChannel is null)
            {
                return new UserError("No archive channel has been set.");
            }

            var archiveChannel = guild.GetTextChannel((ulong)settings.ArchiveChannel);
            if (archiveChannel is null)
            {
                return new UserError("Failed to get the archive channel. Deleted?");
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

            return Result.FromSuccess();
        }

        /// <summary>
        /// Notifies the owner of the roleplay that it was stopped because it timed out.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<Result> NotifyOwnerAsync(Roleplay roleplay)
        {
            var owner = this.Client.GetUser((ulong)roleplay.Owner.DiscordID);
            if (owner is null)
            {
                return new UserError("Could not retrieve the owner of the roleplay.");
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

            return Result.FromSuccess();
        }
    }
}
