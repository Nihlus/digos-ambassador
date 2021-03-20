//
//  ModelBuilderExtensions.cs
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

using DIGOS.Ambassador.Plugins.Autorole.Model;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.Bases;
using DIGOS.Ambassador.Plugins.Autorole.Model.Statistics;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.Core;

namespace DIGOS.Ambassador.Plugins.Autorole.Extensions
{
    /// <summary>
    /// Contains extension methods for the <see cref="ModelBuilder"/> class.
    /// </summary>
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Configures value conversions for entities from the autorole schema.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        /// <returns>The configured model builder.</returns>
        public static ModelBuilder ConfigureAutoroleConversions(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MessageCountInSourceCondition<MessageCountInChannelCondition>>()
                .Property(s => s.SourceID)
                .HasConversion(v => (long)v.Value, v => new Snowflake((ulong)v));

            modelBuilder.Entity<MessageCountInSourceCondition<MessageCountInGuildCondition>>()
                .Property(s => s.SourceID)
                .HasConversion(v => (long)v.Value, v => new Snowflake((ulong)v));

            modelBuilder.Entity<ReactionCondition>()
                .Property(s => s.ChannelID)
                .HasConversion(v => (long)v.Value, v => new Snowflake((ulong)v));

            modelBuilder.Entity<ReactionCondition>()
                .Property(s => s.MessageID)
                .HasConversion(v => (long)v.Value, v => new Snowflake((ulong)v));

            modelBuilder.Entity<RoleCondition>()
                .Property(s => s.RoleID)
                .HasConversion(v => (long)v.Value, v => new Snowflake((ulong)v));

            modelBuilder.Entity<UserChannelStatistics>()
                .Property(s => s.ChannelID)
                .HasConversion(v => (long)v.Value, v => new Snowflake((ulong)v));

            modelBuilder.Entity<AutoroleConfiguration>()
                .Property(s => s.DiscordRoleID)
                .HasConversion(v => (long)v.Value, v => new Snowflake((ulong)v));

            modelBuilder.Entity<AutoroleServerSettings>()
                .Property(s => s.AffirmationRequiredNotificationChannelID)
                .HasConversion<long?>
                (
                    v => v.HasValue ? (long)v.Value.Value : default,
                    v => v.HasValue ? new Snowflake((ulong)v) : default
                );

            return modelBuilder;
        }
    }
}
