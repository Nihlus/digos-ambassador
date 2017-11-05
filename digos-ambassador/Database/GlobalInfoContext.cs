//
//  GlobalInfoContext.cs
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

using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Database.UserInfo;
using DIGOS.Ambassador.Permissions;

using Discord;
using Discord.WebSocket;

using Microsoft.EntityFrameworkCore;

using PermissionTarget = DIGOS.Ambassador.Permissions.PermissionTarget;

namespace DIGOS.Ambassador.Database
{
	/// <summary>
	/// Database context for global information.
	/// </summary>
	public class GlobalInfoContext : DbContext
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
		/// Gets or sets the database where server-specific settings are stored.
		/// </summary>
		public DbSet<Server> Servers { get; set; }

		/// <summary>
		/// Gets or sets the database where granted local permissions are stored.
		/// </summary>
		public DbSet<LocalPermission> LocalPermissions { get; set; }

		/// <summary>
		/// Gets or sets the database where granted global permissions are stored.
		/// </summary>
		public DbSet<GlobalPermission> GlobalPermissions { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="GlobalInfoContext"/> class.
		/// </summary>
		public GlobalInfoContext()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GlobalInfoContext"/> class.
		/// </summary>
		/// <param name="options">The context options.</param>
		public GlobalInfoContext(DbContextOptions<GlobalInfoContext> options)
			: base(options)
		{
		}

		/// <summary>
		/// Grants the specified user the given permission. If the user already has the permission, it is augmented with
		/// the new scope and target (if they are more permissive than the existing ones).
		/// </summary>
		/// <param name="discordServer">The Discord server the permission was granted on.</param>
		/// <param name="discordUser">The Discord user.</param>
		/// <param name="grantedPermission">The granted permission.</param>
		/// <returns>A task wrapping the granting of the permission.</returns>
		public async Task GrantLocalPermissionAsync(IGuild discordServer, IUser discordUser, LocalPermission grantedPermission)
		{
			var user = await GetOrRegisterUserAsync(discordUser);

			var existingPermission = user.LocalPermissions.FirstOrDefault
			(
				p =>
				p.Permission == grantedPermission.Permission &&
				p.Server.DiscordGuildID == discordServer.Id
			);

			if (existingPermission is null)
			{
				user.LocalPermissions.Add(grantedPermission);
			}
			else
			{
				// Include the new target permissions
				existingPermission.Target |= grantedPermission.Target;
			}

			await SaveChangesAsync();
		}

		/// <summary>
		/// Revokes the given permission from the given Discord user. If the user does not have the permission, no
		/// changes are made.
		/// </summary>
		/// <param name="discordServer">The Discord server the permission was revoked on.</param>
		/// <param name="discordUser">The Discord user.</param>
		/// <param name="revokedPermission">The revoked permission.</param>
		/// <returns>A task wrapping the revoking of the permission.</returns>
		public async Task RevokeLocalPermissionAsync(IGuild discordServer, IUser discordUser, Permission revokedPermission)
		{
			var user = await GetOrRegisterUserAsync(discordUser);

			var existingPermission = user.LocalPermissions.FirstOrDefault
			(
				p =>
				p.Permission == revokedPermission &&
				p.Server.DiscordGuildID == discordServer.Id
			);

			if (existingPermission != null)
			{
				user.LocalPermissions = user.LocalPermissions.Except(new[] { existingPermission }).ToList();
				await SaveChangesAsync();
			}
		}

		/// <summary>
		/// Revokes the given target permission from the given Discord user. If the user does not have the permission, no
		/// changes are made.
		/// </summary>
		/// <param name="discordServer">The Discord server the permission was revoked on.</param>
		/// <param name="discordUser">The Discord user.</param>
		/// <param name="permission">The permission to alter.</param>
		/// <param name="revokedTarget">The revoked permission.</param>
		/// <returns>A task wrapping the revoking of the permission.</returns>
		public async Task RevokeLocalPermissionTargetAsync(IGuild discordServer, IUser discordUser, Permission permission, PermissionTarget revokedTarget)
		{
			var user = await GetOrRegisterUserAsync(discordUser);

			var existingPermission = user.LocalPermissions.FirstOrDefault
			(
				p =>
					p.Permission == permission &&
					p.Server.DiscordGuildID == discordServer.Id
			);

			if (existingPermission != null)
			{
				existingPermission.Target &= ~revokedTarget;
				await SaveChangesAsync();
			}
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
		/// Determines whether or not a Discord server is stored in the database.
		/// </summary>
		/// <param name="discordServer">The Discord server.</param>
		/// <returns><value>true</value> if the server is stored; otherwise, <value>false</value>.</returns>
		public async Task<bool> IsServerKnown(IGuild discordServer)
		{
			return await this.Servers.AnyAsync(u => u.DiscordGuildID == discordServer.Id);
		}

		/// <summary>
		/// Gets an existing set of information about a Discord server, or registers it with the database if one is not found.
		/// </summary>
		/// <param name="discordServer">The Discord server.</param>
		/// <returns>Stored information about the server.</returns>
		public async Task<Server> GetOrRegisterServerAsync(SocketGuild discordServer)
		{
			if (!await IsServerKnown(discordServer))
			{
				return await AddServerAsync(discordServer);
			}

			return await GetServer(discordServer);
		}

		/// <summary>
		/// Gets a stored server from the database that matches the given Discord server.
		/// </summary>
		/// <param name="discordServer">The Discord server.</param>
		/// <returns>Stored information about the server.</returns>
		public async Task<Server> GetServer(IGuild discordServer)
		{
			return await this.Servers.FirstAsync(u => u.DiscordGuildID == discordServer.Id);
		}

		/// <summary>
		/// Adds a Discord server to the database.
		/// </summary>
		/// <param name="discordServer">The Discord server.</param>
		/// <returns>The freshly created information about the server.</returns>
		/// <exception cref="ArgumentException">Thrown if the server already exists in the database.</exception>
		public async Task<Server> AddServerAsync(IGuild discordServer)
		{
			if (await IsServerKnown(discordServer))
			{
				throw new ArgumentException($"A server with the ID {discordServer.Id} has already been added to the database.", nameof(discordServer));
			}

			var server = Server.CreateDefault(discordServer);

			await this.Servers.AddAsync(server);

			await SaveChangesAsync();

			return await this.Servers.FirstAsync(u => u.DiscordGuildID == discordServer.Id);
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
				.Include(u => u.LocalPermissions)
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

			var newUser = new User
			{
				DiscordID = discordUser.Id,
				Class = UserClass.Other,
				Bio = null,
				Timezone = null
			};

			await this.Users.AddAsync(newUser);

			await SaveChangesAsync();

			return await this.Users.FirstAsync(u => u.DiscordID == discordUser.Id);
		}

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				optionsBuilder.UseSqlite($"Data Source={Path.Combine("Content", "Databases", "global.db")}");
			}
		}
	}
}
