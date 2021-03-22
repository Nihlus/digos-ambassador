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
using DIGOS.Ambassador.Plugins.Permissions.Model;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Core;
using Remora.Results;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

namespace DIGOS.Ambassador.Plugins.Permissions.Services
{
    /// <summary>
    /// Encapsulates business logic for permissions.
    /// </summary>
    public sealed class PermissionService
    {
        private readonly PermissionsDatabaseContext _database;
        private readonly IDiscordRestGuildAPI _guildAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionService"/> class.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="guildAPI">The guild API.</param>
        public PermissionService(PermissionsDatabaseContext database, IDiscordRestGuildAPI guildAPI)
        {
            _database = database;
            _guildAPI = guildAPI;
        }

        /// <summary>
        /// Grants the specified user the given permission.
        /// </summary>
        /// <param name="discordServer">The Discord server the permission was granted on.</param>
        /// <param name="discordUser">The Discord user.</param>
        /// <param name="grantedPermission">The granted permission.</param>
        /// <param name="target">The granted target.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> GrantPermissionAsync
        (
            Snowflake discordServer,
            Snowflake discordUser,
            IPermission grantedPermission,
            PermissionTarget target,
            CancellationToken ct = default
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
                    PermissionTarget.Self,
                    ct
                );

                var grantOtherResult = await GrantPermissionAsync
                (
                    discordServer,
                    discordUser,
                    grantedPermission,
                    PermissionTarget.Other,
                    ct
                );

                if (grantSelfResult.IsSuccess || grantOtherResult.IsSuccess)
                {
                    return Result.FromSuccess();
                }

                // Both are false, so we'll just inherit the error from the self grant.
                return grantSelfResult;
            }

            var getPermissionResult = await GetOrCreateUserPermissionAsync
            (
                discordServer,
                discordUser,
                grantedPermission,
                target,
                ct
            );

            if (!getPermissionResult.IsSuccess)
            {
                return Result.FromError(getPermissionResult);
            }

            var permission = getPermissionResult.Entity;
            if (permission.IsGranted)
            {
                return new GenericError("The user already has permission to do that.");
            }

            permission.IsGranted = true;
            await _database.SaveChangesAsync(ct);

