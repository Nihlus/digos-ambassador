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
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Roleplaying;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Services;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Behaviours
{
    /// <summary>
    /// Times out roleplays, stopping them if they've been inactive for more than a set time.
    /// </summary>
    public class RoleplayTimeoutBehaviour : BehaviourBase
    {
        /// <summary>
        /// Gets the database context.
        /// </summary>
        [NotNull]
        private GlobalInfoContext Database { get; }

        /// <summary>
        /// Gets the roleplay service.
        /// </summary>
        [NotNull]
        private RoleplayService Roleplays { get; }

        /// <summary>
        /// Gets the feedback service.
        /// </summary>
        private UserFeedbackService Feedback { get; }

        /// <summary>
        /// Gets or sets the timeout task.
        /// </summary>
        [NotNull]
        private Task TimeoutTask { get; set; }

        /// <summary>
        /// Gets the cancellation source.
        /// </summary>
        [NotNull]
        private CancellationTokenSource CancellationSource { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayTimeoutBehaviour"/> class.
        /// </summary>
        /// <param name="client">The discord client.</param>
        /// <param name="database">The database.</param>
        /// <param name="roleplays">The roleplay service.</param>
        /// <param name="feedback">The feedback service.</param>
        public RoleplayTimeoutBehaviour
        (
            DiscordSocketClient client,
            GlobalInfoContext database,
            RoleplayService roleplays,
            UserFeedbackService feedback
        )
            : base(client)
        {
            this.Database = database;
            this.Roleplays = roleplays;
            this.Feedback = feedback;
            this.CancellationSource = new CancellationTokenSource();

            this.TimeoutTask = Task.CompletedTask;
        }

        private async Task ContinuousTimeout(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (this.Client.ConnectionState != ConnectionState.Connected)
                {
                    continue;
                }

                var roleplays = await this.Database.Roleplays.ToListAsync(ct);
                foreach (var roleplay in roleplays)
                {
                    if (!roleplay.IsActive)
                    {
                        continue;
                    }

                    if (roleplay.LastUpdated is null)
                    {
                        continue;
                    }

                    var timeSinceLastActivity = DateTime.Now - roleplay.LastUpdated.Value;
                    if (timeSinceLastActivity > TimeSpan.FromHours(48))
                    {
                        await StopRoleplayAsync(roleplay);
                        await NotifyOwner(roleplay);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
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
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task StopRoleplayAsync(Roleplay roleplay)
        {
            roleplay.IsActive = false;
            roleplay.ActiveChannelID = null;

            await this.Database.SaveChangesAsync();

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

                var getDedicatedChannelResult = await this.Roleplays.GetDedicatedRoleplayChannelAsync
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

                        await this.Roleplays.SetDedicatedChannelWritabilityForUserAsync
                        (
                            dedicatedChannel,
                            user,
                            false
                        );

                        await this.Roleplays.SetDedicatedChannelVisibilityForUserAsync
                        (
                            dedicatedChannel,
                            user,
                            false
                        );
                    }

                    if (roleplay.IsPublic)
                    {
                        var everyoneRole = guild.EveryoneRole;
                        await this.Roleplays.SetDedicatedChannelVisibilityForRoleAsync
                        (
                            dedicatedChannel,
                            everyoneRole,
                            false
                        );
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override Task OnStartingAsync()
        {
            this.TimeoutTask = ContinuousTimeout(this.CancellationSource.Token);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override async Task OnStoppingAsync()
        {
            this.CancellationSource.Cancel();
            await this.TimeoutTask;
        }
    }
}
