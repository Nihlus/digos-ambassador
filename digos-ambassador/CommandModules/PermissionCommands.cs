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
using Discord;
using Discord.Commands;
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Permissions;
using DIGOS.Ambassador.Permissions.Preconditions;
using Humanizer;
using static DIGOS.Ambassador.Permissions.Permission;
using static DIGOS.Ambassador.Permissions.PermissionScope;
using static DIGOS.Ambassador.Permissions.PermissionTarget;
using PermissionTarget = DIGOS.Ambassador.Permissions.PermissionTarget;

namespace DIGOS.Ambassador.CommandModules
{
	/// <summary>
	/// Permission-related commands.
	/// </summary>
	[Group("permission")]
	public class PermissionCommands : ModuleBase<SocketCommandContext>
	{
		/// <summary>
		/// Lists all available permissions.
		/// </summary>
		/// <returns>A task wrapping the command.</returns>
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
		[Command("list-granted")]
		[Summary("Lists all permissions that have been granted to the invoking user.")]
		public async Task ListGrantedPermissionsAsync()
		{
			using (var db = new GlobalUserInfoContext())
			{
				var user = await db.GetOrRegisterUserAsync(this.Context.Message.Author);

				var embed = CreateHumanizedPermissionEmbed(user.Permissions);
				embed.WithAuthor(this.Context.Message.Author);

				await this.Context.Channel.SendMessageAsync(string.Empty, false, embed);
			}
		}

		/// <summary>
		/// Lists all permissions that have been granted to target user.
		/// </summary>
		/// <returns>A task wrapping the command.</returns>
		[Command("list-granted")]
		[Summary("Lists all permissions that have been granted to target user.")]
		public async Task ListGrantedPermissionsAsync(IUser discordUser)
		{
			using (var db = new GlobalUserInfoContext())
			{
				var user = await db.GetOrRegisterUserAsync(discordUser);

				var embed = CreateHumanizedPermissionEmbed(user.Permissions);
				embed.WithAuthor(discordUser);

				await this.Context.Channel.SendMessageAsync(string.Empty, false, embed);
			}
		}

		private static EmbedBuilder CreateHumanizedPermissionEmbed(IEnumerable<Permission> permissions)
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

		private static EmbedBuilder CreateHumanizedPermissionEmbed(IEnumerable<UserPermission> userPermissions)
		{
			var eb = new EmbedBuilder();
			var humanizedPermissions = new List<(string Name, string Description, string Target, string Scope)>();
			foreach (var userPermission in userPermissions)
			{
				var permission = userPermission.Permission;
				humanizedPermissions.Add
				(
					(
						permission.ToString().Humanize().Transform(To.TitleCase),
						permission.Humanize(),
						userPermission.Target.Humanize(),
						userPermission.Scope.Humanize()
					)
				);
			}

			humanizedPermissions = humanizedPermissions.OrderBy(s => s).ToList();
			foreach (var permission in humanizedPermissions)
			{
				eb.AddField(permission.Name, permission.Description);
				eb.AddInlineField("Allowed targets", permission.Target);
				eb.AddInlineField("Scope", permission.Scope);
			}

			return eb;
		}

		private static async Task<bool> CheckIsBotOwnerAsync(SocketCommandContext context)
		{
			var ownerId = (await context.Client.GetApplicationInfoAsync()).Owner.Id;
			if (context.Message.Author.Id != ownerId)
			{
				return false;
			}

			return true;
		}

		[Group("grant")]
		public class GrantCommands : ModuleBase<SocketCommandContext>
		{
			/// <summary>
			/// Grant the targeted user the given permission.
			/// </summary>
			/// <returns>A task wrapping the command.</returns>
			[Command]
			[Summary("Grant the targeted user the given permission.")]
			[RequirePermission(ManagePermissions, Other)]
			public async Task Default(IUser discordUser, Permission grantedPermission, PermissionTarget grantedTarget = Self, PermissionScope grantedScope = Local)
			{
				// Check that only the bot owner can grant global permissions
				if (grantedScope == Global)
				{
					if (!await CheckIsBotOwnerAsync(this.Context))
					{
						await this.Context.Channel.SendMessageAsync("Only the bot owner can grant global permissions.");
						return;
					}
				}

				var newPermission = new UserPermission
				{
					Permission = grantedPermission,
					Target = grantedTarget,
					Scope = grantedScope
				};

				using (var db = new GlobalUserInfoContext())
				{
					await db.GrantPermissionAsync(discordUser, newPermission);
				}

				await this.Context.Channel.SendMessageAsync("Permission granted.");
			}

			/// <summary>
			/// Grant the targeted user the given target permission.
			/// </summary>
			/// <returns>A task wrapping the command.</returns>
			[Command("target")]
			[Summary("Grant the targeted user the given target permission.")]
			[RequirePermission(ManagePermissions, Other)]
			public async Task GrantTargetAsync(IUser discordUser, Permission grantedPermission, PermissionTarget grantedTarget)
			{

			}

			/// <summary>
			/// Grant the targeted user the given scope permission.
			/// </summary>
			/// <returns>A task wrapping the command.</returns>
			[Command("scope")]
			[Summary("Grant the targeted user the given scope permission.")]
			[RequirePermission(ManagePermissions, Other)]
			public async Task GrantScopeAsync(IUser discordUser, Permission grantedPermission, PermissionScope grantedScope)
			{
				// Check that only the bot owner can grant global permissions
				if (grantedScope == Global)
				{
					if (!await CheckIsBotOwnerAsync(this.Context))
					{
						await this.Context.Channel.SendMessageAsync("Only the bot owner can grant global permissions.");
						return;
					}
				}
			}
		}

		[Group("revoke")]
		public class RevokeCommands : ModuleBase<SocketCommandContext>
		{
			/// <summary>
			/// Revoke the given permission from the targeted user.
			/// </summary>
			/// <returns>A task wrapping the command.</returns>
			[Command]
			[Summary("Revoke the given permission from the targeted user.")]
			[RequirePermission(ManagePermissions, Other)]
			public async Task Default(IUser discordUser, Permission revokedPermission)
			{

			}

			/// <summary>
			/// Revoke the given target permission from the targeted user.
			/// </summary>
			/// <returns>A task wrapping the command.</returns>
			[Command("target")]
			[Summary("Revoke the given target permission from the targeted user.")]
			[RequirePermission(ManagePermissions, Other)]
			public async Task RevokeTargetAsync(IUser discordUser, PermissionTarget revokedTarget)
			{

			}

			/// <summary>
			/// Revoke the given scope permission from the targeted user.
			/// </summary>
			/// <returns>A task wrapping the command.</returns>
			[Command("scope")]
			[Summary("Revoke the given scope permission from the targeted user.")]
			[RequirePermission(ManagePermissions, Other)]
			public async Task RevokeScopeAsync(IUser discordUser, PermissionScope revokedScope)
			{
				// Check that only the bot owner can grant global permissions
				if (revokedScope == Global)
				{
					if (!await CheckIsBotOwnerAsync(this.Context))
					{
						await this.Context.Channel.SendMessageAsync("Only the bot owner can grant global permissions.");
						return;
					}
				}
			}
		}
	}
}
