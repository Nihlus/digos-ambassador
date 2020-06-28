//
//  RoleplayServerCommands.cs
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

using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using DIGOS.Ambassador.Plugins.Permissions.Preconditions;
using DIGOS.Ambassador.Plugins.Roleplaying.Permissions;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using Discord.Commands;
using JetBrains.Annotations;
using static Discord.Commands.ContextType;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Roleplaying.CommandModules
{
    public partial class RoleplayCommands
    {
        /// <summary>
        /// Server-related commands, such as viewing or editing info about a specific server.
        /// </summary>
        [UsedImplicitly]
        [Group("server")]
        [Alias("server", "guild")]
        [Summary("Server-related commands, such as viewing or editing info about a specific server.")]
        public partial class RoleplayServerCommands : ModuleBase
        {
            private readonly UserFeedbackService _feedback;
            private readonly ServerService _servers;
            private readonly RoleplayServerSettingsService _serverSettings;

            /// <summary>
            /// Initializes a new instance of the <see cref="RoleplayServerCommands"/> class.
            /// </summary>
            /// <param name="feedback">The user feedback service.</param>
            /// <param name="serverSettings">The roleplaying server settings service.</param>
            /// <param name="servers">The server service.</param>
            public RoleplayServerCommands
            (
                UserFeedbackService feedback,
                RoleplayServerSettingsService serverSettings,
                ServerService servers)
            {
                _feedback = feedback;
                _serverSettings = serverSettings;
                _servers = servers;
            }

            /// <summary>
            /// Clears the channel category to use for dedicated roleplays.
            /// </summary>
            [UsedImplicitly]
            [Command("clear-roleplay-category")]
            [Summary("Clears the channel category to use for dedicated roleplays.")]
            [RequireContext(Guild)]
            [RequirePermission(typeof(EditRoleplayServerSettings), PermissionTarget.Self)]
            public async Task ClearDedicatedRoleplayChannelCategory()
            {
                var getServerResult = await _servers.GetOrRegisterServerAsync(this.Context.Guild);
                if (!getServerResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getServerResult.ErrorReason);
                    return;
                }

                var server = getServerResult.Entity;

                var result = await _serverSettings.SetDedicatedChannelCategoryAsync(server, null);

                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Dedicated channel category cleared.");
            }

            /// <summary>
            /// Clears the role to use as a default @everyone role in dynamic roleplays.
            /// </summary>
            [UsedImplicitly]
            [Command("clear-default-user-role")]
            [Summary("Clears the role to use as a default @everyone role in dynamic roleplays.")]
            [RequireContext(Guild)]
            [RequirePermission(typeof(EditRoleplayServerSettings), PermissionTarget.Self)]
            public async Task SetDefaultUserRole()
            {
                var getServerResult = await _servers.GetOrRegisterServerAsync(this.Context.Guild);
                if (!getServerResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getServerResult.ErrorReason);
                    return;
                }

                var server = getServerResult.Entity;

                var result = await _serverSettings.SetDefaultUserRoleAsync
                (
                    server,
                    null
                );

                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Default user role cleared.");
            }
        }
    }
}
