//
//  ExpirationBehaviour.cs
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
using DIGOS.Ambassador.Plugins.Moderation.Services;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Discord.Behaviours;

namespace DIGOS.Ambassador.Plugins.Moderation.Behaviours
{
    /// <summary>
    /// Rescinds expired warnings and bans.
    /// </summary>
    [UsedImplicitly]
    internal sealed class ExpirationBehaviour : ContinuousDiscordBehaviour<ExpirationBehaviour>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpirationBehaviour"/> class.
        /// </summary>
        /// <param name="client">The Discord client.</param>
        /// <param name="serviceScope">The service scope in use.</param>
        /// <param name="logger">The logging instance for this type.</param>
        public ExpirationBehaviour
        (
            DiscordSocketClient client,
            IServiceScope serviceScope,
            ILogger<ExpirationBehaviour> logger
        )
            : base(client, serviceScope, logger)
        {
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
            var warningService = tickScope.ServiceProvider.GetRequiredService<WarningService>();
            var banService = tickScope.ServiceProvider.GetRequiredService<BanService>();
            var loggingService = tickScope.ServiceProvider.GetRequiredService<ChannelLoggingService>();

            var now = DateTime.UtcNow;

            foreach (var guild in this.Client.Guilds)
            {
                // Using .HasValue instead of .IsTemporary here to allow server-side evaluation
                var warnings = await warningService.GetWarnings(guild).Where(w => w.ExpiresOn.HasValue).ToListAsync(ct);
                foreach (var warning in warnings)
                {
                    if (warning.ExpiresOn <= now)
                    {
                        var rescinder = guild.GetUser(this.Client.CurrentUser.Id);
                        await loggingService.NotifyUserWarningRemoved(warning, rescinder);

                        await warningService.DeleteWarningAsync(warning);
                    }
                }

                if (!guild.GetUser(this.Client.CurrentUser.Id).GuildPermissions.BanMembers)
                {
                    // No point in trying to rescind bans if the bot doesn't have ban perms
                    continue;
                }

                // Using .HasValue instead of .IsTemporary here to allow server-side evaluation
                var bans = await banService.GetBans(guild).Where(b => b.ExpiresOn.HasValue).ToListAsync(ct);
                foreach (var ban in bans)
                {
                    if (ban.ExpiresOn <= now)
                    {
                        var rescinder = guild.GetUser(this.Client.CurrentUser.Id);
                        await loggingService.NotifyUserUnbanned(ban, rescinder);

                        await banService.DeleteBanAsync(ban);
                        await guild.RemoveBanAsync((ulong)ban.User.DiscordID);
                    }
                }
            }

            await Task.Delay(TimeSpan.FromHours(1), ct);
        }
    }
}
