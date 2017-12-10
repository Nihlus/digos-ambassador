//
//  UserIdentifier.cs
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

using DIGOS.Ambassador.Database.Interfaces;
using Discord;

namespace DIGOS.Ambassador.Database.Users
{
	/// <summary>
	/// A user identifier entity.
	/// </summary>
	public class UserIdentifier : IEFEntity
	{
		/// <inheritdoc />
		public uint ID { get; set; }

		/// <summary>
		/// Gets or sets the Discord ID of the user.
		/// </summary>
		public ulong DiscordID { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="UserIdentifier"/> class.
		/// </summary>
		/// <param name="discordUser">The user to identify.</param>
		public UserIdentifier(IUser discordUser)
		{
			this.DiscordID = discordUser.Id;
		}

		/// <summary>
		/// Implicitly converts a <see cref="UserIdentifier"/> to a ulong.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <returns>The encapsulated ID.</returns>
		public static implicit operator ulong(UserIdentifier id)
		{
			return id.DiscordID;
		}
	}
}
