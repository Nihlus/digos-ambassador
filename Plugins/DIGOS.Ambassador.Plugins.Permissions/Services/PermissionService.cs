//
//  PermissionService.cs
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
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Core.Services.TransientState;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using Discord;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoreLinq.Extensions;
using Remora.Results;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

namespace DIGOS.Ambassador.Plugins.Permissions.Services
{
    /// <summary>
    /// Encapsulates business logic for permissions.
    /// </summary>
    [PublicAPI]
    public sealed class PermissionService : AbstractTransientStateService
    {
        private readonly PermissionsDatabaseContext _database;

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionService"/> class.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="log">The logging instance.</param>
        public PermissionService(PermissionsDatabaseContext database, ILogger<AbstractTransientStateService> log)
            : base(log)
        {
            _database = database;
        }

        /// <summary>
        /// Grants the specified user the given permission.
        /// </summary>
        /// <param name="discordServer">The Discord server the permission was granted on.</param>
        /// <param name="discordUser">The Discord user.</param>
        /// <param name="grantedPermission">The granted permission.</param>
        /// <param name="target">The granted target.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> GrantPermissionAsync
        (
            IGuild discordServer,
            IUser discordUser,
            IPermission grantedPermission,
            PermissionTarget target
        )
        {
            // Special All target handling
            if (target == PermissionTarget.All)
            {
                var grantSelfResult = await GrantPermissionAsync
                (
                    discordServer,
                    discordUser,
                    grantedPermission,
                    PermissionTarget.Self
                );

                var grantOtherResult = await GrantPermissionAsync
                (
                    discordServer,
                    discordUser,
                    grantedPermission,
                    PermissionTarget.Other
                );

                if (grantSelfResult.IsSuccess || grantOtherResult.IsSuccess)
                {
                    return ModifyEntityResult.FromSuccess();
                }

                // Both are false, so we'll just inherit the error from the self grant.
                return ModifyEntityResult.FromError(grantSelfResult);
            }

            var getPermissionResult = await GetOrCreateUserPermissionAsync
            (
                discordServer,
                discordUser,
                grantedPermission,
                target
            );

            if (!getPermissionResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getPermissionResult);
            }

            var permission = getPermissionResult.Entity;
            if (permission.IsGranted)
            {
                return ModifyEntityResult.FromError("The user already has permission to do that.");
            }

            permission.IsGranted = true;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Grants the specified role the given permission.
        /// </summary>
        /// <param name="discordRole">The Discord role.</param>
        /// <param name="grantedPermission">The granted permission.</param>
        /// <param name="target">The granted target.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> GrantPermissionAsync
        (
            IRole discordRole,
            IPermission grantedPermission,
            PermissionTarget target
        )
        {
            // Special All target handling
            if (target == PermissionTarget.All)
            {
                var grantSelfResult = await GrantPermissionAsync
                (
                    discordRole,
                    grantedPermission,
                    PermissionTarget.Self
                );

                var grantOtherResult = await GrantPermissionAsync
                (
                    discordRole,
                    grantedPermission,
                    PermissionTarget.Other
                );

                if (grantSelfResult.IsSuccess || grantOtherResult.IsSuccess)
                {
                    return ModifyEntityResult.FromSuccess();
                }

                // Both are false, so we'll just inherit the self result.
                return ModifyEntityResult.FromError(grantSelfResult);
            }

            var getPermissionResult = await GetOrCreateRolePermissionAsync
            (
                discordRole,
                grantedPermission,
                target
            );

            if (!getPermissionResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getPermissionResult);
            }

            var permission = getPermissionResult.Entity;
            if (permission.IsGranted)
            {
                return ModifyEntityResult.FromError("The user already has permission to do that.");
            }

            permission.IsGranted = true;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Revokes the given permission from the given Discord user. If the user does not have the permission, no
        /// changes are made.
        /// </summary>
        /// <param name="discordServer">The Discord server the permission was revoked on.</param>
        /// <param name="discordUser">The Discord user.</param>
        /// <param name="revokedPermission">The revoked permission.</param>
        /// <param name="target">The revoked target.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> RevokePermissionAsync
        (
            IGuild discordServer,
            IUser discordUser,
            IPermission revokedPermission,
            PermissionTarget target
        )
        {
            // Special All target handling
            if (target == PermissionTarget.All)
            {
                var revokeSelfResult = await RevokePermissionAsync
                (
                    discordServer,
                    discordUser,
                    revokedPermission,
                    PermissionTarget.Self
                );

                var revokeOtherResult = await RevokePermissionAsync
                (
                    discordServer,
                    discordUser,
                    revokedPermission,
                    PermissionTarget.Other
                );

                if (revokeSelfResult.IsSuccess || revokeOtherResult.IsSuccess)
                {
                    return ModifyEntityResult.FromSuccess();
                }

                // Both are false, so we'll just inherit the self result.
                return ModifyEntityResult.FromError(revokeSelfResult);
            }

            var getPermissionResult = await GetOrCreateUserPermissionAsync
            (
                discordServer,
                discordUser,
                revokedPermission,
                target
            );

            if (!getPermissionResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getPermissionResult);
            }

