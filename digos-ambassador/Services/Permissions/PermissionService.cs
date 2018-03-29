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

using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Permissions;

using Discord;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PermissionTarget = DIGOS.Ambassador.Permissions.PermissionTarget;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Encapsulates business logic for permissions.
	/// </summary>
	public class PermissionService
	{
		/// <summary>
		/// Grants the specified user the given permission. If the user already has the permission, it is augmented with
		/// the new scope and target (if they are more permissive than the existing ones).
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordServer">The Discord server the permission was granted on.</param>
		/// <param name="discordUser">The Discord user.</param>
		/// <param name="grantedPermission">The granted permission.</param>
		/// <returns>A task wrapping the granting of the permission.</returns>
		public async Task GrantLocalPermissionAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IGuild discordServer,
			[NotNull] IUser discordUser,
			[NotNull] LocalPermission grantedPermission
		)
		{
			var existingPermission = await GetLocalUserPermissions(db, discordUser, discordServer).FirstOrDefaultAsync
			(
				p =>
				p.Permission == grantedPermission.Permission &&
				p.ServerDiscordID == discordServer.Id
			);

			if (existingPermission is null)
			{
				await db.LocalPermissions.AddAsync(grantedPermission);
			}
			else
			{
				// Include the new target permissions
				existingPermission.Target |= grantedPermission.Target;
			}

			await db.SaveChangesAsync();
		}

		/// <summary>
		/// Revokes the given permission from the given Discord user. If the user does not have the permission, no
		/// changes are made.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordServer">The Discord server the permission was revoked on.</param>
		/// <param name="discordUser">The Discord user.</param>
		/// <param name="revokedPermission">The revoked permission.</param>
		/// <returns>A task wrapping the revoking of the permission.</returns>
		public async Task RevokeLocalPermissionAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IGuild discordServer,
			[NotNull] IUser discordUser,
			Permission revokedPermission
		)
		{
			var existingPermission = await GetLocalUserPermissions(db, discordUser, discordServer).FirstOrDefaultAsync
			(
				p =>
				p.Permission == revokedPermission &&
				p.ServerDiscordID == discordServer.Id
			);

			if (existingPermission != null)
			{
				db.LocalPermissions.Remove(existingPermission);
				await db.SaveChangesAsync();
			}
		}

		/// <summary>
		/// Revokes the given target permission from the given Discord user. If the user does not have the permission, no
		/// changes are made.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordServer">The Discord server the permission was revoked on.</param>
		/// <param name="discordUser">The Discord user.</param>
		/// <param name="permission">The permission to alter.</param>
		/// <param name="revokedTarget">The revoked permission.</param>
		/// <returns>A task wrapping the revoking of the permission.</returns>
		public async Task RevokeLocalPermissionTargetAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IGuild discordServer,
			[NotNull] IUser discordUser,
			Permission permission,
			PermissionTarget revokedTarget
		)
		{
			var existingPermission = await GetLocalUserPermissions(db, discordUser, discordServer).FirstOrDefaultAsync
			(
				p =>
					p.Permission == permission &&
					p.ServerDiscordID == discordServer.Id
			);

			if (existingPermission != null)
			{
				existingPermission.Target &= ~revokedTarget;
				await db.SaveChangesAsync();
			}
		}

		/// <summary>
		/// Grants the specified user their default permissions on the given server.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="server">The server to grant the permissions on.</param>
		/// <param name="user">The user to grant the permissions to.</param>
		/// <returns>A task wrapping the granting of default permissions.</returns>
		public async Task GrantDefaultPermissionsAsync([NotNull] GlobalInfoContext db, [NotNull] IGuild server, [NotNull] IUser user)
		{
			foreach (var permission in DefaultPermissions.DefaultPermissionSet)
			{
				var scopedPermission = new LocalPermission
				{
					Permission = permission.Permission,
					Target = permission.Target,
					UserDiscordID = user.Id,
					ServerDiscordID = server.Id
				};

				await db.LocalPermissions.AddAsync(scopedPermission);
			}

			await db.SaveChangesAsync();
		}

		/// <summary>
		/// Determines whether or not the user has the given permission.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordServer">The Discord server that the command was executed on.</param>
		/// <param name="discordUser">The user.</param>
		/// <param name="requiredPermission">The permission.</param>
		/// <returns><value>true</value> if the user has the permission; otherwise, <value>false</value>.</returns>
		[Pure]
		public async Task<bool> HasPermissionAsync
		(
			[NotNull] GlobalInfoContext db,
			[CanBeNull] IGuild discordServer,
			[NotNull] IUser discordUser,
			(Permission Permission, PermissionTarget Target) requiredPermission
		)
		{
			if (discordServer is null)
			{
				return DefaultPermissions.DefaultPermissionSet.Any
				(
					dp =>
						dp.Permission == requiredPermission.Permission &&
						dp.Target.HasFlag(requiredPermission.Target)
				);
			}

			// The server owner always has all permissions by default
			if (discordServer.OwnerId == discordUser.Id)
			{
				return true;
			}

			// First, check if the user has the permission on a global level
			var hasGlobalPermission = await GetGlobalUserPermissions(db, discordUser).AnyAsync
			(
				gp =>
					gp.Permission == requiredPermission.Permission &&
					gp.Target.HasFlag(requiredPermission.Target)
			);

			if (hasGlobalPermission)
			{
				return true;
			}

			// Then, check the user's local permissions
			return await GetLocalUserPermissions(db, discordUser, discordServer).AnyAsync
			(
				lp =>
					lp.Permission == requiredPermission.Permission &&
					lp.Target.HasFlag(requiredPermission.Target)
			);
		}

		/// <summary>
		/// Gets the local permissions granted to the given user on the given server.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="contextUser">The user.</param>
		/// <param name="guild">The server.</param>
		/// <returns>The permissions.</returns>
		[NotNull]
		public IQueryable<LocalPermission> GetLocalUserPermissions([NotNull] GlobalInfoContext db, IUser contextUser, IGuild guild)
		{
			return db.LocalPermissions
				.Where
				(
					p =>
						p.ServerDiscordID == guild.Id &&
						p.UserDiscordID == contextUser.Id
				);
		}

		/// <summary>
		/// Gets the global permissions granted to the given user.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="contextUser">The user.</param>
		/// <returns>The permissions.</returns>
		[NotNull]
		public IQueryable<GlobalPermission> GetGlobalUserPermissions([NotNull] GlobalInfoContext db, IUser contextUser)
		{
			return db.GlobalPermissions
				.Where
				(
					p =>
						p.UserDiscordID == contextUser.Id
				);
		}
	}
}
