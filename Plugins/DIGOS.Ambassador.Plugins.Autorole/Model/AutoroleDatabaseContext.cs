//
//  AutoroleDatabaseContext.cs
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

using DIGOS.Ambassador.Core.Database.Context;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.Bases;
using DIGOS.Ambassador.Plugins.Autorole.Model.Statistics;
using Microsoft.EntityFrameworkCore;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Autorole.Model;

/// <summary>
/// Represents the database model of the dossier plugin.
/// </summary>
public class AutoroleDatabaseContext : AmbassadorDbContext
{
    private const string _schemaName = "AutoroleModule";

    /// <summary>
    /// Gets or sets the table where autoroles are stored.
    /// </summary>
    public DbSet<AutoroleConfiguration> Autoroles => Set<AutoroleConfiguration>();

    /// <summary>
    /// Gets or sets the table where user statistics are stored.
    /// </summary>
    public DbSet<UserStatistics> UserStatistics => Set<UserStatistics>();

    /// <summary>
    /// Gets or sets the table where autorole confirmations are stored.
    /// </summary>
    public DbSet<AutoroleConfirmation> AutoroleConfirmations => Set<AutoroleConfirmation>();

    /// <summary>
    /// Gets or sets the table where autorole server settings are stored.
    /// </summary>
    public DbSet<AutoroleServerSettings> AutoroleServerSettings => Set<AutoroleServerSettings>();

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoroleDatabaseContext"/> class.
    /// </summary>
    /// <param name="contextOptions">The context options.</param>
    public AutoroleDatabaseContext(DbContextOptions<AutoroleDatabaseContext> contextOptions)
        : base(_schemaName, contextOptions)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AutoroleConfiguration>()
            .HasMany(ac => ac.Conditions)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AutoroleConfiguration>()
            .HasOne(a => a.Server)
            .WithMany()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReactionCondition>()
            .HasBaseType<AutoroleCondition>();

        modelBuilder.Entity<RoleCondition>()
            .HasBaseType<AutoroleCondition>();

        ConfigureTimeSinceEventCondition<TimeSinceJoinCondition>(modelBuilder);
        ConfigureTimeSinceEventCondition<TimeSinceLastActivityCondition>(modelBuilder);

        ConfigureMessageCountCondition<MessageCountInChannelCondition>(modelBuilder);
        ConfigureMessageCountCondition<MessageCountInGuildCondition>(modelBuilder);

        modelBuilder.Entity<UserStatistics>()
            .HasMany(s => s.ServerStatistics)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserStatistics>()
            .HasIndex("UserID")
            .IsUnique();

        modelBuilder.Entity<UserServerStatistics>()
            .HasMany(s => s.ChannelStatistics)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserServerStatistics>()
            .HasIndex("ServerID", "UserStatisticsID")
            .IsUnique();

        modelBuilder.Entity<UserChannelStatistics>()
            .HasIndex("ChannelID", "UserServerStatisticsID")
            .IsUnique();
    }

    private void ConfigureTimeSinceEventCondition<TTimeSinceEventCondition>(ModelBuilder modelBuilder)
        where TTimeSinceEventCondition : TimeSinceEventCondition<TTimeSinceEventCondition>
    {
        modelBuilder.Entity<TTimeSinceEventCondition>()
            .HasBaseType<TimeSinceEventCondition<TTimeSinceEventCondition>>()
            .HasDiscriminator()
            .HasValue(typeof(TTimeSinceEventCondition).Name);

        modelBuilder.Entity<TTimeSinceEventCondition>()
            .Property(c => c.RequiredTime)
            .HasColumnName(nameof(TimeSinceEventCondition<TTimeSinceEventCondition>.RequiredTime));
    }

    private void ConfigureMessageCountCondition<TMessageCountCondition>(ModelBuilder modelBuilder)
        where TMessageCountCondition : MessageCountInSourceCondition<TMessageCountCondition>
    {
        modelBuilder.Entity<TMessageCountCondition>()
            .HasBaseType<MessageCountInSourceCondition<TMessageCountCondition>>()
            .HasDiscriminator()
            .HasValue(typeof(TMessageCountCondition).Name);

        modelBuilder.Entity<TMessageCountCondition>()
            .Property(e => e.RequiredCount)
            .HasColumnName(nameof(MessageCountInSourceCondition<TMessageCountCondition>.RequiredCount));

        modelBuilder.Entity<TMessageCountCondition>()
            .Property(e => e.SourceID)
            .HasColumnName(nameof(MessageCountInSourceCondition<TMessageCountCondition>.SourceID));
    }
}
