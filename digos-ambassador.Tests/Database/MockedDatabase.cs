//
//  MockedDatabase.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Database.UserInfo;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Tests.Database
{
	public class MockedDatabase : IDisposable
	{
		private readonly SqliteConnection Connection;
		private readonly DbContextOptions<GlobalInfoContext> DatabaseOptions;

		public MockedDatabase()
		{
			this.Connection = new SqliteConnection("DataSource=:memory:");
			this.Connection.Open();

			this.DatabaseOptions = new DbContextOptionsBuilder<GlobalInfoContext>().UseSqlite(this.Connection).Options;

			using (var db = GetDatabaseContext())
			{
				db.Database.EnsureCreated();
			}
		}

		public GlobalInfoContext GetDatabaseContext()
		{
			return new GlobalInfoContext(this.DatabaseOptions);
		}

		public void AddMockedUser(User user)
		{
			using (var db = GetDatabaseContext())
			{
				db.Users.Add(user);
				db.SaveChanges();
			}
		}

		public void AddMockedServer(ulong discordServerID)
		{
			var server = new Server { DiscordGuildID = discordServerID };
			AddMockedServer(server);
		}

		public void AddMockedServer(Server server)
		{
			using (var db = GetDatabaseContext())
			{
				db.Servers.Add(server);
				db.SaveChanges();
			}
		}

		public void AddMockedGlobalPermission(GlobalPermission globalPermission)
		{
			using (var db = GetDatabaseContext())
			{
				db.GlobalPermissions.Add(globalPermission);
				db.SaveChanges();
			}
		}

		public void Dispose()
		{
			this.Connection?.Close();
			this.Connection?.Dispose();
		}
	}
}