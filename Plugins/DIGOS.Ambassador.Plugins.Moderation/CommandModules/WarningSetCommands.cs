//
//  WarningSetCommands.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Moderation.Permissions;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using DIGOS.Ambassador.Plugins.Permissions.Preconditions;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;

using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Moderation.CommandModules
{
    public partial class WarningCommands
    {
        /// <summary>
        /// Warning setter commands.
        /// </summary>
        [PublicAPI]
        [Group("set")]
        public class WarningSetCommands : ModuleBase
        {
            private readonly WarningService _warnings;

            private readonly UserFeedbackService _feedback;

            /// <summary>
            /// Initializes a new instance of the <see cref="WarningSetCommands"/> class.
            /// </summary>
            /// <param name="warnings">The moderation service.</param>
            /// <param name="feedback">The feedback service.</param>
            public WarningSetCommands
            (
                WarningService warnings,
                UserFeedbackService feedback
            )
            {
                _warnings = warnings;
                _feedback = feedback;
            }

            /// <summary>
            /// Sets the reason for the warning.
            /// </summary>
            /// <param name="warningID">The ID of the warning to edit.</param>
            /// <param name="newReason">The new reason for the warning.</param>
            [Command("reason")]
            [Summary("Sets the reason for the warning.")]
            [RequirePermission(typeof(ManageWarnings), PermissionTarget.All)]
            [RequireContext(ContextType.Guild)]
            public async Task SetWarningReasonAsync(long warningID, string newReason)
            {
                var getWarning = await _warnings.GetWarningAsync(this.Context.Guild, warningID);
                if (!getWarning.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getWarning.ErrorReason);
                    return;
                }

                var warning = getWarning.Entity;

                var setContents = await _warnings.SetWarningReasonAsync(warning, newReason);
                if (!setContents.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, setContents.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Warning reason updated.");
                _warnings.SaveChanges();
            }

            /// <summary>
            /// Sets the contextually relevant message for the warning.
            /// </summary>
            /// <param name="warningID">The ID of the warning to edit.</param>
            /// <param name="newMessage">The new reason for the warning.</param>
            [Command("context-message")]
            [Summary("Sets the contextually relevant message for the warning.")]
            [RequirePermission(typeof(ManageWarnings), PermissionTarget.All)]
            [RequireContext(ContextType.Guild)]
            public async Task SetWarningContextMessageAsync(long warningID, IMessage newMessage)
            {
                var getWarning = await _warnings.GetWarningAsync(this.Context.Guild, warningID);
                if (!getWarning.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getWarning.ErrorReason);
                    return;
                }

                var warning = getWarning.Entity;

                var setMessage = await _warnings.SetWarningContextMessageAsync(warning, (long)newMessage.Id);
                if (!setMessage.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, setMessage.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Warning context message updated.");
                _warnings.SaveChanges();
            }

            /// <summary>
            /// Sets the duration of the warning.
            /// </summary>
            /// <param name="warningID">The ID of the warning to edit.</param>
            /// <param name="newDuration">The new duration of the warning.</param>
            [Command("duration")]
            [Summary("Sets the duration of the warning.")]
            [RequirePermission(typeof(ManageWarnings), PermissionTarget.All)]
            [RequireContext(ContextType.Guild)]
            public async Task SetWarningDurationAsync(long warningID, TimeSpan newDuration)
            {
                var getWarning = await _warnings.GetWarningAsync(this.Context.Guild, warningID);
                if (!getWarning.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getWarning.ErrorReason);
                    return;
                }

                var warning = getWarning.Entity;

                var newExpiration = warning.CreatedAt.Add(newDuration);

                var setExpiration = await _warnings.SetWarningExpiryDateAsync(warning, newExpiration);
                if (!setExpiration.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, setExpiration.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Warning duration updated.");
                _warnings.SaveChanges();
            }
        }
    }
}
