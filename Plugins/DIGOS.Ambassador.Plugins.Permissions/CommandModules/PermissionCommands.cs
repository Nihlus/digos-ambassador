//
//  PermissionCommands.cs
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

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using DIGOS.Ambassador.Plugins.Permissions.Extensions;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using DIGOS.Ambassador.Plugins.Permissions.Services;
using Humanizer;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

namespace DIGOS.Ambassador.Plugins.Permissions.CommandModules
{
    /// <summary>
    /// Permission-related commands for granting, revoking and checking user permissions.
    /// </summary>
    [Group("permission")]
    [Description("Permission-related commands for granting, revoking and checking user permissions.")]
    public class PermissionCommands : CommandGroup
    {
        private readonly UserFeedbackService _feedback;
        private readonly InteractivityService _interactivity;
        private readonly PermissionService _permissions;
        private readonly PermissionRegistryService _permissionRegistry;

        private readonly ICommandContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionCommands"/> class.
        /// </summary>
        /// <param name="database">A database context from the context pool.</param>
        /// <param name="feedback">The user feedback service.</param>
        /// <param name="permissions">The permission service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="permissionRegistry">The permission registry service.</param>
        /// <param name="context">The command context.</param>
        public PermissionCommands
        (
            PermissionsDatabaseContext database,
            UserFeedbackService feedback,
            PermissionService permissions,
            InteractivityService interactivity,
            PermissionRegistryService permissionRegistry,
            ICommandContext context
        )
        {
            _feedback = feedback;
            _permissions = permissions;
            _interactivity = interactivity;
            _permissionRegistry = permissionRegistry;
            _context = context;
        }

        /// <summary>
        /// Lists all available permissions.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Command("list")]
        [Description("Lists all available permissions.")]
        public async Task<IResult> ListPermissionsAsync()
        {
            var availablePermissions = _permissionRegistry.RegisteredPermissions
                .OrderBy(p => p.GetType().Name)
                .ThenBy(p => p.FriendlyName)
                .ToList();

            var appearance = PaginatedAppearanceOptions.Default with
            {
                HelpText = "These are the available bot-specific permissions. " +
                           "Scroll through the pages by using the reactions."
            };

            var pages = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                availablePermissions,
                p => p.FormatTitle(),
                p => p.Description,
                "No permissions available. This is most likely an error."
            );

            return await _interactivity.SendInteractiveMessageAsync
            (
                _context.ChannelID,
                (channelID, messageID) => new PaginatedMessage
                (
                    channelID,
                    messageID,
                    _context.User.ID,
                    pages,
                    appearance
                ),
                this.CancellationToken
            );
        }

