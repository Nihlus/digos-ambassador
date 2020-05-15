//
//  CharacterService.cs
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

using System;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Plugins.Characters.Extensions;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.Results;
using Image = DIGOS.Ambassador.Plugins.Characters.Model.Data.Image;

namespace DIGOS.Ambassador.Plugins.Characters.Services
{
    /// <summary>
    /// Acts as an interface for accessing and modifying user characters.
    /// </summary>
    [PublicAPI]
    public sealed class CharacterService
    {
        private readonly CharactersDatabaseContext _database;

        private readonly ServerService _servers;

        private readonly CommandService _commands;

        private readonly OwnedEntityService _ownedEntities;

        private readonly ContentService _content;

        private readonly UserService _users;

        private readonly PronounService _pronouns;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterService"/> class.
        /// </summary>
        /// <param name="commands">The application's command service.</param>
        /// <param name="entityService">The application's owned entity service.</param>
        /// <param name="content">The content service.</param>
        /// <param name="users">The user service.</param>
        /// <param name="servers">The server service.</param>
        /// <param name="database">The core database.</param>
        /// <param name="pronouns">The pronoun service.</param>
        public CharacterService
        (
            CommandService commands,
            OwnedEntityService entityService,
            ContentService content,
            UserService users,
            ServerService servers,
            CharactersDatabaseContext database,
            PronounService pronouns
        )
        {
            _commands = commands;
            _ownedEntities = entityService;
            _content = content;
            _users = users;
            _servers = servers;
            _database = database;
            _pronouns = pronouns;
        }

