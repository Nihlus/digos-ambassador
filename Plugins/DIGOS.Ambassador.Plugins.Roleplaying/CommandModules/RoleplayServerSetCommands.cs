//
//  RoleplayServerSetCommands.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using DIGOS.Ambassador.Plugins.Roleplaying.Permissions;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Results;

using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Roleplaying.CommandModules;

public partial class RoleplayCommands
{
    /// <summary>
    /// Server info setter commands.
    /// </summary>
    public partial class RoleplayServerCommands
    {
        /// <summary>
        /// Sets the channel category to use for dedicated roleplays.
        /// </summary>
        /// <param name="category">The category to use.</param>
        [UsedImplicitly]
        [Command("set-roleplay-category")]
        [Description("Sets the channel category to use for dedicated roleplays.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(EditRoleplayServerSettings), PermissionTarget.Self)]
        public async Task<Result<FeedbackMessage>> SetDedicatedRoleplayChannelCategory(IChannel category)
        {
            if (!_context.TryGetGuildID(out var guildID))
            {
                throw new InvalidOperationException();
            }

            var result = await _serverSettings.SetDedicatedChannelCategoryAsync
            (
                guildID,
                category.ID
            );

            return !result.IsSuccess
                ? Result<FeedbackMessage>.FromError(result)
                : new FeedbackMessage("Dedicated channel category set.", _feedback.Theme.Secondary);
        }

        /// <summary>
        /// Sets the channel to use for archival of roleplays.
        /// </summary>
        /// <param name="channel">The channel to use.</param>
        [UsedImplicitly]
        [Command("set-archive-channel")]
        [Description("Sets the channel to use for archival of roleplays.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(EditRoleplayServerSettings), PermissionTarget.Self)]
        public async Task<Result<FeedbackMessage>> SetArchiveChannelAsync(IChannel channel)
        {
            if (!_context.TryGetGuildID(out var guildID))
            {
                throw new InvalidOperationException();
            }

            var result = await _serverSettings.SetArchiveChannelAsync
            (
                guildID,
                channel.ID
            );

            return !result.IsSuccess
                ? Result<FeedbackMessage>.FromError(result)
                : new FeedbackMessage("Archive channel set.", _feedback.Theme.Secondary);
        }

        /// <summary>
        /// Sets the role to use as a default @everyone role in dynamic roleplays.
        /// </summary>
        /// <param name="role">The role to use.</param>
        [UsedImplicitly]
        [Command("set-default-user-role")]
        [Description("Sets the role to use as a default @everyone role in dynamic roleplays.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(EditRoleplayServerSettings), PermissionTarget.Self)]
        public async Task<Result<FeedbackMessage>> SetDefaultUserRole(IRole role)
        {
            if (!_context.TryGetGuildID(out var guildID))
            {
                throw new InvalidOperationException();
            }

            var result = await _serverSettings.SetDefaultUserRoleAsync
            (
                guildID,
                role.ID
            );

            return !result.IsSuccess
                ? Result<FeedbackMessage>.FromError(result)
                : new FeedbackMessage("Default user role set.", _feedback.Theme.Secondary);
        }
    }
}
