//
//  RoleplayService.cs
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

using System.Linq;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Roleplaying;

using Discord.WebSocket;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Acts as an interface for accessing, enabling, and disabling ongoing roleplays.
	/// </summary>
	public class RoleplayService
	{
		/// <summary>
		/// Consumes a message, adding it to the active roleplay in its channel if the author is a participant.
		/// </summary>
		/// <param name="message">The message to consume.</param>
		public async void ConsumeMessage(SocketMessage message)
		{
			using (var db = new GlobalInfoContext())
			{
				if (!await db.HasActiveRoleplayAsync(message.Channel))
				{
					return;
				}

				var roleplay = await db.GetActiveRoleplayAsync(message.Channel);

				if (roleplay.Participants == null || !roleplay.Participants.Any(p => p.DiscordID == message.Author.Id))
				{
					return;
				}

				string userNick = message.Author.Username;
				if (message.Author is SocketGuildUser guildUser && !string.IsNullOrEmpty(guildUser.Nickname))
				{
					userNick = guildUser.Nickname;
				}

				if (roleplay.Messages.Any(m => m.DiscordMessageID == message.Id))
				{
					// Edit the existing message
					var existingMessage = roleplay.Messages.Find(m => m.DiscordMessageID == message.Id);
					existingMessage.Contents = message.Content;
				}
				else
				{
					var roleplayMessage = await UserMessage.FromDiscordMessageAsync(message, userNick);
					roleplay.Messages.Add(roleplayMessage);
				}

				await db.SaveChangesAsync();
			}
		}
	}
}
