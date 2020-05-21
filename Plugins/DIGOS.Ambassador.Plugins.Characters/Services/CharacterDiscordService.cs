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
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using Discord;
using Discord.Commands;
using Remora.Results;

using Image = DIGOS.Ambassador.Plugins.Characters.Model.Data.Image;

namespace DIGOS.Ambassador.Plugins.Characters.Services
{
    /// <summary>
    /// Handles Discord-facing business logic.
    /// </summary>
    public class CharacterDiscordService
    {
        private readonly CharacterService _characters;
        private readonly CommandService _commands;
        private readonly OwnedEntityService _ownedEntities;
        private readonly UserService _users;
        private readonly ServerService _servers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterDiscordService"/> class.
        /// </summary>
        /// <param name="characters">The character service.</param>
        /// <param name="users">The user service.</param>
        /// <param name="servers">The server service.</param>
        /// <param name="commands">The command service.</param>
        /// <param name="ownedEntities">The owned entity service.</param>
        public CharacterDiscordService
        (
            CharacterService characters,
            UserService users,
            ServerService servers,
            CommandService commands,
            OwnedEntityService ownedEntities
        )
        {
            _characters = characters;
            _users = users;
            _servers = servers;
            _commands = commands;
            _ownedEntities = ownedEntities;
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
        /// <param name="character">The character to delete.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<DeleteEntityResult> DeleteCharacterAsync(Character character)
        {
            return await _characters.DeleteCharacterAsync(character);
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
            if (!(commandModule is null))
            {
                var validNameResult = _ownedEntities.IsEntityNameValid(commandModule.GetAllCommandNames(), name);
                if (!validNameResult.IsSuccess)
                {
                    return ModifyEntityResult.FromError(validNameResult);
                }
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
        /// <param name="character">The character.</param>
        /// <param name="nickname">The new nickname.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterNicknameAsync(Character character, string nickname)
        {
            return await _characters.SetCharacterNicknameAsync(character, nickname);
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
            string imageCaption,
            bool isNSFW
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

            return await _characters.MakeCharacterCurrentAsync(user, server, character);
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

            return await _characters.ClearCurrentCharacterAsync(user, server);
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
