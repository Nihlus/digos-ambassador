//
//  RoleplayTimeoutBehaviour.cs
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
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
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
    /// Times out roleplays, stopping them if they've been inactive for more than a set time.
    /// </summary>
    [UsedImplicitly]
    internal sealed class RoleplayTimeoutBehaviour : ContinuousDiscordBehaviour<RoleplayTimeoutBehaviour>
    {
        /// <summary>
        /// Gets the feedback service.
        /// </summary>
        private UserFeedbackService Feedback { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayTimeoutBehaviour"/> class.
        /// </summary>
        /// <param name="client">The discord client.</param>
        /// <param name="serviceScope">The service scope in use.</param>
        /// <param name="logger">The logging instance for this type.</param>
        /// <param name="feedback">The feedback service.</param>
        public RoleplayTimeoutBehaviour
        (
            DiscordSocketClient client,
            IServiceScope serviceScope,
            ILogger<RoleplayTimeoutBehaviour> logger,
            UserFeedbackService feedback
        )
            : base(client, serviceScope, logger)
        {
            this.Feedback = feedback;
        }

        /// <inheritdoc/>
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

            foreach (var guild in this.Client.Guilds)
            {
                var getRoleplays = await roleplayService.GetRoleplaysAsync(guild);
                if (!getRoleplays.IsSuccess)
                {
                    continue;
                }

                var timedOutRoleplays = await getRoleplays.Entity
                    .Where(r => r.IsActive)
                    .Where(r => r.LastUpdated.HasValue)
                    .Where(r => DateTime.Now - r.LastUpdated > TimeSpan.FromHours(72))
                    .ToListAsync(ct);

                foreach (var roleplay in timedOutRoleplays)
                {
                    var stopRoleplay = await roleplayService.StopRoleplayAsync(roleplay);
                    if (!stopRoleplay.IsSuccess)
                    {
                        this.Log.LogWarning(stopRoleplay.Exception, stopRoleplay.ErrorReason);
                    }

                    await NotifyOwner(roleplay);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }

        /// <summary>
        /// Notifies the owner of the roleplay that it was stopped because it timed out.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task NotifyOwner(Roleplay roleplay)
        {
            var owner = this.Client.GetUser((ulong)roleplay.Owner.DiscordID);
            if (owner is null)
            {
                return;
            }

            var notification = this.Feedback.CreateEmbedBase();
            notification.WithDescription
            (
                $"Due to inactivity, your roleplay \"{roleplay.Name}\" has been stopped."
            );

            notification.WithFooter
            (
                $"You can restart it by running !rp start \"{roleplay.Name}\"."
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
