//
//  DatabaseCredentials.cs
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
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Core.Database.Credentials
{
    /// <summary>
    /// Represents a set of configured database credentials.
    /// </summary>
    public class DatabaseCredentials
    {
        /// <summary>
        /// Gets the hostname the database is on.
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Gets the port the database listens on.
        /// </summary>
        public ushort Port { get; }

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        public string Database { get; }

        /// <summary>
        /// Gets the username to use when logging in to the database.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Gets the password to use when logging in to the database.
        /// </summary>
        public string Password { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseCredentials"/> class.
        /// </summary>
        /// <param name="host">The host the database is on.</param>
        /// <param name="port">The port the database listens on.</param>
        /// <param name="database">The name of the database.</param>
        /// <param name="username">The username to use when logging in to the database.</param>
        /// <param name="password">The password to use when logging in to the database.</param>
        public DatabaseCredentials(string host, ushort port, string database, string username, string password)
        {
            this.Host = host;
            this.Port = port;
            this.Database = database;
            this.Username = username;
            this.Password = password;
        }

        /// <summary>
        /// Gets a connection string formed from the credentials.
        /// </summary>
        /// <returns>The connection string.</returns>
        public string GetConnectionString()
        {
            return $"Server={this.Host};" +
                   $"Port={this.Port};" +
                   $"Database={this.Database};" +
                   $"Username={this.Username};" +
                   $"Password={this.Password}";
        }

        /// <summary>
        /// Attempts to parse a <see cref="DatabaseCredentials"/> object from the contents of the file at the given
        /// path.
        /// </summary>
        /// <param name="value">The string to parse.</param>
        /// <param name="credentials">The resulting credentials.</param>
        /// <returns>true if the credentials were successfully parsed; otherwise, false.</returns>
        [ContractAnnotation("=> true, credentials : notnull; => false, credentials : null")]
        public static bool TryParse(string value, [NotNullWhen(true)] out DatabaseCredentials? credentials)
        {
            credentials = null;

            var parts = value.Split(':');
            if (parts.Length != 5)
            {
                return false;
            }

            var hostname = parts[0];
            if (!ushort.TryParse(parts[1], out var port))
            {
                return false;
            }

            var database = parts[2];
            var username = parts[3];
            var password = parts[4];

            credentials = new DatabaseCredentials(hostname, port, database, username, password);
            return true;
        }
    }
}
