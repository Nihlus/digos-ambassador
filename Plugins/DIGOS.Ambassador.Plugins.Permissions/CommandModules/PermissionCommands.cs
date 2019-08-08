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
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Discord.TypeReaders;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using DIGOS.Ambassador.Plugins.Permissions.Model.Permissions;
using DIGOS.Ambassador.Plugins.Permissions.Permissions;
using DIGOS.Ambassador.Plugins.Permissions.Permissions.Preconditions;
using DIGOS.Ambassador.Plugins.Permissions.Services.Permissions;
using Discord;
using Discord.Commands;
using Humanizer;
using JetBrains.Annotations;
using static Discord.Commands.ContextType;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Permissions.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Permissions.CommandModules
{
    /// <summary>
    /// Permission-related commands for granting, revoking and checking user permissions.
    /// </summary>
    [UsedImplicitly]
    [Group("permission")]
    [Summary("Permission-related commands for granting, revoking and checking user permissions.")]
    public class PermissionCommands : ModuleBase<SocketCommandContext>
    {
        private readonly UserFeedbackService _feedback;
        private readonly InteractivityService _interactivity;
        private readonly PermissionService _permissions;

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionCommands"/> class.
        /// </summary>
        /// <param name="database">A database context from the context pool.</param>
        /// <param name="feedback">The user feedback service.</param>
        /// <param name="permissions">The permission service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        public PermissionCommands
        (
            PermissionsDatabaseContext database,
            UserFeedbackService feedback,
            PermissionService permissions,
            InteractivityService interactivity
        )
        {
            _feedback = feedback;
            _permissions = permissions;
            _interactivity = interactivity;
        }

        /// <summary>
        /// Lists all available permissions.
        /// </summary>
        [UsedImplicitly]
        [Command("list")]
        [Summary("Lists all available permissions.")]
        public async Task ListPermissionsAsync()
        {
            var enumValues = (Permission[])Enum.GetValues(typeof(Permission));

            var appearance = PaginatedAppearanceOptions.Default;
            appearance.HelpText =
                "These are the available bot-specific permissions. Scroll through the pages by using the reactions.";

            var paginatedEmbed = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                this.Context.User,
                enumValues,
                p => p.ToString().Humanize().Transform(To.TitleCase),
                p => p.Humanize(),
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
        [NotNull]
        [UsedImplicitly]
        [Command("list-granted")]
        [Summary("Lists all permissions that have been granted to the invoking user.")]
        [RequireContext(Guild)]
        public Task ListGrantedPermissionsAsync() => ListGrantedPermissionsAsync(this.Context.User);

        /// <summary>
        /// Lists all permissions that have been granted to target user.
        /// </summary>
        /// <param name="discordUser">The Discord user.</param>
        [UsedImplicitly]
        [Command("list-granted")]
        [Summary("Lists all permissions that have been granted to target user.")]
        [RequireContext(Guild)]
        public async Task ListGrantedPermissionsAsync([NotNull] IUser discordUser)
        {
            var localPermissions = _permissions.GetLocalUserPermissions
            (
                discordUser,
                this.Context.Guild
            );

            var appearance = PaginatedAppearanceOptions.Default;
            appearance.Author = discordUser;
            appearance.HelpText =
                "These are the permissions granted to the given user. Scroll through the pages by using the reactions.";

            var paginatedEmbed = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                this.Context.User,
                localPermissions,
                p => p.Permission.Humanize(),
                p => $"*Allowed targets: {p.Target}*",
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
        [UsedImplicitly]
        [Group("grant")]
        public class GrantCommands : ModuleBase<SocketCommandContext>
        {
            private readonly UserFeedbackService _feedback;
            private readonly PermissionService _permissions;

            /// <summary>
            /// Initializes a new instance of the <see cref="GrantCommands"/> class.
            /// </summary>
            /// <param name="feedback">The user feedback service.</param>
            /// <param name="permissions">The permission service.</param>
            public GrantCommands
            (
                UserFeedbackService feedback,
                PermissionService permissions)
            {
                _feedback = feedback;
                _permissions = permissions;
            }

            /// <summary>
            /// Grant the targeted user the given permission.
            /// </summary>
            /// <param name="discordUser">The Discord user.</param>
            /// <param name="grantedPermission">The permission that is to be granted.</param>
            /// <param name="grantedTarget">The target that the permission should be valid for.</param>
            [UsedImplicitly]
            [Command]
            [Summary("Grant the targeted user the given permission.")]
            [RequirePermission(Permission.ManagePermissions, PermissionTarget.Other)]
            public async Task Default
            (
                [NotNull] IUser discordUser,
                [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Permission>))]
                Permission grantedPermission,
                [OverrideTypeReader(typeof(HumanizerEnumTypeReader<PermissionTarget>))]
                PermissionTarget grantedTarget = PermissionTarget.Self
            )
            {
                var newPermission = new LocalPermission
                {
                    Permission = grantedPermission,
                    Target = grantedTarget,
                    ServerDiscordID = (long)this.Context.Guild.Id
                };

                await _permissions.GrantLocalPermissionAsync(this.Context.Guild, discordUser, newPermission);

                await _feedback.SendConfirmationAsync(this.Context, $"{grantedPermission.ToString().Humanize().Transform(To.TitleCase)} granted to {discordUser.Mention}.");
            }
        }

        /// <summary>
        /// Commands for revoking permissions from users.
        /// </summary>
        [UsedImplicitly]
        [Group("revoke")]
        public class RevokeCommands : ModuleBase<SocketCommandContext>
        {
            private readonly UserFeedbackService _feedback;
            private readonly PermissionService _permissions;

            /// <summary>
            /// Initializes a new instance of the <see cref="RevokeCommands"/> class.
            /// </summary>
            /// <param name="feedback">The user feedback service.</param>
            /// <param name="permissions">The permission service.</param>
            public RevokeCommands
            (
                UserFeedbackService feedback,
                PermissionService permissions)
            {
                _feedback = feedback;
                _permissions = permissions;
            }

            /// <summary>
            /// Revoke the given permission from the targeted user.
            /// </summary>
            /// <param name="discordUser">The Discord user.</param>
            /// <param name="revokedPermission">The permission that is to be revoked.</param>
            [UsedImplicitly]
            [Command]
            [Summary("Revoke the given permission from the targeted user.")]
            [RequirePermission(Permission.ManagePermissions, PermissionTarget.Other)]
            public async Task Default
            (
                [NotNull] IUser discordUser,
                [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Permission>))]
                Permission revokedPermission
            )
            {
                await _permissions.RevokeLocalPermissionAsync(this.Context.Guild, discordUser, revokedPermission);

                await _feedback.SendConfirmationAsync(this.Context, $"${revokedPermission.ToString().Humanize().Transform(To.TitleCase)} revoked from {discordUser.Mention}.");
            }

            /// <summary>
            /// Revoke the given target permission from the targeted user.
            /// </summary>
            /// <param name="discordUser">The Discord user.</param>
            /// <param name="permission">The permission to revoke the target from.</param>
            /// <param name="revokedTarget">The permission target to revoke.</param>
            [UsedImplicitly]
            [Command("target")]
            [Summary("Revoke the given target permission from the targeted user.")]
            [RequirePermission(Permission.ManagePermissions, PermissionTarget.Other)]
            public async Task RevokeTargetAsync
            (
                [NotNull] IUser discordUser,
                [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Permission>))]
                Permission permission,
                [OverrideTypeReader(typeof(HumanizerEnumTypeReader<PermissionTarget>))]
                PermissionTarget revokedTarget
            )
            {
                await _permissions.RevokeLocalPermissionTargetAsync(this.Context.Guild, discordUser, permission, revokedTarget);

                await _feedback.SendConfirmationAsync
                (
                    this.Context,
                    $"{permission.ToString().Humanize().Transform(To.TitleCase)} ({revokedTarget.ToString().Humanize().Transform(To.TitleCase)}) revoked from {discordUser.Mention}."
                );
            }
        }
    }
}
