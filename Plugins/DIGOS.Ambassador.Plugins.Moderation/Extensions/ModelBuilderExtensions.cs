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

using DIGOS.Ambassador.Plugins.Moderation.Model;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.Core;

namespace DIGOS.Ambassador.Plugins.Moderation.Extensions
{
    /// <summary>
    /// Contains extension methods for the <see cref="ModelBuilder"/> class.
    /// </summary>
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Configures value conversions for entities from the moderation schema.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        /// <returns>The configured model builder.</returns>
        public static ModelBuilder ConfigureModerationConversions(this ModelBuilder modelBuilder)
        {
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

            return modelBuilder;
        }
    }
}
