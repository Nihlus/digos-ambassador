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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Plugins.Roleplaying.Extensions;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using DIGOS.Ambassador.Plugins.Roleplaying.Services.Exporters;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using Remora.Behaviours.Bases;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Behaviours
{
    /// <summary>
    /// Continuously archives old roleplays.
    /// </summary>
    [UsedImplicitly]
    public class RoleplayArchivalBehaviour : ContinuousBehaviour<RoleplayArchivalBehaviour>
    {
        /// <inheritdoc />
        protected override TimeSpan TickDelay => TimeSpan.FromMinutes(1);

        /// <inheritdoc />
        protected override bool UseTransaction => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayArchivalBehaviour"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="logger">The logging instance for this type.</param>
        public RoleplayArchivalBehaviour
        (
            IServiceProvider services,
            ILogger<RoleplayArchivalBehaviour> logger
        )
            : base(services, logger)
        {
        }

        /// <inheritdoc />
        protected override async Task<Result> OnTickAsync(CancellationToken ct, IServiceProvider tickServices)
        {
            var feedback = tickServices.GetRequiredService<FeedbackService>();
            var roleplayService = tickServices.GetRequiredService<RoleplayDiscordService>();
            var serverSettings = tickServices.GetRequiredService<RoleplayServerSettingsService>();
            var dedicatedChannels = tickServices.GetRequiredService<DedicatedChannelService>();

            var roleplays = await roleplayService.QueryRoleplaysAsync
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
                    this.TransactionOptions,
                    TransactionScopeAsyncFlowOption.Enabled
                );

                var archiveResult = await ArchiveRoleplayAsync
                (
                    tickServices,
                    feedback,
                    roleplayService,
                    dedicatedChannels,
                    serverSettings,
                    roleplay
                );

                if (!archiveResult.IsSuccess)
                {
                    return archiveResult;
                }

                var notifyResult = await NotifyOwnerAsync(feedback, roleplay);
                if (!notifyResult.IsSuccess)
                {
                    return notifyResult;
                }

                archivalTransaction.Complete();
            }

            return Result.FromSuccess();
        }

        private async Task<Result> ArchiveRoleplayAsync
        (
            IServiceProvider services,
            FeedbackService feedback,
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

            var ensureLogged = await roleplayService.EnsureAllMessagesAreLoggedAsync(roleplay);
            if (!ensureLogged.IsSuccess)
            {
                return Result.FromError(ensureLogged);
            }

            if (roleplay.IsPublic)
            {
                var postResult = await PostArchivedRoleplayAsync(services, feedback, serverSettings, roleplay);
                if (!postResult.IsSuccess)
                {
                    return postResult;
                }
            }

            return await dedicatedChannels.DeleteChannelAsync(roleplay);
        }

        private async Task<Result> PostArchivedRoleplayAsync
        (
            IServiceProvider services,
            FeedbackService feedback,
            RoleplayServerSettingsService serverSettings,
            Roleplay roleplay
        )
        {
            var channelAPI = services.GetRequiredService<IDiscordRestChannelAPI>();

            var getSettings = await serverSettings.GetOrCreateServerRoleplaySettingsAsync(roleplay.Server.DiscordID);
            if (!getSettings.IsSuccess)
            {
                return Result.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            if (settings.ArchiveChannel is null)
            {
                return new UserError("No archive channel has been set.");
            }

            var exporter = new PDFRoleplayExporter();
            using var exportedRoleplay = await exporter.ExportAsync(services, roleplay);

            var embed = new Embed
            {
                Colour = feedback.Theme.Secondary,
                Title = $"{exportedRoleplay.Title} - Archived",
                Description = roleplay.Summary,
                Footer = new EmbedFooter($"Archived on {DateTimeOffset.UtcNow:d}.")
            };

            var fileData = new FileData
            (
                $"{exportedRoleplay.Title}.{exportedRoleplay.Format.GetFileExtension()}",
                exportedRoleplay.Data
            );

            var send = await channelAPI.CreateMessageAsync
            (
                settings.ArchiveChannel.Value,
                embeds: new[] { embed },
                attachments: new List<OneOf<FileData, IPartialAttachment>> { fileData }
            );

            return send.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(send);
        }

        private async Task<Result> NotifyOwnerAsync(FeedbackService feedback, Roleplay roleplay)
        {
            var notification = new Embed
            {
                Colour = feedback.Theme.Secondary,
                Description =
                $"Your roleplay \"{roleplay.Name}\" has been inactive for more than 28 days, and has been " +
                "archived.\n" +
                "\n" +
                "This means that the dedicated channel that the roleplay had has been deleted. All messages in the " +
                "roleplay have been saved, and can be exported or replayed as normal.",
                Footer = new EmbedFooter($"You can export it by running !rp export \"{roleplay.Name}\".")
            };

            var send = await feedback.SendPrivateEmbedAsync(roleplay.Owner.DiscordID, notification);
            return send.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(send);
        }
    }
}