        /// <summary>
        /// Deletes the given character.
        /// </summary>
        /// <param name="character">The character to delete.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<DeleteEntityResult> DeleteCharacterAsync(Character character)
        {
            _database.Characters.Remove(character);
            await _database.SaveChangesAsync();

            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// This method searches for the best matching character given an owner and a name. If no owner is provided, then
        /// the global list is searched for a unique name. If no match can be found, a failed result is returned.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="characterOwner">The owner of the character, if any.</param>
        /// <param name="characterName">The name of the character.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, ItemNotNull]
        public async Task<RetrieveEntityResult<Character>> GetBestMatchingCharacterAsync
        (
            ICommandContext context,
            User? characterOwner,
            string? characterName
        )
        {
            var getInvokerResult = await _users.GetOrRegisterUserAsync(context.User);
            if (!getInvokerResult.IsSuccess)
            {
                return RetrieveEntityResult<Character>.FromError(getInvokerResult);
            }

            var invoker = getInvokerResult.Entity;

            if (characterOwner is null && characterName is null)
            {
                return await GetCurrentCharacterAsync(context, invoker);
            }

            if (characterOwner is null)
            {
                // Search the invoker's characters first
                var invokerCharacterResult = await GetUserCharacterByNameAsync
                (
                    context,
                    invoker,
                    characterName!
                );

                if (invokerCharacterResult.IsSuccess)
                {
                    return invokerCharacterResult;
                }

                return await GetNamedCharacterAsync(characterName!, context.Guild);
            }

            if (characterName.IsNullOrWhitespace())
            {
                return await GetCurrentCharacterAsync(context, characterOwner);
            }

            return await GetUserCharacterByNameAsync(context, characterOwner, characterName);
        }

        /// <summary>
        /// Gets the current character a user has assumed the form of.
        /// </summary>
        /// <param name="context">The context of the user.</param>
        /// <param name="discordUser">The user to get the current character of.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, ItemNotNull]
        public async Task<RetrieveEntityResult<Character>> GetCurrentCharacterAsync
        (
            ICommandContext context,
            User discordUser
        )
        {
            if (!await HasActiveCharacterOnServerAsync(discordUser, context.Guild))
            {
                var isCurrentUser = context.Message.Author.Id == (ulong)discordUser.DiscordID;
                var errorMessage = isCurrentUser
                    ? "You haven't assumed a character."
                    : "The user hasn't assumed a character.";

                return RetrieveEntityResult<Character>.FromError(errorMessage);
            }

            var currentCharacter = await GetUserCharacters(discordUser, context.Guild)
            .FirstOrDefaultAsync
            (
                ch => ch.IsCurrent
            );

            if (currentCharacter is null)
            {
                return RetrieveEntityResult<Character>.FromError("Failed to retrieve a current character.");
            }

            return RetrieveEntityResult<Character>.FromSuccess(currentCharacter);
        }

        /// <summary>
        /// Gets a character by its given name.
        /// </summary>
        /// <param name="characterName">The name of the character.</param>
        /// <param name="guild">The guild that the character is on.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, ItemNotNull]
        public async Task<RetrieveEntityResult<Character>> GetNamedCharacterAsync
        (
            string characterName,
            IGuild guild
        )
        {
            var guildCharacters = _database.Characters.AsQueryable().Where(ch => ch.Server.DiscordID == (long)guild.Id);
            if (await guildCharacters.CountAsync(ch => string.Equals(ch.Name.ToLower(), characterName.ToLower())) > 1)
            {
                return RetrieveEntityResult<Character>.FromError
                (
                    "There's more than one character with that name. Please specify which user it belongs to."
                );
            }

            var character = GetCharacters(guild)
                .FirstOrDefault(ch => string.Equals(ch.Name.ToLower(), characterName.ToLower()));

            if (character is null)
            {
                return RetrieveEntityResult<Character>.FromError("No character with that name found.");
            }

            return RetrieveEntityResult<Character>.FromSuccess(character);
        }

        /// <summary>
        /// Gets the characters in the database along with their navigation properties.
        /// </summary>
        /// <param name="guild">The guild where the characters are.</param>
        /// <returns>A queryable set of characters.</returns>
        [Pure, ItemNotNull]
        public IQueryable<Character> GetCharacters(IGuild guild)
        {
            return _database.Characters.AsQueryable()
                .Where(c => c.Server.DiscordID == (long)guild.Id)
                .Include(c => c.Owner);
        }

        /// <summary>
        /// Gets a character belonging to a given user by a given name.
        /// </summary>
        /// <param name="context">The context of the user.</param>
        /// <param name="characterOwner">The user to get the character from.</param>
        /// <param name="characterName">The name of the character.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, ItemNotNull]
        public async Task<RetrieveEntityResult<Character>> GetUserCharacterByNameAsync
        (
            ICommandContext context,
            User characterOwner,
            string characterName
        )
        {
            var character = await GetUserCharacters(characterOwner, context.Guild)
            .FirstOrDefaultAsync
            (
                ch => string.Equals(ch.Name.ToLower(), characterName.ToLower())
            );

            if (!(character is null))
            {
                return RetrieveEntityResult<Character>.FromSuccess(character);
            }

            var isCurrentUser = context.Message.Author.Id == (ulong)characterOwner.DiscordID;
            var errorMessage = isCurrentUser
                ? "You don't own a character with that name."
                : "The user doesn't own a character with that name.";

            return RetrieveEntityResult<Character>.FromError(errorMessage);
        }

        /// <summary>
        /// Makes the given character current on the given server.
        /// </summary>
        /// <param name="context">The context of the user.</param>
        /// <param name="discordServer">The server to make the character current on.</param>
        /// <param name="character">The character to make current.</param>
        /// <returns>A task that must be awaited.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> MakeCharacterCurrentOnServerAsync
        (
            ICommandContext context,
            IGuild discordServer,
            Character character
        )
        {
            var getInvokerResult = await _users.GetOrRegisterUserAsync(context.User);
            if (!getInvokerResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getInvokerResult);
            }

            var user = getInvokerResult.Entity;

            if (character.IsCurrent)
            {
                return ModifyEntityResult.FromError("The character is already current on the server.");
            }

            await ClearCurrentCharacterOnServerAsync(user, discordServer);

            character.IsCurrent = true;

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Clears any current characters in the server from the given user.
        /// </summary>
        /// <param name="discordUser">The user to clear the characters from.</param>
        /// <param name="discordServer">The server to clear the characters on.</param>
        /// <returns>A task that must be awaited.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> ClearCurrentCharacterOnServerAsync
        (
            User discordUser,
            IGuild discordServer
        )
        {
            if (!await HasActiveCharacterOnServerAsync(discordUser, discordServer))
            {
                return ModifyEntityResult.FromError("There's no current character on this server.");
            }

            var currentCharactersOnServer = GetUserCharacters(discordUser, discordServer).Where(ch => ch.IsCurrent);

            await currentCharactersOnServer.ForEachAsync
            (
                ch => ch.IsCurrent = false
            );

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Determines whether or not the given user has an active character on the given server.
        /// </summary>
        /// <param name="discordUser">The user to check.</param>
        /// <param name="discordServer">The server to check.</param>
        /// <returns>true if the user has an active character on the server; otherwise, false.</returns>
        [Pure]
        public async Task<bool> HasActiveCharacterOnServerAsync
        (
            User discordUser,
            IGuild discordServer
        )
        {
            var userCharacters = GetUserCharacters(discordUser, discordServer);

            return await userCharacters
            .AnyAsync
            (
                c => c.IsCurrent
            );
        }

        /// <summary>
        /// Creates a character with the given name and default settings.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <param name="characterName">The name of the character.</param>
        /// <returns>A creation result which may or may not have been successful.</returns>
        [ItemNotNull]
        public async Task<CreateEntityResult<Character>> CreateCharacterAsync
        (
            ICommandContext context,
            string characterName
        )
        {
            return await CreateCharacterAsync
            (
                context,
                characterName,
                _content.GetDefaultAvatarUri().ToString(),
                null,
                null,
                null
            );
        }

        /// <summary>
        /// Creates a character with the given parameters.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <param name="characterName">The name of the character.</param>
        /// <param name="characterAvatarUrl">The character's avatar url.</param>
        /// <param name="characterNickname">The nickname that should be applied to the user when the character is active.</param>
        /// <param name="characterSummary">The summary of the character.</param>
        /// <param name="characterDescription">The full description of the character.</param>
        /// <returns>A creation result which may or may not have been successful.</returns>
        [ItemNotNull]
        public async Task<CreateEntityResult<Character>> CreateCharacterAsync
        (
            ICommandContext context,
            string characterName,
            string characterAvatarUrl,
            string? characterNickname,
            string? characterSummary,
            string? characterDescription
        )
        {
            // Default the nickname to the character name
            characterNickname ??= characterName;

            var getOwnerResult = await _users.GetOrRegisterUserAsync(context.Message.Author);
            if (!getOwnerResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(getOwnerResult);
            }

            var owner = getOwnerResult.Entity;

            var getServer = await _servers.GetOrRegisterServerAsync(context.Guild);
            if (!getServer.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(getServer);
            }

            var server = getServer.Entity;

            // Use a dummy name here, because we'll set it using the service afterwards
            var character = new Character(server, owner, string.Empty);

            var modifyEntityResult = await SetCharacterNameAsync(context, character, characterName);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            modifyEntityResult = await SetCharacterAvatarAsync(character, characterAvatarUrl);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            modifyEntityResult = await SetCharacterNicknameAsync(character, characterNickname);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            if (!(characterSummary is null))
            {
                modifyEntityResult = await SetCharacterSummaryAsync(character, characterSummary);
                if (!modifyEntityResult.IsSuccess)
                {
                    return CreateEntityResult<Character>.FromError(modifyEntityResult);
                }
            }

            if (!(characterDescription is null))
            {
                modifyEntityResult = await SetCharacterDescriptionAsync(character, characterDescription);
                if (!modifyEntityResult.IsSuccess)
                {
                    return CreateEntityResult<Character>.FromError(modifyEntityResult);
                }
            }

            _database.Characters.Update(character);
            await _database.SaveChangesAsync();

            return CreateEntityResult<Character>.FromSuccess(character);
        }

        /// <summary>
        /// Sets the default character of a user.
        /// </summary>
        /// <param name="context">The context of the operation.</param>
        /// <param name="newDefaultCharacter">The new default character.</param>
        /// <param name="targetUser">The user to set the default character of.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetDefaultCharacterForUserAsync
        (
            ICommandContext context,
            Character newDefaultCharacter,
            User targetUser
        )
        {
            var getDefaultCharacterResult = await GetDefaultCharacterAsync(targetUser, context.Guild);
            if (getDefaultCharacterResult.IsSuccess)
            {
                var currentDefault = getDefaultCharacterResult.Entity;
                if (currentDefault == newDefaultCharacter)
                {
                    var isCurrentUser = context.Message.Author.Id == (ulong)newDefaultCharacter.Owner.DiscordID;

                    var errorMessage = isCurrentUser
                        ? "That's already your default character."
                        : "That's already the user's default character.";

                    return ModifyEntityResult.FromError(errorMessage);
                }

                currentDefault.IsDefault = false;
            }

            newDefaultCharacter.IsDefault = true;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Clears the default character from the given user.
        /// </summary>
        /// <param name="context">The context of the operation.</param>
        /// <param name="targetUser">The user to clear the default character of.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> ClearDefaultCharacterForUserAsync
        (
            ICommandContext context,
            User targetUser
        )
        {
            var getDefaultCharacterResult = await GetDefaultCharacterAsync(targetUser, context.Guild);
            if (!getDefaultCharacterResult.IsSuccess)
            {
                var isCurrentUser = context.Message.Author.Id == (ulong)targetUser.DiscordID;
                var errorMessage = isCurrentUser
                    ? "You don't have a default character."
                    : "That user doesn't have a default character.";

                return ModifyEntityResult.FromError(errorMessage);
            }

            getDefaultCharacterResult.Entity.IsDefault = false;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the name of the given character.
        /// </summary>
        /// <param name="context">The context of the operation.</param>
        /// <param name="character">The character to set the name of.</param>
        /// <param name="newCharacterName">The new name.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetCharacterNameAsync
        (
            ICommandContext context,
            Character character,
            string newCharacterName
        )
        {
            if (string.IsNullOrWhiteSpace(newCharacterName))
            {
                return ModifyEntityResult.FromError("You need to provide a name.");
            }

            if (string.Equals(character.Name, newCharacterName, StringComparison.OrdinalIgnoreCase))
            {
                return ModifyEntityResult.FromError("The character already has that name.");
            }

            if (newCharacterName.Contains("\""))
            {
                return ModifyEntityResult.FromError("The name may not contain double quotes.");
            }

            var isCurrentUser = context.Message.Author.Id == (ulong)character.Owner.DiscordID;
            if (!await IsCharacterNameUniqueForUserAsync(character.Owner, newCharacterName, context.Guild))
            {
                var errorMessage = isCurrentUser
                    ? "You already have a character with that name."
                    : "The user already has a character with that name.";

                return ModifyEntityResult.FromError(errorMessage);
            }

            var commandModule = _commands.Modules.FirstOrDefault(m => m.Name == "character");
            if (!(commandModule is null))
            {
                var validNameResult = _ownedEntities.IsEntityNameValid(commandModule.GetAllCommandNames(), newCharacterName);
                if (!validNameResult.IsSuccess)
                {
                    return ModifyEntityResult.FromError(validNameResult);
                }
            }

            character.Name = newCharacterName;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the avatar of the given character.
        /// </summary>
        /// <param name="character">The character to set the avatar of.</param>
        /// <param name="newCharacterAvatarUrl">The new avatar.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetCharacterAvatarAsync
        (
            Character character,
            string newCharacterAvatarUrl
        )
        {
            if (string.IsNullOrWhiteSpace(newCharacterAvatarUrl))
            {
                return ModifyEntityResult.FromError("You need to provide a new avatar url.");
            }

            if (!Uri.TryCreate(newCharacterAvatarUrl, UriKind.Absolute, out _))
            {
                return ModifyEntityResult.FromError("The given image URL wasn't valid.");
            }

            if (character.AvatarUrl == newCharacterAvatarUrl)
            {
                return ModifyEntityResult.FromError("The character's avatar is already set to that URL.");
            }

            character.AvatarUrl = newCharacterAvatarUrl;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the nickname of the given character.
        /// </summary>
        /// <param name="character">The character to set the nickname of.</param>
        /// <param name="newCharacterNickname">The new nickname.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetCharacterNicknameAsync
        (
            Character character,
            string newCharacterNickname
        )
        {
            if (string.IsNullOrWhiteSpace(newCharacterNickname))
            {
                return ModifyEntityResult.FromError("You need to provide a new nickname.");
            }

            if (character.Nickname == newCharacterNickname)
            {
                return ModifyEntityResult.FromError("The character already has that nickname.");
            }

            if (newCharacterNickname.Length > 32)
            {
                return ModifyEntityResult.FromError("The nickname is too long. It can be at most 32 characters.");
            }

            character.Nickname = newCharacterNickname;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the summary of the given character.
        /// </summary>
        /// <param name="character">The character to set the summary of.</param>
        /// <param name="newCharacterSummary">The new summary.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetCharacterSummaryAsync
        (
            Character character,
            string newCharacterSummary
        )
        {
            if (string.IsNullOrWhiteSpace(newCharacterSummary))
            {
                return ModifyEntityResult.FromError("You need to provide a new summary.");
            }

            if (character.Summary == newCharacterSummary)
            {
                return ModifyEntityResult.FromError("That's already the character's summary.");
            }

            if (newCharacterSummary.Length > 240)
            {
                return ModifyEntityResult.FromError("The summary is too long. It can be at most 240 characters.");
            }

            character.Summary = newCharacterSummary;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the description of the given character.
        /// </summary>
        /// <param name="character">The character to set the description of.</param>
        /// <param name="newCharacterDescription">The new description.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetCharacterDescriptionAsync
        (
            Character character,
            string newCharacterDescription
        )
        {
            if (string.IsNullOrWhiteSpace(newCharacterDescription))
            {
                return ModifyEntityResult.FromError("You need to provide a new description.");
            }

            if (character.Description == newCharacterDescription)
            {
                return ModifyEntityResult.FromError("The character already has that description.");
            }

            if (newCharacterDescription.Length > 1000)
            {
                return ModifyEntityResult.FromError("The description is too long. It can be at most 1000 characters.");
            }
            character.Description = newCharacterDescription;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the preferred pronoun for the given character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="pronounFamily">The pronoun family.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetCharacterPronounAsync
        (
            Character character,
            string pronounFamily
        )
        {
            if (pronounFamily.IsNullOrWhitespace())
            {
                return ModifyEntityResult.FromError("You need to provide a pronoun family.");
            }

            if (character.PronounProviderFamily == pronounFamily)
            {
                return ModifyEntityResult.FromError("The character is already using that pronoun set.");
            }

            var getPronounProviderResult = _pronouns.GetPronounProvider(pronounFamily);
            if (!getPronounProviderResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getPronounProviderResult);
            }

            var pronounProvider = getPronounProviderResult.Entity;
            character.PronounProviderFamily = pronounProvider.Family;

            await _database.SaveChangesAsync();
            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets whether or not a character is NSFW.
        /// </summary>
        /// <param name="character">The character to edit.</param>
        /// <param name="isNSFW">Whether or not the character is NSFW.</param>
        /// <returns>A task that must be awaited.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetCharacterIsNSFWAsync
        (
            Character character,
            bool isNSFW
        )
        {
            if (character.IsNSFW == isNSFW)
            {
                var message = character.IsNSFW
                    ? "The character is already NSFW."
                    : "The character is already SFW.";

                return ModifyEntityResult.FromError(message);
            }

            character.IsNSFW = isNSFW;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Transfers ownership of the named character to the specified user.
        /// </summary>
        /// <param name="newOwner">The new owner.</param>
        /// <param name="character">The character to transfer.</param>
        /// <param name="guild">The guild to scope the character search to.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> TransferCharacterOwnershipAsync
        (
            User newOwner,
            Character character,
            IGuild guild
        )
        {
            var newOwnerCharacters = GetUserCharacters(newOwner, guild);
            return await _ownedEntities.TransferEntityOwnershipAsync
            (
                _database,
                newOwner,
                newOwnerCharacters,
                character
            );
        }

        /// <summary>
        /// Get the characters owned by the given user.
        /// </summary>
        /// <param name="discordUser">The user to get the characters of.</param>
        /// <param name="guild">The guild to get the user's characters on.</param>
        /// <returns>A queryable list of characters belonging to the user.</returns>
        [Pure, ItemNotNull]
        public IQueryable<Character> GetUserCharacters
        (
            User discordUser,
            IGuild guild
        )
        {
            var characters = GetCharacters(guild).Where(ch => ch.Owner.DiscordID == discordUser.DiscordID);
            return characters;
        }

        /// <summary>
        /// Determines whether or not the given character name is unique for a given user.
        /// </summary>
        /// <param name="discordUser">The user to check.</param>
        /// <param name="characterName">The character name to check.</param>
        /// <param name="guild">The guild to scope the character search to.</param>
        /// <returns>true if the name is unique; otherwise, false.</returns>
        [Pure]
        public async Task<bool> IsCharacterNameUniqueForUserAsync
        (
            User discordUser,
            string characterName,
            IGuild guild
        )
        {
            var userCharacters = GetUserCharacters(discordUser, guild);
            return await _ownedEntities.IsEntityNameUniqueForUserAsync(userCharacters, characterName);
        }

        /// <summary>
        /// Adds the given image with the given metadata to the given character.
        /// </summary>
        /// <param name="character">The character to add the image to.</param>
        /// <param name="imageName">The name of the image.</param>
        /// <param name="imageUrl">The url of the image.</param>
        /// <param name="imageCaption">The caption of the image.</param>
        /// <param name="isNSFW">Whether or not the image is NSFW.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> AddImageToCharacterAsync
        (
            Character character,
            string imageName,
            string imageUrl,
            string? imageCaption = null,
            bool isNSFW = false
        )
        {
            var isImageNameUnique = !character.Images.Any(i => string.Equals(i.Name.ToLower(), imageName.ToLower()));
            if (!isImageNameUnique)
            {
                return ModifyEntityResult.FromError("The character already has an image with that name.");
            }

            if (imageName.IsNullOrWhitespace())
            {
                return ModifyEntityResult.FromError("You need to specify a name.");
            }

            if (imageCaption.IsNullOrWhitespace())
            {
                imageCaption = "No caption set.";
            }

            if (!Uri.IsWellFormedUriString(imageUrl, UriKind.RelativeOrAbsolute))
            {
                return ModifyEntityResult.FromError
                (
                    $"That URL doesn't look valid. Please check \"{imageUrl}\" for errors."
                );
            }

            var image = new Image(imageName, imageUrl, imageCaption)
            {
                IsNSFW = isNSFW
            };

            character.Images.Add(image);
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Removes the named image from the given character.
        /// </summary>
        /// <param name="character">The character to remove the image from.</param>
        /// <param name="imageName">The name of the image.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> RemoveImageFromCharacterAsync
        (
            Character character,
            string imageName
        )
        {
            var hasNamedImage = character.Images.Any(i => string.Equals(i.Name.ToLower(), imageName.ToLower()));
            if (!hasNamedImage)
            {
                return ModifyEntityResult.FromError("The character has no image with that name.");
            }

            var deletedImages = character.Images
                .Where(i => string.Equals(i.Name.ToLower(), imageName.ToLower()))
                .ToList();

            character.Images = character.Images.Except(deletedImages).ToList();
            foreach (var deletedImage in deletedImages)
            {
                _database.Remove(deletedImage);
            }

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Creates a new character role from the given Discord role and access condition.
        /// </summary>
        /// <param name="role">The discord role.</param>
        /// <param name="access">The access conditions.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<CreateEntityResult<CharacterRole>> CreateCharacterRoleAsync
        (
            IRole role,
            RoleAccess access
        )
        {
            var getExistingRoleResult = await GetCharacterRoleAsync(role);
            if (getExistingRoleResult.IsSuccess)
            {
                return CreateEntityResult<CharacterRole>.FromError
                (
                    "That role is already registered as a character role."
                );
            }

            var getServerResult = await _servers.GetOrRegisterServerAsync(role.Guild);
            if (!getServerResult.IsSuccess)
            {
                return CreateEntityResult<CharacterRole>.FromError(getServerResult);
            }

            var server = getServerResult.Entity;

            var characterRole = new CharacterRole(server, (long)role.Id, access);

            _database.CharacterRoles.Update(characterRole);
            await _database.SaveChangesAsync();

            return CreateEntityResult<CharacterRole>.FromSuccess(characterRole);
        }

        /// <summary>
        /// Deletes the character role for the given Discord role.
        /// </summary>
        /// <param name="role">The character role.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<DeleteEntityResult> DeleteCharacterRoleAsync
        (
            CharacterRole role
        )
        {
            _database.CharacterRoles.Remove(role);
            await _database.SaveChangesAsync();

            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets an existing character role from the database.
        /// </summary>
        /// <param name="role">The discord role.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<RetrieveEntityResult<CharacterRole>> GetCharacterRoleAsync
        (
            IRole role
        )
        {
            var characterRole = await _database.CharacterRoles.AsQueryable()
                .FirstOrDefaultAsync(r => r.Server.DiscordID == (long)role.Guild.Id && r.DiscordID == (long)role.Id);

            if (characterRole is null)
            {
                return RetrieveEntityResult<CharacterRole>.FromError
                (
                    "That role is not registered as a character role."
                );
            }

            return RetrieveEntityResult<CharacterRole>.FromSuccess(characterRole);
        }

        /// <summary>
        /// Gets the roles available on the given server.
        /// </summary>
        /// <param name="guild">The Discord guild.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<RetrieveEntityResult<IQueryable<CharacterRole>>> GetCharacterRolesAsync
        (
            IGuild guild
        )
        {
            var getServerResult = await _servers.GetOrRegisterServerAsync(guild);
            if (!getServerResult.IsSuccess)
            {
                return RetrieveEntityResult<IQueryable<CharacterRole>>.FromError(getServerResult);
            }

            var server = getServerResult.Entity;

            var roles = _database.CharacterRoles.AsQueryable().Where(r => r.Server == server);

            return RetrieveEntityResult<IQueryable<CharacterRole>>.FromSuccess(roles);
        }

        /// <summary>
        /// Sets the access conditions for the given character role.
        /// </summary>
        /// <param name="role">The character role.</param>
        /// <param name="access">The access conditions.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetCharacterRoleAccessAsync
        (
            CharacterRole role,
            RoleAccess access
        )
        {
            if (role.Access == access)
            {
                return ModifyEntityResult.FromError
                (
                    "The role already has those access conditions."
                );
            }

            role.Access = access;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the custom role of a character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="characterRole">The role to set.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetCharacterRoleAsync
        (
            Character character,
            CharacterRole characterRole
        )
        {
            if (character.Role == characterRole)
            {
                return ModifyEntityResult.FromError
                (
                    "The character already has that role."
                );
            }

            character.Role = characterRole;

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Clears the custom role of a character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> ClearCharacterRoleAsync
        (
            Character character
        )
        {
            if (character.Role is null)
            {
                return ModifyEntityResult.FromError
                (
                    "The character doesn't have a role set."
                );
            }

            character.Role = null;

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Retrieves the given user's default character.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="guild">The server the user is on.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<RetrieveEntityResult<Character>> GetDefaultCharacterAsync
        (
            User user,
            IGuild guild
        )
        {
            var userCharacters = GetUserCharacters(user, guild);
            var defaultCharacter = await userCharacters
                .FirstOrDefaultAsync(c => c.IsDefault);

            if (defaultCharacter is null)
            {
                return RetrieveEntityResult<Character>.FromError("The user doesn't have a default character.");
            }

            return RetrieveEntityResult<Character>.FromSuccess(defaultCharacter);
        }
    }
}