            return Result.FromSuccess();
        }

        /// <summary>
        /// Grants the specified role the given permission.
        /// </summary>
        /// <param name="discordRole">The Discord role.</param>
        /// <param name="grantedPermission">The granted permission.</param>
        /// <param name="target">The granted target.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> GrantPermissionAsync
        (
            Snowflake discordRole,
            IPermission grantedPermission,
            PermissionTarget target,
            CancellationToken ct = default
        )
        {
            // Special All target handling
            if (target == PermissionTarget.All)
            {
                var grantSelfResult = await GrantPermissionAsync
                (
                    discordRole,
                    grantedPermission,
                    PermissionTarget.Self,
                    ct
                );

                var grantOtherResult = await GrantPermissionAsync
                (
                    discordRole,
                    grantedPermission,
                    PermissionTarget.Other,
                    ct
                );

                if (grantSelfResult.IsSuccess || grantOtherResult.IsSuccess)
                {
                    return Result.FromSuccess();
                }

                // Both are false, so we'll just inherit the self result.
                return grantSelfResult;
            }

            var getPermissionResult = await GetOrCreateRolePermissionAsync
            (
                discordRole,
                grantedPermission,
                target,
                ct
            );

            if (!getPermissionResult.IsSuccess)
            {
                return Result.FromError(getPermissionResult);
            }

            var permission = getPermissionResult.Entity;
            if (permission.IsGranted)
            {
                return new GenericError("The user already has permission to do that.");
            }

            permission.IsGranted = true;
            await _database.SaveChangesAsync(ct);

            return Result.FromSuccess();
        }

        /// <summary>
        /// Revokes the given permission from the given Discord user. If the user does not have the permission, no
        /// changes are made.
        /// </summary>
        /// <param name="discordServer">The Discord server the permission was revoked on.</param>
        /// <param name="discordUser">The Discord user.</param>
        /// <param name="revokedPermission">The revoked permission.</param>
        /// <param name="target">The revoked target.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> RevokePermissionAsync
        (
            Snowflake discordServer,
            Snowflake discordUser,
            IPermission revokedPermission,
            PermissionTarget target,
            CancellationToken ct = default
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
                    PermissionTarget.Self,
                    ct
                );

                var revokeOtherResult = await RevokePermissionAsync
                (
                    discordServer,
                    discordUser,
                    revokedPermission,
                    PermissionTarget.Other,
                    ct
                );

                if (revokeSelfResult.IsSuccess || revokeOtherResult.IsSuccess)
                {
                    return Result.FromSuccess();
                }

                // Both are false, so we'll just inherit the self result.
                return revokeSelfResult;
            }

            var getPermissionResult = await GetOrCreateUserPermissionAsync
            (
                discordServer,
                discordUser,
                revokedPermission,
                target,
                ct
            );

            if (!getPermissionResult.IsSuccess)
            {
                return Result.FromError(getPermissionResult);
            }

            var permission = getPermissionResult.Entity;
            if (!permission.IsGranted)
            {
                return new GenericError("The user is already prohibited from doing that.");
            }

            permission.IsGranted = false;
            await _database.SaveChangesAsync(ct);

            return Result.FromSuccess();
        }

        /// <summary>
        /// Revokes the given permission from the given Discord role.
        /// </summary>
        /// <param name="discordRole">The Discord role.</param>
        /// <param name="revokedPermission">The revoked permission.</param>
        /// <param name="target">The revoked target.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> RevokePermissionAsync
        (
            Snowflake discordRole,
            IPermission revokedPermission,
            PermissionTarget target,
            CancellationToken ct = default
        )
        {
            // Special All target handling
            if (target == PermissionTarget.All)
            {
                var revokeSelfResult = await RevokePermissionAsync
                (
                    discordRole,
                    revokedPermission,
                    PermissionTarget.Self,
                    ct
                );

                var revokeOtherResult = await RevokePermissionAsync
                (
                    discordRole,
                    revokedPermission,
                    PermissionTarget.Other,
                    ct
                );

                if (revokeSelfResult.IsSuccess || revokeOtherResult.IsSuccess)
                {
                    return Result.FromSuccess();
                }

                // Both are false, so we'll just inherit the self result.
                return revokeSelfResult;
            }

            var getPermissionResult = await GetOrCreateRolePermissionAsync
            (
                discordRole,
                revokedPermission,
                target,
                ct
            );

            if (!getPermissionResult.IsSuccess)
            {
                return Result.FromError(getPermissionResult);
            }

            var permission = getPermissionResult.Entity;
            if (!permission.IsGranted)
            {
                return new GenericError("The role is already prohibited from doing that.");
            }

            permission.IsGranted = false;
            await _database.SaveChangesAsync(ct);

            return Result.FromSuccess();
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
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns><value>true</value> if the user has the permission; otherwise, <value>false</value>.</returns>
        [Pure]
        public async Task<Result> HasPermissionAsync
        (
            Snowflake discordServer,
            Snowflake discordUser,
            IPermission requiredPermission,
            PermissionTarget target,
            CancellationToken ct = default
        )
        {
            var getDiscordServer = await _guildAPI.GetGuildAsync(discordServer, ct: ct);
            if (!getDiscordServer.IsSuccess)
            {
                return Result.FromError(getDiscordServer);
            }

            var guild = getDiscordServer.Entity;

            // The server owner always has all permissions by default
            if (guild.OwnerID == discordUser)
            {
                return Result.FromSuccess();
            }

            // Special handling for the All target
            if (target == PermissionTarget.All)
            {
                var hasSelf = await HasPermissionAsync
                (
                    discordServer,
                    discordUser,
                    requiredPermission,
                    PermissionTarget.Self,
                    ct
                );

                var hasOther = await HasPermissionAsync
                (
                    discordServer,
                    discordUser,
                    requiredPermission,
                    PermissionTarget.Other,
                    ct
                );

                if (hasSelf.IsSuccess && hasOther.IsSuccess)
                {
                    return Result.FromSuccess();
                }

                return new GenericError("Permission denied.");
            }

            var hasPermission = false;

            var getGuildMember = await _guildAPI.GetGuildMemberAsync(discordServer, discordUser, ct);
            if (!getGuildMember.IsSuccess)
            {
                return Result.FromError(getGuildMember);
            }

            var member = getGuildMember.Entity;

            // Check if the user is part of any roles which this permission applies to
            var rolePermissions = await GetApplicableRolePermissionsAsync(member.Roles, ct);
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
            var userPermission = await _database.UserPermissions.ServersideQueryAsync
            (
                q => q
                    .Where(p => p.ServerID == discordServer)
                    .Where(p => p.UserID == discordUser)
                    .Where(p => p.Permission == requiredPermission.UniqueIdentifier)
                    .Where(p => p.Target == target)
                    .SingleOrDefaultAsync(ct)
            );

            if (!(userPermission is null))
            {
                hasPermission = userPermission.IsGranted;
            }

            if (rolePermission is null && userPermission is null)
            {
                // Use the permission's default value
                hasPermission = requiredPermission.IsGrantedByDefaultTo(target);
            }

            return hasPermission
                ? Result.FromSuccess()
                : new GenericError("Permission denied.");
        }

        /// <summary>
        /// Retrieves the user-level permissions applicable to the given user.
        /// </summary>
        /// <param name="discordServer">The server the user is on.</param>
        /// <param name="discordUser">The user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>An object representing the query.</returns>
        public async Task<IEnumerable<UserPermission>> GetApplicableUserPermissionsAsync
        (
            Snowflake discordServer,
            Snowflake discordUser,
            CancellationToken ct = default
        )
        {
            return await _database.UserPermissions.ServersideQueryAsync
            (
                q => q
                    .Where(p => p.ServerID == discordServer)
                    .Where(p => p.UserID == discordUser),
                ct
            );
        }

        /// <summary>
        /// Retrieves the role-level permissions applicable to the given user.
        /// </summary>
        /// <param name="discordUserRoles">The user's roles.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>An object representing the query.</returns>
        public async Task<IEnumerable<RolePermission>> GetApplicableRolePermissionsAsync
        (
            IReadOnlyList<Snowflake> discordUserRoles,
            CancellationToken ct = default
        )
        {
            var userRoles = discordUserRoles.ToList();

            var permissions = await _database.RolePermissions.ServersideQueryAsync
            (
                q => q.Where(p => discordUserRoles.Contains(p.RoleID)),
                ct
            );

            return permissions.OrderBy(p => userRoles.IndexOf(p.RoleID));
        }

        /// <summary>
        /// Gets an existing role permission, or creates a default one if it does not exist.
        /// </summary>
        /// <param name="discordRole">The role.</param>
        /// <param name="permission">The permission.</param>
        /// <param name="target">The target.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have finished.</returns>
        private async Task<Result<RolePermission>> GetOrCreateRolePermissionAsync
        (
            Snowflake discordRole,
            IPermission permission,
            PermissionTarget target,
            CancellationToken ct = default
        )
        {
            if (target == PermissionTarget.All)
            {
                throw new ArgumentException("Invalid permission target.", nameof(target));
            }

            var rolePermissions = await _database.RolePermissions.ServersideQueryAsync
            (
                q => q
                    .Where(p => p.RoleID == discordRole)
                    .Where(p => p.Permission == permission.UniqueIdentifier)
                    .Where(p => p.Target == target),
                ct
            );

            var rolePermission = rolePermissions.SingleOrDefault();

            if (!(rolePermission is null))
            {
                return rolePermission;
            }

            var newPermission = _database.CreateProxy<RolePermission>
            (
                discordRole,
                permission.UniqueIdentifier,
                target
            );

            _database.RolePermissions.Update(newPermission);
            newPermission.IsGranted = permission.IsGrantedByDefaultTo(target);

            await _database.SaveChangesAsync(ct);

            return newPermission;
        }

        /// <summary>
        /// Gets an existing user permission, or creates a default one if it does not exist.
        /// </summary>
        /// <param name="discordGuild">The guild the user is in.</param>
        /// <param name="discordUser">The user.</param>
        /// <param name="permission">The permission.</param>
        /// <param name="target">The target.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have finished.</returns>
        private async Task<Result<UserPermission>> GetOrCreateUserPermissionAsync
        (
            Snowflake discordGuild,
            Snowflake discordUser,
            IPermission permission,
            PermissionTarget target,
            CancellationToken ct = default
        )
        {
            if (target == PermissionTarget.All)
            {
                throw new ArgumentException("Invalid permission target.", nameof(target));
            }

            var userPermissions = await _database.UserPermissions.ServersideQueryAsync
            (
                q => q.Where
                (
                    p =>
                        p.ServerID == discordGuild &&
                        p.UserID == discordUser &&
                        p.Permission == permission.UniqueIdentifier &&
                        p.Target == target
                ),
                ct
            );

            var userPermission = userPermissions.SingleOrDefault();

            if (!(userPermission is null))
            {
                return userPermission;
            }

            var newPermission = _database.CreateProxy<UserPermission>
            (
                discordGuild,
                discordUser,
                permission.UniqueIdentifier,
                target
            );

            _database.UserPermissions.Update(newPermission);
            newPermission.IsGranted = permission.IsGrantedByDefaultTo(target);

            await _database.SaveChangesAsync(ct);

            return newPermission;
        }
    }
}
