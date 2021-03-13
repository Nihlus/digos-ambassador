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

using System.ComponentModel;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback.Results;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Permissions;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Conditions;
using Remora.Results;

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
        [Description("Server-related commands, such as viewing or editing info about a specific server.")]
        public partial class RoleplayServerCommands : CommandGroup
        {
            private readonly ServerService _servers;
            private readonly RoleplayServerSettingsService _serverSettings;

            /// <summary>
            /// Initializes a new instance of the <see cref="RoleplayServerCommands"/> class.
            /// </summary>
            /// <param name="serverSettings">The roleplaying server settings service.</param>
            /// <param name="servers">The server service.</param>
            public RoleplayServerCommands
            (
                RoleplayServerSettingsService serverSettings,
                ServerService servers)
            {
                _serverSettings = serverSettings;
                _servers = servers;
            }

            /// <summary>
            /// Clears the channel category to use for dedicated roleplays.
            /// </summary>
            [UsedImplicitly]
            [Command("clear-roleplay-category")]
            [Description("Clears the channel category to use for dedicated roleplays.")]
            [RequireContext(ChannelContext.Guild)]
            [RequirePermission(typeof(EditRoleplayServerSettings), PermissionTarget.Self)]
            public async Task<Result<UserMessage>> ClearDedicatedRoleplayChannelCategory()
            {
                var getServerResult = await _servers.GetOrRegisterServerAsync(_context.Guild);
                if (!getServerResult.IsSuccess)
                {
                    return Result<UserMessage>.FromError(getServerResult);
                }

                var server = getServerResult.Entity;

                var result = await _serverSettings.SetDedicatedChannelCategoryAsync(server, null);

                if (!result.IsSuccess)
                {
                    return Result<UserMessage>.FromError(result);
                }

                return new ConfirmationMessage("Dedicated channel category cleared.");
            }

            /// <summary>
            /// Clears the role to use as a default @everyone role in dynamic roleplays.
            /// </summary>
            [UsedImplicitly]
            [Command("clear-default-user-role")]
            [Description("Clears the role to use as a default @everyone role in dynamic roleplays.")]
            [RequireContext(ChannelContext.Guild)]
            [RequirePermission(typeof(EditRoleplayServerSettings), PermissionTarget.Self)]
            public async Task<Result<UserMessage>> SetDefaultUserRole()
            {
                var getServerResult = await _servers.GetOrRegisterServerAsync(_context.Guild);
                if (!getServerResult.IsSuccess)
                {
                    return Result<UserMessage>.FromError(getServerResult);
                }

                var server = getServerResult.Entity;

                var result = await _serverSettings.SetDefaultUserRoleAsync
                (
                    server,
                    null
                );

                if (!result.IsSuccess)
                {
                    return Result<UserMessage>.FromError(result);
                }

                return new ConfirmationMessage("Default user role cleared.");
            }
        }
    }
}
