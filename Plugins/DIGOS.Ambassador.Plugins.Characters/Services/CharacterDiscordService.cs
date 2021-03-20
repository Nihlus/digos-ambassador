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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Discord.Feedback.Errors;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services.Interfaces;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Abstractions.Results;
using Remora.Discord.Core;
using Remora.Discord.Rest.Results;
using Remora.Results;

using Image = DIGOS.Ambassador.Plugins.Characters.Model.Data.Image;

namespace DIGOS.Ambassador.Plugins.Characters.Services
{
    /// <summary>
    /// Handles Discord-facing business logic.
    /// </summary>
    public class CharacterDiscordService
    {
        private readonly ICharacterService _characters;
        private readonly ICharacterEditor _characterEditor;

        private readonly CharacterRoleService _characterRoles;
        private readonly UserService _users;
        private readonly ServerService _servers;

        private readonly IDiscordRestGuildAPI _guildAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterDiscordService"/> class.
        /// </summary>
        /// <param name="characters">The character service.</param>
        /// <param name="characterEditor">The character editor.</param>
        /// <param name="users">The user service.</param>
        /// <param name="servers">The server service.</param>
        /// <param name="characterRoles">The character role service.</param>
        /// <param name="guildAPI">The guild API.</param>
        public CharacterDiscordService
        (
            ICharacterService characters,
            ICharacterEditor characterEditor,
            UserService users,
            ServerService servers,
            CharacterRoleService characterRoles,
            IDiscordRestGuildAPI guildAPI
        )
        {
            _characters = characters;
            _characterEditor = characterEditor;
            _users = users;
            _servers = servers;
            _characterRoles = characterRoles;
            _guildAPI = guildAPI;
        }

