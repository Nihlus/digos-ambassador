//
//  ModerationDatabaseContext.cs
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

using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.Core;
using Remora.EntityFrameworkCore.Modular;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Moderation.Model
{
    /// <summary>
    /// Represents the database model of the dossier plugin.
    /// </summary>
    [PublicAPI]
    public class ModerationDatabaseContext : SchemaAwareDbContext
    {
        private const string SchemaName = "ModerationModule";

        /// <summary>
        /// Gets the database set of server settings.
        /// </summary>
        public DbSet<ServerModerationSettings> ServerSettings { get; private set; } = null!;

        /// <summary>
        /// Gets the database set of user notes.
        /// </summary>
        public DbSet<UserNote> UserNotes { get; private set; } = null!;

        /// <summary>
        /// Gets the database set of user warnings.
        /// </summary>
        public DbSet<UserWarning> UserWarnings { get; private set; } = null!;

        /// <summary>
        /// Gets the database set of user bans.
        /// </summary>
        public DbSet<UserBan> UserBans { get; private set; } = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModerationDatabaseContext"/> class.
        /// </summary>
        /// <param name="contextOptions">The context options.</param>
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized", Justification = "Initialized by EF Core.")]
        public ModerationDatabaseContext(DbContextOptions<ModerationDatabaseContext> contextOptions)
            : base(SchemaName, contextOptions)
        {
        }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ServerModerationSettings>()
                .Property(s => s.MonitoringChannel)
                .HasConversion<long?>
                (
                    v => v.HasValue ? (long)v.Value.Value : null,
                    v => v.HasValue ? new Snowflake((ulong)v.Value) : null
                );

            modelBuilder.Entity<ServerModerationSettings>()
                .Property(s => s.ModerationLogChannel)
                .HasConversion<long?>
                (
                    v => v.HasValue ? (long)v.Value.Value : null,
                    v => v.HasValue ? new Snowflake((ulong)v.Value) : null
                );

            modelBuilder.Entity<UserBan>()
                .Property(b => b.MessageID)
                .HasConversion<long?>
                (
                    v => v.HasValue ? (long)v.Value.Value : null,
                    v => v.HasValue ? new Snowflake((ulong)v.Value) : null
                );

            modelBuilder.Entity<UserWarning>()
                .Property(w => w.MessageID)
                .HasConversion<long?>
                (
                    v => v.HasValue ? (long)v.Value.Value : null,
                    v => v.HasValue ? new Snowflake((ulong)v.Value) : null
                );

            modelBuilder.Entity<Server>()
                .Property(s => s.DiscordID)
                .HasConversion(v => (long)v.Value, v => new Snowflake((ulong)v));

            modelBuilder.Entity<User>()
                .Property(s => s.DiscordID)
                .HasConversion(v => (long)v.Value, v => new Snowflake((ulong)v));
        }
    }
}
