//
//  HostedRoleplayArchivalService.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using DIGOS.Ambassador.Plugins.Roleplaying.Extensions;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Services.Exporters;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Services;

/// <summary>
/// Continuously archives old roleplays.
/// </summary>
[UsedImplicitly]
public class HostedRoleplayArchivalService : BackgroundService
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostedRoleplayArchivalService"/> class.
    /// </summary>
    /// <param name="services">The services.</param>
    public HostedRoleplayArchivalService(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct))
        {
            await using var scope = _services.CreateAsyncScope();

            var scopedExpirationService = scope.ServiceProvider.GetRequiredService<ScopedRoleplayArchivalService>();
            await scopedExpirationService.ExecuteAsync(ct);
        }
    }

    /// <summary>
    /// Handles scoped execution of the actual service code.
    /// </summary>
    internal sealed class ScopedRoleplayArchivalService
    {
        private readonly ILogger<ScopedRoleplayArchivalService> _log;
        private readonly FeedbackService _feedback;
        private readonly RoleplayDiscordService _roleplayService;
        private readonly RoleplayServerSettingsService _serverSettings;
        private readonly DedicatedChannelService _dedicatedChannels;
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly PDFRoleplayExporter _exporter;

        private readonly TransactionOptions _transactionOptions = new()
        {
            Timeout = TransactionManager.DefaultTimeout,
            IsolationLevel = IsolationLevel.Serializable
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ScopedRoleplayArchivalService"/> class.
        /// </summary>
        /// <param name="log">The logging instance fpr this type.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="roleplayService">The roleplaying service.</param>
        /// <param name="serverSettings">The roleplaying server settings.</param>
        /// <param name="dedicatedChannels">The dedicated channel service.</param>
        /// <param name="channelAPI">The channel API.</param>
        /// <param name="exporter">The PDF roleplay exporter.</param>
        public ScopedRoleplayArchivalService
        (
            ILogger<ScopedRoleplayArchivalService> log,
            FeedbackService feedback,
            RoleplayDiscordService roleplayService,
            RoleplayServerSettingsService serverSettings,
            DedicatedChannelService dedicatedChannels,
            IDiscordRestChannelAPI channelAPI,
            PDFRoleplayExporter exporter
        )
        {
            _log = log;
            _feedback = feedback;
            _roleplayService = roleplayService;
            _serverSettings = serverSettings;
            _dedicatedChannels = dedicatedChannels;
            _channelAPI = channelAPI;
            _exporter = exporter;
        }

        /// <summary>
        /// Executes the service's workload.
        /// </summary>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            var roleplays = await _roleplayService.QueryRoleplaysAsync
            (
                q => q
                    .Where(r => r.DedicatedChannelID.HasValue)
                    .Where(r => r.LastUpdated.HasValue)
                    .Where(r => DateTimeOffset.UtcNow - r.LastUpdated > TimeSpan.FromDays(28))
            );

            foreach (var roleplay in roleplays)
            {
                ct.ThrowIfCancellationRequested();

                // We'll use a transaction per action to avoid timeouts
                using var archivalTransaction = new TransactionScope
                (
                    TransactionScopeOption.Required,
                    _transactionOptions,
                    TransactionScopeAsyncFlowOption.Enabled
                );

                var archiveResult = await ArchiveRoleplayAsync(roleplay);
                if (!archiveResult.IsSuccess)
                {
                    // skip this one
                    _log.LogWarning("Failed to archive roleplay \"{Name}\"", roleplay.Name);
                    continue;
                }

                var notifyResult = await NotifyOwnerAsync(roleplay);
                if (!notifyResult.IsSuccess)
                {
                    // it's fine
                    _log.LogWarning("Failed to notify owner of roleplay \"{Name}\" that it has been archived", roleplay.Name);
                }

                archivalTransaction.Complete();
            }
        }

        private async Task<Result> ArchiveRoleplayAsync(Roleplay roleplay)
        {
            var ensureLogged = await _roleplayService.EnsureAllMessagesAreLoggedAsync(roleplay);
            if (!ensureLogged.IsSuccess)
            {
                return Result.FromError(ensureLogged);
            }

            if (!roleplay.IsPublic)
            {
                return await _dedicatedChannels.DeleteChannelAsync(roleplay);
            }

            var postResult = await PostArchivedRoleplayAsync(roleplay);
            if (!postResult.IsSuccess)
            {
                return postResult;
            }

            return await _dedicatedChannels.DeleteChannelAsync(roleplay);
        }

        private async Task<Result> PostArchivedRoleplayAsync(Roleplay roleplay)
        {
            var getSettings = await _serverSettings.GetOrCreateServerRoleplaySettingsAsync(roleplay.Server.DiscordID);
            if (!getSettings.IsSuccess)
            {
                return Result.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            if (settings.ArchiveChannel is null)
            {
                // nowhere to post it, that's fine
                return Result.FromSuccess();
            }

            using var exportedRoleplay = await _exporter.ExportAsync(roleplay);

            var embed = new Embed
            {
                Colour = _feedback.Theme.Secondary,
                Title = $"{exportedRoleplay.Title} - Archived",
                Description = roleplay.GetSummaryOrDefault(),
                Footer = new EmbedFooter($"Archived on {DateTimeOffset.UtcNow:d}.")
            };

            var fileData = new FileData
            (
                $"{exportedRoleplay.Title}.{exportedRoleplay.Format.GetFileExtension()}",
                exportedRoleplay.Data
            );

            var send = await _channelAPI.CreateMessageAsync
            (
                settings.ArchiveChannel.Value,
                embeds: new[] { embed },
                attachments: new List<OneOf<FileData, IPartialAttachment>> { fileData }
            );

            return send.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(send);
        }

        private async Task<Result> NotifyOwnerAsync(Roleplay roleplay)
        {
            var notification = new Embed
            {
                Colour = _feedback.Theme.Secondary,
                Description =
                    $"Your roleplay \"{roleplay.Name}\" has been inactive for more than 28 days, and has been " +
                    "archived.\n" +
                    "\n" +
                    "This means that the dedicated channel that the roleplay had has been deleted. All messages in the " +
                    "roleplay have been saved, and can be exported or replayed as normal.",
                Footer = new EmbedFooter($"You can export it by running !rp export \"{roleplay.Name}\".")
            };

            var send = await _feedback.SendPrivateEmbedAsync(roleplay.Owner.DiscordID, notification);
            return send.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(send);
        }
    }
}
