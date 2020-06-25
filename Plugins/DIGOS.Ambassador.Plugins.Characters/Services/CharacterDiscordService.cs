//
//  CharacterDiscordService.cs
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

using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Core.Services.TransientState;
using DIGOS.Ambassador.Discord;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

using Image = DIGOS.Ambassador.Plugins.Characters.Model.Data.Image;

namespace DIGOS.Ambassador.Plugins.Characters.Services
{
    /// <summary>
    /// Handles Discord-facing business logic.
    /// </summary>
    public class CharacterDiscordService : AbstractTransientStateService
    {
        private readonly CharacterService _characters;
        private readonly CharacterRoleService _characterRoles;
        private readonly CommandService _commands;
        private readonly OwnedEntityService _ownedEntities;
        private readonly UserService _users;
        private readonly ServerService _servers;
        private readonly DiscordService _discord;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterDiscordService"/> class.
        /// </summary>
        /// <param name="characters">The character service.</param>
        /// <param name="users">The user service.</param>
        /// <param name="servers">The server service.</param>
        /// <param name="commands">The command service.</param>
        /// <param name="ownedEntities">The owned entity service.</param>
        /// <param name="discord">The Discord service.</param>
        /// <param name="characterRoles">The character role service.</param>
        public CharacterDiscordService
        (
            CharacterService characters,
            UserService users,
            ServerService servers,
            CommandService commands,
            OwnedEntityService ownedEntities,
            DiscordService discord,
            CharacterRoleService characterRoles
        )
            : base(characters, characterRoles, users, servers)
        {
            _characters = characters;
            _users = users;
            _servers = servers;
            _commands = commands;
            _ownedEntities = ownedEntities;
            _discord = discord;
            _characterRoles = characterRoles;
        }

        /// <summary>
        /// Determines whether the given name is unique among the given user's characters.
        /// </summary>
        /// <param name="guildUser">The user.</param>
        /// <param name="name">The name.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<bool>> IsNameUniqueForUserAsync(IGuildUser guildUser, string name)
        {
            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return RetrieveEntityResult<bool>.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildUser.Guild);
            if (!getServer.IsSuccess)
            {
                return RetrieveEntityResult<bool>.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return await _characters.IsNameUniqueForUserAsync(user, server, name);
        }

        /// <summary>
        /// Creates a character with the given parameters.
        /// </summary>
        /// <param name="guildUser">The owner of the character..</param>
        /// <param name="name">The name of the character.</param>
        /// <param name="avatarUrl">The character's avatar url.</param>
        /// <param name="nickname">The nickname that should be applied to the user when the character is active.</param>
        /// <param name="summary">The summary of the character.</param>
        /// <param name="description">The full description of the character.</param>
        /// <param name="pronounFamily">The pronoun family of the character.</param>
        /// <returns>A creation result which may or may not have been successful.</returns>
        public async Task<CreateEntityResult<Character>> CreateCharacterAsync
        (
            IGuildUser guildUser,
            string name,
            string? avatarUrl = null,
            string? nickname = null,
            string? summary = null,
            string? description = null,
            string? pronounFamily = null
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildUser.Guild);
            if (!getServer.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return await _characters.CreateCharacterAsync
            (
                user,
                server,
                name,
                avatarUrl,
                nickname,
                summary,
                description,
                pronounFamily
            );
        }

