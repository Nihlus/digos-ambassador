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
using DIGOS.Ambassador.Plugins.Characters.Extensions;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Model.Data;
using DIGOS.Ambassador.Plugins.Characters.Services.Interfaces;
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Plugins.Core.Extensions;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Characters.Services
{
    /// <summary>
    /// Acts as an interface for accessing and modifying user characters.
    /// </summary>
    public sealed class CharacterService : ICharacterService, ICharacterEditor
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
        public CharacterService
        (
            OwnedEntityService entityService,
            ContentService content,
            CharactersDatabaseContext database,
            PronounService pronouns
        )
        {
            _ownedEntities = entityService;
            _content = content;
            _database = database;
            _pronouns = pronouns;
        }

        /// <inheritdoc />
        public async Task<CreateEntityResult<Character>> CreateCharacterAsync
        (
            User user,
            Server server,
            string name,
            string? avatarUrl = null,
            string? nickname = null,
            string? summary = null,
            string? description = null,
            string? pronounFamily = null,
            CancellationToken ct = default
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

            var modifyEntityResult = await SetCharacterNameAsync(character, name, ct);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            modifyEntityResult = await SetCharacterAvatarAsync(character, avatarUrl, ct);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            modifyEntityResult = await SetCharacterNicknameAsync(character, nickname, ct);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            modifyEntityResult = await SetCharacterSummaryAsync(character, summary, ct);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            modifyEntityResult = await SetCharacterDescriptionAsync(character, description, ct);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            modifyEntityResult = await SetCharacterPronounsAsync(character, pronounFamily, ct);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            await _database.SaveChangesAsync(ct);

            return character;
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<Character>> GetUserCharactersAsync
        (
            Server server,
            User user,
            CancellationToken ct = default
        )
        {
            server = _database.NormalizeReference(server);
            user = _database.NormalizeReference(user);

            return _database.Characters.UserScopedServersideQueryAsync
            (
                user,
                server,
                c => c,
                ct
            );
        }

        /// <inheritdoc />
        public async Task<DeleteEntityResult> DeleteCharacterAsync(Character character, CancellationToken ct = default)
        {
            _database.Characters.Remove(character);
            await _database.SaveChangesAsync(ct);

            return DeleteEntityResult.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<RetrieveEntityResult<Character>> GetBestMatchingCharacterAsync
        (
            Server server,
            User? user,
            string? name,
            CancellationToken ct = default
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
                return await GetCharacterByNameAsync(server, name!, ct);
            }

            if (name.IsNullOrWhitespace())
            {
                return await GetCurrentCharacterAsync(user, server, ct);
            }

            return await GetUserCharacterByNameAsync(user, server, name, ct);
        }

        /// <inheritdoc />
        public async Task<RetrieveEntityResult<Character>> GetCurrentCharacterAsync
        (
            User user,
            Server server,
            CancellationToken ct = default
        )
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            if (!await HasCurrentCharacterAsync(user, server, ct))
            {
                return RetrieveEntityResult<Character>.FromError("The user hasn't assumed a character.");
            }

            var currentCharacter = await _database.Characters.UserScopedServersideQueryAsync
            (
                user,
                server,
                q => q
                    .Where(ch => ch.IsCurrent)
                    .SingleOrDefaultAsync(ct)
            );

            if (!(currentCharacter is null))
            {
                return currentCharacter;
            }

            return RetrieveEntityResult<Character>.FromError("Failed to retrieve a current character.");
        }

        /// <inheritdoc />
        public async Task<RetrieveEntityResult<Character>> GetCharacterByNameAsync
        (
            Server server,
            string name,
            CancellationToken ct = default
        )
        {
            server = _database.NormalizeReference(server);

            var characters = await _database.Characters.ServerScopedServersideQueryAsync
            (
                server,
                q => q.Where(ch => string.Equals(ch.Name.ToLower(), name.ToLower())),
                ct
            );

            if (characters.Count > 1)
            {
                return RetrieveEntityResult<Character>.FromError
                (
                    "There's more than one character with that name. Please specify which user it belongs to."
                );
            }

            var character = characters.SingleOrDefault();

            if (!(character is null))
            {
                return character;
            }

            return RetrieveEntityResult<Character>.FromError("No character with that name found.");
        }

        /// <inheritdoc />
        public async Task<RetrieveEntityResult<Character>> GetUserCharacterByNameAsync
        (
            User user,
            Server server,
            string name,
            CancellationToken ct = default
        )
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            var character = await _database.Characters.UserScopedServersideQueryAsync
            (
                user,
                server,
                q => q
                    .Where(ch => string.Equals(ch.Name.ToLower(), name.ToLower()))
                    .SingleOrDefaultAsync(ct)
            );

            if (!(character is null))
            {
                return character;
            }

            return RetrieveEntityResult<Character>.FromError("The user doesn't own a character with that name.");
        }

        /// <inheritdoc />
        public async Task<ModifyEntityResult> MakeCharacterCurrentAsync
        (
            User user,
            Server server,
            Character character,
            CancellationToken ct = default
        )
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            if (character.IsCurrent)
            {
                return ModifyEntityResult.FromError("The character is already current on the server.");
            }

            await ClearCurrentCharacterAsync(user, server, ct);

            character.IsCurrent = true;
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<ModifyEntityResult> ClearCurrentCharacterAsync
        (
            User user,
            Server server,
            CancellationToken ct = default
        )
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            var getCurrentCharacter = await GetCurrentCharacterAsync(user, server, ct);
            if (!getCurrentCharacter.IsSuccess)
            {
                return ModifyEntityResult.FromError(getCurrentCharacter);
            }

            var currentCharacter = getCurrentCharacter.Entity;
            currentCharacter.IsCurrent = false;

            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<bool> HasCurrentCharacterAsync
        (
            User user,
            Server server,
            CancellationToken ct = default
        )
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            var hasCurrent = await _database.Characters.UserScopedServersideQueryAsync
            (
                user,
                server,
                q => q.Where(c => c.IsCurrent).AnyAsync(ct)
            );

            return hasCurrent;
        }

        /// <inheritdoc />
        public async Task<RetrieveEntityResult<Character>> GetDefaultCharacterAsync
        (
            User user,
            Server server,
            CancellationToken ct = default
        )
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            var defaultCharacter = await _database.Characters.UserScopedServersideQueryAsync
            (
                user,
                server,
                q => q
                    .Where(ch => ch.IsDefault)
                    .SingleOrDefaultAsync(ct)
            );

            if (!(defaultCharacter is null))
            {
                return defaultCharacter;
            }

            return RetrieveEntityResult<Character>.FromError("The user doesn't have a default character.");
        }

        /// <inheritdoc />
        public async Task<ModifyEntityResult> SetDefaultCharacterAsync
        (
            User user,
            Server server,
            Character character,
            CancellationToken ct = default
        )
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            if (character.Owner != user)
            {
                return ModifyEntityResult.FromError("The user doesn't own that character.");
            }

            var getDefaultCharacterResult = await GetDefaultCharacterAsync(user, server, ct);
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
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<ModifyEntityResult> ClearDefaultCharacterAsync
        (
            User user,
            Server server,
            CancellationToken ct = default
        )
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            var getDefaultCharacterResult = await GetDefaultCharacterAsync(user, server, ct);
            if (!getDefaultCharacterResult.IsSuccess)
            {
                return ModifyEntityResult.FromError("That user doesn't have a default character.");
            }

            getDefaultCharacterResult.Entity.IsDefault = false;
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<ModifyEntityResult> SetCharacterNameAsync
        (
            Character character,
            string name,
            CancellationToken ct = default
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

            if (!await IsNameUniqueForUserAsync(character.Owner, character.Server, name, ct))
            {
                return ModifyEntityResult.FromError("The user already has a character with that name.");
            }

            character.Name = name;
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<ModifyEntityResult> SetCharacterAvatarAsync
        (
            Character character,
            string avatarUrl,
            CancellationToken ct = default
        )
        {
            avatarUrl = avatarUrl.Unquote(new[] { '<', '>' });

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
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<ModifyEntityResult> SetCharacterNicknameAsync
        (
            Character character,
            string nickname,
            CancellationToken ct = default
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
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<ModifyEntityResult> SetCharacterSummaryAsync
        (
            Character character,
            string summary,
            CancellationToken ct = default
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
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<ModifyEntityResult> SetCharacterDescriptionAsync
        (
            Character character,
            string description,
            CancellationToken ct = default
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
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<ModifyEntityResult> SetCharacterPronounsAsync
        (
            Character character,
            string pronounFamily,
            CancellationToken ct = default
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
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<ModifyEntityResult> SetCharacterIsNSFWAsync
        (
            Character character,
            bool isNSFW,
            CancellationToken ct = default
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
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<ModifyEntityResult> TransferCharacterOwnershipAsync
        (
            User newOwner,
            Server server,
            Character character,
            CancellationToken ct = default
        )
        {
            newOwner = _database.NormalizeReference(newOwner);
            server = _database.NormalizeReference(server);

            var newOwnerCharacters = await _database.Characters.UserScopedServersideQueryAsync
            (
                newOwner,
                server,
                c => c,
                ct
            );

            return _ownedEntities.TransferEntityOwnership
            (
                newOwner,
                newOwnerCharacters,
                character
            );
        }

        /// <inheritdoc />
        public async Task<bool> IsNameUniqueForUserAsync
        (
            User user,
            Server server,
            string characterName,
            CancellationToken ct = default
        )
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            var userCharacters = await _database.Characters.UserScopedServersideQueryAsync
            (
                user,
                server,
                c => c,
                ct
            );

            return _ownedEntities.IsEntityNameUniqueForUser(userCharacters, characterName);
        }

        /// <inheritdoc />
        public async Task<CreateEntityResult<Image>> AddImageToCharacterAsync
        (
            Character character,
            string imageName,
            string imageUrl,
            string? imageCaption = null,
            bool isNSFW = false,
            CancellationToken ct = default
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
            _database.Images.Update(image);

            image.IsNSFW = isNSFW;
            character.Images.Add(image);

            await _database.SaveChangesAsync(ct);

            return image;
        }

        /// <inheritdoc />
        public async Task<DeleteEntityResult> RemoveImageFromCharacterAsync
        (
            Character character,
            Image image,
            CancellationToken ct = default
        )
        {
            if (!character.Images.Contains(image))
            {
                return DeleteEntityResult.FromError("The character has no image with that name.");
            }

            character.Images.Remove(image);
            await _database.SaveChangesAsync(ct);

            return DeleteEntityResult.FromSuccess();
        }
    }
}