        /// <summary>
        /// Lists all permissions that have been granted to target user.
        /// </summary>
        /// <param name="discordUser">The Discord user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Command("list-granted")]
        [Description("Lists all permissions that have been granted to target user.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<IResult> ListGrantedPermissionsAsync(IUser discordUser)
        {
            var userPermissions = await _permissions.GetApplicableUserPermissionsAsync
            (
                _context.GuildID.Value,
                discordUser.ID
            );

            var permissions = _permissionRegistry.RegisteredPermissions
                .Where(r => userPermissions.Any(u => u.Permission == r.UniqueIdentifier))
                .ToDictionary(p => p.UniqueIdentifier);

            var permissionInfos = new List<(string Title, string Description)>();
            foreach (var permissionGroup in userPermissions.GroupBy(p => p.Permission))
            {
                var permission = permissions[permissionGroup.Key];
                var titleBuilder = new StringBuilder();
                titleBuilder.Append(permission.FriendlyName);
                titleBuilder.Append(" (");

                var grants = permissionGroup.Select
                (
                    up =>
                        $"{(up.IsGranted ? ":white_check_mark:" : ":no_entry_sign: ")} {up.Target.Humanize()}"
                );

                titleBuilder.Append(grants.Humanize(",").Transform(To.SentenceCase));
                titleBuilder.Append(")");

                permissionInfos.Add((titleBuilder.ToString(), permission.Description));
            }

            var appearance = PaginatedAppearanceOptions.Default with
            {
                HelpText = "These are the permissions granted to the given user. " +
                           "Scroll through the pages by using the reactions."
            };

            var pages = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                permissionInfos,
                p => p.Title,
                p => p.Description,
                "No permissions set."
            );

            return await _interactivity.SendInteractiveMessageAsync
            (
                _context.ChannelID,
                (channelID, messageID) => new PaginatedMessage
                (
                    channelID,
                    messageID,
                    _context.User.ID,
                    pages,
                    appearance
                ),
                this.CancellationToken
            );
        }

        /// <summary>
        /// Grant the targeted user the given permission.
        /// </summary>
        /// <param name="discordUser">The Discord user.</param>
        /// <param name="permissionName">The permission that is to be granted.</param>
        /// <param name="grantedTarget">The target that the permission should be valid for.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Command("grant")]
        [Description("Grant the targeted user the given permission.")]
        [RequirePermission(typeof(GrantPermission), PermissionTarget.Other)]
        [RequireContext(ChannelContext.Guild)]
        public async Task<IResult> GrantAsync
        (
            IUser discordUser,
            string permissionName,
            PermissionTarget grantedTarget = PermissionTarget.Self
        )
        {
            var getPermissionResult = _permissionRegistry.GetPermission(permissionName);
            if (!getPermissionResult.IsSuccess)
            {
                return getPermissionResult;
            }

            var permission = getPermissionResult.Entity;
            var grantPermissionResult = await _permissions.GrantPermissionAsync
            (
                _context.GuildID.Value,
                discordUser.ID,
                permission,
                grantedTarget,
                this.CancellationToken
            );

            if (!grantPermissionResult.IsSuccess)
            {
                return grantPermissionResult;
            }

            return Result<string>.FromSuccess
            (
                $"{permission.FriendlyName} granted to <@{discordUser.ID}>."
            );
        }

        /// <summary>
        /// Grant the targeted role the given permission.
        /// </summary>
        /// <param name="discordRole">The Discord role.</param>
        /// <param name="permissionName">The permission that is to be granted.</param>
        /// <param name="grantedTarget">The target that the permission should be valid for.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Command("grant-to-role")]
        [Description("Grant the targeted role the given permission.")]
        [RequirePermission(typeof(GrantPermission), PermissionTarget.Other)]
        public async Task<IResult> GrantAsync
        (
            IRole discordRole,
            string permissionName,
            PermissionTarget grantedTarget = PermissionTarget.Self
        )
        {
            var getPermissionResult = _permissionRegistry.GetPermission(permissionName);
            if (!getPermissionResult.IsSuccess)
            {
                return getPermissionResult;
            }

            var permission = getPermissionResult.Entity;
            var grantPermissionResult = await _permissions.GrantPermissionAsync
            (
                discordRole.ID,
                permission,
                grantedTarget
            );

            if (!grantPermissionResult.IsSuccess)
            {
                return grantPermissionResult;
            }

            return Result<string>.FromSuccess
            (
                $"{permission.FriendlyName} granted to <&@{discordRole.ID}>."
            );
        }

        /// <summary>
        /// Revoke the given permission from the targeted user.
        /// </summary>
        /// <param name="discordUser">The Discord user.</param>
        /// <param name="permissionName">The permission that is to be revoked.</param>
        /// <param name="revokedTarget">The target that is to be revoked.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Command("revoke")]
        [Description("Revoke the given permission from the targeted user.")]
        [RequirePermission(typeof(RevokePermission), PermissionTarget.Other)]
        [RequireContext(ChannelContext.Guild)]
        public async Task<IResult> RevokeAsync
        (
            IUser discordUser,
            string permissionName,
            PermissionTarget revokedTarget = PermissionTarget.Self
        )
        {
            var getPermissionResult = _permissionRegistry.GetPermission(permissionName);
            if (!getPermissionResult.IsSuccess)
            {
                return getPermissionResult;
            }

            var permission = getPermissionResult.Entity;
            var revokePermissionResult = await _permissions.RevokePermissionAsync
            (
                _context.GuildID.Value,
                discordUser.ID,
                permission,
                revokedTarget
            );

            if (!revokePermissionResult.IsSuccess)
            {
                return revokePermissionResult;
            }

            return Result<string>.FromSuccess
            (
                $"{permission.FriendlyName} revoked from <@{discordUser.ID}>."
            );
        }

        /// <summary>
        /// Revoke the given permission from the targeted role.
        /// </summary>
        /// <param name="discordRole">The Discord role.</param>
        /// <param name="permissionName">The permission that is to be revoked.</param>
        /// <param name="revokedTarget">The target that is to be revoked.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Command("revoke-from-role")]
        [Description("Revoke the given permission from the targeted role.")]
        [RequirePermission(typeof(RevokePermission), PermissionTarget.Other)]
        public async Task<IResult> RevokeAsync
        (
            IRole discordRole,
            string permissionName,
            PermissionTarget revokedTarget = PermissionTarget.Self
        )
        {
            var getPermissionResult = _permissionRegistry.GetPermission(permissionName);
            if (!getPermissionResult.IsSuccess)
            {
                return getPermissionResult;
            }

            var permission = getPermissionResult.Entity;
            var revokePermissionResult = await _permissions.RevokePermissionAsync
            (
                discordRole.ID,
                permission,
                revokedTarget
            );

            if (!revokePermissionResult.IsSuccess)
            {
                return revokePermissionResult;
            }

            return Result<string>.FromSuccess
            (
                $"{permission.FriendlyName} revoked from <&@{discordRole.ID}>."
            );
        }
    }
}
