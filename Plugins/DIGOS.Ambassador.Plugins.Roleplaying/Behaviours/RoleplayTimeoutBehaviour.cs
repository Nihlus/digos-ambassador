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
            var roleplayService = tickScope.ServiceProvider.GetRequiredService<RoleplayService>();
            var channelService = tickScope.ServiceProvider.GetRequiredService<DedicatedChannelService>();

            var roleplays = await roleplayService.GetRoleplays()
                .Where(r => r.IsActive)
                .Where(r => r.LastUpdated.HasValue)
                .ToListAsync(ct);

            foreach (var roleplay in roleplays)
            {
                // ReSharper disable once PossibleInvalidOperationException
                var timeSinceLastActivity = DateTime.Now - roleplay.LastUpdated!.Value;
                if (timeSinceLastActivity <= TimeSpan.FromHours(72))
                {
                    continue;
                }

                await StopRoleplayAsync(roleplayService, channelService, roleplay);
                await NotifyOwner(roleplay);
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

        /// <summary>
        /// Stops the given roleplay, and hides the dedicated channel if it has one.
        /// </summary>
        /// <param name="roleplayService">The roleplaying service in use.</param>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task StopRoleplayAsync(RoleplayService roleplayService, DedicatedChannelService channelService, Roleplay roleplay)
        {
            var stopRoleplayAsync = await roleplayService.StopRoleplayAsync(roleplay);
            if (!stopRoleplayAsync.IsSuccess)
            {
                this.Log.LogWarning($"Failed to stop the roleplay {roleplay.Name}: {stopRoleplayAsync.ErrorReason}");
                return;
            }

            if (!(roleplay.DedicatedChannelID is null))
            {
                var guild = this.Client.Guilds.FirstOrDefault
                (
                    g => g.Channels.Any(c => c.Id == (ulong)roleplay.DedicatedChannelID.Value)
                );

                if (guild is null)
                {
                    return;
                }

                var getDedicatedChannelResult = await channelService.GetDedicatedChannelAsync
                (
                    guild,
                    roleplay
                );

                // Hide the channel for all participants
                if (getDedicatedChannelResult.IsSuccess)
                {
                    var dedicatedChannel = getDedicatedChannelResult.Entity;

                    foreach (var participant in roleplay.ParticipatingUsers)
                    {
                        var user = guild.GetUser((ulong)participant.User.DiscordID);
                        if (user is null)
                        {
                            continue;
                        }

                        await channelService.SetChannelWritabilityForUserAsync
                        (
                            dedicatedChannel,
                            user,
                            false
                        );

                        await channelService.SetChannelVisibilityForUserAsync
                        (
                            dedicatedChannel,
                            user,
                            false
                        );
                    }

                    if (roleplay.IsPublic)
                    {
                        var everyoneRole = guild.EveryoneRole;
                        await channelService.SetChannelVisibilityForRoleAsync
                        (
                            dedicatedChannel,
                            everyoneRole,
                            false
                        );
                    }
                }
            }
        }
    }
}
