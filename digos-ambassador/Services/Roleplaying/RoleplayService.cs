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

using System;
using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Roleplaying;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DIGOS.Ambassador.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Services.Roleplaying
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
		public async void ConsumeMessage([NotNull]IMessage message)
		{
			using (var db = new GlobalInfoContext())
			{
				var result = await GetActiveRoleplayAsync(db, message.Channel);
				if (!result.IsSuccess)
				{
					return;
				}

				var roleplay = result.Entity;

				await AddToOrUpdateMessageInRoleplay(db, roleplay, message);
			}
		}

		/// <summary>
		/// Adds a new message to the given roleplay, or edits it if there is an existing one.
		/// </summary>
		/// <param name="db">The database where the roleplays are stored.</param>
		/// <param name="roleplay">The roleplay to modify.</param>
		/// <param name="message">The message to add or update.</param>
		/// <returns>A task wrapping the update action.</returns>
		public async Task<ModifyEntityResult> AddToOrUpdateMessageInRoleplay
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] Roleplay roleplay,
			[NotNull] IMessage message
		)
		{
			if (roleplay.Participants == null || !roleplay.Participants.Any(p => p.DiscordID == message.Author.Id))
			{
				return ModifyEntityResult.FromError(CommandError.Unsuccessful, "The given message was not authored by a participant of the roleplay.");
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

				if (existingMessage.Contents.Equals(message.Content))
				{
					return ModifyEntityResult.FromError(CommandError.Unsuccessful, "Nothing to do; message content match.");
				}

				existingMessage.Contents = message.Content;

				await db.SaveChangesAsync();
				return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
			}

			var roleplayMessage = await UserMessage.FromDiscordMessageAsync(message, userNick);
			roleplay.Messages.Add(roleplayMessage);

			await db.SaveChangesAsync();
			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Added);
		}

		/// <summary>
		/// This method searches for the best matching roleplay given an owner and a name. If no owner is provided, then
		/// the global list is searched for a unique name. If neither are provided, the currently active roleplay is
		/// returned. If no match can be found, a failed result is returned.
		/// </summary>
		/// <param name="db">The database where the roleplays are stored.</param>
		/// <param name="context">The command context.</param>
		/// <param name="roleplayOwner">The owner of the roleplay, if any.</param>
		/// <param name="roleplayName">The name of the roleplay, if any.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public async Task<RetrieveEntityResult<Roleplay>> GetBestMatchingRoleplayAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] SocketCommandContext context,
			[CanBeNull] IUser roleplayOwner,
			[CanBeNull] string roleplayName
		)
		{
			if (roleplayOwner is null && roleplayName is null)
			{
				return await GetActiveRoleplayAsync(db, context.Channel);
			}

			if (roleplayOwner is null)
			{
				return await GetNamedRoleplayAsync(db, roleplayName);
			}

			if (roleplayName.IsNullOrWhitespace())
			{
				return RetrieveEntityResult<Roleplay>.FromError(CommandError.ObjectNotFound, "Roleplays can't have empty or null names.");
			}

			return await GetUserRoleplayByNameAsync(db, context, roleplayOwner, roleplayName);
		}

		/// <summary>
		/// Gets a roleplay by its given name.
		/// </summary>
		/// <param name="db">The database context where the data is stored.</param>
		/// <param name="roleplayName">The name of the roleplay.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public async Task<RetrieveEntityResult<Roleplay>> GetNamedRoleplayAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] string roleplayName
		)
		{
			if (await db.Roleplays.CountAsync(rp => rp.Name.Equals(roleplayName, StringComparison.OrdinalIgnoreCase)) > 1)
			{
				return RetrieveEntityResult<Roleplay>.FromError
				(
					CommandError.MultipleMatches,
					"There's more than one roleplay with that name. Please specify which user it belongs to."
				);
			}

			var roleplay = db.Roleplays
				.Include(rp => rp.Owner)
				.Include(rp => rp.Participants)
				.Include(rp => rp.Messages)
				.FirstOrDefault(rp => rp.Name.Equals(roleplayName, StringComparison.OrdinalIgnoreCase));

			if (roleplay is null)
			{
				return RetrieveEntityResult<Roleplay>.FromError(CommandError.ObjectNotFound, "No roleplay with that name found.");
			}

			return RetrieveEntityResult<Roleplay>.FromSuccess(roleplay);
		}

		/// <summary>
		/// Gets the current active roleplay in the given channel.
		/// </summary>
		/// <param name="db">The database where the roleplays are stored.</param>
		/// <param name="channel">The channel to get the roleplay from.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public async Task<RetrieveEntityResult<Roleplay>> GetActiveRoleplayAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IChannel channel
		)
		{
			var roleplay = await db.Roleplays
				.Include(rp => rp.Owner)
				.Include(rp => rp.Participants)
				.Include(rp => rp.Messages)
				.FirstOrDefaultAsync(rp => rp.IsActive && rp.ActiveChannelID == channel.Id);

			if (roleplay is null)
			{
				return RetrieveEntityResult<Roleplay>.FromError(CommandError.ObjectNotFound, "There is no roleplay that is currently active in this channel.");
			}

			return RetrieveEntityResult<Roleplay>.FromSuccess(roleplay);
		}

		/// <summary>
		/// Determines whether or not there is an active roleplay in the given channel.
		/// </summary>
		/// <param name="db">The database where the roleplays are stored.</param>
		/// <param name="channel">The channel to check.</param>
		/// <returns>true if there is an active roleplay; otherwise, false.</returns>
		public async Task<bool> HasActiveRoleplayAsync([NotNull] GlobalInfoContext db, [NotNull] IChannel channel)
		{
			return await db.Roleplays.AnyAsync(rp => rp.IsActive && rp.ActiveChannelID == channel.Id);
		}

		/// <summary>
		/// Determines whether or not the given roleplay name is unique for a given user.
		/// </summary>
		/// <param name="db">The database where the roleplays are stored.</param>
		/// <param name="discordUser">The user to check.</param>
		/// <param name="roleplayName">The roleplay name to check.</param>
		/// <returns>true if the name is unique; otherwise, false.</returns>
		public async Task<bool> IsRoleplayNameUniqueForUserAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser discordUser,
			[NotNull] string roleplayName
		)
		{
			var userRoleplays = GetUserRoleplays(db, discordUser);
			if (await userRoleplays.CountAsync() <= 0)
			{
				return true;
			}

			return !await userRoleplays.AnyAsync(rp => rp.Name.Equals(roleplayName, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Get the roleplays owned by the given user.
		/// </summary>
		/// <param name="db">The database where the roleplays are stored.</param>
		/// <param name="discordUser">The user to get the roleplays of.</param>
		/// <returns>A queryable list of roleplays belonging to the user.</returns>
		[NotNull]
		[ItemNotNull]
		public IQueryable<Roleplay> GetUserRoleplays([NotNull]GlobalInfoContext db, [NotNull]IUser discordUser)
		{
			return db.Roleplays
				.Include(rp => rp.Owner)
				.Include(rp => rp.Participants)
				.Include(rp => rp.Messages)
				.Where(rp => rp.Owner.DiscordID == discordUser.Id);
		}

		/// <summary>
		/// Gets a roleplay belonging to a given user by a given name.
		/// </summary>
		/// <param name="db">The database where the roleplays are stored.</param>
		/// <param name="context">The context of the user.</param>
		/// <param name="discordUser">The user to get the roleplay from.</param>
		/// <param name="roleplayName">The name of the roleplay.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public async Task<RetrieveEntityResult<Roleplay>> GetUserRoleplayByNameAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] SocketCommandContext context,
			[NotNull] IUser discordUser,
			[NotNull] string roleplayName
		)
		{
			var roleplay = await db.Roleplays
			.Include(rp => rp.Owner)
			.Include(rp => rp.Participants)
			.Include(rp => rp.Messages)
			.FirstOrDefaultAsync
			(
				rp =>
					rp.Name.Equals(roleplayName, StringComparison.OrdinalIgnoreCase) &&
					rp.Owner.DiscordID == discordUser.Id
			);

			if (roleplay is null)
			{
				var isCurrentUser = context.Message.Author.Id == discordUser.Id;
				var errorMessage = isCurrentUser
					? "You don't own a roleplay with that name."
					: "The user doesn't own a roleplay with that name.";

				return RetrieveEntityResult<Roleplay>.FromError(CommandError.ObjectNotFound, errorMessage);
			}

			return RetrieveEntityResult<Roleplay>.FromSuccess(roleplay);
		}

		/// <summary>
		/// Removes the given user from the given roleplay.
		/// </summary>
		/// <param name="db">The database where the roleplays are stored.</param>
		/// <param name="context">The context of the user.</param>
		/// <param name="roleplay">The roleplay to remove the user from.</param>
		/// <param name="discordUser">The user to remove from the roleplay.</param>
		/// <returns>An execution result which may or may not have succeeded.</returns>
		public async Task<ExecuteResult> RemoveUserFromRoleplayAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] SocketCommandContext context,
			[NotNull] Roleplay roleplay,
			[NotNull] IUser discordUser
		)
		{
			var isCurrentUser = context.Message.Author.Id == discordUser.Id;
			if (!roleplay.Participants.Any(p => p.DiscordID == discordUser.Id))
			{
				var errorMessage = isCurrentUser
					? "You're not in that roleplay."
					: "No matching user found in the roleplay.";

				return ExecuteResult.FromError(CommandError.Unsuccessful, errorMessage);
			}

			if (roleplay.Owner.DiscordID == discordUser.Id)
			{
				var errorMessage = isCurrentUser
					? "You can't leave a roleplay you own."
					: "The owner of a roleplay can't be removed from it.";

				return ExecuteResult.FromError(CommandError.Unsuccessful, errorMessage);
			}

			roleplay.Participants = roleplay.Participants.Where(p => p.DiscordID != discordUser.Id).ToList();
			await db.SaveChangesAsync();

			return ExecuteResult.FromSuccess();
		}

		/// <summary>
		/// Adds the given user to the given roleplay.
		/// </summary>
		/// <param name="db">The database where the roleplays are stored.</param>
		/// <param name="context">The context of the user.</param>
		/// <param name="roleplay">The roleplay to add the user to.</param>
		/// <param name="discordUser">The user to add to the roleplay.</param>
		/// <returns>An execution result which may or may not have succeeded.</returns>
		public async Task<ExecuteResult> AddUserToRoleplayAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] SocketCommandContext context,
			[NotNull] Roleplay roleplay,
			[NotNull] IUser discordUser
		)
		{
			var isCurrentUser = context.Message.Author.Id == discordUser.Id;
			if (roleplay.Participants.Any(p => p.DiscordID == discordUser.Id))
			{
				var errorMessage = isCurrentUser
					? "You're already in that roleplay."
					: "The user is aleady in that roleplay.";

				return ExecuteResult.FromError(CommandError.Unsuccessful, errorMessage);
			}

			roleplay.Participants.Add(await db.GetOrRegisterUserAsync(discordUser));
			await db.SaveChangesAsync();

			return ExecuteResult.FromSuccess();
		}

		/// <summary>
		/// Transfers ownership of the named roleplay to the specified user.
		/// </summary>
		/// <param name="db">The database where the roleplays are stored.</param>
		/// <param name="newOwner">The new owner.</param>
		/// <param name="roleplay">The roleplay to transfer.</param>
		/// <returns>An execution result which may or may not have succeeded.</returns>
		public async Task<ExecuteResult> TransferRoleplayOwnershipAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser newOwner,
			[NotNull] Roleplay roleplay
		)
		{
			if (roleplay.Owner.DiscordID == newOwner.Id)
			{
				return ExecuteResult.FromError(CommandError.Unsuccessful, "That person already owns the roleplay.");
			}

			if (GetUserRoleplays(db, newOwner).Any(rp => rp.Name.Equals(roleplay.Name, StringComparison.OrdinalIgnoreCase)))
			{
				return ExecuteResult.FromError(CommandError.MultipleMatches, $"That user already owns a roleplay named {roleplay.Name}. Please rename it first.");
			}

			var newUser = await db.GetOrRegisterUserAsync(newOwner);
			roleplay.Owner = newUser;

			await db.SaveChangesAsync();

			return ExecuteResult.FromSuccess();
		}
	}
}
