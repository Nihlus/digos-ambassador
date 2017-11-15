//
//  CharacterService.cs
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
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Services.Entity;

using Discord;
using Discord.Commands;

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Services.Characters
{
	/// <summary>
	/// Acts as an interface for accessing and modifying user characters.
	/// </summary>
	public class CharacterService
	{
		private readonly CommandService Commands;

		private readonly OwnedEntityService OwnedEntities;

		/// <summary>
		/// Initializes a new instance of the <see cref="CharacterService"/> class.
		/// </summary>
		/// <param name="commands">The application's command service.</param>
		/// <param name="entityService">The application's owned entity service.</param>
		public CharacterService(CommandService commands, OwnedEntityService entityService)
		{
			this.Commands = commands;
			this.OwnedEntities = entityService;
		}

		/// <summary>
		/// Gets a character belonging to a given user by a given name.
		/// </summary>
		/// <param name="db">The database where the characters are stored.</param>
		/// <param name="context">The context of the user.</param>
		/// <param name="characterOwner">The user to get the character from.</param>
		/// <param name="characterName">The name of the character.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public async Task<RetrieveEntityResult<Character>> GetUserCharacterByNameAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] SocketCommandContext context,
			[NotNull] IUser characterOwner,
			[NotNull] string characterName
		)
		{
			var character = await GetUserCharacters(db, characterOwner)
			.FirstOrDefaultAsync
			(
				ch => ch.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase)
			);

			if (character is null)
			{
				var isCurrentUser = context.Message.Author.Id == characterOwner.Id;
				var errorMessage = isCurrentUser
					? "You don't own a character with that name."
					: "The user doesn't own a character with that name.";

				return RetrieveEntityResult<Character>.FromError(CommandError.ObjectNotFound, errorMessage);
			}

			return RetrieveEntityResult<Character>.FromSuccess(character);
		}

		/// <summary>
		/// Creates a character with the given parameters.
		/// </summary>
		/// <param name="db">The database where the characters are stored.</param>
		/// <param name="context">The context of the command.</param>
		/// <param name="characterName">The name of the character.</param>
		/// <param name="characterAvatarUrl">The character's avatar url.</param>
		/// <param name="characterNickname">The nicknme that should be applied to the user when the character is active.</param>
		/// <param name="characterSummary">The summary of the character.</param>
		/// <param name="characterDescription">The full description of the character.</param>
		/// <returns>A creation result which may or may not have been successful.</returns>
		public async Task<CreateEntityResult<Character>> CreateCharacterAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] SocketCommandContext context,
			[NotNull] string characterName,
			[NotNull] string characterAvatarUrl,
			[NotNull] string characterNickname,
			[NotNull] string characterSummary,
			[NotNull] string characterDescription
		)
		{
			var owner = await db.GetOrRegisterUserAsync(context.Message.Author);
			var character = new Character
			{
				Owner = owner,
			};

			var modifyEntityResult = await SetCharacterNameAsync(db, context, character, characterName);
			if (!modifyEntityResult.IsSuccess)
			{
				return CreateEntityResult<Character>.FromError(modifyEntityResult);
			}

			modifyEntityResult = await SetCharacterAvatarAsync(db, character, characterAvatarUrl);
			if (!modifyEntityResult.IsSuccess)
			{
				return CreateEntityResult<Character>.FromError(modifyEntityResult);
			}

			modifyEntityResult = await SetCharacterNicknameAsync(db, character, characterNickname);
			if (!modifyEntityResult.IsSuccess)
			{
				return CreateEntityResult<Character>.FromError(modifyEntityResult);
			}

			modifyEntityResult = await SetCharacterSummaryAsync(db, character, characterSummary);
			if (!modifyEntityResult.IsSuccess)
			{
				return CreateEntityResult<Character>.FromError(modifyEntityResult);
			}

			modifyEntityResult = await SetCharacterDescriptionAsync(db, character, characterDescription);
			if (!modifyEntityResult.IsSuccess)
			{
				return CreateEntityResult<Character>.FromError(modifyEntityResult);
			}

			await db.Characters.AddAsync(character);
			await db.SaveChangesAsync();

			var getCharacterResult = await GetUserCharacterByNameAsync(db, context, context.Message.Author, characterName);
			if (!getCharacterResult.IsSuccess)
			{
				return CreateEntityResult<Character>.FromError(getCharacterResult);
			}

			return CreateEntityResult<Character>.FromSuccess(getCharacterResult.Entity);
		}

		/// <summary>
		/// Sets the name of the given character.
		/// </summary>
		/// <param name="db">The database containing the characters.</param>
		/// <param name="context">The context of the operation.</param>
		/// <param name="character">The character to set the name of.</param>
		/// <param name="newCharacterName">The new name.</param>
		/// <returns>A modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetCharacterNameAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] SocketCommandContext context,
			[NotNull] Character character,
			[NotNull] string newCharacterName
		)
		{
			var isCurrentUser = context.Message.Author.Id == character.Owner.DiscordID;
			if (string.IsNullOrWhiteSpace(newCharacterName))
			{
				return ModifyEntityResult.FromError(CommandError.BadArgCount, "You need to provide a name.");
			}

			if (!await IsCharacterNameUniqueForUserAsync(db, context.Message.Author, newCharacterName))
			{
				var errorMessage = isCurrentUser
					? "You already have a character with that name."
					: "The user already has a character with that name.";

				return ModifyEntityResult.FromError(CommandError.MultipleMatches, errorMessage);
			}

			var commandModule = this.Commands.Modules.First(m => m.Name == "character");
			if (!this.OwnedEntities.IsEntityNameValid(commandModule, newCharacterName))
			{
				return ModifyEntityResult.FromError(CommandError.UnmetPrecondition, "The given name is not valid.");
			}

			character.Name = newCharacterName;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Sets the avatar of the given character.
		/// </summary>
		/// <param name="db">The database containing the characters.</param>
		/// <param name="character">The character to set the avatar of.</param>
		/// <param name="newCharacterAvatarUrl">The new avatar.</param>
		/// <returns>A modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetCharacterAvatarAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] Character character,
			[NotNull] string newCharacterAvatarUrl
		)
		{
			if (string.IsNullOrWhiteSpace(newCharacterAvatarUrl))
			{
				return ModifyEntityResult.FromError(CommandError.BadArgCount, "You need to provide a new nickname.");
			}

			character.AvatarUrl = newCharacterAvatarUrl;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Sets the nickname of the given character.
		/// </summary>
		/// <param name="db">The database containing the characters.</param>
		/// <param name="character">The character to set the nickname of.</param>
		/// <param name="newCharacterNickname">The new nickname.</param>
		/// <returns>A modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetCharacterNicknameAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] Character character,
			[NotNull] string newCharacterNickname
		)
		{
			if (string.IsNullOrWhiteSpace(newCharacterNickname))
			{
				return ModifyEntityResult.FromError(CommandError.BadArgCount, "You need to provide a new nickname.");
			}

			if (newCharacterNickname.Length > 32)
			{
				return ModifyEntityResult.FromError(CommandError.Unsuccessful, "The summary is too long. Nicknames can be at most 32 characters.");
			}

			character.Nickname = newCharacterNickname;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Sets the summary of the given character.
		/// </summary>
		/// <param name="db">The database containing the characters.</param>
		/// <param name="character">The character to set the summary of.</param>
		/// <param name="newCharacterSummary">The new summary.</param>
		/// <returns>A modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetCharacterSummaryAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] Character character,
			[NotNull] string newCharacterSummary
		)
		{
			if (string.IsNullOrWhiteSpace(newCharacterSummary))
			{
				return ModifyEntityResult.FromError(CommandError.BadArgCount, "You need to provide a new summary.");
			}

			if (newCharacterSummary.Length > 240)
			{
				return ModifyEntityResult.FromError(CommandError.Unsuccessful, "The summary is too long.");
			}

			character.Summary = newCharacterSummary;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Sets the description of the given character.
		/// </summary>
		/// <param name="db">The database containing the characters.</param>
		/// <param name="character">The character to set the description of.</param>
		/// <param name="newCharacterDescription">The new description.</param>
		/// <returns>A modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetCharacterDescriptionAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] Character character,
			[NotNull] string newCharacterDescription
		)
		{
			if (string.IsNullOrWhiteSpace(newCharacterDescription))
			{
				return ModifyEntityResult.FromError(CommandError.BadArgCount, "You need to provide a new description.");
			}

			character.Description = newCharacterDescription;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Transfers ownership of the named character to the specified user.
		/// </summary>
		/// <param name="db">The database where the characters are stored.</param>
		/// <param name="newOwner">The new owner.</param>
		/// <param name="character">The character to transfer.</param>
		/// <returns>An execution result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> TransferCharacterOwnershipAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser newOwner,
			[NotNull] Character character
		)
		{
			var newOwnerCharacters = GetUserCharacters(db, newOwner);
			return await this.OwnedEntities.TransferEntityOwnershipAsync
			(
				db,
				newOwner,
				newOwnerCharacters,
				character
			);
		}

		/// <summary>
		/// Get the characters owned by the given user.
		/// </summary>
		/// <param name="db">The database where the characters are stored.</param>
		/// <param name="discordUser">The user to get the characters of.</param>
		/// <returns>A queryable list of characters belonging to the user.</returns>
		[NotNull]
		[ItemNotNull]
		public IQueryable<Character> GetUserCharacters([NotNull]GlobalInfoContext db, [NotNull]IUser discordUser)
		{
			return db.Characters
				.Include(ch => ch.Owner)
				.Include(ch => ch.DefaultAppearance)
				.Include(ch => ch.TransformedAppearance)
				.Where(ch => ch.Owner.DiscordID == discordUser.Id);
		}

		/// <summary>
		/// Determines whether or not the given character name is unique for a given user.
		/// </summary>
		/// <param name="db">The database where the characters are stored.</param>
		/// <param name="discordUser">The user to check.</param>
		/// <param name="characterName">The character name to check.</param>
		/// <returns>true if the name is unique; otherwise, false.</returns>
		public async Task<bool> IsCharacterNameUniqueForUserAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser discordUser,
			[NotNull] string characterName
		)
		{
			var userCharacters = GetUserCharacters(db, discordUser);
			return await this.OwnedEntities.IsEntityNameUniqueForUserAsync(userCharacters, characterName);
		}
	}
}