            var permission = getPermissionResult.Entity;
            if (!permission.IsGranted)
            {
                return ModifyEntityResult.FromError("The user is already prohibited from doing that.");
            }

            permission.IsGranted = false;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Revokes the given permission from the given Discord role.
        /// </summary>
        /// <param name="discordRole">The Discord role.</param>
        /// <param name="revokedPermission">The revoked permission.</param>
        /// <param name="target">The revoked target.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> RevokePermissionAsync
        (
            IRole discordRole,
            IPermission revokedPermission,
            PermissionTarget target
        )
        {
            // Special All target handling
            if (target == PermissionTarget.All)
            {
                var revokeSelfResult = await RevokePermissionAsync
                (
                    discordRole,
                    revokedPermission,
                    PermissionTarget.Self
                );

                var revokeOtherResult = await RevokePermissionAsync
                (
                    discordRole,
                    revokedPermission,
                    PermissionTarget.Other
                );

                if (revokeSelfResult.IsSuccess || revokeOtherResult.IsSuccess)
                {
                    return ModifyEntityResult.FromSuccess();
                }

                // Both are false, so we'll just inherit the self result.
                return ModifyEntityResult.FromError(revokeSelfResult);
            }

            var getPermissionResult = await GetOrCreateRolePermissionAsync
            (
                discordRole,
                revokedPermission,
                target
            );

            if (!getPermissionResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getPermissionResult);
            }

            var permission = getPermissionResult.Entity;
            if (!permission.IsGranted)
            {
                return ModifyEntityResult.FromError("The role is already prohibited from doing that.");
            }

            permission.IsGranted = false;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Determines whether or not the user has the given permission. The permission hierarchy roughly follows that
        /// of Discord, wherein user-specific permissions override those given implicitly through role memberships.
        ///
        /// That is, if a user is denied a permission based on their roles, but is granted it personally, the permission
        /// will be considered granted. Corollary, a granted role permission can be denied on a personal basis.
        /// </summary>
        /// <param name="discordServer">The Discord server that the command was executed on.</param>
        /// <param name="discordUser">The user.</param>
        /// <param name="requiredPermission">The permission.</param>
        /// <param name="target">The target of the permission.</param>
        /// <returns><value>true</value> if the user has the permission; otherwise, <value>false</value>.</returns>
        [Pure]
        public async Task<DetermineConditionResult> HasPermissionAsync
        (
            IGuild discordServer,
            IGuildUser discordUser,
            IPermission requiredPermission,
            PermissionTarget target
        )
        {
            // The server owner always has all permissions by default
            if (discordServer.OwnerId == discordUser.Id)
            {
                return DetermineConditionResult.FromSuccess();
            }

            // Special handling for the All target
            if (target == PermissionTarget.All)
            {
                var hasSelf = await HasPermissionAsync
                (
                    discordServer,
                    discordUser,
                    requiredPermission,
                    PermissionTarget.Self
                );

                var hasOther = await HasPermissionAsync
                (
                    discordServer,
                    discordUser,
                    requiredPermission,
                    PermissionTarget.Other
                );

                if (hasSelf.IsSuccess && hasOther.IsSuccess)
                {
                    return DetermineConditionResult.FromSuccess();
                }

                return DetermineConditionResult.FromError("Permission denied.");
            }

            var hasPermission = false;

            // Check if the user is part of any roles which this permission applies to
            var rolePermissions = await GetApplicableRolePermissionsAsync(discordUser);
            var rolePermission = rolePermissions.FirstOrDefault
            (
                p =>
                    p.Permission == requiredPermission.UniqueIdentifier &&
                    p.Target == target
            );

            if (!(rolePermission is null))
            {
                hasPermission = rolePermission.IsGranted;
            }

            // Check if the user has the permission applied to themselves
            var userPermissions = await GetApplicableUserPermissionsAsync
            (
                discordServer,
                discordUser,
                q => q.Where
                (
                    p =>
                        p.Permission == requiredPermission.UniqueIdentifier &&
                        p.Target == target
                )
            );

            var userPermission = userPermissions.SingleOrDefault();

            if (!(userPermission is null))
            {
                hasPermission = userPermission.IsGranted;
            }

            if (rolePermission is null && userPermission is null)
            {
                // Use the permission's default value
                hasPermission = requiredPermission.IsGrantedByDefaultTo(target);
            }

            if (hasPermission)
            {
                return DetermineConditionResult.FromSuccess();
            }

            return DetermineConditionResult.FromError("Permission denied.");
        }

