//
//  GlobalUserInfoContext.cs
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Database.UserInfo;
using DIGOS.Ambassador.Permissions;
using Microsoft.EntityFrameworkCore;
using PermissionTarget = DIGOS.Ambassador.Permissions.PermissionTarget;

namespace DIGOS.Ambassador.Database
{
	/// <summary>
	/// Database context for global user information.
	/// </summary>
	public class GlobalUserInfoContext : DbContext
	{
		/// <summary>
		/// Gets or sets the database where the user information is stored.
		/// </summary>
		public DbSet<User> Users { get; set; }

		/// <summary>
		/// Gets or sets the database where characters are stored.
		/// </summary>
		public DbSet<Character> Characters { get; set; }

		/// <summary>
		/// Gets or sets the database where kinks are stored.
		/// </summary>
		public DbSet<Kink> Kinks { get; set; }

		/// <summary>
		/// Gets or sets the database where global server-specific settings are stored.
		/// </summary>
		public DbSet<Server> Servers { get; set; }

		/// <summary>
		/// Gets or sets the database where granted user permissions are stored.
		/// </summary>
		public DbSet<UserPermission> UserPermissions { get; set; }

		/// <summary>
		/// Grants the specified user the given permission. If the user already has the permission, it is augmented with
		/// the new scope and target (if they are more permissive than the existing ones).
		/// </summary>
		/// <param name="discordUser">The user.</param>
		/// <param name="grantedPermission">The granted permission.</param>
		/// <returns>A task wrapping the granting of the permission.</returns>
		public async Task GrantPermissionAsync(IUser discordUser, UserPermission grantedPermission)
		{
			var user = await GetOrRegisterUserAsync(discordUser);

			var existingPermission = user.Permissions.FirstOrDefault(p => p.Permission == grantedPermission.Permission);
			if (existingPermission is null)
			{
				user.Permissions.Add(grantedPermission);
			}
			else
			{
				if (existingPermission.Target < grantedPermission.Target)
				{
					existingPermission.Target = grantedPermission.Target;
				}

				if (existingPermission.Scope < grantedPermission.Scope)
				{
					existingPermission.Scope = grantedPermission.Scope;
				}
			}

			await SaveChangesAsync();
		}

		public async Task RevokePermission(IUser discordUser, Permission revokedPermission)
		{

		}

		/// <summary>
		/// Updates the kink database, adding in new entries. Duplicates are not added.
		/// </summary>
		/// <param name="newKinks">The new kinks.</param>
		/// <returns>The number of updated kinks.</returns>
		public async Task<int> UpdateKinksAsync(IEnumerable<Kink> newKinks)
		{
			foreach (var kink in newKinks)
			{
				if (!await this.Kinks.AnyAsync(k => k.FListID == kink.FListID))
				{
					await this.Kinks.AddAsync(kink);
				}
			}

			return await SaveChangesAsync();
		}

		/// <summary>
		/// Determines whether or not a Discord user is stored in the database.
		/// </summary>
		/// <param name="discordUser">The Discord user.</param>
		/// <returns><value>true</value> if the user is stored; otherwise, <value>false</value>.</returns>
		public async Task<bool> IsUserKnown(IUser discordUser)
		{
			return await this.Users.AnyAsync(u => u.DiscordID == discordUser.Id);
		}

		/// <summary>
		/// Gets an existing set of information about a Discord user, or registers them with the database if one is not found.
		/// </summary>
		/// <param name="discordUser">The Discord user.</param>
		/// <returns>Stored information about the user.</returns>
		public async Task<User> GetOrRegisterUserAsync(IUser discordUser)
		{
			if (!await IsUserKnown(discordUser))
			{
				return await AddUserAsync(discordUser);
			}

			return await GetUser(discordUser);
		}

		/// <summary>
		/// Gets a stored user from the database that matches the given Discord user.
		/// </summary>
		/// <param name="discordUser">The Discord user.</param>
		/// <returns>Stored information about the user.</returns>
		public async Task<User> GetUser(IUser discordUser)
		{
			return await this.Users
				.Include(u => u.Characters)
				.Include(u => u.Kinks)
				.Include(u => u.Permissions)
				.FirstAsync(u => u.DiscordID == discordUser.Id);
		}

		/// <summary>
		/// Adds a Discord user to the database.
		/// </summary>
		/// <param name="discordUser">The Discord user.</param>
		/// <returns>The freshly created information about the user.</returns>
		/// <exception cref="ArgumentException">Thrown if the user already exists in the database.</exception>
		public async Task<User> AddUserAsync(IUser discordUser)
		{
			if (await IsUserKnown(discordUser))
			{
				throw new ArgumentException($"A user with the ID {discordUser.Id} has already been added to the database.", nameof(discordUser));
			}

			var defaultPermissions = new List<UserPermission>
			{
				new UserPermission
				{
					Permission = Permission.EditUser,
					Target = PermissionTarget.Self,
					Scope = PermissionScope.Local
				},
				new UserPermission
				{
					Permission = Permission.CreateCharacter,
					Target = PermissionTarget.Self,
					Scope = PermissionScope.Local
				},
				new UserPermission
				{
					Permission = Permission.DeleteCharacter,
					Target = PermissionTarget.Self,
					Scope = PermissionScope.Local
				},
				new UserPermission
				{
					Permission = Permission.ImportCharacter,
					Target = PermissionTarget.Self,
					Scope = PermissionScope.Local
				},
			};

			var newUser = new User
			{
				DiscordID = discordUser.Id,
				Class = UserClass.Other,
				Bio = null,
				Timezone = null,
				Permissions = defaultPermissions
			};

			await this.Users.AddAsync(newUser);

			await SaveChangesAsync();

			return newUser;
		}

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite($"Data Source={Path.Combine("Content", "Databases", "global_userinfo.db")}");
		}
	}
}
