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
using System.ComponentModel;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback.Results;
using DIGOS.Ambassador.Plugins.Moderation.Permissions;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Moderation.CommandModules
{
    public partial class WarningCommands
    {
        /// <summary>
        /// Warning setter commands.
        /// </summary>
        [Group("set")]
        public class WarningSetCommands : CommandGroup
        {
            private readonly WarningService _warnings;
            private readonly ICommandContext _context;

            /// <summary>
            /// Initializes a new instance of the <see cref="WarningSetCommands"/> class.
            /// </summary>
            /// <param name="warnings">The moderation service.</param>
            /// <param name="context">The command context.</param>
            public WarningSetCommands
            (
                WarningService warnings,
                ICommandContext context
            )
            {
                _warnings = warnings;
                _context = context;
            }

            /// <summary>
            /// Sets the reason for the warning.
            /// </summary>
            /// <param name="warningID">The ID of the warning to edit.</param>
            /// <param name="newReason">The new reason for the warning.</param>
            [Command("reason")]
            [Description("Sets the reason for the warning.")]
            [RequirePermission(typeof(ManageWarnings), PermissionTarget.All)]
            [RequireContext(ChannelContext.Guild)]
            public async Task<Result<UserMessage>> SetWarningReasonAsync(long warningID, string newReason)
            {
                var getWarning = await _warnings.GetWarningAsync(_context.GuildID.Value, warningID);
                if (!getWarning.IsSuccess)
                {
                    return Result<UserMessage>.FromError(getWarning);
                }

                var warning = getWarning.Entity;

                var setContents = await _warnings.SetWarningReasonAsync(warning, newReason);
                if (!setContents.IsSuccess)
                {
                    return Result<UserMessage>.FromError(setContents);
                }

                return new ConfirmationMessage("Warning reason updated.");
            }

            /// <summary>
            /// Sets the contextually relevant message for the warning.
            /// </summary>
            /// <param name="warningID">The ID of the warning to edit.</param>
            /// <param name="newMessage">The new reason for the warning.</param>
            [Command("context-message")]
            [Description("Sets the contextually relevant message for the warning.")]
            [RequirePermission(typeof(ManageWarnings), PermissionTarget.All)]
            [RequireContext(ChannelContext.Guild)]
            public async Task<Result<UserMessage>> SetWarningContextMessageAsync(long warningID, IMessage newMessage)
            {
                var getWarning = await _warnings.GetWarningAsync(_context.GuildID.Value, warningID);
                if (!getWarning.IsSuccess)
                {
                    return Result<UserMessage>.FromError(getWarning);
                }

                var warning = getWarning.Entity;

                var setMessage = await _warnings.SetWarningContextMessageAsync(warning, newMessage.ID);
                if (!setMessage.IsSuccess)
                {
                    return Result<UserMessage>.FromError(setMessage);
                }

                return new ConfirmationMessage("Warning context message updated.");
            }

            /// <summary>
            /// Sets the duration of the warning.
            /// </summary>
            /// <param name="warningID">The ID of the warning to edit.</param>
            /// <param name="newDuration">The new duration of the warning.</param>
            [Command("duration")]
            [Description("Sets the duration of the warning.")]
            [RequirePermission(typeof(ManageWarnings), PermissionTarget.All)]
            [RequireContext(ChannelContext.Guild)]
            public async Task<Result<UserMessage>> SetWarningDurationAsync(long warningID, TimeSpan newDuration)
            {
                var getWarning = await _warnings.GetWarningAsync(_context.GuildID.Value, warningID);
                if (!getWarning.IsSuccess)
                {
                    return Result<UserMessage>.FromError(getWarning);
                }

                var warning = getWarning.Entity;

                var newExpiration = warning.CreatedAt.Add(newDuration);

                var setExpiration = await _warnings.SetWarningExpiryDateAsync(warning, newExpiration);
                if (!setExpiration.IsSuccess)
                {
                    return Result<UserMessage>.FromError(setExpiration);
                }

                return new ConfirmationMessage("Warning duration updated.");
            }
        }
    }
}
