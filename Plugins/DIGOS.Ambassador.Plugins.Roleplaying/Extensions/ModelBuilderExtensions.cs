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

using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Extensions
{
    /// <summary>
    /// Contains extension methods for the <see cref="ModelBuilder"/> class.
    /// </summary>
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Configures value conversions for entities from the roleplay schema.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        /// <returns>The configured model builder.</returns>
        public static ModelBuilder ConfigureRoleplayConversions(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServerRoleplaySettings>()
                .Property(s => s.ArchiveChannel)
                .HasConversion<long?>
                (
                    v => v.HasValue ? (long)v.Value.Value : default,
                    v => v.HasValue ? new Snowflake((ulong)v) : default
                );

            modelBuilder.Entity<ServerRoleplaySettings>()
                .Property(s => s.DefaultUserRole)
                .HasConversion<long?>
                (
                    v => v.HasValue ? (long)v.Value.Value : default,
                    v => v.HasValue ? new Snowflake((ulong)v) : default
                );

            modelBuilder.Entity<ServerRoleplaySettings>()
                .Property(s => s.DedicatedRoleplayChannelsCategory)
                .HasConversion<long?>
                (
                    v => v.HasValue ? (long)v.Value.Value : default,
                    v => v.HasValue ? new Snowflake((ulong)v) : default
                );

            modelBuilder.Entity<UserMessage>()
                .Property(s => s.DiscordMessageID)
                .HasConversion(v => (long)v.Value, v => new Snowflake((ulong)v));

            modelBuilder.Entity<Roleplay>()
                .Property(s => s.ActiveChannelID)
                .HasConversion<long?>
                (
                    v => v.HasValue ? (long)v.Value.Value : default,
                    v => v.HasValue ? new Snowflake((ulong)v) : default
                );

            modelBuilder.Entity<Roleplay>()
                .Property(s => s.DedicatedChannelID)
                .HasConversion<long?>
                (
                    v => v.HasValue ? (long)v.Value.Value : default,
                    v => v.HasValue ? new Snowflake((ulong)v) : default
                );

            return modelBuilder;
        }
    }
}
