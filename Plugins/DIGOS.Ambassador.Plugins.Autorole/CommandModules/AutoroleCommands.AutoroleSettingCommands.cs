//
//  AutoroleCommands.AutoroleSettingCommands.cs
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
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Extensions.Results;
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
        /// <summary>
        /// Contains commands for server-wide autorole settings.
        /// </summary>
        [UsedImplicitly]
        [Group("settings")]
        [Summary("Commands for server-wide autorole settings.")]
        public partial class AutoroleSettingCommands : ModuleBase
        {
            private readonly AutoroleService _autoroles;
            private readonly UserFeedbackService _feedback;

            /// <summary>
            /// Initializes a new instance of the <see cref="AutoroleSettingCommands"/> class.
            /// </summary>
            /// <param name="autoroles">The autorole service.</param>
            /// <param name="feedback">The feedback service.</param>
            public AutoroleSettingCommands(AutoroleService autoroles, UserFeedbackService feedback)
            {
                _autoroles = autoroles;
                _feedback = feedback;
            }

            /// <summary>
            /// Shows the server-wide settings.
            /// </summary>
            [UsedImplicitly]
            [Alias("show", "view")]
            [Command("show")]
            [Summary("Shows the server-wide autorole settings.")]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(typeof(ShowAutoroleServerSettings), PermissionTarget.Self)]
            public async Task<RuntimeResult> ShowSettingsAsync()
            {
                var getSettings = await _autoroles.GetOrCreateServerSettingsAsync(this.Context.Guild);
                if (!getSettings.IsSuccess)
                {
                    return getSettings.ToRuntimeResult();
                }

                var settings = getSettings.Entity;

                var notificationChannelValue = settings.AffirmationRequiredNotificationChannelID.HasValue
                    ? MentionUtils.MentionChannel((ulong)settings.AffirmationRequiredNotificationChannelID.Value)
                    : "None";

                var embed = _feedback.CreateEmbedBase()
                    .WithTitle("Autorole Settings")
                    .AddField("Confirmation Notification Channel", notificationChannelValue);

                await _feedback.SendEmbedAsync(this.Context.Channel, embed.Build());
                return RuntimeCommandResult.FromSuccess();
            }

            /// <summary>
            /// Clears the confirmation notification channel.
            /// </summary>
            [UsedImplicitly]
            [Alias("clear-confirmation-notification-channel")]
            [Command("clear-notification-channel")]
            [Summary("clears the confirmation notification channel.")]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(typeof(EditAutoroleServerSettings), PermissionTarget.Self)]
            public async Task<RuntimeResult> ClearAffirmationNotificationChannel()
            {
                var clearResult = await _autoroles.ClearAffirmationNotificationChannelAsync
                (
                    this.Context.Guild
                );

                if (!clearResult.IsSuccess)
                {
                    return clearResult.ToRuntimeResult();
                }

                return RuntimeCommandResult.FromSuccess("Channel cleared.");
            }
        }
    }
}