        /// <summary>
        /// Determines whether the given name is unique among the given user's characters.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="name">The name.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<bool>> IsNameUniqueForUserAsync
        (
            Snowflake guildID,
            Snowflake userID,
            string name,
            CancellationToken ct = default
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(userID, ct);
            if (!getUser.IsSuccess)
            {
                return Result<bool>.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result<bool>.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return await _characters.IsNameUniqueForUserAsync(user, server, name, ct);
        }

        /// <summary>
        /// Creates a character with the given parameters.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="name">The name of the character.</param>
        /// <param name="avatarUrl">The character's avatar url.</param>
        /// <param name="nickname">The nickname that should be applied to the user when the character is active.</param>
        /// <param name="summary">The summary of the character.</param>
        /// <param name="description">The full description of the character.</param>
        /// <param name="pronounFamily">The pronoun family of the character.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A creation result which may or may not have been successful.</returns>
        public async Task<Result<Character>> CreateCharacterAsync
        (
            Snowflake guildID,
            Snowflake userID,
            string name,
            string? avatarUrl = null,
            string? nickname = null,
            string? summary = null,
            string? description = null,
            string? pronounFamily = null,
            CancellationToken ct = default
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(userID, ct);
            if (!getUser.IsSuccess)
            {
                return Result<Character>.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result<Character>.FromError(getServer);
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
                pronounFamily,
                ct
            );
        }

        /// <summary>
        /// Deletes the given character.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="character">The character to delete.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<Result> DeleteCharacterAsync
        (
            Snowflake guildID,
            Snowflake userID,
            Character character,
            CancellationToken ct = default
        )
        {
            var getCurrentCharacter = await _characters.GetCurrentCharacterAsync(character.Owner, character.Server, ct);
            if (getCurrentCharacter.IsSuccess)
            {
                // Forcibly load the role so we can access it later
                _ = getCurrentCharacter.Entity.Role;
            }

            var deleteCharacter = await _characters.DeleteCharacterAsync(character, ct);
            if (!deleteCharacter.IsSuccess)
            {
                return deleteCharacter;
            }

            if (!getCurrentCharacter.IsSuccess)
            {
                return Result.FromSuccess();
            }

            var currentCharacter = getCurrentCharacter.Entity;
            if (currentCharacter != character)
            {
                return Result.FromSuccess();
            }

            // Update the user's nickname
            var updateNickname = await UpdateUserNickname(guildID, userID, ct);
            if (!updateNickname.IsSuccess)
            {
                return updateNickname;
            }

            return await _characterRoles.UpdateUserRolesAsync(guildID, userID, getCurrentCharacter.Entity, ct);
        }

        /// <summary>
        /// Gets a queryable list of characters belonging to the given user on their guild.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<IEnumerable<Character>>> GetUserCharactersAsync
        (
            Snowflake guildID,
            Snowflake userID,
            CancellationToken ct = default
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(userID, ct);
            if (!getUser.IsSuccess)
            {
                return Result<IEnumerable<Character>>.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result<IEnumerable<Character>>.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return Result<IEnumerable<Character>>.FromSuccess
            (
                await _characters.GetUserCharactersAsync(server, user, ct)
            );
        }

        /// <summary>
        /// Gets the current character of the given user.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<Character>> GetCurrentCharacterAsync
        (
            Snowflake guildID,
            Snowflake userID,
            CancellationToken ct = default
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(userID, ct);
            if (!getUser.IsSuccess)
            {
                return Result<Character>.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result<Character>.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return await _characters.GetCurrentCharacterAsync(user, server, ct);
        }

        /// <summary>
        /// Gets a character by its given name.
        /// </summary>
        /// <param name="guildID">The guild the character is on.</param>
        /// <param name="name">The name of the character.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<Character>> GetCharacterByNameAsync
        (
            Snowflake guildID,
            string name,
            CancellationToken ct = default
        )
        {
            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result<Character>.FromError(getServer);
            }

            var server = getServer.Entity;

            return await _characters.GetCharacterByNameAsync(server, name, ct);
        }

        /// <summary>
        /// Gets a character owned by the given user by its given name.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="name">The name of the character.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<Character>> GetUserCharacterByName
        (
            Snowflake guildID,
            Snowflake userID,
            string name,
            CancellationToken ct = default
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(userID, ct);
            if (!getUser.IsSuccess)
            {
                return Result<Character>.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result<Character>.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return await _characters.GetUserCharacterByNameAsync(user, server, name, ct);
        }

        /// <summary>
        /// Gets a random character from the user's characters.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<Character>> GetRandomUserCharacterAsync
        (
            Snowflake guildID,
            Snowflake userID,
            CancellationToken ct = default
        )
        {
            var getUserCharacters = await GetUserCharactersAsync(guildID, userID, ct);
            if (!getUserCharacters.IsSuccess)
            {
                return Result<Character>.FromError(getUserCharacters);
            }

            var userCharacters = getUserCharacters.Entity.ToList();
            if (userCharacters.Count == 0)
            {
                return new UserError("The user doesn't have any characters.");
            }

            if (userCharacters.Count == 1)
            {
                return new UserError("The user only has one character.");
            }

            var getCurrentCharacter = await GetCurrentCharacterAsync(guildID, userID, ct);
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
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="name">The name of the character.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<Character>> GetBestMatchingCharacterAsync
        (
            Snowflake guildID,
            Snowflake? userID,
            string? name,
            CancellationToken ct = default
        )
        {
            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result<Character>.FromError(getServer);
            }

            var server = getServer.Entity;

            if (userID is null)
            {
                return await _characters.GetBestMatchingCharacterAsync(server, null, name, ct);
            }

            var getUser = await _users.GetOrRegisterUserAsync(userID.Value, ct);
            if (!getUser.IsSuccess)
            {
                return Result<Character>.FromError(getUser);
            }

            var user = getUser.Entity;

            return await _characters.GetBestMatchingCharacterAsync(server, user, name, ct);
        }

        /// <summary>
        /// Sets the name of the given character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="name">The new name.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetCharacterNameAsync
        (
            Character character,
            string name,
            CancellationToken ct = default
        )
        {
            return await _characterEditor.SetCharacterNameAsync(character, name, ct);
        }

        /// <summary>
        /// Sets the avatar of the given character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="avatarUrl">The new avatar.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetCharacterAvatarAsync
        (
            Character character,
            string avatarUrl,
            CancellationToken ct = default
        )
        {
            return await _characterEditor.SetCharacterAvatarAsync(character, avatarUrl, ct);
        }

        /// <summary>
        /// Sets the nickname of the given character.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="character">The character.</param>
        /// <param name="nickname">The new nickname.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetCharacterNicknameAsync
        (
            Snowflake guildID,
            Snowflake userID,
            Character character,
            string nickname,
            CancellationToken ct = default
        )
        {
            var setNickname = await _characterEditor.SetCharacterNicknameAsync(character, nickname, ct);
            if (!setNickname.IsSuccess)
            {
                return setNickname;
            }

            var getCurrentCharacter = await GetCurrentCharacterAsync(guildID, userID, ct);
            if (!getCurrentCharacter.IsSuccess)
            {
                return Result.FromSuccess();
            }

            var currentCharacter = getCurrentCharacter.Entity;
            if (currentCharacter != character)
            {
                return Result.FromSuccess();
            }

            return await UpdateUserNickname(guildID, userID, ct);
        }

        /// <summary>
        /// Sets the summary of the given character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="summary">The new summary.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public Task<Result> SetCharacterSummaryAsync
        (
            Character character,
            string summary,
            CancellationToken ct = default
        ) => _characterEditor.SetCharacterSummaryAsync(character, summary, ct);

        /// <summary>
        /// Sets the description of the given character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="description">The new description.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public Task<Result> SetCharacterDescriptionAsync
        (
            Character character,
            string description,
            CancellationToken ct = default
        ) => _characterEditor.SetCharacterDescriptionAsync(character, description, ct);

        /// <summary>
        /// Sets the preferred pronouns of the given character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="pronounFamily">The new pronoun family.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public Task<Result> SetCharacterPronounsAsync
        (
            Character character,
            string pronounFamily,
            CancellationToken ct = default
        ) => _characterEditor.SetCharacterPronounsAsync(character, pronounFamily, ct);

        /// <summary>
        /// Sets whether the character is NSFW.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="isNSFW">Whether the character is NSFW.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public Task<Result> SetCharacterIsNSFWAsync
        (
            Character character,
            bool isNSFW,
            CancellationToken ct = default
        ) => _characterEditor.SetCharacterIsNSFWAsync(character, isNSFW, ct);

        /// <summary>
        /// Adds the given image with the given metadata to the given character.
        /// </summary>
        /// <param name="character">The character to add the image to.</param>
        /// <param name="imageName">The name of the image.</param>
        /// <param name="imageUrl">The url of the image.</param>
        /// <param name="imageCaption">The caption of the image.</param>
        /// <param name="isNSFW">Whether or not the image is NSFW.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public Task<Result<Image>> AddImageToCharacterAsync
        (
            Character character,
            string imageName,
            string imageUrl,
            string? imageCaption = null,
            bool isNSFW = false,
            CancellationToken ct = default
        ) => _characterEditor.AddImageToCharacterAsync(character, imageName, imageUrl, imageCaption, isNSFW, ct);

        /// <summary>
        /// Removes the given image from the given character.
        /// </summary>
        /// <param name="character">The character to remove the image from.</param>
        /// <param name="image">The image.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public Task<Result> RemoveImageFromCharacterAsync
        (
            Character character,
            Image image,
            CancellationToken ct = default
        ) => _characterEditor.RemoveImageFromCharacterAsync(character, image, ct);

        /// <summary>
        /// Makes the given character the given user's current character.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="character">The character.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> MakeCharacterCurrentAsync
        (
            Snowflake guildID,
            Snowflake userID,
            Character character,
            CancellationToken ct = default
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(userID, ct);
            if (!getUser.IsSuccess)
            {
                return Result.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            var getOriginalCharacter = await _characters.GetCurrentCharacterAsync(user, server, ct);

            var makeCurrent = await _characters.MakeCharacterCurrentAsync(user, server, character, ct);
            if (!makeCurrent.IsSuccess)
            {
                return makeCurrent;
            }

            // Update the user's nickname
            var updateNickname = await UpdateUserNickname(guildID, userID, ct);
            if (!updateNickname.IsSuccess)
            {
                return updateNickname;
            }

            var originalCharacter = getOriginalCharacter.IsSuccess
                ? getOriginalCharacter.Entity
                : null;

            var updateRoles = await _characterRoles.UpdateUserRolesAsync(guildID, userID, originalCharacter, ct);
            if (!updateRoles.IsSuccess)
            {
                return updateRoles;
            }

            return Result.FromSuccess();
        }

        /// <summary>
        /// Clears any current character in the server from the given user.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> ClearCurrentCharacterAsync
        (
            Snowflake guildID,
            Snowflake userID,
            CancellationToken ct = default
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(userID, ct);
            if (!getUser.IsSuccess)
            {
                return Result.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            var getOriginalCharacter = await _characters.GetCurrentCharacterAsync(user, server, ct);

            var clearResult = await _characters.ClearCurrentCharacterAsync(user, server, ct);
            if (!clearResult.IsSuccess)
            {
                return clearResult;
            }

            // Update the user's nickname
            var updateNickname = await UpdateUserNickname(guildID, userID, ct);
            if (!updateNickname.IsSuccess)
            {
                return updateNickname;
            }

            var originalCharacter = getOriginalCharacter.IsSuccess ? getOriginalCharacter.Entity : null;
            return await _characterRoles.UpdateUserRolesAsync(guildID, userID, originalCharacter, ct);
        }

        /// <summary>
        /// Updates the user's current nickname based on their character.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        private async Task<Result> UpdateUserNickname
        (
            Snowflake guildID,
            Snowflake userID,
            CancellationToken ct = default
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(userID, ct);
            if (!getUser.IsSuccess)
            {
                return Result.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            var getMember = await _guildAPI.GetGuildMemberAsync(guildID, userID, ct);
            if (!getMember.IsSuccess)
            {
                return Result.FromError(getMember);
            }

            var member = getMember.Entity;
            if (!member.User.HasValue)
            {
                throw new InvalidOperationException();
            }

            string newNick;
            var getNewCharacter = await _characters.GetCurrentCharacterAsync(user, server, ct);
            if (getNewCharacter.IsSuccess)
            {
                var newCharacter = getNewCharacter.Entity;
                newNick = newCharacter.Nickname.IsNullOrWhitespace()
                    ? member.User.Value.Username
                    : newCharacter.Nickname;
            }
            else
            {
                newNick = member.User.Value.Username;
            }

            var modifyNickname = await _guildAPI.ModifyGuildMemberAsync(guildID, userID, newNick, ct: ct);
            if (!modifyNickname.IsSuccess)
            {
                if (modifyNickname.Unwrap() is not DiscordRestResultError rre)
                {
                    if (modifyNickname.Unwrap() is not HttpResultError hre)
                    {
                        return modifyNickname;
                    }

                    if (hre.StatusCode is not HttpStatusCode.Forbidden)
                    {
                        return modifyNickname;
                    }

                    return new UserError
                    (
                        "I'm forbidden from setting the user's nickname - typically, this means the target was the " +
                        "server owner. This is a Discord limitation, and can't be fixed."
                    );
                }

                return rre.DiscordError.Code is not DiscordError.MissingPermission
                    ? modifyNickname
                    : new UserError("I don't have permission to set the user's nickname.");
            }

            return modifyNickname;
        }

        /// <summary>
        /// Determines whether the given user has a current character on the server.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<bool>> HasCurrentCharacterAsync
        (
            Snowflake guildID,
            Snowflake userID,
            CancellationToken ct = default
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(userID, ct);
            if (!getUser.IsSuccess)
            {
                return Result<bool>.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result<bool>.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return await _characters.HasCurrentCharacterAsync(user, server, ct);
        }

        /// <summary>
        /// Sets the given user's default character to the given character.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="character">The character.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetDefaultCharacterAsync
        (
            Snowflake guildID,
            Snowflake userID,
            Character character,
            CancellationToken ct = default
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(userID, ct);
            if (!getUser.IsSuccess)
            {
                return Result.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return await _characters.SetDefaultCharacterAsync(user, server, character, ct);
        }

        /// <summary>
        /// Gets the given user's default character.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<Character>> GetDefaultCharacterAsync
        (
            Snowflake guildID,
            Snowflake userID,
            CancellationToken ct = default
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(userID, ct);
            if (!getUser.IsSuccess)
            {
                return Result<Character>.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result<Character>.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return await _characters.GetDefaultCharacterAsync(user, server, ct);
        }

        /// <summary>
        /// Clears the given user's default character.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> ClearDefaultCharacterAsync
        (
            Snowflake guildID,
            Snowflake userID,
            CancellationToken ct = default
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(userID, ct);
            if (!getUser.IsSuccess)
            {
                return Result.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return await _characters.ClearDefaultCharacterAsync(user, server, ct);
        }

        /// <summary>
        /// Transfers ownership of the given character to the specified user.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="character">The character to transfer.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> TransferCharacterOwnershipAsync
        (
            Snowflake guildID,
            Snowflake userID,
            Character character,
            CancellationToken ct = default
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(userID, ct);
            if (!getUser.IsSuccess)
            {
                return Result.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            return await _characters.TransferCharacterOwnershipAsync(user, server, character, ct);
        }
    }
}
