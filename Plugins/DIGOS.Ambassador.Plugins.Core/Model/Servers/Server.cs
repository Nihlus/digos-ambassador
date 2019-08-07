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
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using Discord;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Core.Model.Servers
{
    /// <summary>
    /// Represents stored settings for a Discord server.
    /// </summary>
    [Table("Servers", Schema = "Core")]
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
        [NotNull]
        public virtual List<User> KnownUsers { get; set; } = new List<User>();

        /// <summary>
        /// Gets or sets the server's description.
        /// </summary>
        [CanBeNull]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the first-join message should be sent to users when they first join
        /// the server.
        /// </summary>
        public bool SendJoinMessage { get; set; }

        /// <summary>
        /// Gets or sets the server's first-join message.
        /// </summary>
        [CanBeNull]
        public string JoinMessage { get; set; }

        /// <summary>
        /// Gets or sets the channel category generated roleplay channels should be created under.
        /// </summary>
        public long? DedicatedRoleplayChannelsCategory { get; set; }

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
