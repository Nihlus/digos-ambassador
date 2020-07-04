//
//  BanSetCommands.cs
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
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Extensions.Results;
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
    public partial class BanCommands
    {
        /// <summary>
        /// Ban setter commands.
        /// </summary>
        [PublicAPI]
        [Group("set")]
        public class BanSetCommands : ModuleBase
        {
            private readonly BanService _bans;

            private readonly UserFeedbackService _feedback;

            /// <summary>
            /// Initializes a new instance of the <see cref="BanSetCommands"/> class.
            /// </summary>
            /// <param name="bans">The moderation service.</param>
            /// <param name="feedback">The feedback service.</param>
            public BanSetCommands
            (
                BanService bans,
                UserFeedbackService feedback
            )
            {
                _bans = bans;
                _feedback = feedback;
            }

            /// <summary>
            /// Sets the reason for the ban.
            /// </summary>
            /// <param name="banID">The ID of the ban to edit.</param>
            /// <param name="newReason">The new reason for the ban.</param>
            [Command("reason")]
            [Summary("Sets the reason for the ban.")]
            [RequirePermission(typeof(ManageBans), PermissionTarget.All)]
            [RequireContext(ContextType.Guild)]
            public async Task<RuntimeResult> SetBanReasonAsync(long banID, string newReason)
            {
                var getBan = await _bans.GetBanAsync(this.Context.Guild, banID);
                if (!getBan.IsSuccess)
                {
                    return getBan.ToRuntimeResult();
                }

                var ban = getBan.Entity;

                var setContents = await _bans.SetBanReasonAsync(ban, newReason);
                if (!setContents.IsSuccess)
                {
                    return setContents.ToRuntimeResult();
                }

                return RuntimeCommandResult.FromSuccess("Ban reason updated.");
            }

            /// <summary>
            /// Sets the contextually relevant message for the ban.
            /// </summary>
            /// <param name="banID">The ID of the ban to edit.</param>
            /// <param name="newMessage">The new reason for the ban.</param>
            [Command("context-message")]
            [Summary("Sets the contextually relevant message for the ban.")]
            [RequirePermission(typeof(ManageBans), PermissionTarget.All)]
            [RequireContext(ContextType.Guild)]
            public async Task<RuntimeResult> SetBanContextMessageAsync(long banID, IMessage newMessage)
            {
                var getBan = await _bans.GetBanAsync(this.Context.Guild, banID);
                if (!getBan.IsSuccess)
                {
                    return getBan.ToRuntimeResult();
                }

                var ban = getBan.Entity;

                var setMessage = await _bans.SetBanContextMessageAsync(ban, (long)newMessage.Id);
                if (!setMessage.IsSuccess)
                {
                    return setMessage.ToRuntimeResult();
                }

                return RuntimeCommandResult.FromSuccess("Ban context message updated.");
            }

            /// <summary>
            /// Sets the duration of the ban.
            /// </summary>
            /// <param name="banID">The ID of the ban to edit.</param>
            /// <param name="newDuration">The new duration of the ban.</param>
            [Command("duration")]
            [Summary("Sets the duration of the ban.")]
            [RequirePermission(typeof(ManageBans), PermissionTarget.All)]
            [RequireContext(ContextType.Guild)]
            public async Task<RuntimeResult> SetBanDurationAsync(long banID, TimeSpan newDuration)
            {
                var getBan = await _bans.GetBanAsync(this.Context.Guild, banID);
                if (!getBan.IsSuccess)
                {
                    return getBan.ToRuntimeResult();
                }

                var ban = getBan.Entity;

                var newExpiration = ban.CreatedAt.Add(newDuration);

                var setExpiration = await _bans.SetBanExpiryDateAsync(ban, newExpiration);
                if (!setExpiration.IsSuccess)
                {
                    return setExpiration.ToRuntimeResult();
                }

                return RuntimeCommandResult.FromSuccess("Ban duration updated.");
            }
        }
    }
}
