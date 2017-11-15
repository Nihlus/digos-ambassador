//
//  UserMessage.cs
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
using System.Threading.Tasks;
using Discord;
using DIGOS.Ambassador.Database.Users;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Database.Roleplaying
{
	/// <summary>
	/// Represents a saved user message.
	/// </summary>
	public class UserMessage
	{
		/// <summary>
		/// Gets or sets the unique message ID.
		/// </summary>
		public uint UserMessageID { get; set; }

		/// <summary>
		/// Gets or sets the unique Discord message ID.
		/// </summary>
		public ulong DiscordMessageID { get; set; }

		/// <summary>
		/// Gets or sets the author of the message.
		/// </summary>
		public User Author { get; set; }

		/// <summary>
		/// Gets or sets the timestamp of the message.
		/// </summary>
		public DateTimeOffset Timestamp { get; set; }

		/// <summary>
		/// Gets or sets the author's nickname at the time of sending.
		/// </summary>
		public string AuthorNickname { get; set; }

		/// <summary>
		/// Gets or sets the contents of the message.
		/// </summary>
		public string Contents { get; set; }

		/// <summary>
		/// Creates a new <see cref="UserMessage"/> from the specified Discord message.
		/// </summary>
		/// <param name="message">The message to create from.</param>
		/// <param name="authorNickname">The current display name of the author.</param>
		/// <returns>A new UserMessage.</returns>
		[ItemNotNull]
		public static async Task<UserMessage> FromDiscordMessageAsync
		(
			[NotNull] IMessage message,
			[NotNull] string authorNickname
		)
		{
			using (var db = new GlobalInfoContext())
			{
				return new UserMessage
				{
					DiscordMessageID = message.Id,
					Author = await db.GetOrRegisterUserAsync(message.Author),
					Timestamp = message.Timestamp,
					AuthorNickname = authorNickname,
					Contents = message.Content
				};
			}
		}
	}
}