        /// <summary>
        /// Deletes the given character.
        /// </summary>
        /// <param name="guildUser">The user that owns the character.</param>
        /// <param name="character">The character to delete.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<DeleteEntityResult> DeleteCharacterAsync(IGuildUser guildUser, Character character)
        {
            var getCurrentCharacter = await _characters.GetCurrentCharacterAsync(character.Owner, character.Server);
            if (getCurrentCharacter.IsSuccess)
            {
                // Forcibly load the role so we can access it later
                _ = getCurrentCharacter.Entity.Role;
            }

            var deleteCharacter = await _characters.DeleteCharacterAsync(character);
            if (!deleteCharacter.IsSuccess)
            {
                return deleteCharacter;
            }

            if (!getCurrentCharacter.IsSuccess)
            {
                return DeleteEntityResult.FromSuccess();
            }

            var currentCharacter = getCurrentCharacter.Entity;
            if (currentCharacter != character)
            {
                return DeleteEntityResult.FromSuccess();
            }

            // Update the user's nickname
            var updateNickname = await UpdateUserNickname(guildUser);
            if (!updateNickname.IsSuccess)
            {
                return DeleteEntityResult.FromError(updateNickname);
            }

            var updateRoles = await _characterRoles.UpdateUserRolesAsync(guildUser, getCurrentCharacter.Entity);
            if (!updateRoles.IsSuccess)
            {
                return DeleteEntityResult.FromError(updateRoles);
            }

            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets a queryable list of characters on the given guild.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<IQueryable<Character>>> GetCharacters(IGuild guild)
        {
            var getServer = await _servers.GetOrRegisterServerAsync(guild);
            if (!getServer.IsSuccess)
            {
                return RetrieveEntityResult<IQueryable<Character>>.FromError(getServer);
            }

            var server = getServer.Entity;

            return RetrieveEntityResult<IQueryable<Character>>.FromSuccess(_characters.GetCharacters(server));
        }

        /// <summary>
        /// Gets a queryable list of characters belonging to the given user on their guild.
        /// </summary>
        /// <param name="guildUser">The user.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<IQueryable<Character>>> GetUserCharactersAsync(IGuildUser guildUser)
        {
            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return RetrieveEntityResult<IQueryable<Character>>.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildUser.Guild);
            if (!getServer.IsSuccess)
            {
                return RetrieveEntityResult<IQueryable<Character>>.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return RetrieveEntityResult<IQueryable<Character>>.FromSuccess(_characters.GetUserCharacters(user, server));
        }

        /// <summary>
        /// Gets the current character of the given user.
        /// </summary>
        /// <param name="guildUser">The user.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<Character>> GetCurrentCharacterAsync(IGuildUser guildUser)
        {
            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return RetrieveEntityResult<Character>.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildUser.Guild);
            if (!getServer.IsSuccess)
            {
                return RetrieveEntityResult<Character>.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return await _characters.GetCurrentCharacterAsync(user, server);
        }

        /// <summary>
        /// Gets a character by its given name.
        /// </summary>
        /// <param name="guild">The guild the character is on.</param>
        /// <param name="name">The name of the character.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<Character>> GetCharacterByNameAsync(IGuild guild, string name)
        {
            var getServer = await _servers.GetOrRegisterServerAsync(guild);
            if (!getServer.IsSuccess)
            {
                return RetrieveEntityResult<Character>.FromError(getServer);
            }

            var server = getServer.Entity;

            return await _characters.GetCharacterByNameAsync(server, name);
        }

        /// <summary>
        /// Gets a character owned by the given user by its given name.
        /// </summary>
        /// <param name="guildUser">The user.</param>
        /// <param name="name">The name of the character.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<Character>> GetUserCharacterByName(IGuildUser guildUser, string name)
        {
            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return RetrieveEntityResult<Character>.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildUser.Guild);
            if (!getServer.IsSuccess)
            {
                return RetrieveEntityResult<Character>.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return await _characters.GetUserCharacterByNameAsync(user, server, name);
        }

        /// <summary>
        /// Gets a random character from the user's characters.
        /// </summary>
        /// <param name="guildUser">The user.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<Character>> GetRandomUserCharacterAsync(IGuildUser guildUser)
        {
            var getUserCharacters = await GetUserCharactersAsync(guildUser);
            if (!getUserCharacters.IsSuccess)
            {
                return RetrieveEntityResult<Character>.FromError(getUserCharacters);
            }

            var userCharacters = await getUserCharacters.Entity.ToListAsync();
            if (userCharacters.Count == 0)
            {
                return RetrieveEntityResult<Character>.FromError("The user doesn't have any characters.");
            }

            if (userCharacters.Count == 1)
            {
                return RetrieveEntityResult<Character>.FromError("The user only has one character.");
            }

            var getCurrentCharacter = await GetCurrentCharacterAsync(guildUser);
            if (!getCurrentCharacter.IsSuccess)
            {
                return userCharacters.PickRandom();
            }

            var currentCharacter = getCurrentCharacter.Entity;
            return userCharacters.Except(new[] { currentCharacter }).ToList().PickRandom();
        }

        /// <summary>
        /// Gets the best matching character for the given owner and name combination. If no owner is provided, then the
        /// global list is searched for a unique name. If no name is provided, then the user's current character is
        /// used. If neither are set, no character will ever be returned.
        /// </summary>
        /// <param name="guild">The guild the user is on.</param>
        /// <param name="guildUser">The user.</param>
        /// <param name="name">The name of the character.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<Character>> GetBestMatchingCharacterAsync
        (
            IGuild guild,
            IGuildUser? guildUser,
            string? name
        )
        {
            var getServer = await _servers.GetOrRegisterServerAsync(guild);
            if (!getServer.IsSuccess)
            {
                return RetrieveEntityResult<Character>.FromError(getServer);
            }

            var server = getServer.Entity;

            if (guildUser is null)
            {
                return await _characters.GetBestMatchingCharacterAsync(server, null, name);
            }

            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return RetrieveEntityResult<Character>.FromError(getUser);
            }

            var user = getUser.Entity;

            return await _characters.GetBestMatchingCharacterAsync(server, user, name);
        }

