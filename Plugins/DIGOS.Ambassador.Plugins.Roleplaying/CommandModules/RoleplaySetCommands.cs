//
//  RoleplaySetCommands.cs
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
using DIGOS.Ambassador.Plugins.Core.Preconditions;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Permissions;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using Discord.Commands;
using JetBrains.Annotations;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Roleplaying.CommandModules
{
    public partial class RoleplayCommands
    {
        /// <summary>
        /// Setter commands for roleplay properties.
        /// </summary>
        [UsedImplicitly]
        [Group("set")]
        public class SetCommands : ModuleBase
        {
            private readonly RoleplayService _roleplays;
            private readonly UserFeedbackService _feedback;

            /// <summary>
            /// Initializes a new instance of the <see cref="SetCommands"/> class.
            /// </summary>
            /// <param name="roleplays">The roleplay service.</param>
            /// <param name="feedback">The user feedback service.</param>
            public SetCommands
            (
                RoleplayService roleplays,
                UserFeedbackService feedback)
            {
                _roleplays = roleplays;
                _feedback = feedback;
            }

            /// <summary>
            /// Sets the name of the named roleplay.
            /// </summary>
            /// <param name="newRoleplayName">The roleplay's new name.</param>
            /// <param name="roleplay">The roleplay.</param>
            [UsedImplicitly]
            [Command("name")]
            [Summary("Sets the new name of the named roleplay.")]
            [RequireContext(ContextType.Guild)]
            public async Task SetRoleplayNameAsync
            (
                string newRoleplayName,
                [RequireEntityOwnerOrPermission(typeof(EditRoleplay), PermissionTarget.Other)]
                Roleplay roleplay
            )
            {
                var result = await _roleplays.SetRoleplayNameAsync(this.Context.Guild, roleplay, newRoleplayName);
                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                var getDedicatedChannelResult = await _roleplays.GetDedicatedRoleplayChannelAsync
                (
                    this.Context.Guild,
                    roleplay
                );

                if (getDedicatedChannelResult.IsSuccess)
                {
                    var dedicatedChannel = getDedicatedChannelResult.Entity;

                    await dedicatedChannel.ModifyAsync(p => p.Name = $"{roleplay.Name}-rp");
                }

                await _feedback.SendConfirmationAsync(this.Context, "Roleplay name set.");
            }

            /// <summary>
            /// Sets the summary of the named roleplay.
            /// </summary>
            /// <param name="newRoleplaySummary">The roleplay's new summary.</param>
            /// <param name="roleplay">The roleplay.</param>
            [UsedImplicitly]
            [Command("summary")]
            [Summary("Sets the summary of the named roleplay.")]
            [RequireContext(ContextType.Guild)]
            public async Task SetRoleplaySummaryAsync
            (
                string newRoleplaySummary,
                [RequireEntityOwnerOrPermission(typeof(EditRoleplay), PermissionTarget.Other)]
                Roleplay roleplay
            )
            {
                var result = await _roleplays.SetRoleplaySummaryAsync(roleplay, newRoleplaySummary);
                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Roleplay summary set.");
            }

            /// <summary>
            /// Sets a value indicating whether or not the named roleplay is NSFW. This restricts which channels it
            /// can be made active in.
            /// </summary>
            /// <param name="isNSFW">true if the roleplay is NSFW; otherwise, false.</param>
            /// <param name="roleplay">The roleplay.</param>
            [UsedImplicitly]
            [Command("nsfw")]
            [Summary("Sets a value indicating whether or not the named roleplay is NSFW. This restricts which channels it can be made active in.")]
            [RequireContext(ContextType.Guild)]
            public async Task SetRoleplayIsNSFW
            (
                bool isNSFW,
                [RequireEntityOwnerOrPermission(typeof(EditRoleplay), PermissionTarget.Other)]
                Roleplay roleplay
            )
            {
                var result = await _roleplays.SetRoleplayIsNSFWAsync(roleplay, isNSFW);
                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, $"Roleplay set to {(isNSFW ? "NSFW" : "SFW")}");
            }

            /// <summary>
            /// Sets a value indicating whether or not the named roleplay is private. This restricts replays to participants.
            /// </summary>
            /// <param name="isPrivate">true if the roleplay is private; otherwise, false.</param>
            /// <param name="roleplay">The roleplay.</param>
            [UsedImplicitly]
            [Command("private")]
            [Summary("Sets a value indicating whether or not the named roleplay is private. This restricts replays to participants.")]
            [RequireContext(ContextType.Guild)]
            public Task SetRoleplayIsPrivate
            (
                bool isPrivate,
                [RequireEntityOwnerOrPermission(typeof(EditRoleplay), PermissionTarget.Other)]
                Roleplay roleplay
            )
                => SetRoleplayIsPublic(!isPrivate, roleplay);

            /// <summary>
            /// Sets a value indicating whether or not the named roleplay is publíc. This restricts replays to participants.
            /// </summary>
            /// <param name="isPublic">true if the roleplay is public; otherwise, false.</param>
            /// <param name="roleplay">The roleplay.</param>
            [UsedImplicitly]
            [Command("public")]
            [Summary("Sets a value indicating whether or not the named roleplay is public. This restricts replays to participants.")]
            [RequireContext(ContextType.Guild)]
            public async Task SetRoleplayIsPublic
            (
                bool isPublic,
                [RequireEntityOwnerOrPermission(typeof(EditRoleplay), PermissionTarget.Other)]
                Roleplay roleplay
            )
            {
                var result = await _roleplays.SetRoleplayIsPublicAsync(roleplay, isPublic);
                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                var getDedicatedChannelResult = await _roleplays.GetDedicatedRoleplayChannelAsync
                (
                    this.Context.Guild,
                    roleplay
                );

                if (getDedicatedChannelResult.IsSuccess)
                {
                    var dedicatedChannel = getDedicatedChannelResult.Entity;
                    var everyoneRole = this.Context.Guild.EveryoneRole;

                    await _roleplays.SetDedicatedChannelVisibilityForRoleAsync
                    (
                        dedicatedChannel,
                        everyoneRole,
                        isPublic
                    );
                }

                await _feedback.SendConfirmationAsync(this.Context, $"Roleplay set to {(isPublic ? "public" : "private")}");
            }
        }
    }
}
