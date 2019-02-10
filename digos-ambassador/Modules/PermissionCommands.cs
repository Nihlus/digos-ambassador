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
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Permissions;
using DIGOS.Ambassador.Permissions.Preconditions;
using DIGOS.Ambassador.Services;

using Discord;
using Discord.Commands;

using Humanizer;
using JetBrains.Annotations;
using static Discord.Commands.ContextType;
using static Discord.Commands.RunMode;
using PermissionTarget = DIGOS.Ambassador.Permissions.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Modules
{
    /// <summary>
    /// Permission-related commands for granting, revoking and checking user permissions.
    /// </summary>
    [UsedImplicitly]
    [Group("permission")]
    [Summary("Permission-related commands for granting, revoking and checking user permissions.")]
    public class PermissionCommands : ModuleBase<SocketCommandContext>
    {
        [ProvidesContext]
        private readonly GlobalInfoContext Database;
        private readonly UserFeedbackService Feedback;

        private readonly PermissionService Permissions;

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionCommands"/> class.
        /// </summary>
        /// <param name="database">A database context from the context pool.</param>
        /// <param name="feedback">The user feedback service.</param>
        /// <param name="permissions">The permission service.</param>
        public PermissionCommands(GlobalInfoContext database, UserFeedbackService feedback, PermissionService permissions)
        {
            this.Database = database;
            this.Feedback = feedback;
            this.Permissions = permissions;
        }

        /// <summary>
        /// Lists all available permissions.
        /// </summary>
        [UsedImplicitly]
        [Command("list", RunMode = Async)]
        [Summary("Lists all available permissions.")]
        public async Task ListPermissionsAsync()
        {
            var enumValues = (Permission[])Enum.GetValues(typeof(Permission));

            await this.Context.Channel.SendMessageAsync(string.Empty, false, CreateHumanizedPermissionEmbedBase(enumValues).Build());
        }

        /// <summary>
        /// Lists all permissions that have been granted to the invoking user.
        /// </summary>
        [UsedImplicitly]
        [Command("list-granted", RunMode = Async)]
        [Summary("Lists all permissions that have been granted to the invoking user.")]
        [RequireContext(Guild)]
        public async Task ListGrantedPermissionsAsync()
        {
            var embed = CreateHumanizedPermissionEmbedBase(this.Permissions.GetLocalUserPermissions(this.Database, this.Context.User, this.Context.Guild));
            embed.WithAuthor(this.Context.Message.Author);

            await this.Feedback.SendEmbedAsync(this.Context.Channel, embed.Build());
        }

        /// <summary>
        /// Lists all permissions that have been granted to target user.
        /// </summary>
        /// <param name="discordUser">The Discord user.</param>
        [UsedImplicitly]
        [Command("list-granted", RunMode = Async)]
        [Summary("Lists all permissions that have been granted to target user.")]
        [RequireContext(Guild)]
        public async Task ListGrantedPermissionsAsync([NotNull] IUser discordUser)
        {
            var embed = CreateHumanizedPermissionEmbedBase(this.Permissions.GetLocalUserPermissions(this.Database, discordUser, this.Context.Guild));
            embed.WithAuthor(discordUser);

            await this.Feedback.SendEmbedAsync(this.Context.Channel, embed.Build());
        }

        [NotNull]
        private EmbedBuilder CreateHumanizedPermissionEmbedBase([NotNull] IEnumerable<Permission> permissions)
        {
            var eb = this.Feedback.CreateEmbedBase();
            var humanizedPermissions = new List<(string Name, string Description)>();
            foreach (var permission in permissions)
            {
                humanizedPermissions.Add
                (
                    (
                        permission.ToString().Humanize().Transform(To.TitleCase),
                        permission.Humanize()
                    )
                );
            }

            humanizedPermissions = humanizedPermissions.OrderBy(s => s).ToList();
            foreach (var permission in humanizedPermissions)
            {
                eb.AddField(permission.Name, permission.Description);
            }

            return eb;
        }

        [NotNull]
        private EmbedBuilder CreateHumanizedPermissionEmbedBase([NotNull] [ItemNotNull] IEnumerable<LocalPermission> userPermissions)
        {
            var eb = this.Feedback.CreateEmbedBase();
            var humanizedPermissions = new List<(string Name, string Description, string Target)>();
            foreach (var userPermission in userPermissions)
            {
                var permission = userPermission.Permission;
                humanizedPermissions.Add
                (
                    (
                        permission.ToString().Humanize().Transform(To.TitleCase),
                        permission.Humanize(),
                        userPermission.Target.Humanize()
                    )
                );
            }

            humanizedPermissions = humanizedPermissions.OrderBy(s => s).ToList();
            foreach (var permission in humanizedPermissions)
            {
                eb.AddField(permission.Name, permission.Description);
                eb.AddField("Allowed targets", permission.Target, true);
            }

            return eb;
        }

        /// <summary>
        /// Commands for granting users permissions.
        /// </summary>
        [UsedImplicitly]
        [Group("grant")]
        public class GrantCommands : ModuleBase<SocketCommandContext>
        {
            [ProvidesContext]
            private readonly GlobalInfoContext Database;
            private readonly UserFeedbackService Feedback;

            private readonly PermissionService Permissions;

            /// <summary>
            /// Initializes a new instance of the <see cref="GrantCommands"/> class.
            /// </summary>
            /// <param name="database">A database context from the context pool.</param>
            /// <param name="feedback">The user feedback service.</param>
            /// <param name="permissions">The permission service.</param>
            public GrantCommands(GlobalInfoContext database, UserFeedbackService feedback, PermissionService permissions)
            {
                this.Database = database;
                this.Feedback = feedback;
                this.Permissions = permissions;
            }

            /// <summary>
            /// Grant the targeted user the given permission.
            /// </summary>
            /// <param name="discordUser">The Discord user.</param>
            /// <param name="grantedPermission">The permission that is to be granted.</param>
            /// <param name="grantedTarget">The target that the permission should be valid for.</param>
            [UsedImplicitly]
            [Command(RunMode = Async)]
            [Summary("Grant the targeted user the given permission.")]
            [RequirePermission(Permission.ManagePermissions, PermissionTarget.Other)]
            public async Task Default([NotNull] IUser discordUser, Permission grantedPermission, PermissionTarget grantedTarget = PermissionTarget.Self)
            {
                var newPermission = new LocalPermission
                {
                    Permission = grantedPermission,
                    Target = grantedTarget,
                    ServerDiscordID = (long)this.Context.Guild.Id
                };

                await this.Permissions.GrantLocalPermissionAsync(this.Database, this.Context.Guild, discordUser, newPermission);

                await this.Feedback.SendConfirmationAsync(this.Context, $"{grantedPermission.ToString().Humanize().Transform(To.TitleCase)} granted to {discordUser.Mention}.");
            }
        }

        /// <summary>
        /// Commands for revoking permissions from users.
        /// </summary>
        [UsedImplicitly]
        [Group("revoke")]
        public class RevokeCommands : ModuleBase<SocketCommandContext>
        {
            [ProvidesContext]
            private readonly GlobalInfoContext Database;
            private readonly UserFeedbackService Feedback;
            private readonly PermissionService Permissions;

            /// <summary>
            /// Initializes a new instance of the <see cref="RevokeCommands"/> class.
            /// </summary>
            /// <param name="database">A database context from the context pool.</param>
            /// <param name="feedback">The user feedback service.</param>
            /// <param name="permissions">The permission service.</param>
            public RevokeCommands(GlobalInfoContext database, UserFeedbackService feedback, PermissionService permissions)
            {
                this.Database = database;
                this.Feedback = feedback;
                this.Permissions = permissions;
            }

            /// <summary>
            /// Revoke the given permission from the targeted user.
            /// </summary>
            /// <param name="discordUser">The Discord user.</param>
            /// <param name="revokedPermission">The permission that is to be revoked.</param>
            [UsedImplicitly]
            [Command(RunMode = Async)]
            [Summary("Revoke the given permission from the targeted user.")]
            [RequirePermission(Permission.ManagePermissions, PermissionTarget.Other)]
            public async Task Default([NotNull] IUser discordUser, Permission revokedPermission)
            {
                await this.Permissions.RevokeLocalPermissionAsync(this.Database, this.Context.Guild, discordUser, revokedPermission);

                await this.Feedback.SendConfirmationAsync(this.Context, $"${revokedPermission.ToString().Humanize().Transform(To.TitleCase)} revoked from {discordUser.Mention}.");
            }

            /// <summary>
            /// Revoke the given target permission from the targeted user.
            /// </summary>
            /// <param name="discordUser">The Discord user.</param>
            /// <param name="permission">The permission to revoke the target from.</param>
            /// <param name="revokedTarget">The permission target to revoke.</param>
            [UsedImplicitly]
            [Command("target", RunMode = Async)]
            [Summary("Revoke the given target permission from the targeted user.")]
            [RequirePermission(Permission.ManagePermissions, PermissionTarget.Other)]
            public async Task RevokeTargetAsync([NotNull] IUser discordUser, Permission permission, PermissionTarget revokedTarget)
            {
                await this.Permissions.RevokeLocalPermissionTargetAsync(this.Database, this.Context.Guild, discordUser, permission, revokedTarget);

                await this.Feedback.SendConfirmationAsync
                (
                    this.Context,
                    $"{permission.ToString().Humanize().Transform(To.TitleCase)} ({revokedTarget.ToString().Humanize().Transform(To.TitleCase)}) revoked from {discordUser.Mention}."
                );
            }
        }
    }
}
