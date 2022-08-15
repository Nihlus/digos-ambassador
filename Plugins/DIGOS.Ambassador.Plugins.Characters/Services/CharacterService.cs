//
//  CharacterService.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Plugins.Characters.Extensions;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Model.Data;
using DIGOS.Ambassador.Plugins.Characters.Services.Interfaces;
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Characters.Services;

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
    public async Task<Result<Character>> CreateCharacterAsync
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

        var result = await SetCharacterNameAsync(character, name, ct);
        if (!result.IsSuccess)
        {
            return Result<Character>.FromError(result);
        }

        result = await SetCharacterAvatarAsync(character, avatarUrl, ct);
        if (!result.IsSuccess)
        {
            return Result<Character>.FromError(result);
        }

        result = await SetCharacterNicknameAsync(character, nickname, ct);
        if (!result.IsSuccess)
        {
            return Result<Character>.FromError(result);
        }

        if (summary is not null)
        {
            result = await SetCharacterSummaryAsync(character, summary, ct);
            if (!result.IsSuccess)
            {
                return Result<Character>.FromError(result);
            }
        }

        if (description is not null)
        {
            result = await SetCharacterDescriptionAsync(character, description, ct);
            if (!result.IsSuccess)
            {
                return Result<Character>.FromError(result);
            }
        }

        result = await SetCharacterPronounsAsync(character, pronounFamily, ct);
        if (!result.IsSuccess)
        {
            return Result<Character>.FromError(result);
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

        return _database.Characters.ServersideQueryAsync
        (
            q => q
                .Where(ch => ch.Owner == user)
                .Where(ch => ch.Server == server),
            ct
        );
    }

    /// <inheritdoc />
    public async Task<Result> DeleteCharacterAsync(Character character, CancellationToken ct = default)
    {
        _database.Characters.Remove(character);
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <inheritdoc />
    public async Task<Result<Character>> GetBestMatchingCharacterAsync
    (
        Server server,
        User? user,
        string? name,
        CancellationToken ct = default
    )
    {
        while (true)
        {
            name = name?.Trim();

            server = _database.NormalizeReference(server);
            if (user is not null)
            {
                user = _database.NormalizeReference(user);
            }

            switch (user)
            {
                case null when name is null:
                {
                    return new UserError("No user and no name specified.");
                }
                case null:
                {
                    return await GetCharacterByNameAsync(server, name, ct);
                }
            }

            if (name.IsNullOrWhitespace())
            {
                return await GetCurrentCharacterAsync(user, server, ct);
            }

            var getUserCharacter = await GetUserCharacterByNameAsync(user, server, name, ct);
            if (getUserCharacter.IsSuccess)
            {
                return getUserCharacter;
            }

            // Search again, but this time globally
            user = null;
        }
    }

    /// <inheritdoc />
    public async Task<Result<Character>> GetCurrentCharacterAsync
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
            return new UserError("The user hasn't assumed a character.");
        }

        var currentCharacter = await _database.Characters.ServersideQueryAsync
        (
            q => q
                .Where(ch => ch.Owner == user)
                .Where(ch => ch.Server == server)
                .Where(ch => ch.IsCurrent)
                .SingleOrDefaultAsync(ct)
        );

        if (currentCharacter is not null)
        {
            return currentCharacter;
        }

        return new UserError("Failed to retrieve a current character.");
    }

    /// <inheritdoc />
    public async Task<Result<Character>> GetCharacterByNameAsync
    (
        Server server,
        string name,
        CancellationToken ct = default
    )
    {
        name = name.Trim();

        server = _database.NormalizeReference(server);

        var characters = await _database.Characters.ServersideQueryAsync
        (
            q => q
                .Where(ch => ch.Server == server)
                .Where(ch => string.Equals(ch.Name.ToLower(), name.ToLower())),
            ct
        );

        if (characters.Count > 1)
        {
            return new UserError
            (
                "There's more than one character with that name. " +
                "Please specify which user it belongs to by searching like this: \"@someone:name\"."
            );
        }

        var character = characters.SingleOrDefault();

        if (character is not null)
        {
            return character;
        }

        return new UserError("No character with that name found.");
    }

    /// <inheritdoc />
    public async Task<Result<Character>> GetUserCharacterByNameAsync
    (
        User user,
        Server server,
        string name,
        CancellationToken ct = default
    )
    {
        name = name.Trim();

        user = _database.NormalizeReference(user);
        server = _database.NormalizeReference(server);

        var character = await _database.Characters.ServersideQueryAsync
        (
            q => q
                .Where(ch => ch.Owner == user)
                .Where(ch => ch.Server == server)
                .Where(ch => string.Equals(ch.Name.ToLower(), name.ToLower()))
                .SingleOrDefaultAsync(ct)
        );

        if (character is not null)
        {
            return character;
        }

        return new UserError("The user doesn't own a character with that name.");
    }

    /// <inheritdoc />
    public async Task<Result> MakeCharacterCurrentAsync
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
            return new UserError("The character is already current on the server.");
        }

        await ClearCurrentCharacterAsync(user, server, ct);

        character.IsCurrent = true;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <inheritdoc />
    public async Task<Result> ClearCurrentCharacterAsync
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
            return Result.FromError(getCurrentCharacter);
        }

        var currentCharacter = getCurrentCharacter.Entity;
        currentCharacter.IsCurrent = false;

        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
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

        var hasCurrent = await _database.Characters.ServersideQueryAsync
        (
            q => q
                .Where(ch => ch.Owner == user)
                .Where(ch => ch.Server == server)
                .Where(c => c.IsCurrent)
                .AnyAsync(ct)
        );

        return hasCurrent;
    }

    /// <inheritdoc />
    public async Task<Result<Character>> GetDefaultCharacterAsync
    (
        User user,
        Server server,
        CancellationToken ct = default
    )
    {
        user = _database.NormalizeReference(user);
        server = _database.NormalizeReference(server);

        var defaultCharacter = await _database.Characters.ServersideQueryAsync
        (
            q => q
                .Where(ch => ch.Owner == user)
                .Where(ch => ch.Server == server)
                .Where(ch => ch.IsDefault)
                .SingleOrDefaultAsync(ct)
        );

        if (defaultCharacter is not null)
        {
            return defaultCharacter;
        }

        return new UserError("The user doesn't have a default character.");
    }

    /// <inheritdoc />
    public async Task<Result> SetDefaultCharacterAsync
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
            return new UserError("The user doesn't own that character.");
        }

        var getDefaultCharacterResult = await GetDefaultCharacterAsync(user, server, ct);
        if (getDefaultCharacterResult.IsSuccess)
        {
            var currentDefault = getDefaultCharacterResult.Entity;
            if (currentDefault == character)
            {
                return new UserError("That's already the user's default character.");
            }

            currentDefault.IsDefault = false;
        }

        character.IsDefault = true;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <inheritdoc />
    public async Task<Result> ClearDefaultCharacterAsync
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
            return new UserError("That user doesn't have a default character.");
        }

        getDefaultCharacterResult.Entity.IsDefault = false;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <inheritdoc />
    public async Task<Result> SetCharacterNameAsync
    (
        Character character,
        string name,
        CancellationToken ct = default
    )
    {
        name = name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return new UserError("You need to provide a name.");
        }

        if (string.Equals(character.Name, name, StringComparison.OrdinalIgnoreCase))
        {
            return new UserError("The character already has that name.");
        }

        if (name.Contains('"'))
        {
            return new UserError("The name may not contain double quotes.");
        }

        if (name.Contains(':'))
        {
            return new UserError("The name may not contain colons.");
        }

        if (!await IsNameUniqueForUserAsync(character.Owner, character.Server, name, ct))
        {
            return new UserError("The user already has a character with that name.");
        }

        character.Name = name;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <inheritdoc />
    public async Task<Result> SetCharacterAvatarAsync
    (
        Character character,
        string avatarUrl,
        CancellationToken ct = default
    )
    {
        avatarUrl = avatarUrl.Trim();
        avatarUrl = avatarUrl.Unquote(new[] { '<', '>' });

        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            return new UserError("You need to provide a new avatar url.");
        }

        if (!Uri.TryCreate(avatarUrl, UriKind.Absolute, out _))
        {
            return new UserError("The given image URL wasn't valid.");
        }

        if (character.AvatarUrl == avatarUrl)
        {
            return new UserError("The character's avatar is already set to that URL.");
        }

        character.AvatarUrl = avatarUrl;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <inheritdoc />
    public async Task<Result> SetCharacterNicknameAsync
    (
        Character character,
        string nickname,
        CancellationToken ct = default
    )
    {
        nickname = nickname.Trim();

        if (string.IsNullOrWhiteSpace(nickname))
        {
            return new UserError("You need to provide a new nickname.");
        }

        if (character.Nickname == nickname)
        {
            return new UserError("The character already has that nickname.");
        }

        if (nickname.Length > 32)
        {
            return new UserError("The nickname is too long. It can be at most 32 characters.");
        }

        character.Nickname = nickname;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <inheritdoc />
    public async Task<Result> SetCharacterSummaryAsync
    (
        Character character,
        string summary,
        CancellationToken ct = default
    )
    {
        summary = summary.Trim();

        if (string.IsNullOrWhiteSpace(summary))
        {
            return new UserError("You need to provide a new summary.");
        }

        if (character.Summary == summary)
        {
            return new UserError("That's already the character's summary.");
        }

        if (summary.Length > 240)
        {
            return new UserError("The summary is too long. It can be at most 240 characters.");
        }

        character.Summary = summary;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <inheritdoc />
    public async Task<Result> SetCharacterDescriptionAsync
    (
        Character character,
        string description,
        CancellationToken ct = default
    )
    {
        description = description.Trim();

        if (string.IsNullOrWhiteSpace(description))
        {
            return new UserError("You need to provide a new description.");
        }

        if (character.Description == description)
        {
            return new UserError("The character already has that description.");
        }

        if (description.Length > 1000)
        {
            return new UserError("The description is too long. It can be at most 1000 characters.");
        }
        character.Description = description;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <inheritdoc />
    public async Task<Result> SetCharacterPronounsAsync
    (
        Character character,
        string pronounFamily,
        CancellationToken ct = default
    )
    {
        pronounFamily = pronounFamily.Trim();

        if (pronounFamily.IsNullOrWhitespace())
        {
            return new UserError("You need to provide a pronoun family.");
        }

        if (character.PronounProviderFamily == pronounFamily)
        {
            return new UserError("The character is already using that pronoun set.");
        }

        var getPronounProviderResult = _pronouns.GetPronounProvider(pronounFamily);
        if (!getPronounProviderResult.IsSuccess)
        {
            return Result.FromError(getPronounProviderResult);
        }

        var pronounProvider = getPronounProviderResult.Entity;
        character.PronounProviderFamily = pronounProvider.Family;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <inheritdoc />
    public async Task<Result> SetCharacterIsNSFWAsync
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

            return new UserError(message);
        }

        character.IsNSFW = isNSFW;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <inheritdoc />
    public async Task<Result> TransferCharacterOwnershipAsync
    (
        User newOwner,
        Server server,
        Character character,
        CancellationToken ct = default
    )
    {
        newOwner = _database.NormalizeReference(newOwner);
        server = _database.NormalizeReference(server);

        var newOwnerCharacters = await _database.Characters.ServersideQueryAsync
        (
            c => c
                .Where(ch => ch.Owner == newOwner)
                .Where(ch => ch.Server == server),
            ct
        );

        return await _ownedEntities.TransferEntityOwnershipAsync
        (
            newOwner.DiscordID,
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
        characterName = characterName.Trim();

        user = _database.NormalizeReference(user);
        server = _database.NormalizeReference(server);

        var userCharacters = await _database.Characters.ServersideQueryAsync
        (
            c => c
                .Where(ch => ch.Owner == user)
                .Where(ch => ch.Server == server),
            ct
        );

        return OwnedEntityService.IsEntityNameUniqueForUser(userCharacters, characterName);
    }

    /// <inheritdoc />
    public async Task<Result<Image>> AddImageToCharacterAsync
    (
        Character character,
        string imageName,
        string imageUrl,
        string? imageCaption = null,
        bool isNSFW = false,
        CancellationToken ct = default
    )
    {
        imageCaption ??= "No caption set.";

        imageName = imageName.Trim();
        imageUrl = imageUrl.Trim();
        imageCaption = imageCaption.Trim();

        var isImageNameUnique = !character.Images.Any(i => string.Equals(i.Name.ToLower(), imageName.ToLower()));
        if (!isImageNameUnique)
        {
            return new UserError("The character already has an image with that name.");
        }

        if (imageName.IsNullOrWhitespace())
        {
            return new UserError("You need to specify a name.");
        }

        if (!Uri.IsWellFormedUriString(imageUrl, UriKind.RelativeOrAbsolute))
        {
            return new UserError
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
    public async Task<Result> RemoveImageFromCharacterAsync
    (
        Character character,
        Image image,
        CancellationToken ct = default
    )
    {
        if (!character.Images.Contains(image))
        {
            return new UserError("The character has no image with that name.");
        }

        character.Images.Remove(image);
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }
}
