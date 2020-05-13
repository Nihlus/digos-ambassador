//
//  AutoroleCommands.AutoroleSettingCommands.AutoroleSettingSetCommands.cs
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
using DIGOS.Ambassador.Plugins.Autorole.Permissions;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using DIGOS.Ambassador.Plugins.Permissions.Preconditions;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Autorole.CommandModules
{
    public partial class AutoroleCommands
    {
        public partial class AutoroleSettingCommands
        {
            /// <summary>
            /// Contains commands for editing server-wide autorole settings.
            /// </summary>
            [UsedImplicitly]
            [Group("set")]
            [Summary("Commands for editing server-wide autorole settings.")]
            public class AutoroleSettingSetCommands : ModuleBase
            {
                private readonly AutoroleService _autoroles;
                private readonly UserFeedbackService _feedback;

                /// <summary>
                /// Initializes a new instance of the <see cref="AutoroleSettingSetCommands"/> class.
                /// </summary>
                /// <param name="autoroles">The autorole service.</param>
                /// <param name="feedback">The feedback service.</param>
                public AutoroleSettingSetCommands(AutoroleService autoroles, UserFeedbackService feedback)
                {
                    _autoroles = autoroles;
                    _feedback = feedback;
                }

                /// <summary>
                /// Sets the confirmation notification channel.
                /// </summary>
                /// <param name="textChannel">The channel.</param>
                [UsedImplicitly]
                [Alias("confirmation-notification-channel")]
                [Command("confirmation-notification-channel")]
                [Summary("Sets the confirmation notification channel.")]
                [RequireContext(ContextType.Guild)]
                [RequirePermission(typeof(EditAutoroleServerSettings), PermissionTarget.Self)]
                public async Task SetAffirmationNotificationChannel(ITextChannel textChannel)
                {
                    var setResult = await _autoroles.SetAffirmationNotificationChannelAsync
                    (
                        this.Context.Guild,
                        textChannel
                    );

                    if (!setResult.IsSuccess)
                    {
                        await _feedback.SendErrorAsync(this.Context, setResult.ErrorReason);
                        return;
                    }

                    await _feedback.SendConfirmationAsync(this.Context, "Channel set.");
                }
            }
        }
    }
}
