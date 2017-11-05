using System;
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Database.UserInfo;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace digos_ambassador.Tests.Database
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
			this.Connection.Close();
			this.Connection?.Dispose();
		}
	}
}