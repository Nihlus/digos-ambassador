//
//  TransformationsDatabaseContext.cs
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
using DIGOS.Ambassador.Core.Database.Context;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Transformations.Model;

/// <summary>
/// Represents the database model of the dossier plugin.
/// </summary>
[PublicAPI]
public class TransformationsDatabaseContext : AmbassadorDbContext
{
    private const string _schemaName = "TransformationModule";

    /// <summary>
    /// Gets the table where transformation species are stored.
    /// </summary>
    public DbSet<Species> Species => Set<Species>();

    /// <summary>
    /// Gets the table where transformations are stored.
    /// </summary>
    public DbSet<Transformation> Transformations => Set<Transformation>();

    /// <summary>
    /// Gets the table where global transformation protections are stored.
    /// </summary>
    public DbSet<GlobalUserProtection> GlobalUserProtections => Set<GlobalUserProtection>();

    /// <summary>
    /// Gets the table where server-specific transformation protections are stored.
    /// </summary>
    public DbSet<ServerUserProtection> ServerUserProtections => Set<ServerUserProtection>();

    /// <summary>
    /// Gets the table where appearance configurations are stored.
    /// </summary>
    public DbSet<Appearance> Appearances => Set<Appearance>();

    /// <summary>
    /// Initializes a new instance of the <see cref="TransformationsDatabaseContext"/> class.
    /// </summary>
    /// <param name="contextOptions">The context options.</param>
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized", Justification = "Initialized by EF Core.")]
    public TransformationsDatabaseContext
    (
        DbContextOptions<TransformationsDatabaseContext> contextOptions
    )
        : base(_schemaName, contextOptions)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transformation>()
            .OwnsOne(t => t.DefaultBaseColour, od => od.ToTable("DefaultBaseColours", _schemaName))
            .OwnsOne(t => t.DefaultPatternColour, od => od.ToTable("DefaultPatternColours", _schemaName));

        modelBuilder.Entity<Appearance>().OwnsMany(a => a.Components, ao =>
        {
            ao.ToTable("AppearanceComponents", _schemaName);

            ao.OwnsOne(c => c.BaseColour, od => od.ToTable("BaseColours", _schemaName));
            ao.OwnsOne(c => c.PatternColour, od => od.ToTable("PatternColours", _schemaName));

            ao.Property<long>("ID");
            ao.HasKey("ID");
        });
    }
}