        /// <summary>
        /// Sets the name of the given character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="name">The new name.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterNameAsync(Character character, string name)
        {
            var commandModule = _commands.Modules.FirstOrDefault(m => m.Name == "character");
            if (commandModule is null)
            {
                return await _characters.SetCharacterNameAsync(character, name);
            }

            var validNameResult = _ownedEntities.IsEntityNameValid(commandModule.GetAllCommandNames(), name);
            if (!validNameResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(validNameResult);
            }

            return await _characters.SetCharacterNameAsync(character, name);
        }

        /// <summary>
        /// Sets the avatar of the given character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="avatarUrl">The new avatar.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterAvatarAsync(Character character, string avatarUrl)
        {
            return await _characters.SetCharacterAvatarAsync(character, avatarUrl);
        }

        /// <summary>
        /// Sets the nickname of the given character.
        /// </summary>
        /// <param name="guildUser">The owner of the character.</param>
        /// <param name="character">The character.</param>
        /// <param name="nickname">The new nickname.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterNicknameAsync
        (
            IGuildUser guildUser,
            Character character,
            string nickname
        )
        {
            var setNickname = await _characters.SetCharacterNicknameAsync(character, nickname);
            if (!setNickname.IsSuccess)
            {
                return setNickname;
            }

            var getCurrentCharacter = await GetCurrentCharacterAsync(guildUser);
            if (!getCurrentCharacter.IsSuccess)
            {
                return ModifyEntityResult.FromSuccess();
            }

            var currentCharacter = getCurrentCharacter.Entity;
            if (currentCharacter != character)
            {
                return ModifyEntityResult.FromSuccess();
            }

            return await UpdateUserNickname(guildUser);
        }

        /// <summary>
        /// Sets the summary of the given character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="summary">The new summary.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterSummaryAsync(Character character, string summary)
        {
            return await _characters.SetCharacterSummaryAsync(character, summary);
        }

        /// <summary>
        /// Sets the description of the given character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="description">The new description.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterDescriptionAsync(Character character, string description)
        {
            return await _characters.SetCharacterDescriptionAsync(character, description);
        }

        /// <summary>
        /// Sets the preferred pronouns of the given character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="pronounFamily">The new pronoun family.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterPronounsAsync(Character character, string pronounFamily)
        {
            return await _characters.SetCharacterPronounsAsync(character, pronounFamily);
        }

        /// <summary>
        /// Sets whether the character is NSFW.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="isNSFW">Whether the character is NSFW.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterIsNSFWAsync(Character character, bool isNSFW)
        {
            return await _characters.SetCharacterIsNSFWAsync(character, isNSFW);
        }

        /// <summary>
        /// Adds the given image with the given metadata to the given character.
        /// </summary>
        /// <param name="character">The character to add the image to.</param>
        /// <param name="imageName">The name of the image.</param>
        /// <param name="imageUrl">The url of the image.</param>
        /// <param name="imageCaption">The caption of the image.</param>
        /// <param name="isNSFW">Whether or not the image is NSFW.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<Image>> AddImageToCharacterAsync
        (
            Character character,
            string imageName,
            string imageUrl,
            string? imageCaption = null,
            bool isNSFW = false
        )
        {
            return await _characters.AddImageToCharacterAsync(character, imageName, imageUrl, imageCaption, isNSFW);
        }

        /// <summary>
        /// Removes the given image from the given character.
        /// </summary>
        /// <param name="character">The character to remove the image from.</param>
        /// <param name="image">The image.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<DeleteEntityResult> RemoveImageFromCharacterAsync(Character character, Image image)
        {
            return await _characters.RemoveImageFromCharacterAsync(character, image);
        }

