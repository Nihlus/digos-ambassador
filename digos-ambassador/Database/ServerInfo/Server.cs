//
//  Server.cs
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

using System.Collections.Generic;
using Discord;
using DIGOS.Ambassador.Database.UserInfo;

namespace DIGOS.Ambassador.Database.ServerInfo
{
	/// <summary>
	/// Represents stored settings for a Discord server.
	/// </summary>
	public class Server
	{
		/// <summary>
		/// Gets or sets the unique ID of the server in the database.
		/// </summary>
		public uint ServerID { get; set; }

		/// <summary>
		/// Gets or sets the globally unique guild ID of the server.
		/// </summary>
		public virtual ulong DiscordGuildID { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the server allows NSFW content globally.
		/// </summary>
		public bool IsNSFW { get; set; }

		/// <summary>
		/// Gets or sets the users known to the bot on this server.
		/// </summary>
		public List<User> KnownUsers { get; set; }

		/// <summary>
		/// Creates a default server entity based on a Discord guild.
		/// </summary>
		/// <param name="discordServer">The Discord server.</param>
		/// <returns>A default server entity with some information filled in.</returns>
		public static Server CreateDefault(IGuild discordServer)
		{
			return new Server
			{
				DiscordGuildID = discordServer.Id,
				IsNSFW = true,
			};
		}
	}
}
