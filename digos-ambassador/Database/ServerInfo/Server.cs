//
//  Server.cs
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

using System.Collections.Generic;
using System.Linq;
using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Database.Users;

using Discord;

using JetBrains.Annotations;

namespace DIGOS.Ambassador.Database.ServerInfo
{
    /// <summary>
    /// Represents stored settings for a Discord server.
    /// </summary>
    public class Server : IEFEntity
    {
        /// <inheritdoc />
        public long ID { get; set; }

        /// <summary>
        /// Gets or sets the globally unique guild ID of the server.
        /// </summary>
        public virtual long DiscordID { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the server allows NSFW content globally.
        /// </summary>
        public bool IsNSFW { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the server should suppress permission warnings.
        /// </summary>
        public bool SuppressPermissonWarnings { get; set; }

        /// <summary>
        /// Gets or sets the users known to the bot on this server.
        /// </summary>
        public List<User> KnownUsers { get; set; }

        /// <summary>
        /// Determines whether or not a given user is known to this server.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>true if the user is known; otherwise, false.</returns>
        public bool IsUserKnown([NotNull] IUser user)
        {
            return this.KnownUsers.Any(u => u.DiscordID == (long)user.Id);
        }

        /// <summary>
        /// Creates a default server entity based on a Discord guild.
        /// </summary>
        /// <param name="discordServer">The Discord server.</param>
        /// <returns>A default server entity with some information filled in.</returns>
        [Pure]
        [NotNull]
        public static Server CreateDefault([NotNull] IGuild discordServer)
        {
            return new Server
            {
                DiscordID = (long)discordServer.Id,
                IsNSFW = true,
            };
        }
    }
}
