//
//  DiscordService.cs
//
//  Author:
//        Jarl Gullberg <jarl.gullberg@gmail.com>
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
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Handles integration with Discord.
	/// </summary>
	public class DiscordService
	{
		private static readonly HttpClient Client = new HttpClient();

		static DiscordService()
		{
			Client.Timeout = TimeSpan.FromSeconds(4);
		}

		/// <summary>
		/// Gets the byte stream from an attachment.
		/// </summary>
		/// <param name="attachment">The attachment to get.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		[Pure]
		[MustUseReturnValue("The resulting stream must be disposed.")]
		public async Task<RetrieveEntityResult<Stream>> GetAttachmentStreamAsync([NotNull] Attachment attachment)
		{
			try
			{
				var stream = await Client.GetStreamAsync(attachment.Url);
				return RetrieveEntityResult<Stream>.FromSuccess(stream);
			}
			catch (HttpRequestException hex)
			{
				return RetrieveEntityResult<Stream>.FromError(CommandError.Exception, hex.ToString());
			}
			catch (TaskCanceledException)
			{
				return RetrieveEntityResult<Stream>.FromError(CommandError.Unsuccessful, "The download operation timed out.");
			}
		}

		/// <summary>
		/// Sets the nickname of the given user.
		/// </summary>
		/// <param name="context">The context in which the user is.</param>
		/// <param name="guildUser">The guild user to set the nick of.</param>
		/// <param name="nickname">The nickname to set.</param>
		/// <returns>A modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetUserNicknameAsync
		(
			[NotNull] SocketCommandContext context,
			[NotNull] IGuildUser guildUser,
			[CanBeNull] string nickname
		)
		{
			if (!HasPermission(context, GuildPermission.ManageNicknames))
			{
				return ModifyEntityResult.FromError(CommandError.UnmetPrecondition, "I'm not allowed to set nicknames on this server.");
			}

			if (context.Guild.OwnerId == guildUser.Id)
			{
				return ModifyEntityResult.FromError(CommandError.UnmetPrecondition, "I can't set the nickname of the server's owner.");
			}

			await guildUser.ModifyAsync(u => u.Nickname = nickname);
			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Determines whether or not the ambassador has the given permission in the given context.
		/// </summary>
		/// <param name="context">The command context.</param>
		/// <param name="guildPermission">The permission to check.</param>
		/// <returns>true if she has permission; otherwise, false.</returns>
		[Pure]
		public bool HasPermission([NotNull] SocketCommandContext context, GuildPermission guildPermission)
		{
			var amby = context.Guild.GetUser(context.Client.CurrentUser.Id) as IGuildUser;
			if (amby is null)
			{
				return false;
			}

			return amby.GuildPermissions.Has(guildPermission);
		}
	}
}