        /// <summary>
        /// Retrieves the user-level permissions applicable to the given user.
        /// </summary>
        /// <param name="discordServer">The server the user is on.</param>
        /// <param name="discordUser">The user.</param>
        /// <param name="query">Additional query parameters.</param>
        /// <returns>An object representing the query.</returns>
        public async Task<IEnumerable<UserPermission>> GetApplicableUserPermissionsAsync
        (
            IGuild discordServer,
            IUser discordUser,
            Func<IQueryable<UserPermission>, IQueryable<UserPermission>>? query = null
        )
        {
            query ??= q => q;

            return await _database.UserPermissions.UnifiedQueryAsync
            (
                q => query(q
                    .Where(p => p.ServerID == (long)discordServer.Id)
                    .Where(p => p.UserID == (long)discordUser.Id))
            );
        }

        /// <summary>
        /// Retrieves the role-level permissions applicable to the given user.
        /// </summary>
        /// <param name="discordUser">The user.</param>
        /// <returns>An object representing the query.</returns>
        public async Task<IEnumerable<RolePermission>> GetApplicableRolePermissionsAsync
        (
            IGuildUser discordUser
        )
        {
            var userRoles = discordUser.RoleIds.Select(r => (long)r).ToList();

            var permissions = await _database.RolePermissions.UnifiedQueryAsync
            (
                q => q.Where(p => userRoles.Contains(p.RoleID))
            );

            return permissions.OrderBy(p => userRoles.IndexOf(p.RoleID));
        }

        /// <summary>
        /// Gets an existing role permission, or creates a default one if it does not exist.
        /// </summary>
        /// <param name="discordRole">The role.</param>
        /// <param name="permission">The permission.</param>
        /// <param name="target">The target.</param>
        /// <returns>A retrieval result which may or may not have finished.</returns>
        private async Task<RetrieveEntityResult<RolePermission>> GetOrCreateRolePermissionAsync
        (
            IRole discordRole,
            IPermission permission,
            PermissionTarget target
        )
        {
            if (target == PermissionTarget.All)
            {
                throw new ArgumentException("Invalid permission target.", nameof(target));
            }

            var rolePermissions = await _database.RolePermissions.UnifiedQueryAsync
            (
                q => q.Where
                (
                    p =>
                        p.RoleID == (long)discordRole.Id &&
                        p.Permission == permission.UniqueIdentifier &&
                        p.Target == target
                )
            );

            var rolePermission = rolePermissions.SingleOrDefault();

            if (!(rolePermission is null))
            {
                return rolePermission;
            }

            var newPermission = _database.CreateProxy<RolePermission>
            (
                (long)discordRole.Id,
                permission.UniqueIdentifier,
                target
            );

            _database.RolePermissions.Update(newPermission);
            newPermission.IsGranted = permission.IsGrantedByDefaultTo(target);

            return newPermission;
        }

        /// <summary>
        /// Gets an existing user permission, or creates a default one if it does not exist.
        /// </summary>
        /// <param name="discordGuild">The guild the user is in.</param>
        /// <param name="discordUser">The user.</param>
        /// <param name="permission">The permission.</param>
        /// <param name="target">The target.</param>
        /// <returns>A retrieval result which may or may not have finished.</returns>
        private async Task<RetrieveEntityResult<UserPermission>> GetOrCreateUserPermissionAsync
        (
            IGuild discordGuild,
            IUser discordUser,
            IPermission permission,
            PermissionTarget target
        )
        {
            if (target == PermissionTarget.All)
            {
                throw new ArgumentException("Invalid permission target.", nameof(target));
            }

            var userPermissions = await _database.UserPermissions.UnifiedQueryAsync
            (
                q => q.Where
                (
                    p =>
                        p.ServerID == (long)discordGuild.Id &&
                        p.UserID == (long)discordUser.Id &&
                        p.Permission == permission.UniqueIdentifier &&
                        p.Target == target
                )
            );

            var userPermission = userPermissions.SingleOrDefault();

            if (!(userPermission is null))
            {
                return userPermission;
            }

            var newPermission = _database.CreateProxy<UserPermission>
            (
                (long)discordGuild.Id,
                (long)discordUser.Id,
                permission.UniqueIdentifier,
                target
            );

            _database.UserPermissions.Update(newPermission);
            newPermission.IsGranted = permission.IsGrantedByDefaultTo(target);

            return newPermission;
        }

        /// <inheritdoc/>
        protected override void OnSavingChanges()
        {
            _database.SaveChanges();
        }

        /// <inheritdoc/>
        protected override async ValueTask OnSavingChangesAsync(CancellationToken ct = default)
        {
            await _database.SaveChangesAsync(ct);
        }
    }
}