        /// <summary>
        /// Makes the given character the given user's current character.
        /// </summary>
        /// <param name="guildUser">The user.</param>
        /// <param name="character">The character.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> MakeCharacterCurrentAsync(IGuildUser guildUser, Character character)
        {
            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return ModifyEntityResult.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildUser.Guild);
            if (!getServer.IsSuccess)
            {
                return ModifyEntityResult.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            var getOriginalCharacter = await _characters.GetCurrentCharacterAsync(user, server);

            var makeCurrent = await _characters.MakeCharacterCurrentAsync(user, server, character);
            if (!makeCurrent.IsSuccess)
            {
                return makeCurrent;
            }

            // Update the user's nickname
            var updateNickname = await UpdateUserNickname(guildUser);
            if (!updateNickname.IsSuccess)
            {
                return updateNickname;
            }

            var originalCharacter = getOriginalCharacter.IsSuccess ? getOriginalCharacter.Entity : null;
            var updateRoles = await _characterRoles.UpdateUserRolesAsync(guildUser, originalCharacter);
            if (!updateRoles.IsSuccess)
            {
                return updateRoles;
            }

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Clears any current character in the server from the given user.
        /// </summary>
        /// <param name="guildUser">The user.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> ClearCurrentCharacterAsync(IGuildUser guildUser)
        {
            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return ModifyEntityResult.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildUser.Guild);
            if (!getServer.IsSuccess)
            {
                return ModifyEntityResult.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            var getOriginalCharacter = await _characters.GetCurrentCharacterAsync(user, server);

            var clearResult = await _characters.ClearCurrentCharacterAsync(user, server);
            if (!clearResult.IsSuccess)
            {
                return clearResult;
            }

            // Update the user's nickname
            var updateNickname = await UpdateUserNickname(guildUser);
            if (!updateNickname.IsSuccess)
            {
                return updateNickname;
            }

            var originalCharacter = getOriginalCharacter.IsSuccess ? getOriginalCharacter.Entity : null;
            var updateRoles = await _characterRoles.UpdateUserRolesAsync(guildUser, originalCharacter);
            if (!updateRoles.IsSuccess)
            {
                return updateRoles;
            }

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Updates the user's current nickname based on their character.
        /// </summary>
        /// <param name="guildUser">The user.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        private async Task<ModifyEntityResult> UpdateUserNickname(IGuildUser guildUser)
        {
            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return ModifyEntityResult.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildUser.Guild);
            if (!getServer.IsSuccess)
            {
                return ModifyEntityResult.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            string newNick;
            var getNewCharacter = await _characters.GetCurrentCharacterAsync(user, server);
            if (getNewCharacter.IsSuccess)
            {
                var newCharacter = getNewCharacter.Entity;
                newNick = newCharacter.Nickname.IsNullOrWhitespace()
                    ? guildUser.Username
                    : newCharacter.Nickname;
            }
            else
            {
                newNick = guildUser.Username;
            }

            var setNick = await _discord.SetUserNicknameAsync(guildUser, newNick);
            if (!setNick.IsSuccess)
            {
                return setNick;
            }

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Determines whether the given user has a current character on the server.
        /// </summary>
        /// <param name="guildUser">The user.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<bool>> HasCurrentCharacterAsync(IGuildUser guildUser)
        {
            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return RetrieveEntityResult<bool>.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildUser.Guild);
            if (!getServer.IsSuccess)
            {
                return RetrieveEntityResult<bool>.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return await _characters.HasCurrentCharacterAsync(user, server);
        }

        /// <summary>
        /// Sets the given user's default character to the given character.
        /// </summary>
        /// <param name="guildUser">The user.</param>
        /// <param name="character">The character.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetDefaultCharacterAsync(IGuildUser guildUser, Character character)
        {
            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return ModifyEntityResult.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildUser.Guild);
            if (!getServer.IsSuccess)
            {
                return ModifyEntityResult.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return await _characters.SetDefaultCharacterAsync(user, server, character);
        }

        /// <summary>
        /// Gets the given user's default character.
        /// </summary>
        /// <param name="guildUser">The user.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<Character>> GetDefaultCharacterAsync(IGuildUser guildUser)
        {
            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return RetrieveEntityResult<Character>.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildUser.Guild);
            if (!getServer.IsSuccess)
            {
                return RetrieveEntityResult<Character>.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return await _characters.GetDefaultCharacterAsync(user, server);
        }

        /// <summary>
        /// Clears the given user's default character.
        /// </summary>
        /// <param name="guildUser">The user.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> ClearDefaultCharacterAsync(IGuildUser guildUser)
        {
            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return ModifyEntityResult.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildUser.Guild);
            if (!getServer.IsSuccess)
            {
                return ModifyEntityResult.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return await _characters.ClearDefaultCharacterAsync(user, server);
        }

        /// <summary>
        /// Transfers ownership of the given character to the specified user.
        /// </summary>
        /// <param name="newOwner">The new owner.</param>
        /// <param name="character">The character to transfer.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> TransferCharacterOwnershipAsync(IGuildUser newOwner, Character character)
        {
            var getUser = await _users.GetOrRegisterUserAsync(newOwner);
            if (!getUser.IsSuccess)
            {
                return ModifyEntityResult.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(newOwner.Guild);
            if (!getServer.IsSuccess)
            {
                return ModifyEntityResult.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return await _characters.TransferCharacterOwnershipAsync(user, server, character);
        }
    }
}
