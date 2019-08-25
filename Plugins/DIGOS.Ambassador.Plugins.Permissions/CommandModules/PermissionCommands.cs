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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Discord.TypeReaders;
using DIGOS.Ambassador.Plugins.Permissions.Extensions;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using DIGOS.Ambassador.Plugins.Permissions.Preconditions;
using DIGOS.Ambassador.Plugins.Permissions.Services;
using Discord;
using Discord.Commands;
using Humanizer;
using JetBrains.Annotations;
using static Discord.Commands.ContextType;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Permissions.CommandModules
{
    /// <summary>
    /// Permission-related commands for granting, revoking and checking user permissions.
    /// </summary>
    [PublicAPI]
    [Group("permission")]
    [Summary("Permission-related commands for granting, revoking and checking user permissions.")]
    public class PermissionCommands : ModuleBase
    {
        [NotNull]
        private readonly UserFeedbackService _feedback;

        [NotNull]
        private readonly InteractivityService _interactivity;

        [NotNull]
        private readonly PermissionService _permissions;

        [NotNull]
        private readonly PermissionRegistryService _permissionRegistry;

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionCommands"/> class.
        /// </summary>
        /// <param name="database">A database context from the context pool.</param>
        /// <param name="feedback">The user feedback service.</param>
        /// <param name="permissions">The permission service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="permissionRegistry">The permission registry service.</param>
        public PermissionCommands
        (
            [NotNull] PermissionsDatabaseContext database,
            [NotNull] UserFeedbackService feedback,
            [NotNull] PermissionService permissions,
            [NotNull] InteractivityService interactivity,
            [NotNull] PermissionRegistryService permissionRegistry
        )
        {
            _feedback = feedback;
            _permissions = permissions;
            _interactivity = interactivity;
            _permissionRegistry = permissionRegistry;
        }

        /// <summary>
        /// Lists all available permissions.
        /// </summary>
        [Command("list")]
        [Summary("Lists all available permissions.")]
        public async Task ListPermissionsAsync()
        {
            var availablePermissions = _permissionRegistry.RegisteredPermissions
                .OrderBy(p => p.GetType().Name)
                .ThenBy(p => p.FriendlyName);

            var appearance = PaginatedAppearanceOptions.Default;
            appearance.HelpText =
                "These are the available bot-specific permissions. Scroll through the pages by using the reactions.";

            var paginatedEmbed = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                this.Context.User,
                availablePermissions,
                p => p.FormatTitle(),
                p => p.Description,
                "No permissions available. This is most likely an error.",
                appearance
            );

            await _interactivity.SendPrivateInteractiveMessageAndDeleteAsync
            (
                this.Context,
                _feedback,
                paginatedEmbed,
                TimeSpan.FromMinutes(5)
            );
        }

        /// <summary>
        /// Lists all permissions that have been granted to the invoking user.
        /// </summary>
        [Command("list-granted")]
        [Summary("Lists all permissions that have been granted to the invoking user.")]
        [RequireContext(Guild)]
        public Task ListGrantedPermissionsAsync() => ListGrantedPermissionsAsync(this.Context.User);

        /// <summary>
        /// Lists all permissions that have been granted to target user.
        /// </summary>
        /// <param name="discordUser">The Discord user.</param>
        [Command("list-granted")]
        [Summary("Lists all permissions that have been granted to target user.")]
        [RequireContext(Guild)]
        public async Task ListGrantedPermissionsAsync([NotNull] IUser discordUser)
        {
            var userPermissions = _permissions.GetApplicableUserPermissions(this.Context.Guild, discordUser);

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

            var appearance = PaginatedAppearanceOptions.Default;
            appearance.Author = discordUser;
            appearance.HelpText =
                "These are the permissions granted to the given user. Scroll through the pages by using the reactions.";

            var paginatedEmbed = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                this.Context.User,
                permissionInfos,
                p => p.Title,
                p => p.Description,
                "No permissions set.",
                appearance
            );

            await _interactivity.SendPrivateInteractiveMessageAndDeleteAsync
            (
                this.Context,
                _feedback,
                paginatedEmbed,
                TimeSpan.FromMinutes(5)
            );
        }

        /// <summary>
        /// Commands for granting users permissions.
        /// </summary>
        [PublicAPI]
        [Group("grant")]
        public class GrantCommands : ModuleBase
        {
            [NotNull]
            private readonly UserFeedbackService _feedback;

            [NotNull]
            private readonly PermissionService _permissions;

            [NotNull]
            private readonly PermissionRegistryService _permissionRegistry;

            /// <summary>
            /// Initializes a new instance of the <see cref="GrantCommands"/> class.
            /// </summary>
            /// <param name="feedback">The user feedback service.</param>
            /// <param name="permissions">The permission service.</param>
            /// <param name="permissionRegistry">The permission registry service.</param>
            public GrantCommands
            (
                [NotNull] UserFeedbackService feedback,
                [NotNull] PermissionService permissions,
                [NotNull] PermissionRegistryService permissionRegistry
            )
            {
                _feedback = feedback;
                _permissions = permissions;
                _permissionRegistry = permissionRegistry;
            }

            /// <summary>
            /// Grant yourself the given permission.
            /// </summary>
            /// <param name="permissionName">The permission that is to be revoked.</param>
            /// <param name="revokedTarget">The target that is to be revoked.</param>
            [Command]
            [Summary("Grant yourself the given permission.")]
            [RequirePermission(typeof(RevokePermission), PermissionTarget.Self)]
            public async Task Default
            (
                string permissionName,
                [OverrideTypeReader(typeof(HumanizerEnumTypeReader<PermissionTarget>))]
                PermissionTarget revokedTarget = PermissionTarget.Self
            )
                => await Default(this.Context.User, permissionName, revokedTarget);

            /// <summary>
            /// Grant the targeted user the given permission.
            /// </summary>
            /// <param name="discordUser">The Discord user.</param>
            /// <param name="permissionName">The permission that is to be granted.</param>
            /// <param name="grantedTarget">The target that the permission should be valid for.</param>
            [Command]
            [Summary("Grant the targeted user the given permission.")]
            [RequirePermission(typeof(GrantPermission), PermissionTarget.Other)]
            public async Task Default
            (
                [NotNull] IUser discordUser,
                [NotNull] string permissionName,
                [OverrideTypeReader(typeof(HumanizerEnumTypeReader<PermissionTarget>))]
                PermissionTarget grantedTarget = PermissionTarget.Self
            )
            {
                var getPermissionResult = _permissionRegistry.GetPermission(permissionName);
                if (!getPermissionResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getPermissionResult.ErrorReason);
                    return;
                }

                var permission = getPermissionResult.Entity;
                var grantPermissionResult = await _permissions.GrantPermissionAsync
                (
                    this.Context.Guild,
                    discordUser,
                    permission,
                    grantedTarget
                );

                if (!grantPermissionResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, grantPermissionResult.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync
                (
                    this.Context,
                    $"{permission.FriendlyName} granted to {discordUser.Mention}."
                );
            }
        }

        /// <summary>
        /// Commands for revoking permissions from users.
        /// </summary>
        [PublicAPI]
        [Group("revoke")]
        public class RevokeCommands : ModuleBase
        {
            [NotNull]
            private readonly UserFeedbackService _feedback;

            [NotNull]
            private readonly PermissionService _permissions;

            [NotNull]
            private readonly PermissionRegistryService _permissionRegistry;

            /// <summary>
            /// Initializes a new instance of the <see cref="RevokeCommands"/> class.
            /// </summary>
            /// <param name="feedback">The user feedback service.</param>
            /// <param name="permissions">The permission service.</param>
            /// <param name="permissionRegistry">The permission registry service.</param>
            public RevokeCommands
            (
                [NotNull] UserFeedbackService feedback,
                [NotNull] PermissionService permissions,
                [NotNull] PermissionRegistryService permissionRegistry
            )
            {
                _feedback = feedback;
                _permissions = permissions;
                _permissionRegistry = permissionRegistry;
            }

            /// <summary>
            /// Revoke the given permission from yourself.
            /// </summary>
            /// <param name="permissionName">The permission that is to be revoked.</param>
            /// <param name="revokedTarget">The target that is to be revoked.</param>
            [Command]
            [Summary("Revoke the given permission from yourself.")]
            [RequirePermission(typeof(RevokePermission), PermissionTarget.Self)]
            public async Task Default
            (
                string permissionName,
                [OverrideTypeReader(typeof(HumanizerEnumTypeReader<PermissionTarget>))]
                PermissionTarget revokedTarget = PermissionTarget.Self
            )
                => await Default(this.Context.User, permissionName, revokedTarget);

            /// <summary>
            /// Revoke the given permission from the targeted user.
            /// </summary>
            /// <param name="discordUser">The Discord user.</param>
            /// <param name="permissionName">The permission that is to be revoked.</param>
            /// <param name="revokedTarget">The target that is to be revoked.</param>
            [Command]
            [Summary("Revoke the given permission from the targeted user.")]
            [RequirePermission(typeof(RevokePermission), PermissionTarget.Other)]
            public async Task Default
            (
                [NotNull] IUser discordUser,
                string permissionName,
                [OverrideTypeReader(typeof(HumanizerEnumTypeReader<PermissionTarget>))]
                PermissionTarget revokedTarget = PermissionTarget.Self
            )
            {
                var getPermissionResult = _permissionRegistry.GetPermission(permissionName);
                if (!getPermissionResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getPermissionResult.ErrorReason);
                    return;
                }

                var permission = getPermissionResult.Entity;
                var revokePermissionResult = await _permissions.RevokePermissionAsync
                (
                    this.Context.Guild,
                    discordUser,
                    permission,
                    revokedTarget
                );

                if (!revokePermissionResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, revokePermissionResult.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync
                (
                    this.Context,
                    $"{permission.FriendlyName} revoked from {discordUser.Mention}."
                );
            }
        }
    }
}
