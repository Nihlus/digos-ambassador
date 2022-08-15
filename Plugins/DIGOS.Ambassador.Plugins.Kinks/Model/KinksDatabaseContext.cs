//
//  KinksDatabaseContext.cs
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
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Kinks.Model;

/// <summary>
/// Represents the database model of the dossier plugin.
/// </summary>
[PublicAPI]
public class KinksDatabaseContext : AmbassadorDbContext
{
    private const string _schemaName = "KinkModule";

    /// <summary>
    /// Gets the table where kinks are stored.
    /// </summary>
    public DbSet<Kink> Kinks => Set<Kink>();

    /// <summary>
    /// Gets the table where user kinks are stored.
    /// </summary>
    public DbSet<UserKink> UserKinks => Set<UserKink>();

    /// <summary>
    /// Initializes a new instance of the <see cref="KinksDatabaseContext"/> class.
    /// </summary>
    /// <param name="contextOptions">The context options.</param>
    public KinksDatabaseContext(DbContextOptions<KinksDatabaseContext> contextOptions)
        : base(_schemaName, contextOptions)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Kink>()
            .HasIndex(u => u.FListID)
            .IsUnique();
    }
}
