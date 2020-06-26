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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Core.Services.TransientState;
using DIGOS.Ambassador.Plugins.Characters.Extensions;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Model.Data;
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Characters.Services
{
    /// <summary>
    /// Acts as an interface for accessing and modifying user characters.
    /// </summary>
    public sealed class CharacterService : AbstractTransientStateService
    {
        private readonly CharactersDatabaseContext _database;
        private readonly OwnedEntityService _ownedEntities;
        private readonly ContentService _content;
        private readonly PronounService _pronouns;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterService"/> class.
        /// </summary>
        /// <param name="entityService">The application's owned entity service.</param>
        /// <param name="content">The content service.</param>
        /// <param name="database">The core database.</param>
        /// <param name="pronouns">The pronoun service.</param>
        /// <param name="log">The logging instance.</param>
        public CharacterService
        (
            OwnedEntityService entityService,
            ContentService content,
            CharactersDatabaseContext database,
            PronounService pronouns,
            ILogger<AbstractTransientStateService> log
        )
            : base(log)
        {
            _ownedEntities = entityService;
            _content = content;
            _database = database;
            _pronouns = pronouns;
        }

        /// <summary>
        /// Creates a character with the given parameters.
        /// </summary>
        /// <param name="user">The owner of the character..</param>
        /// <param name="server">The server the owner is on.</param>
        /// <param name="name">The name of the character.</param>
        /// <param name="avatarUrl">The character's avatar url.</param>
        /// <param name="nickname">The nickname that should be applied to the user when the character is active.</param>
        /// <param name="summary">The summary of the character.</param>
        /// <param name="description">The full description of the character.</param>
        /// <param name="pronounFamily">The pronoun family of the character.</param>
        /// <returns>A creation result which may or may not have been successful.</returns>
        public async Task<CreateEntityResult<Character>> CreateCharacterAsync
        (
            User user,
            Server server,
            string name,
            string? avatarUrl = null,
            string? nickname = null,
            string? summary = null,
            string? description = null,
            string? pronounFamily = null
        )
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            avatarUrl ??= _content.GetDefaultAvatarUri().ToString();
            nickname ??= name;
            summary ??= "No summary set.";
            description ??= "No description set.";
            pronounFamily ??= new TheyPronounProvider().Family;

            // Use dummy values here - we'll set them with the service so we can ensure they're correctly formatted.
            var character = _database.CreateProxy<Character>
            (
                user,
                server,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            );

            _database.Characters.Update(character);

            var modifyEntityResult = await SetCharacterNameAsync(character, name);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            modifyEntityResult = await SetCharacterAvatarAsync(character, avatarUrl);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            modifyEntityResult = await SetCharacterNicknameAsync(character, nickname);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            modifyEntityResult = await SetCharacterSummaryAsync(character, summary);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            modifyEntityResult = await SetCharacterDescriptionAsync(character, description);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            modifyEntityResult = await SetCharacterPronounsAsync(character, pronounFamily);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            return character;
        }

        /// <summary>
        /// Deletes the given character.
        /// </summary>
        /// <param name="character">The character to delete.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<DeleteEntityResult> DeleteCharacterAsync(Character character)
        {
            _database.Characters.Remove(character);

            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// This method searches for the best matching character given an owner and a name. If no owner is provided, then
        /// the global list is searched for a unique name. If no match can be found, a failed result is returned.
        /// </summary>
        /// <param name="server">The server the user is on.</param>
        /// <param name="user">The owner of the character, if any.</param>
        /// <param name="name">The name of the character, if any.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public async Task<RetrieveEntityResult<Character>> GetBestMatchingCharacterAsync
        (
            Server server,
            User? user,
            string? name
        )
        {
            server = _database.NormalizeReference(server);
            if (!(user is null))
            {
                user = _database.NormalizeReference(user);
            }

            if (user is null && name is null)
            {
                return RetrieveEntityResult<Character>.FromError("No user and no name specified.");
            }

            if (user is null)
            {
                return await GetCharacterByNameAsync(server, name!);
            }

            if (name.IsNullOrWhitespace())
            {
                return await GetCurrentCharacterAsync(user, server);
            }

            return await GetUserCharacterByNameAsync(user, server, name);
        }

        /// <summary>
        /// Gets the current character a user has assumed the form of.
        /// </summary>
        /// <param name="user">The user to get the current character of.</param>
        /// <param name="server">The server the user is on.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public async Task<RetrieveEntityResult<Character>> GetCurrentCharacterAsync(User user, Server server)
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            if (!await HasCurrentCharacterAsync(user, server))
            {
                return RetrieveEntityResult<Character>.FromError("The user hasn't assumed a character.");
            }

            var currentCharacter = (await GetUserCharactersAsync(user, server, q => q.Where(ch => ch.IsCurrent)))
                .SingleOrDefault();

            if (!(currentCharacter is null))
            {
                return currentCharacter;
            }

            return RetrieveEntityResult<Character>.FromError("Failed to retrieve a current character.");
        }

        /// <summary>
        /// Gets a character by its given name.
        /// </summary>
        /// <param name="server">The server that the character is on.</param>
        /// <param name="name">The name of the character.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public async Task<RetrieveEntityResult<Character>> GetCharacterByNameAsync
        (
            Server server,
            string name
        )
        {
            server = _database.NormalizeReference(server);

            var characters = (await GetCharactersAsync
            (
                server,
                q => q.Where(ch => string.Equals(ch.Name.ToLower(), name.ToLower()))
            )).ToList();

            if (characters.Count > 1)
            {
                return RetrieveEntityResult<Character>.FromError
                (
                    "There's more than one character with that name. Please specify which user it belongs to."
                );
            }

            var character = characters.FirstOrDefault();

            if (!(character is null))
            {
                return character;
            }

            return RetrieveEntityResult<Character>.FromError("No character with that name found.");
        }

        /// <summary>
        /// Gets the characters on the given server.
        /// </summary>
        /// <param name="server">The server to scope the search to.</param>
        /// <param name="query">Additional query statements.</param>
        /// <returns>A queryable set of characters.</returns>
        [Pure]
        public Task<IEnumerable<Character>> GetCharactersAsync
        (
            Server server,
            Func<IQueryable<Character>, IQueryable<Character>>? query = null
        )
        {
            query ??= q => q;
            server = _database.NormalizeReference(server);

            return _database.Characters.UnifiedQueryAsync(q => query(q.Where(a => a.Server == server)));
        }

        /// <summary>
        /// Get the characters owned by the given user.
        /// </summary>
        /// <param name="user">The user to get the characters of.</param>
        /// <param name="server">The server to scope the search to.</param>
        /// <param name="query">Additional query statements.</param>
        /// <returns>A queryable list of characters belonging to the user.</returns>
        [Pure]
        public async Task<IEnumerable<Character>> GetUserCharactersAsync
        (
            User user,
            Server server,
            Func<IQueryable<Character>, IQueryable<Character>>? query = null
        )
        {
            query ??= q => q;

            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            return await GetCharactersAsync(server, q => query(q.Where(ch => ch.Owner == user)));
        }

        /// <summary>
        /// Gets a character belonging to a given user by a given name.
        /// </summary>
        /// <param name="user">The user to get the character from.</param>
        /// <param name="server">The server that the user is on.</param>
        /// <param name="name">The name of the character.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public async Task<RetrieveEntityResult<Character>> GetUserCharacterByNameAsync
        (
            User user,
            Server server,
            string name
        )
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            var characters = await GetUserCharactersAsync
            (
                user,
                server,
                q => q.Where(ch => string.Equals(ch.Name.ToLower(), name.ToLower()))
            );

            var character = characters.SingleOrDefault();

            if (!(character is null))
            {
                return character;
            }

            return RetrieveEntityResult<Character>.FromError("The user doesn't own a character with that name.");
        }

        /// <summary>
        /// Makes the given character current on the given server.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="server">The server the user is on.</param>
        /// <param name="character">The character to make current.</param>
        /// <returns>A task that must be awaited.</returns>
        public async Task<ModifyEntityResult> MakeCharacterCurrentAsync(User user, Server server, Character character)
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            if (character.IsCurrent)
            {
                return ModifyEntityResult.FromError("The character is already current on the server.");
            }

            await ClearCurrentCharacterAsync(user, server);

            character.IsCurrent = true;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Clears any current characters in the server from the given user, returning them to their default form.
        /// </summary>
        /// <param name="user">The user to clear the characters from.</param>
        /// <param name="server">The server to clear the characters on.</param>
        /// <returns>A task that must be awaited.</returns>
        public async Task<ModifyEntityResult> ClearCurrentCharacterAsync(User user, Server server)
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            var getCurrentCharacter = await GetCurrentCharacterAsync(user, server);
            if (!getCurrentCharacter.IsSuccess)
            {
                return ModifyEntityResult.FromError(getCurrentCharacter);
            }

            var currentCharacter = getCurrentCharacter.Entity;
            currentCharacter.IsCurrent = false;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Determines whether or not the given user has an active character on the given server.
        /// </summary>
        /// <param name="user">The user to check.</param>
        /// <param name="server">The server the user is on.</param>
        /// <returns>true if the user has an active character on the server; otherwise, false.</returns>
        [Pure]
        public async Task<bool> HasCurrentCharacterAsync
        (
            User user,
            Server server
        )
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            var characters = await GetUserCharactersAsync(user, server, q => q.Where(c => c.IsCurrent));
            return characters.Any();
        }

        /// <summary>
        /// Retrieves the given user's default character.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="server">The server the user is on.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<Character>> GetDefaultCharacterAsync(User user, Server server)
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            var characters = await GetUserCharactersAsync(user, server, q => q.Where(ch => ch.IsDefault));
            var defaultCharacter = characters.SingleOrDefault();

            if (!(defaultCharacter is null))
            {
                return defaultCharacter;
            }

            return RetrieveEntityResult<Character>.FromError("The user doesn't have a default character.");
        }

        /// <summary>
        /// Sets the default character of a user.
        /// </summary>
        /// <param name="user">The user to set the default character of.</param>
        /// <param name="server">The server the user is on.</param>
        /// <param name="character">The new default character.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetDefaultCharacterAsync(User user, Server server, Character character)
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            if (character.Owner != user)
            {
                return ModifyEntityResult.FromError("The user doesn't own that character.");
            }

            var getDefaultCharacterResult = await GetDefaultCharacterAsync(user, server);
            if (getDefaultCharacterResult.IsSuccess)
            {
                var currentDefault = getDefaultCharacterResult.Entity;
                if (currentDefault == character)
                {
                    return ModifyEntityResult.FromError("That's already the user's default character.");
                }

                currentDefault.IsDefault = false;
            }

            character.IsDefault = true;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Clears the default character from the given user.
        /// </summary>
        /// <param name="user">The user to clear the default character of.</param>
        /// <param name="server">The server the user is on.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> ClearDefaultCharacterAsync(User user, Server server)
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            var getDefaultCharacterResult = await GetDefaultCharacterAsync(user, server);
            if (!getDefaultCharacterResult.IsSuccess)
            {
                return ModifyEntityResult.FromError("That user doesn't have a default character.");
            }

            getDefaultCharacterResult.Entity.IsDefault = false;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the name of the given character.
        /// </summary>
        /// <param name="character">The character to set the name of.</param>
        /// <param name="name">The new name.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterNameAsync
        (
            Character character,
            string name
        )
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return ModifyEntityResult.FromError("You need to provide a name.");
            }

            if (string.Equals(character.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return ModifyEntityResult.FromError("The character already has that name.");
            }

            if (name.Contains("\""))
            {
                return ModifyEntityResult.FromError("The name may not contain double quotes.");
            }

            if (!await IsNameUniqueForUserAsync(character.Owner, character.Server, name))
            {
                return ModifyEntityResult.FromError("The user already has a character with that name.");
            }

            character.Name = name;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the avatar of the given character.
        /// </summary>
        /// <param name="character">The character to set the avatar of.</param>
        /// <param name="avatarUrl">The new avatar.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterAvatarAsync
        (
            Character character,
            string avatarUrl
        )
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                return ModifyEntityResult.FromError("You need to provide a new avatar url.");
            }

            if (!Uri.TryCreate(avatarUrl, UriKind.Absolute, out _))
            {
                return ModifyEntityResult.FromError("The given image URL wasn't valid.");
            }

            if (character.AvatarUrl == avatarUrl)
            {
                return ModifyEntityResult.FromError("The character's avatar is already set to that URL.");
            }

            character.AvatarUrl = avatarUrl;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the nickname of the given character.
        /// </summary>
        /// <param name="character">The character to set the nickname of.</param>
        /// <param name="nickname">The new nickname.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterNicknameAsync
        (
            Character character,
            string nickname
        )
        {
            if (string.IsNullOrWhiteSpace(nickname))
            {
                return ModifyEntityResult.FromError("You need to provide a new nickname.");
            }

            if (character.Nickname == nickname)
            {
                return ModifyEntityResult.FromError("The character already has that nickname.");
            }

            if (nickname.Length > 32)
            {
                return ModifyEntityResult.FromError("The nickname is too long. It can be at most 32 characters.");
            }

            character.Nickname = nickname;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the summary of the given character.
        /// </summary>
        /// <param name="character">The character to set the summary of.</param>
        /// <param name="summary">The new summary.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterSummaryAsync
        (
            Character character,
            string summary
        )
        {
            if (string.IsNullOrWhiteSpace(summary))
            {
                return ModifyEntityResult.FromError("You need to provide a new summary.");
            }

            if (character.Summary == summary)
            {
                return ModifyEntityResult.FromError("That's already the character's summary.");
            }

            if (summary.Length > 240)
            {
                return ModifyEntityResult.FromError("The summary is too long. It can be at most 240 characters.");
            }

            character.Summary = summary;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the description of the given character.
        /// </summary>
        /// <param name="character">The character to set the description of.</param>
        /// <param name="description">The new description.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterDescriptionAsync
        (
            Character character,
            string description
        )
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return ModifyEntityResult.FromError("You need to provide a new description.");
            }

            if (character.Description == description)
            {
                return ModifyEntityResult.FromError("The character already has that description.");
            }

            if (description.Length > 1000)
            {
                return ModifyEntityResult.FromError("The description is too long. It can be at most 1000 characters.");
            }
            character.Description = description;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the preferred pronoun for the given character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="pronounFamily">The pronoun family.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterPronounsAsync
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
            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets whether or not a character is NSFW.
        /// </summary>
        /// <param name="character">The character to edit.</param>
        /// <param name="isNSFW">Whether or not the character is NSFW.</param>
        /// <returns>A task that must be awaited.</returns>
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

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Transfers ownership of the named character to the specified user.
        /// </summary>
        /// <param name="newOwner">The new owner.</param>
        /// <param name="server">The server to scope the character search to.</param>
        /// <param name="character">The character to transfer.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> TransferCharacterOwnershipAsync
        (
            User newOwner,
            Server server,
            Character character
        )
        {
            newOwner = _database.NormalizeReference(newOwner);
            server = _database.NormalizeReference(server);

            var newOwnerCharacters = (await GetUserCharactersAsync(newOwner, server)).ToList();
            return _ownedEntities.TransferEntityOwnership
            (
                newOwner,
                newOwnerCharacters,
                character
            );
        }

        /// <summary>
        /// Determines whether or not the given character name is unique for a given user.
        /// </summary>
        /// <param name="user">The user to check.</param>
        /// <param name="server">The server to scope the character search to.</param>
        /// <param name="characterName">The character name to check.</param>
        /// <returns>true if the name is unique; otherwise, false.</returns>
        [Pure]
        public async Task<bool> IsNameUniqueForUserAsync
        (
            User user,
            Server server,
            string characterName
        )
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            var userCharacters = (await GetUserCharactersAsync(user, server)).ToList();
            return _ownedEntities.IsEntityNameUniqueForUser(userCharacters, characterName);
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
        public async Task<CreateEntityResult<Image>> AddImageToCharacterAsync
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
                return CreateEntityResult<Image>.FromError("The character already has an image with that name.");
            }

            if (imageName.IsNullOrWhitespace())
            {
                return CreateEntityResult<Image>.FromError("You need to specify a name.");
            }

            if (imageCaption.IsNullOrWhitespace())
            {
                imageCaption = "No caption set.";
            }

            if (!Uri.IsWellFormedUriString(imageUrl, UriKind.RelativeOrAbsolute))
            {
                return CreateEntityResult<Image>.FromError
                (
                    $"That URL doesn't look valid. Please check \"{imageUrl}\" for errors."
                );
            }

            var image = _database.CreateProxy<Image>(imageName, imageUrl, imageCaption);
            image.IsNSFW = isNSFW;

            character.Images.Add(image);

            return image;
        }

        /// <summary>
        /// Removes the named image from the given character.
        /// </summary>
        /// <param name="character">The character to remove the image from.</param>
        /// <param name="image">The image.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<DeleteEntityResult> RemoveImageFromCharacterAsync(Character character, Image image)
        {
            if (!character.Images.Contains(image))
            {
                return DeleteEntityResult.FromError("The character has no image with that name.");
            }

            character.Images.Remove(image);

            return DeleteEntityResult.FromSuccess();
        }

        /// <inheritdoc/>
        protected override void OnSavingChanges()
        {
            _database.SaveChanges();
        }

        /// <inheritdoc/>
        protected override async ValueTask OnSavingChangesAsync(CancellationToken ct = default)
        {
            await _database.SaveChangesAsync(ct);
        }
    }
}
