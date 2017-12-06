//
//  LocalInfoContext.cs
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
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace DIGOS.Ambassador.Database
{
	/// <summary>
	/// Database context for server-local information.
	/// </summary>
	public class LocalInfoContext : DbContext
	{
		/// <summary>
		/// Gets the Discord guild ID of the database.
		/// </summary>
		public ulong Server { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalInfoContext"/> class. This constructor should not be used
		/// at runtime.
		/// </summary>
		/// <param name="guildID">The guild ID of the database context.</param>
		protected LocalInfoContext(ulong guildID)
		{
			this.Server = guildID;
		}

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				optionsBuilder.UseSqlite($"Data Source={Path.Combine("Content", "Databases", $"{this.Server}.db")}");
			}
		}

		/// <summary>
		/// Gets or creates a new local info context for the given guild.
		/// </summary>
		/// <param name="guild">The guild.</param>
		/// <returns>A local info context.</returns>
		public static LocalInfoContext GetOrCreate(IGuild guild) => GetOrCreate(guild.Id);

		/// <summary>
		/// Gets or creates a new local info context for the given guild ID.
		/// </summary>
		/// <param name="guild">The guild ID.</param>
		/// <returns>A local info context.</returns>
		public static LocalInfoContext GetOrCreate(ulong guild)
		{
			var db = new LocalInfoContext(guild);
			if (!((RelationalDatabaseCreator)db.Database.GetService<IDatabaseCreator>()).Exists())
			{
				db.Database.Migrate();
			}

			return db;
		}

		/// <summary>
		/// Design-time factory for local database contexts.
		/// </summary>
		public class DesignTimeLocalInfoContextFactory : IDesignTimeDbContextFactory<LocalInfoContext>
		{
			/// <inheritdoc />
			public LocalInfoContext CreateDbContext(string[] args)
			{
				return new LocalInfoContext(0);
			}
		}
	}
}
