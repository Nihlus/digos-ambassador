//
//  RoleplayServerSetCommands.cs
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
using DIGOS.Ambassador.Plugins.Permissions.Preconditions;
using DIGOS.Ambassador.Plugins.Roleplaying.Permissions;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using static Discord.Commands.ContextType;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Roleplaying.CommandModules
{
    public partial class RoleplayCommands
    {
        public partial class RoleplayServerCommands
        {
            /// <summary>
            /// Server info setter commands.
            /// </summary>
            [UsedImplicitly]
            [Group("set")]
            public class ServerSetCommands : ModuleBase
            {
                private readonly UserFeedbackService _feedback;
                private readonly ServerService _servers;
                private readonly RoleplayService _roleplaying;

                /// <summary>
                /// Initializes a new instance of the <see cref="ServerSetCommands"/> class.
                /// </summary>
                /// <param name="feedback">The user feedback service.</param>
                /// <param name="roleplaying">The roleplaying service.</param>
                /// <param name="servers">The server service.</param>
                public ServerSetCommands
                (
                    UserFeedbackService feedback,
                    RoleplayService roleplaying,
                    ServerService servers)
                {
                    _feedback = feedback;
                    _roleplaying = roleplaying;
                    _servers = servers;
                }

                /// <summary>
                /// Sets the channel category to use for dedicated roleplays.
                /// </summary>
                /// <param name="category">The category to use.</param>
                [UsedImplicitly]
                [Command("roleplay-category")]
                [Summary("Sets the channel category to use for dedicated roleplays.")]
                [RequireContext(Guild)]
                [RequirePermission(typeof(EditRoleplayServerSettings), PermissionTarget.Self)]
                public async Task SetDedicatedRoleplayChannelCategory(ICategoryChannel category)
                {
                    var getServerResult = await _servers.GetOrRegisterServerAsync(this.Context.Guild);
                    if (!getServerResult.IsSuccess)
                    {
                        await _feedback.SendErrorAsync(this.Context, getServerResult.ErrorReason);
                        return;
                    }

                    var server = getServerResult.Entity;

                    var result = await _roleplaying.SetDedicatedRoleplayChannelCategoryAsync
                    (
                        server,
                        category
                    );

                    if (!result.IsSuccess)
                    {
                        await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                        return;
                    }

                    await _feedback.SendConfirmationAsync(this.Context, "Dedicated channel category set.");
                }
            }
        }
    }
}
