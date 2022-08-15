//
//  CoreDatabaseContext.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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

using DIGOS.Ambassador.Core.Database.Context;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using Microsoft.EntityFrameworkCore;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Core.Model;

/// <summary>
/// Represents the database model of the core plugin.
/// </summary>
public class CoreDatabaseContext : AmbassadorDbContext
{
    private const string _schemaName = "Core";

    /// <summary>
    /// Gets the table where the user information is stored.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets the table where server-specific settings are stored.
    /// </summary>
    public DbSet<Server> Servers => Set<Server>();

    /// <summary>
    /// Gets the table where user consents are stored.
    /// </summary>
    public DbSet<UserConsent> UserConsents => Set<UserConsent>();

    /// <summary>
    /// Initializes a new instance of the <see cref="CoreDatabaseContext"/> class.
    /// </summary>
    /// <param name="contextOptions">The context options.</param>
    public CoreDatabaseContext
    (
        DbContextOptions<CoreDatabaseContext> contextOptions
    )
        : base(_schemaName, contextOptions)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ServerUser>().HasOne(su => su.Server).WithMany(s => s.KnownUsers);
        modelBuilder.Entity<ServerUser>().HasOne(su => su.User).WithMany();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.DiscordID)
            .IsUnique();

        modelBuilder.Entity<Server>()
            .HasIndex(s => s.DiscordID)
            .IsUnique();
    }
}
