//
//  PermissionCommands.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
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

using Discord;
using Discord.Commands;

using Humanizer;
using JetBrains.Annotations;
using static DIGOS.Ambassador.Permissions.Permission;
using static DIGOS.Ambassador.Permissions.PermissionTarget;

using PermissionTarget = DIGOS.Ambassador.Permissions.PermissionTarget;

namespace DIGOS.Ambassador.CommandModules
{
	/// <summary>
	/// Permission-related commands.
	/// </summary>
	[UsedImplicitly]
	[Group("permission")]
	public class PermissionCommands : ModuleBase<SocketCommandContext>
	{
		/// <summary>
		/// Lists all available permissions.
		/// </summary>
		/// <returns>A task wrapping the command.</returns>
		[UsedImplicitly]
		[Command("list")]
		[Summary("Lists all available permissions.")]
		public async Task ListPermissionsAsync()
		{
			var enumValues = (Permission[])Enum.GetValues(typeof(Permission));

			await this.Context.Channel.SendMessageAsync(string.Empty, false, CreateHumanizedPermissionEmbed(enumValues));
		}

		/// <summary>
		/// Lists all permissions that have been granted to the invoking user.
		/// </summary>
		/// <returns>A task wrapping the command.</returns>
		[UsedImplicitly]
		[Command("list-granted")]
		[Summary("Lists all permissions that have been granted to the invoking user.")]
		public async Task ListGrantedPermissionsAsync()
		{
			using (var db = new GlobalInfoContext())
			{
				var user = await db.GetOrRegisterUserAsync(this.Context.Message.Author);

				var embed = CreateHumanizedPermissionEmbed(user.LocalPermissions);
				embed.WithAuthor(this.Context.Message.Author);

				await this.Context.Channel.SendMessageAsync(string.Empty, false, embed);
			}
		}

		/// <summary>
		/// Lists all permissions that have been granted to target user.
		/// </summary>
		/// <param name="discordUser">The Discord user.</param>
		/// <returns>A task wrapping the command.</returns>
		[UsedImplicitly]
		[Command("list-granted")]
		[Summary("Lists all permissions that have been granted to target user.")]
		public async Task ListGrantedPermissionsAsync(IUser discordUser)
		{
			using (var db = new GlobalInfoContext())
			{
				var user = await db.GetOrRegisterUserAsync(discordUser);

				var embed = CreateHumanizedPermissionEmbed(user.LocalPermissions);
				embed.WithAuthor(discordUser);

				await this.Context.Channel.SendMessageAsync(string.Empty, false, embed);
			}
		}

		[NotNull]
		private static EmbedBuilder CreateHumanizedPermissionEmbed([NotNull] IEnumerable<Permission> permissions)
		{
			var eb = new EmbedBuilder();
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
		private static EmbedBuilder CreateHumanizedPermissionEmbed([NotNull][ItemNotNull] IEnumerable<LocalPermission> userPermissions)
		{
			var eb = new EmbedBuilder();
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
				eb.AddInlineField("Allowed targets", permission.Target);
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
			/// <summary>
			/// Grant the targeted user the given permission.
			/// </summary>
			/// <param name="discordUser">The Discord user.</param>
			/// <param name="grantedPermission">The permission that is to be granted.</param>
			/// <param name="grantedTarget">The target that the permission should be valid for.</param>
			/// <returns>A task wrapping the command.</returns>
			[UsedImplicitly]
			[Command]
			[Summary("Grant the targeted user the given permission.")]
			[RequirePermission(ManagePermissions, Other)]
			public async Task Default(IUser discordUser, Permission grantedPermission, PermissionTarget grantedTarget = Self)
			{
				using (var db = new GlobalInfoContext())
				{
					var newPermission = new LocalPermission
					{
						Permission = grantedPermission,
						Target = grantedTarget,
						Server = await db.GetOrRegisterServerAsync(this.Context.Guild)
					};

					await db.GrantLocalPermissionAsync(this.Context.Guild, discordUser, newPermission);
				}

				await this.Context.Channel.SendMessageAsync($"{grantedPermission.ToString().Humanize().Transform(To.TitleCase)} granted to {discordUser.Mention}.");
			}
		}

		/// <summary>
		/// Commands for revoking permissions from users.
		/// </summary>
		[UsedImplicitly]
		[Group("revoke")]
		public class RevokeCommands : ModuleBase<SocketCommandContext>
		{
			/// <summary>
			/// Revoke the given permission from the targeted user.
			/// </summary>
			/// <param name="discordUser">The Discord user.</param>
			/// <param name="revokedPermission">The permission that is to be revoked.</param>
			/// <returns>A task wrapping the command.</returns>
			[UsedImplicitly]
			[Command]
			[Summary("Revoke the given permission from the targeted user.")]
			[RequirePermission(ManagePermissions, Other)]
			public async Task Default(IUser discordUser, Permission revokedPermission)
			{
				using (var db = new GlobalInfoContext())
				{
					await db.RevokeLocalPermissionAsync(this.Context.Guild, discordUser, revokedPermission);
				}

				await this.Context.Channel.SendMessageAsync($"${revokedPermission.ToString().Humanize().Transform(To.TitleCase)} revoked from {discordUser.Mention}.");
			}

			/// <summary>
			/// Revoke the given target permission from the targeted user.
			/// </summary>
			/// <param name="discordUser">The Discord user.</param>
			/// <param name="permission">The permission to revoke the target from.</param>
			/// <param name="revokedTarget">The permission target to revoke.</param>
			/// <returns>A task wrapping the command.</returns>
			[UsedImplicitly]
			[Command("target")]
			[Summary("Revoke the given target permission from the targeted user.")]
			[RequirePermission(ManagePermissions, Other)]
			public async Task RevokeTargetAsync(IUser discordUser, Permission permission, PermissionTarget revokedTarget)
			{
				using (var db = new GlobalInfoContext())
				{
					await db.RevokeLocalPermissionTargetAsync(this.Context.Guild, discordUser, permission, revokedTarget);
				}

				await this.Context.Channel.SendMessageAsync
				(
					$"{permission.ToString().Humanize().Transform(To.TitleCase)} ({revokedTarget.ToString().Humanize().Transform(To.TitleCase)}) revoked from {discordUser.Mention}."
				);
			}
		}
	}
}
