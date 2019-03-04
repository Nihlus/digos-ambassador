//
//  MockedDatabase.cs
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

using System;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Database.Users;
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Tests.Database
{
    /// <summary>
    /// Represents a mocked connection to the ambassador's database.
    /// </summary>
    public class MockedDatabase : IDisposable
    {
        private readonly SqliteConnection Connection;
        private readonly DbContextOptions<GlobalInfoContext> DatabaseOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockedDatabase"/> class.
        /// </summary>
        public MockedDatabase()
        {
            this.Connection = new SqliteConnection("DataSource=:memory:");
            this.Connection.Open();

            this.DatabaseOptions = new DbContextOptionsBuilder<GlobalInfoContext>()
                .UseLazyLoadingProxies()
                .UseSqlite(this.Connection).Options;

            using (var db = GetDatabaseContext())
            {
                db.Database.EnsureCreated();
            }
        }

        /// <summary>
        /// Gets a new instance of the database context.
        /// </summary>
        /// <returns>The database context.</returns>
        [NotNull]
        public GlobalInfoContext GetDatabaseContext()
        {
            return new GlobalInfoContext(this.DatabaseOptions);
        }

        /// <summary>
        /// Adds a mocked user to the database.
        /// </summary>
        /// <param name="user">The user to add.</param>
        public void AddMockedUser([NotNull] User user)
        {
            using (var db = GetDatabaseContext())
            {
                db.Users.Add(user);
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Adds a mocked server to the database, creating it from the given ID.
        /// </summary>
        /// <param name="discordServerID">The ID of the server.</param>
        public void AddMockedServer(long discordServerID)
        {
            var server = new Server { DiscordID = discordServerID };
            AddMockedServer(server);
        }

        /// <summary>
        /// Adds a mocked server to the database.
        /// </summary>
        /// <param name="server">The server to add.</param>
        public void AddMockedServer([NotNull] Server server)
        {
            using (var db = GetDatabaseContext())
            {
                db.Servers.Add(server);
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Adds a mocked global permission to the database.
        /// </summary>
        /// <param name="globalPermission">The permission to add.</param>
        public void AddMockedGlobalPermission([NotNull] GlobalPermission globalPermission)
        {
            using (var db = GetDatabaseContext())
            {
                db.GlobalPermissions.Add(globalPermission);
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Adds a mocked local permission to the database.
        /// </summary>
        /// <param name="grantedPermission">The granted permission.</param>
        public void AddMockedLocalPermission([NotNull] LocalPermission grantedPermission)
        {
            using (var db = GetDatabaseContext())
            {
                db.LocalPermissions.Add(grantedPermission);
                db.SaveChanges();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Connection?.Close();
            this.Connection?.Dispose();
        }
    }
}
