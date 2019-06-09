//
//  AmbyDatabaseContext.cs
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

using System.IO;
using DIGOS.Ambassador.Database.Appearances;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Dossiers;
using DIGOS.Ambassador.Database.Kinks;
using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Database.Roleplaying;
using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Database.Users;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Image = DIGOS.Ambassador.Database.Data.Image;

namespace DIGOS.Ambassador.Database
{
    /// <summary>
    /// Database context for global information.
    /// </summary>
    public class AmbyDatabaseContext : DbContext
    {
        /// <summary>
        /// Gets or sets the table where the user information is stored.
        /// </summary>
        public DbSet<User> Users
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where characters are stored.
        /// </summary>
        public DbSet<Character> Characters
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where kinks are stored.
        /// </summary>
        public DbSet<Kink> Kinks
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where server-specific settings are stored.
        /// </summary>
        public DbSet<Server> Servers
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where granted local permissions are stored.
        /// </summary>
        public DbSet<LocalPermission> LocalPermissions
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where granted global permissions are stored.
        /// </summary>
        public DbSet<GlobalPermission> GlobalPermissions
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where roleplays are stored.
        /// </summary>
        public DbSet<Roleplay> Roleplays
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where dossier metadata is stored.
        /// </summary>
        public DbSet<Dossier> Dossiers
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where images are stored.
        /// </summary>
        public DbSet<Image> Images
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where transformation species are stored.
        /// </summary>
        public DbSet<Species> Species
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where transformations are stored.
        /// </summary>
        public DbSet<Transformation> Transformations
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where global transformation protections are stored.
        /// </summary>
        public DbSet<GlobalUserProtection> GlobalUserProtections
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where server-specific transformation protections are stored.
        /// </summary>
        public DbSet<ServerUserProtection> ServerUserProtections
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where user consents are stored.
        /// </summary>
        public DbSet<UserConsent> UserConsents
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Gets or sets the table where character roles are stored.
        /// </summary>
        public DbSet<CharacterRole> CharacterRoles
        {
            get;

            [UsedImplicitly]
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbyDatabaseContext"/> class.
        /// </summary>
        /// <param name="options">The context options.</param>
        public AmbyDatabaseContext([NotNull] DbContextOptions options)
            : base(options)
        {
        }

        /// <summary>
        /// Configures the given options builder to match the settings required for the <see cref="AmbyDatabaseContext"/>.
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

            optionsBuilder
                .UseLazyLoadingProxies()
                .UseNpgsql($"Server=localhost;Database=amby;Username=amby;Password={password}");

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
            modelBuilder.Entity<User>()
                .HasMany(u => u.Characters)
                .WithOne(u => u.Owner)
                .IsRequired();

            modelBuilder.Entity<User>()
                .HasOne(u => u.DefaultCharacter)
                .WithOne()
                .HasForeignKey(typeof(User).FullName, "DefaultCharacterID");

            modelBuilder.Entity<User>().Property(typeof(long?), "DefaultCharacterID").IsRequired(false);
            modelBuilder.Entity<User>().HasMany(u => u.Kinks).WithOne().IsRequired();

            modelBuilder.Entity<Character>().HasOne(ch => ch.Owner).WithMany(u => u.Characters);
            modelBuilder.Entity<Character>().HasMany(ch => ch.Images).WithOne().IsRequired();

            modelBuilder.Entity<Roleplay>().HasOne(u => u.Owner).WithMany().IsRequired();
            modelBuilder.Entity<Roleplay>().HasMany(u => u.ParticipatingUsers).WithOne(p => p.Roleplay);
            modelBuilder.Entity<Roleplay>().HasMany(r => r.Messages).WithOne().IsRequired();

            modelBuilder.Entity<RoleplayParticipant>().HasOne(u => u.User).WithMany();

            modelBuilder.Entity<GlobalUserProtection>().HasMany(p => p.UserListing).WithOne(u => u.GlobalProtection);
            modelBuilder.Entity<UserProtectionEntry>().HasOne(u => u.User).WithMany();

            modelBuilder.Entity<Appearance>().HasMany(a => a.Components).WithOne().IsRequired();
        }
    }
}
