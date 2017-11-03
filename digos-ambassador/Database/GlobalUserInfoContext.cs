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
using System.Linq;
using System.Threading.Tasks;
using Discord;
using DIGOS.Ambassador.Database.UserInfo;
using Microsoft.EntityFrameworkCore;

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
		/// Determines whether or not a Discord user is stored in the database.
		/// </summary>
		/// <param name="discordUser">The Discord user.</param>
		/// <returns><value>true</value> if the user is stored; otherwise, <value>false</value>.</returns>
		public bool IsUserKnown(IUser discordUser)
		{
			return this.Users.Any(u => u.DiscordID == discordUser.Id);
		}

		/// <summary>
		/// Gets an existing set of information about a Discord user, or registers them with the database if one is not found.
		/// </summary>
		/// <param name="discordUser">The Discord user.</param>
		/// <returns>Stored information about the user.</returns>
		public async Task<User> GetOrRegisterUserAsync(IUser discordUser)
		{
			if (!IsUserKnown(discordUser))
			{
				return await AddUserAsync(discordUser);
			}

			return GetUser(discordUser);
		}

		/// <summary>
		/// Gets a stored user from the database that matches the given Discord user.
		/// </summary>
		/// <param name="discordUser">The Discord user.</param>
		/// <returns>Stored information about the user.</returns>
		public User GetUser(IUser discordUser)
		{
			return this.Users.First(u => u.DiscordID == discordUser.Id);
		}

		/// <summary>
		/// Adds a Discord user to the database.
		/// </summary>
		/// <param name="discordUser">The Discord user.</param>
		/// <returns>The freshly created information about the user.</returns>
		/// <exception cref="ArgumentException">Thrown if the user already exists in the database.</exception>
		public async Task<User> AddUserAsync(IUser discordUser)
		{
			if (IsUserKnown(discordUser))
			{
				throw new ArgumentException($"A user with the ID {discordUser.Id} has already been added to the database.", nameof(discordUser));
			}

			var newUser = new User
			{
				DiscordID = discordUser.Id,
				Class = UserClass.Other,
				Bio = string.Empty,
				Timezone = null
			};

			this.Users.Add(newUser);

			await SaveChangesAsync();

			return newUser;
		}

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite("Data Source=global_userinfo.db");
		}
	}
}
