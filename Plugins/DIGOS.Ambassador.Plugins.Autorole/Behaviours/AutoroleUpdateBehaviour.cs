//
//  AutoroleUpdateBehaviour.cs
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
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Autorole.Results;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Discord.Behaviours;

namespace DIGOS.Ambassador.Plugins.Autorole.Behaviours
{
    /// <summary>
    /// Performs continuous updates of autoroles, as well as notifications to a specific channel about users that
    /// require confirmation.
    /// </summary>
    [UsedImplicitly]
    public class AutoroleUpdateBehaviour : ContinuousDiscordBehaviour<AutoroleUpdateBehaviour>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoroleUpdateBehaviour"/> class.
        /// </summary>
        /// <param name="client">The discord client.</param>
        /// <param name="serviceScope">The service scope.</param>
        /// <param name="logger">The logger.</param>
        public AutoroleUpdateBehaviour
        (
            DiscordSocketClient client,
            IServiceScope serviceScope,
            ILogger<AutoroleUpdateBehaviour> logger)
            : base(client, serviceScope, logger)
        {
        }

        /// <inheritdoc />
        protected override async Task OnTickAsync(CancellationToken ct)
        {
            var autoroles = this.Services.GetRequiredService<AutoroleService>();
            var autoroleUpdates = this.Services.GetRequiredService<AutoroleUpdateService>();

            foreach (var guild in this.Client.Guilds)
            {
                var botUser = guild.GetUser(this.Client.CurrentUser.Id);
                if (botUser is null)
                {
                    this.Log.LogError("Failed to get our own user as a guild user. Yikes!");
                    continue;
                }

                if (!botUser.GuildPermissions.ManageRoles)
                {
                    // It's pointless to try to add or remove roles on this server
                    continue;
                }

                var guildAutoroles = autoroles.GetAutoroles(guild);

                if (!await guildAutoroles.AnyAsync(ct))
                {
                    continue;
                }

                if (!guild.HasAllMembers)
                {
                    await guild.DownloadUsersAsync();
                }

                foreach (var autorole in guildAutoroles)
                {
                    var updateResults = autoroleUpdates.UpdateAutoroleAsync(autorole).WithCancellation(ct);
                    await foreach (var updateResult in updateResults)
                    {
                        if (!updateResult.IsSuccess)
                        {
                            this.Log.LogError(updateResult.Exception, updateResult.ErrorReason);
                            continue;
                        }

                        switch (updateResult.Status)
                        {
                            case AutoroleUpdateStatus.RequiresAffirmation:
                            {
                                // TODO: Send affirmation request
                                break;
                            }
                        }
                    }
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }
    }
}
