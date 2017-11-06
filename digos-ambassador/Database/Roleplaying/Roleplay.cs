//
//  Roleplay.cs
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
using DIGOS.Ambassador.Database.UserInfo;

namespace DIGOS.Ambassador.Database.Roleplaying
{
	/// <summary>
	/// Represents a saved roleplay.
	/// </summary>
	public class Roleplay
	{
		/// <summary>
		/// Gets or sets the unique ID of the roleplay.
		/// </summary>
		public uint RoleplayID { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the roleplay is currently active in a channel.
		/// </summary>
		public bool IsActive { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the roleplay can be viewed or replayed by anyone.
		/// </summary>
		public bool IsPublic { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the roleplay is NSFW.
		/// </summary>
		public bool IsNSFW { get; set; }

		/// <summary>
		/// Gets or sets the ID of the channel that the roleplay is active in.
		/// </summary>
		public ulong ActiveChannelID { get; set; }

		/// <summary>
		/// Gets or sets the owner of the roleplay.
		/// </summary>
		public User Owner { get; set; }

		/// <summary>
		/// Gets or sets the participants of the roleplay.
		/// </summary>
		public List<User> Participants { get; set; }

		/// <summary>
		/// Gets or sets the name of the roleplay.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the summary of the roleplay.
		/// </summary>
		public string Summary { get; set; }

		/// <summary>
		/// Gets or sets the saved messages in the roleplay.
		/// </summary>
		public List<UserMessage> Messages { get; set; }
	}
}
