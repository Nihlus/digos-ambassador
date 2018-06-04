//
//  GlobalInfoContext.cs
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database.Appearances;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Dossiers;
using DIGOS.Ambassador.Database.Kinks;
using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Database.Roleplaying;
using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Database.Users;

using Discord;

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Image = DIGOS.Ambassador.Database.Data.Image;

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
		public DbSet<User> Users
		{
			get;

			[UsedImplicitly]
			set;
		}

		/// <summary>
		/// Gets or sets the database where characters are stored.
		/// </summary>
		public DbSet<Character> Characters
		{
			get;

			[UsedImplicitly]
			set;
		}

		/// <summary>
		/// Gets or sets the database where kinks are stored.
		/// </summary>
		public DbSet<Kink> Kinks
		{
			get;

			[UsedImplicitly]
			set;
		}

		/// <summary>
		/// Gets or sets the database where server-specific settings are stored.
		/// </summary>
		public DbSet<Server> Servers
		{
			get;

			[UsedImplicitly]
			set;
		}

		/// <summary>
		/// Gets or sets the database where granted local permissions are stored.
		/// </summary>
		public DbSet<LocalPermission> LocalPermissions
		{
			get;

			[UsedImplicitly]
			set;
		}

		/// <summary>
		/// Gets or sets the database where granted global permissions are stored.
		/// </summary>
		public DbSet<GlobalPermission> GlobalPermissions
		{
			get;

			[UsedImplicitly]
			set;
		}

		/// <summary>
		/// Gets or sets the database where roleplays are stored.
		/// </summary>
		public DbSet<Roleplay> Roleplays
		{
			get;

			[UsedImplicitly]
			set;
		}

		/// <summary>
		/// Gets or sets the database where dossier metadata is stored.
		/// </summary>
		public DbSet<Dossier> Dossiers
		{
			get;

			[UsedImplicitly]
			set;
		}

		/// <summary>
		/// Gets or sets the database where images are stored.
		/// </summary>
		public DbSet<Image> Images
		{
			get;

			[UsedImplicitly]
			set;
		}

		/// <summary>
		/// Gets or sets the database where transformation species are stored.
		/// </summary>
		public DbSet<Species> Species
		{
			get;

			[UsedImplicitly]
			set;
		}

		/// <summary>
		/// Gets or sets the database where transformations are stored.
		/// </summary>
		public DbSet<Transformation> Transformations
		{
			get;

			[UsedImplicitly]
			set;
		}

		/// <summary>
		/// Gets or sets the database where global transformation protections are stored.
		/// </summary>
		public DbSet<GlobalUserProtection> GlobalUserProtections
		{
			get;

			[UsedImplicitly]
			set;
		}

		/// <summary>
		/// Gets or sets the database where server-specific transformation protections are stored.
		/// </summary>
		public DbSet<ServerUserProtection> ServerUserProtections
		{
			get;

			[UsedImplicitly]
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GlobalInfoContext"/> class.
		/// </summary>
		/// <param name="options">The context options.</param>
		public GlobalInfoContext([NotNull] DbContextOptions options)
			: base(options)
		{
		}

		/// <summary>
		/// Updates the kink database, adding in new entries. Duplicates are not added.
		/// </summary>
		/// <param name="newKinks">The new kinks.</param>
		/// <returns>The number of updated kinks.</returns>
		public async Task<int> UpdateKinksAsync([NotNull] [ItemNotNull] IEnumerable<Kink> newKinks)
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
		[Pure]
		public async Task<bool> IsServerKnownAsync([NotNull] IGuild discordServer)
		{
			return await this.Servers.AnyAsync(u => u.DiscordID == (long)discordServer.Id);
		}

		/// <summary>
		/// Gets an existing set of information about a Discord server, or registers it with the database if one is not found.
		/// </summary>
		/// <param name="discordServer">The Discord server.</param>
		/// <returns>Stored information about the server.</returns>
		[ItemNotNull]
		public async Task<Server> GetOrRegisterServerAsync([NotNull] IGuild discordServer)
		{
			if (!await IsServerKnownAsync(discordServer))
			{
				return await AddServerAsync(discordServer);
			}

			return await GetServerAsync(discordServer);
		}

		/// <summary>
		/// Gets a stored server from the database that matches the given Discord server.
		/// </summary>
		/// <param name="discordServer">The Discord server.</param>
		/// <returns>Stored information about the server.</returns>
		[Pure]
		[ItemNotNull]
		public async Task<Server> GetServerAsync([NotNull] IGuild discordServer)
		{
			return await this.Servers.FirstAsync(u => u.DiscordID == (long)discordServer.Id);
		}

		/// <summary>
		/// Adds a Discord server to the database.
		/// </summary>
		/// <param name="discordServer">The Discord server.</param>
		/// <returns>The freshly created information about the server.</returns>
		/// <exception cref="ArgumentException">Thrown if the server already exists in the database.</exception>
		[ItemNotNull]
		public async Task<Server> AddServerAsync([NotNull] IGuild discordServer)
		{
			if (await IsServerKnownAsync(discordServer))
			{
				throw new ArgumentException($"A server with the ID {discordServer.Id} has already been added to the database.", nameof(discordServer));
			}

			var server = Server.CreateDefault(discordServer);

			await this.Servers.AddAsync(server);

			await SaveChangesAsync();

			return await GetServerAsync(discordServer);
		}

		/// <summary>
		/// Determines whether or not a Discord user is stored in the database.
		/// </summary>
		/// <param name="discordUser">The Discord user.</param>
		/// <returns><value>true</value> if the user is stored; otherwise, <value>false</value>.</returns>
		[Pure]
		public async Task<bool> IsUserKnownAsync([NotNull] IUser discordUser)
		{
			return await this.Users.AnyAsync(u => u.DiscordID == (long)discordUser.Id);
		}

		/// <summary>
		/// Gets an existing set of information about a Discord user, or registers them with the database if one is not found.
		/// </summary>
		/// <param name="discordUser">The Discord user.</param>
		/// <returns>Stored information about the user.</returns>
		[ItemNotNull]
		public async Task<User> GetOrRegisterUserAsync([NotNull] IUser discordUser)
		{
			if (!await IsUserKnownAsync(discordUser))
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
		[Pure]
		[ItemNotNull]
		public async Task<User> GetUser([NotNull] IUser discordUser)
		{
			return await this.Users
				.Include(u => u.DefaultCharacter)
				.Include(u => u.Characters)
				.Include(u => u.Kinks).ThenInclude(k => k.Kink)
				.OrderBy(u => u.ID)
				.FirstAsync
				(
					u =>
						u.DiscordID == (long)discordUser.Id
				);
		}

		/// <summary>
		/// Adds a Discord user to the database.
		/// </summary>
		/// <param name="discordUser">The Discord user.</param>
		/// <returns>The freshly created information about the user.</returns>
		/// <exception cref="ArgumentException">Thrown if the user already exists in the database.</exception>
		[ItemNotNull]
		public async Task<User> AddUserAsync([NotNull] IUser discordUser)
		{
			if (await IsUserKnownAsync(discordUser))
			{
				throw new ArgumentException($"A user with the ID {discordUser.Id} has already been added to the database.", nameof(discordUser));
			}

			var newUser = new User
			{
				DiscordID = (long)discordUser.Id,
				Class = UserClass.Other,
				Bio = null,
				Timezone = null
			};

			await this.Users.AddAsync(newUser);

			await SaveChangesAsync();

			return await GetUser(discordUser);
		}

		/// <summary>
		/// Configures the given options builder to match the settings required for the <see cref="GlobalInfoContext"/>.
		/// </summary>
		/// <param name="optionsBuilder">The builder to configure.</param>
		/// <returns>The builder, configured.</returns>
		[NotNull]
		public static DbContextOptionsBuilder ConfigureOptions([NotNull] DbContextOptionsBuilder optionsBuilder)
		{
			var passfilePath = Path.Combine("Content", "database.credentials");
			if (!File.Exists(passfilePath))
			{
				throw new FileNotFoundException("Could not find PostgreSQL credentials.", passfilePath);
			}

			var passfileContents = File.ReadAllText(passfilePath).Split(':');
			if (passfileContents.Length != 5)
			{
				throw new InvalidDataException("The credential file was of an invalid format.");
			}

			var password = passfileContents[4];

			optionsBuilder.UseNpgsql($"Server=localhost;Database=amby;Username=amby;Password={password}");

			return optionsBuilder;
		}

		/// <inheritdoc />
		protected override void OnConfiguring([NotNull] DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				ConfigureOptions(optionsBuilder);
			}
		}

		/// <inheritdoc />
		protected override void OnModelCreating([NotNull] ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<User>().HasMany(u => u.Characters).WithOne(u => u.Owner);
			modelBuilder.Entity<User>()
				.HasOne(u => u.DefaultCharacter)
				.WithOne()
				.HasForeignKey(typeof(User).FullName, "DefaultCharacterID");

			modelBuilder.Entity<User>().Property(typeof(long?), "DefaultCharacterID").IsRequired(false);
			modelBuilder.Entity<User>().HasMany(u => u.Kinks).WithOne().IsRequired();

			modelBuilder.Entity<Character>().HasOne(ch => ch.Owner).WithMany(u => u.Characters);

			modelBuilder.Entity<Roleplay>().HasOne(u => u.Owner).WithMany();
			modelBuilder.Entity<Roleplay>().HasMany(u => u.ParticipatingUsers).WithOne(p => p.Roleplay);
			modelBuilder.Entity<Roleplay>().HasMany(r => r.Messages).WithOne().IsRequired();

			modelBuilder.Entity<RoleplayParticipant>().HasOne(u => u.User).WithMany();

			modelBuilder.Entity<GlobalUserProtection>().HasMany(p => p.UserListing).WithOne(u => u.GlobalProtection);
			modelBuilder.Entity<UserProtectionEntry>().HasOne(u => u.User).WithMany();

			modelBuilder.Entity<Appearance>().HasMany(a => a.Components).WithOne().IsRequired();
		}
	}
}
