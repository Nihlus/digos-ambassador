//
//  TransformationService.cs
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
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Transformations.Extensions;
using DIGOS.Ambassador.Plugins.Transformations.Model;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Results;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using DIGOS.Ambassador.Plugins.Transformations.Transformations.Shifters;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Transformations.Services;

/// <summary>
/// Handles transformations of users and their characters.
/// </summary>
[PublicAPI]
public sealed class TransformationService
{
    private readonly TransformationsDatabaseContext _database;
    private readonly UserService _users;
    private readonly ServerService _servers;
    private readonly ContentService _content;

    private TransformationDescriptionBuilder _descriptionBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransformationService"/> class.
    /// </summary>
    /// <param name="content">The content service.</param>
    /// <param name="users">The user service.</param>
    /// <param name="servers">The server service.</param>
    /// <param name="database">The database.</param>
    /// <param name="descriptionBuilder">The description builder.</param>
    public TransformationService
    (
        ContentService content,
        UserService users,
        ServerService servers,
        TransformationsDatabaseContext database,
        TransformationDescriptionBuilder descriptionBuilder
    )
    {
        _content = content;
        _users = users;
        _servers = servers;
        _database = database;
        _descriptionBuilder = descriptionBuilder;
    }

    /// <summary>
    /// Gets the default appearance for the given character, or creates one if one does not exist.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    public async Task<Result<Appearance>> GetOrCreateDefaultAppearanceAsync
    (
        Character character,
        CancellationToken ct = default
    )
    {
        var getDefaultAppearance = await GetDefaultAppearanceAsync(character, ct);
        if (getDefaultAppearance.IsSuccess)
        {
            return getDefaultAppearance;
        }

        var createDefaultAppearanceResult = await Appearance.CreateDefaultAsync(character, this);
        if (!createDefaultAppearanceResult.IsSuccess)
        {
            return Result<Appearance>.FromError(createDefaultAppearanceResult);
        }

        var defaultAppearance = createDefaultAppearanceResult.Entity;
        defaultAppearance.IsDefault = true;

        _database.Appearances.Update(defaultAppearance);
        await _database.SaveChangesAsync(ct);

        return defaultAppearance;
    }

    /// <summary>
    /// Gets the given character's default appearance.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    private async Task<Result<Appearance>> GetDefaultAppearanceAsync
    (
        Character character,
        CancellationToken ct = default
    )
    {
        var appearance = await _database.Appearances.ServersideQueryAsync
        (
            q => q
                .Where(da => da.Character == character)
                .Where(da => da.IsDefault)
                .SingleOrDefaultAsync(ct)
        );

        if (appearance is not null)
        {
            return appearance;
        }

        return new UserError("HThe character doesn't have a default appearance.");
    }

    /// <summary>
    /// Gets the current appearance for the given character, cloning it from the default appearance if one does not
    /// exist.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    public async Task<Result<Appearance>> GetOrCreateCurrentAppearanceAsync
    (
        Character character,
        CancellationToken ct = default
    )
    {
        var getCurrentAppearanceResult = await GetCurrentAppearanceAsync(character, ct);
        if (getCurrentAppearanceResult.IsSuccess)
        {
            return getCurrentAppearanceResult;
        }

        // There's no current appearance, so we'll grab the default one and copy it
        var getDefaultAppearance = await GetOrCreateDefaultAppearanceAsync(character, ct);
        if (!getDefaultAppearance.IsSuccess)
        {
            return Result<Appearance>.FromError(getDefaultAppearance);
        }

        var defaultAppearance = getDefaultAppearance.Entity;

        var currentAppearance = Appearance.CopyFrom(defaultAppearance);
        currentAppearance.IsCurrent = true;

        _database.Appearances.Update(currentAppearance);
        await _database.SaveChangesAsync(ct);

        return currentAppearance;
    }

    /// <summary>
    /// Gets the given character's current appearance.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    private async Task<Result<Appearance>> GetCurrentAppearanceAsync
    (
        Character character,
        CancellationToken ct = default
    )
    {
        var appearance = await _database.Appearances.ServersideQueryAsync
        (
            q => q
                .Where(da => da.Character == character)
                .Where(da => da.IsCurrent)
                .SingleOrDefaultAsync(ct)
        );

        if (appearance is not null)
        {
            return appearance;
        }

        return new UserError("The character doesn't have a current appearance.");
    }

    /// <summary>
    /// Removes the given character's bodypart.
    /// </summary>
    /// <param name="invokingUser">The user that's performing the action.</param>
    /// <param name="character">The character to shift.</param>
    /// <param name="bodyPart">The bodypart to remove.</param>
    /// <param name="chirality">The chirality of the bodypart.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A shifting result which may or may not have succeeded.</returns>
    public async Task<Result<ShiftBodypartResult>> RemoveBodypartAsync
    (
        Snowflake invokingUser,
        Character character,
        Bodypart bodyPart,
        Chirality chirality = Chirality.Center,
        CancellationToken ct = default
    )
    {
        var canTransformResult = await CanUserTransformUserAsync
        (
            character.Server.DiscordID,
            invokingUser,
            character.Owner.DiscordID,
            ct
        );

        if (!canTransformResult.IsSuccess)
        {
            return Result<ShiftBodypartResult>.FromError(canTransformResult);
        }

        var getAppearanceResult = await GetOrCreateCurrentAppearanceAsync(character, ct);
        if (!getAppearanceResult.IsSuccess)
        {
            return Result<ShiftBodypartResult>.FromError(getAppearanceResult);
        }

        var appearance = getAppearanceResult.Entity;

        var bodypartRemover = new BodypartRemover
        (
            appearance,
            _descriptionBuilder
        );

        var shiftResult = await bodypartRemover.RemoveAsync(bodyPart, chirality);
        if (shiftResult.IsSuccess)
        {
            await _database.SaveChangesAsync(ct);
        }

        return shiftResult;
    }

    /// <summary>
    /// Removes the given character's bodypart.
    /// </summary>
    /// <param name="invokingUser">The user that's performing the action.</param>
    /// <param name="character">The character to shift.</param>
    /// <param name="bodyPart">The bodypart to remove.</param>
    /// <param name="chirality">The chirality of the bodypart.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A shifting result which may or may not have succeeded.</returns>
    public async Task<Result<ShiftBodypartResult>> RemoveBodypartPatternAsync
    (
        Snowflake invokingUser,
        Character character,
        Bodypart bodyPart,
        Chirality chirality = Chirality.Center,
        CancellationToken ct = default
    )
    {
        var canTransformResult = await CanUserTransformUserAsync
        (
            character.Server.DiscordID,
            invokingUser,
            character.Owner.DiscordID,
            ct
        );

        if (!canTransformResult.IsSuccess)
        {
            return Result<ShiftBodypartResult>.FromError(canTransformResult);
        }

        var getAppearanceResult = await GetOrCreateCurrentAppearanceAsync(character, ct);
        if (!getAppearanceResult.IsSuccess)
        {
            return Result<ShiftBodypartResult>.FromError(getAppearanceResult);
        }

        var appearance = getAppearanceResult.Entity;

        var patternRemover = new PatternRemover(appearance, _descriptionBuilder);
        var shiftResult = await patternRemover.RemoveAsync(bodyPart, chirality);

        if (shiftResult.IsSuccess)
        {
            await _database.SaveChangesAsync(ct);
        }

        return shiftResult;
    }

    /// <summary>
    /// Shifts the given character's bodypart to the given species.
    /// </summary>
    /// <param name="invokingUser">The user that's performing the action.</param>
    /// <param name="character">The character to shift.</param>
    /// <param name="bodyPart">The bodypart to shift.</param>
    /// <param name="speciesName">The species to shift the bodypart into.</param>
    /// <param name="chirality">The chirality of the bodypart.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A shifting result which may or may not have succeeded.</returns>
    public async Task<Result<ShiftBodypartResult>> ShiftBodypartAsync
    (
        Snowflake invokingUser,
        Character character,
        Bodypart bodyPart,
        string speciesName,
        Chirality chirality = Chirality.Center,
        CancellationToken ct = default
    )
    {
        var canTransformResult = await CanUserTransformUserAsync
        (
            character.Server.DiscordID,
            invokingUser,
            character.Owner.DiscordID,
            ct
        );

        if (!canTransformResult.IsSuccess)
        {
            return Result<ShiftBodypartResult>.FromError(canTransformResult);
        }

        var getSpeciesResult = await GetSpeciesByNameAsync(speciesName, ct);
        if (!getSpeciesResult.IsSuccess)
        {
            return Result<ShiftBodypartResult>.FromError(getSpeciesResult);
        }

        var species = getSpeciesResult.Entity;

        var getCurrentAppearance = await GetOrCreateCurrentAppearanceAsync(character, ct);
        if (!getCurrentAppearance.IsSuccess)
        {
            return Result<ShiftBodypartResult>.FromError(getCurrentAppearance);
        }

        var appearance = getCurrentAppearance.Entity;

        var speciesShifter = new SpeciesShifter(appearance, species, this, _descriptionBuilder);
        var shiftResult = await speciesShifter.ShiftAsync(bodyPart, chirality);

        if (shiftResult.IsSuccess)
        {
            await _database.SaveChangesAsync(ct);
        }

        return shiftResult;
    }

    /// <summary>
    /// Shifts the colour of the given bodypart on the given character to the given colour.
    /// </summary>
    /// <param name="invokingUser">The user that's performing the action.</param>
    /// <param name="character">The character to shift.</param>
    /// <param name="bodyPart">The bodypart to shift.</param>
    /// <param name="colour">The colour to shift it into.</param>
    /// <param name="chirality">The chirality of the bodypart.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A shifting result which may or may not have succeeded.</returns>
    public async Task<Result<ShiftBodypartResult>> ShiftBodypartColourAsync
    (
        Snowflake invokingUser,
        Character character,
        Bodypart bodyPart,
        Colour colour,
        Chirality chirality = Chirality.Center,
        CancellationToken ct = default
    )
    {
        var canTransformResult = await CanUserTransformUserAsync
        (
            character.Server.DiscordID,
            invokingUser,
            character.Owner.DiscordID,
            ct
        );

        if (!canTransformResult.IsSuccess)
        {
            return Result<ShiftBodypartResult>.FromError(canTransformResult);
        }

        if (bodyPart.IsChiral() && chirality == Chirality.Center)
        {
            return new UserError
            (
                $"Please specify if it's the left or right {bodyPart.Humanize().ToLower()}."
            );
        }

        var getCurrentAppearance = await GetOrCreateCurrentAppearanceAsync(character, ct);
        if (!getCurrentAppearance.IsSuccess)
        {
            return Result<ShiftBodypartResult>.FromError(getCurrentAppearance);
        }

        var appearance = getCurrentAppearance.Entity;

        var colourShifter = new ColourShifter(appearance, colour, _descriptionBuilder);
        var shiftResult = await colourShifter.ShiftAsync(bodyPart, chirality);

        if (shiftResult.IsSuccess)
        {
            await _database.SaveChangesAsync(ct);
        }

        return shiftResult;
    }

    /// <summary>
    /// Shifts the pattern of the given bodypart on the given character to the given pattern with the given colour.
    /// </summary>
    /// <param name="invokingUser">The user that's performing the action.</param>
    /// <param name="character">The character to shift.</param>
    /// <param name="bodyPart">The bodypart to shift.</param>
    /// <param name="pattern">The pattern to shift the bodypart into.</param>
    /// <param name="patternColour">The colour to shift it into.</param>
    /// <param name="chirality">The chirality of the bodypart.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A shifting result which may or may not have succeeded.</returns>
    public async Task<Result<ShiftBodypartResult>> ShiftBodypartPatternAsync
    (
        Snowflake invokingUser,
        Character character,
        Bodypart bodyPart,
        Pattern pattern,
        Colour patternColour,
        Chirality chirality = Chirality.Center,
        CancellationToken ct = default
    )
    {
        var canTransformResult = await CanUserTransformUserAsync
        (
            character.Server.DiscordID,
            invokingUser,
            character.Owner.DiscordID,
            ct
        );

        if (!canTransformResult.IsSuccess)
        {
            return Result<ShiftBodypartResult>.FromError(canTransformResult);
        }

        var getAppearanceResult = await GetOrCreateCurrentAppearanceAsync(character, ct);
        if (!getAppearanceResult.IsSuccess)
        {
            return Result<ShiftBodypartResult>.FromError(getAppearanceResult);
        }

        var appearance = getAppearanceResult.Entity;

        var patternShifter = new PatternShifter(appearance, pattern, patternColour, _descriptionBuilder);
        var shiftResult = await patternShifter.ShiftAsync(bodyPart, chirality);

        if (shiftResult.IsSuccess)
        {
            await _database.SaveChangesAsync(ct);
        }

        return shiftResult;
    }

    /// <summary>
    /// Shifts the colour of the given bodypart's pattern on the given character to the given colour.
    /// </summary>
    /// <param name="invokingUser">The user that's performing the action.</param>
    /// <param name="character">The character to shift.</param>
    /// <param name="bodyPart">The bodypart to shift.</param>
    /// <param name="patternColour">The colour to shift it into.</param>
    /// <param name="chirality">The chirality of the bodypart.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A shifting result which may or may not have succeeded.</returns>
    public async Task<Result<ShiftBodypartResult>> ShiftPatternColourAsync
    (
        Snowflake invokingUser,
        Character character,
        Bodypart bodyPart,
        Colour patternColour,
        Chirality chirality = Chirality.Center,
        CancellationToken ct = default
    )
    {
        var canTransformResult = await CanUserTransformUserAsync
        (
            character.Server.DiscordID,
            invokingUser,
            character.Owner.DiscordID,
            ct
        );

        if (!canTransformResult.IsSuccess)
        {
            return Result<ShiftBodypartResult>.FromError(canTransformResult);
        }

        var getAppearanceResult = await GetOrCreateCurrentAppearanceAsync(character, ct);
        if (!getAppearanceResult.IsSuccess)
        {
            return Result<ShiftBodypartResult>.FromError(getAppearanceResult);
        }

        var appearance = getAppearanceResult.Entity;

        var patternColourShifter = new PatternColourShifter(appearance, patternColour, _descriptionBuilder);
        var shiftResult = await patternColourShifter.ShiftAsync(bodyPart, chirality);

        if (shiftResult.IsSuccess)
        {
            await _database.SaveChangesAsync(ct);
        }

        return shiftResult;
    }

    /// <summary>
    /// Determines whether or not a user is allowed to transform another user.
    /// </summary>
    /// <param name="discordServer">The server the users are on.</param>
    /// <param name="invokingUser">The user trying to transform.</param>
    /// <param name="targetUser">The user being transformed.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A conditional determination with an attached reason if it failed.</returns>
    public async Task<Result> CanUserTransformUserAsync
    (
        Snowflake discordServer,
        Snowflake invokingUser,
        Snowflake targetUser,
        CancellationToken ct = default
    )
    {
        var getLocalProtectionResult = await GetOrCreateServerUserProtectionAsync(targetUser, discordServer, ct);
        if (!getLocalProtectionResult.IsSuccess)
        {
            return Result.FromError(getLocalProtectionResult);
        }

        var localProtection = getLocalProtectionResult.Entity;

        if (!localProtection.HasOptedIn)
        {
            return new UserError("The target hasn't opted into transformations.");
        }

        var getGlobalProtectionResult = await GetOrCreateGlobalUserProtectionAsync(targetUser, ct);
        if (!getGlobalProtectionResult.IsSuccess)
        {
            return Result.FromError(getGlobalProtectionResult);
        }

        var globalProtection = getGlobalProtectionResult.Entity;
        return localProtection.Type switch
        {
            ProtectionType.Blacklist => globalProtection.Blacklist.All(u => u.DiscordID != invokingUser)
                ? Result.FromSuccess()
                : new UserError("You're on that user's blacklist."),
            ProtectionType.Whitelist => globalProtection.Whitelist.Any(u => u.DiscordID == invokingUser)
                ? Result.FromSuccess()
                : new UserError("You're not on that user's whitelist."),
            _ => throw new ArgumentOutOfRangeException(nameof(localProtection.Type))
        };
    }

    /// <summary>
    /// Generate a complete textual description of the given character, and format it into an embed.
    /// </summary>
    /// <param name="character">The character to generate the description for.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>An embed with a formatted description.</returns>
    public async Task<Result<string>> GenerateCharacterDescriptionAsync
    (
        Character character,
        CancellationToken ct = default
    )
    {
        var getCurrentAppearance = await GetOrCreateCurrentAppearanceAsync
        (
            character, ct);

        if (!getCurrentAppearance.IsSuccess)
        {
            return Result<string>.FromError(getCurrentAppearance);
        }

        var currentAppearance = getCurrentAppearance.Entity;

        var visualDescription = _descriptionBuilder.BuildVisualDescription(currentAppearance);
        return Result<string>.FromSuccess(visualDescription);
    }

    /// <summary>
    /// Gets the available species in transformations.
    /// </summary>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A list of the available species.</returns>
    public async Task<IReadOnlyList<Species>> GetAvailableSpeciesAsync(CancellationToken ct = default)
    {
        return await _database.Species.ServersideQueryAsync(q => q, ct);
    }

    /// <summary>
    /// Gets the available transformations for the given bodypart.
    /// </summary>
    /// <param name="bodyPart">The bodypart to get the transformations for.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A list of the available transformations.</returns>
    public async Task<IReadOnlyList<Transformation>> GetAvailableTransformationsAsync
    (
        Bodypart bodyPart,
        CancellationToken ct = default
    )
    {
        return await _database.Transformations.ServersideQueryAsync
        (
            q => q.Where(tf => tf.Part == bodyPart),
            ct
        );
    }

    /// <summary>
    /// Resets the given character's appearance to its default state.
    /// </summary>
    /// <param name="character">The character to reset.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>An entity modification result which may or may not have succeeded.</returns>
    public async Task<Result> ResetCharacterFormAsync
    (
        Character character,
        CancellationToken ct = default
    )
    {
        var getDefaultAppearanceResult = await GetOrCreateDefaultAppearanceAsync(character, ct);
        if (!getDefaultAppearanceResult.IsSuccess)
        {
            return Result.FromError(getDefaultAppearanceResult);
        }

        var getCurrentAppearance = await GetCurrentAppearanceAsync(character, ct);
        if (getCurrentAppearance.IsSuccess)
        {
            // Delete the existing current appearance
            _database.Appearances.Remove(getCurrentAppearance.Entity);
        }

        // Piggyback on the get/create method to clone the default appearance
        var createNewCurrentResult = await GetOrCreateCurrentAppearanceAsync(character, ct);
        if (!createNewCurrentResult.IsSuccess)
        {
            return Result.FromError(createNewCurrentResult);
        }

        await _database.SaveChangesAsync(ct);
        return Result.FromSuccess();
    }

    /// <summary>
    /// Sets the current appearance of the given character as its default appearance.
    /// </summary>
    /// <param name="character">The character to set the default appearance of.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>An entity modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetCurrentAppearanceAsDefaultForCharacterAsync
    (
        Character character,
        CancellationToken ct = default
    )
    {
        // First, erase the existing default
        var getExistingDefaultAppearance = await GetOrCreateDefaultAppearanceAsync(character, ct);
        if (getExistingDefaultAppearance.IsSuccess)
        {
            var existingDefault = getExistingDefaultAppearance.Entity;

            _database.Appearances.Remove(existingDefault);
        }

        // Then, get the existing current appearance
        var getCurrentAppearance = await GetOrCreateCurrentAppearanceAsync(character, ct);
        if (!getCurrentAppearance.IsSuccess)
        {
            return Result.FromError(getCurrentAppearance);
        }

        var existingCurrentAppearance = getCurrentAppearance.Entity;

        // Flip the flags. Note that we don't create a new current appearance right away, because one will
        // automatically be created if one is requested.
        existingCurrentAppearance.IsDefault = true;
        existingCurrentAppearance.IsCurrent = false;

        await _database.SaveChangesAsync(ct);
        return Result.FromSuccess();
    }

    /// <summary>
    /// Sets the default protection type that the user has for transformations.
    /// </summary>
    /// <param name="discordUser">The user to set the protection for.</param>
    /// <param name="protectionType">The protection type to set.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>An entity modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetDefaultProtectionTypeAsync
    (
        Snowflake discordUser,
        ProtectionType protectionType,
        CancellationToken ct = default
    )
    {
        var getGlobalProtectionResult = await GetOrCreateGlobalUserProtectionAsync(discordUser, ct);
        if (!getGlobalProtectionResult.IsSuccess)
        {
            return Result.FromError(getGlobalProtectionResult);
        }

        var protection = getGlobalProtectionResult.Entity;

        if (protection.DefaultType == protectionType)
        {
            return new UserError($"{protectionType.Humanize()} is already your default setting.");
        }

        protection.DefaultType = protectionType;

        await _database.SaveChangesAsync(ct);
        return Result.FromSuccess();
    }

    /// <summary>
    /// Sets the protection type that the user has for transformations on the given server.
    /// </summary>
    /// <param name="discordUser">The user to set the protection for.</param>
    /// <param name="discordServer">The server to set the protection on.</param>
    /// <param name="protectionType">The protection type to set.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>An entity modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetServerProtectionTypeAsync
    (
        Snowflake discordUser,
        Snowflake discordServer,
        ProtectionType protectionType,
        CancellationToken ct = default
    )
    {
        var getServerProtectionResult = await GetOrCreateServerUserProtectionAsync(discordUser, discordServer, ct);
        if (!getServerProtectionResult.IsSuccess)
        {
            return Result.FromError(getServerProtectionResult);
        }

        var protection = getServerProtectionResult.Entity;

        if (protection.Type == protectionType)
        {
            return new UserError($"{protectionType.Humanize()} is already your current setting.");
        }

        protection.Type = protectionType;

        await _database.SaveChangesAsync(ct);
        return Result.FromSuccess();
    }

    /// <summary>
    /// Whitelists the given user, allowing them to transform the <paramref name="discordUser"/>.
    /// </summary>
    /// <param name="discordUser">The user to modify.</param>
    /// <param name="whitelistedUser">The user to add to the whitelist.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>An entity modification result which may or may not have succeeded.</returns>
    public async Task<Result> WhitelistUserAsync
    (
        Snowflake discordUser,
        Snowflake whitelistedUser,
        CancellationToken ct = default
    )
    {
        if (discordUser == whitelistedUser)
        {
            return new UserError("You can't whitelist yourself.");
        }

        var getGlobalProtectionResult = await GetOrCreateGlobalUserProtectionAsync(discordUser, ct);
        if (!getGlobalProtectionResult.IsSuccess)
        {
            return Result.FromError(getGlobalProtectionResult);
        }

        var protection = getGlobalProtectionResult.Entity;

        if (protection.Whitelist.Any(u => u.DiscordID == whitelistedUser))
        {
            return new UserError("You've already whitelisted that user.");
        }

        var protectionEntry = protection.UserListing.FirstOrDefault(u => u.User.DiscordID == discordUser);
        if (protectionEntry is null)
        {
            var getUserResult = await _users.GetOrRegisterUserAsync(whitelistedUser, ct);
            if (!getUserResult.IsSuccess)
            {
                return Result.FromError(getUserResult);
            }

            var user = getUserResult.Entity;

            protectionEntry = _database.CreateProxy<UserProtectionEntry>(protection, user);
            _database.Update(protectionEntry);

            protectionEntry.Type = ListingType.Whitelist;
            protection.UserListing.Add(protectionEntry);
        }
        else
        {
            protectionEntry.Type = ListingType.Whitelist;
        }

        await _database.SaveChangesAsync(ct);
        return Result.FromSuccess();
    }

    /// <summary>
    /// Blacklists the given user, preventing them from transforming the <paramref name="discordUser"/>.
    /// </summary>
    /// <param name="discordUser">The user to modify.</param>
    /// <param name="blacklistedUser">The user to add to the blacklist.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>An entity modification result which may or may not have succeeded.</returns>
    public async Task<Result> BlacklistUserAsync
    (
        Snowflake discordUser,
        Snowflake blacklistedUser,
        CancellationToken ct = default
    )
    {
        if (discordUser == blacklistedUser)
        {
            return new UserError("You can't blacklist yourself.");
        }

        var getGlobalProtectionResult = await GetOrCreateGlobalUserProtectionAsync(discordUser, ct);
        if (!getGlobalProtectionResult.IsSuccess)
        {
            return Result.FromError(getGlobalProtectionResult);
        }

        var protection = getGlobalProtectionResult.Entity;

        if (protection.Blacklist.Any(u => u.DiscordID == blacklistedUser))
        {
            return new UserError("You've already blacklisted that user.");
        }

        var protectionEntry = protection.UserListing.FirstOrDefault(u => u.User.DiscordID == discordUser);
        if (protectionEntry is null)
        {
            var getUserResult = await _users.GetOrRegisterUserAsync(blacklistedUser, ct);
            if (!getUserResult.IsSuccess)
            {
                return Result.FromError(getUserResult);
            }

            var user = getUserResult.Entity;

            protectionEntry = _database.CreateProxy<UserProtectionEntry>(protection, user);
            _database.Update(protectionEntry);

            protectionEntry.Type = ListingType.Blacklist;
            protection.UserListing.Add(protectionEntry);
        }
        else
        {
            protectionEntry.Type = ListingType.Blacklist;
        }

        await _database.SaveChangesAsync(ct);
        return Result.FromSuccess();
    }

    /// <summary>
    /// Gets or creates the global transformation protection data for the given user.
    /// </summary>
    /// <param name="discordUser">The user.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>Global protection data for the given user.</returns>
    public async Task<Result<GlobalUserProtection>> GetOrCreateGlobalUserProtectionAsync
    (
        Snowflake discordUser,
        CancellationToken ct = default
    )
    {
        var protection = await _database.GlobalUserProtections.ServersideQueryAsync
        (
            q => q
                .Where(p => p.User.DiscordID == discordUser)
                .SingleOrDefaultAsync(cancellationToken: ct)
        );

        if (protection is not null)
        {
            return protection;
        }

        var getUserResult = await _users.GetOrRegisterUserAsync(discordUser, ct);
        if (!getUserResult.IsSuccess)
        {
            return Result<GlobalUserProtection>.FromError(getUserResult);
        }

        var user = getUserResult.Entity;

        protection = _database.CreateProxy<GlobalUserProtection>(user);
        _database.GlobalUserProtections.Update(protection);

        await _database.SaveChangesAsync(ct);
        return protection;
    }

    /// <summary>
    /// Gets or creates server-specific transformation protection data for the given user and server.
    /// </summary>
    /// <param name="discordUser">The user.</param>
    /// <param name="guild">The server.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>Server-specific protection data for the given user.</returns>
    public async Task<Result<ServerUserProtection>> GetOrCreateServerUserProtectionAsync
    (
        Snowflake discordUser,
        Snowflake guild,
        CancellationToken ct = default
    )
    {
        var protection = await _database.ServerUserProtections.ServersideQueryAsync
        (
            q => q
                .Where(p => p.User.DiscordID == discordUser)
                .Where(p => p.Server.DiscordID == guild)
                .SingleOrDefaultAsync(ct)
        );

        if (protection is not null)
        {
            return protection;
        }

        var getServerResult = await _servers.GetOrRegisterServerAsync(guild, ct);
        if (!getServerResult.IsSuccess)
        {
            return Result<ServerUserProtection>.FromError(getServerResult);
        }

        var server = getServerResult.Entity;
        server = _database.NormalizeReference(server);

        var getGlobalProtectionResult = await GetOrCreateGlobalUserProtectionAsync(discordUser, ct);
        if (!getGlobalProtectionResult.IsSuccess)
        {
            return Result<ServerUserProtection>.FromError(getGlobalProtectionResult);
        }

        var globalProtection = getGlobalProtectionResult.Entity;

        var newProtection = _database.CreateProxy<ServerUserProtection>(server, globalProtection.User);
        _database.ServerUserProtections.Update(newProtection);

        newProtection.Type = globalProtection.DefaultType;
        newProtection.HasOptedIn = globalProtection.DefaultOptIn;

        await _database.SaveChangesAsync(ct);
        return newProtection;
    }

    /// <summary>
    /// Updates the database with new or changed transformations.
    /// </summary>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>An update result which may or may not have succeeded.</returns>
    public async Task<Result<UpdateTransformationsResult>> UpdateTransformationDatabaseAsync
    (
        CancellationToken ct = default
    )
    {
        uint addedSpecies = 0;
        uint updatedSpecies = 0;

        var bundledSpeciesResult = await _content.DiscoverBundledSpeciesAsync();
        if (!bundledSpeciesResult.IsSuccess)
        {
            return Result<UpdateTransformationsResult>.FromError(bundledSpeciesResult);
        }

        foreach (var species in bundledSpeciesResult.Entity.OrderBy(s => s.GetSpeciesDepth()))
        {
            if (await IsSpeciesNameUniqueAsync(species.Name, ct))
            {
                // Add a new species
                _database.Species.Update(species);
                ++addedSpecies;
            }
            else
            {
                var getExistingSpecies = await GetSpeciesByNameAsync(species.Name, ct);
                if (!getExistingSpecies.IsSuccess)
                {
                    return Result<UpdateTransformationsResult>.FromError(getExistingSpecies);
                }

                // There's an existing species with this name
                var existingSpecies = getExistingSpecies.Entity;

                species.ID = existingSpecies.ID;

                var existingEntry = _database.Entry(existingSpecies);
                existingEntry.CurrentValues.SetValues(species);

                if (existingEntry.State == EntityState.Modified)
                {
                    ++updatedSpecies;
                }
            }
        }

        await _database.SaveChangesAsync(ct);

        uint addedTransformations = 0;
        uint updatedTransformations = 0;

        var availableSpecies = await GetAvailableSpeciesAsync(ct);
        foreach (var species in availableSpecies)
        {
            var bundledTransformationsResult = await _content.DiscoverBundledTransformationsAsync(this, species);
            if (!bundledTransformationsResult.IsSuccess)
            {
                return Result<UpdateTransformationsResult>.FromError(bundledTransformationsResult);
            }

            foreach (var transformation in bundledTransformationsResult.Entity)
            {
                if (await IsPartAndSpeciesCombinationUniqueAsync(transformation.Part, transformation.Species, ct))
                {
                    // Add a new transformation
                    _database.Transformations.Update(transformation);
                    ++addedTransformations;
                }
                else
                {
                    var getExistingTransformation = await GetTransformationsByPartAndSpeciesAsync
                    (
                        transformation.Part,
                        transformation.Species,
                        ct
                    );

                    if (!getExistingTransformation.IsSuccess)
                    {
                        return Result<UpdateTransformationsResult>.FromError(getExistingTransformation);
                    }

                    // We just take the first one, since species can't define composite parts individually
                    var existingTransformation = getExistingTransformation.Entity[0];

                    // Override the new data's ID to match the existing one
                    transformation.ID = existingTransformation.ID;

                    var existingEntry = _database.Entry(existingTransformation);
                    existingEntry.CurrentValues.SetValues(transformation);

                    // Workarounds for some broken EF core behaviour
                    var baseColourNeedsUpdate = !existingTransformation.DefaultBaseColour
                        .IsSameColourAs(transformation.DefaultBaseColour);

                    if (baseColourNeedsUpdate)
                    {
                        // This catches null-to-null changes
                        if (existingTransformation.DefaultBaseColour != transformation.DefaultBaseColour)
                        {
                            existingEntry.State = EntityState.Modified;
                        }

                        existingTransformation.DefaultBaseColour =
                            transformation.DefaultBaseColour.Clone();
                    }

                    var patternColourNeedsUpdate = existingTransformation.DefaultPatternColour is null ||
                                                   !existingTransformation.DefaultPatternColour
                                                       .IsSameColourAs(transformation.DefaultPatternColour);

                    if (patternColourNeedsUpdate)
                    {
                        // This catches null-to-null changes
                        if (existingTransformation.DefaultPatternColour != transformation.DefaultPatternColour)
                        {
                            existingEntry.State = EntityState.Modified;
                        }

                        existingTransformation.DefaultPatternColour =
                            transformation.DefaultPatternColour?.Clone();
                    }

                    if (existingEntry.State == EntityState.Modified)
                    {
                        ++updatedTransformations;
                    }
                }
            }
        }

        await _database.SaveChangesAsync(ct);
        return new UpdateTransformationsResult
        (
            addedSpecies,
            addedTransformations,
            updatedSpecies,
            updatedTransformations
        );
    }

    /// <summary>
    /// Gets a set of transformations from the database by their part and species. This method typically returns a
    /// single transformation, but may return more than one in the case of a composite part.
    /// </summary>
    /// <param name="bodypart">The part.</param>
    /// <param name="species">The species.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    public async Task<Result<IReadOnlyList<Transformation>>> GetTransformationsByPartAndSpeciesAsync
    (
        Bodypart bodypart,
        Species species,
        CancellationToken ct = default
    )
    {
        var bodyparts = new List<Bodypart>();
        if (bodypart.IsComposite())
        {
            bodyparts.AddRange(bodypart.GetComposingParts());
        }
        else
        {
            bodyparts.Add(bodypart);
        }

        var transformations = await _database.Transformations.ServersideQueryAsync
        (
            q => q
                .Where(tf => bodyparts.Contains(tf.Part))
                .Where(tf => tf.Species.Name.ToLower().Equals(species.Name.ToLower())),
            ct
        );

        if (!transformations.Any())
        {
            return new UserError
            (
                $"{species.Name} doesn't have that bodypart."
            );
        }

        return Result<IReadOnlyList<Transformation>>.FromSuccess(transformations);
    }

    /// <summary>
    /// Determines whether a combination of a part and a species is a unique transformation.
    /// </summary>
    /// <param name="bodypart">The bodypart that is transformed.</param>
    /// <param name="species">The species to transform into.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>true if the combination is unique; otherwise, false.</returns>
    public async Task<bool> IsPartAndSpeciesCombinationUniqueAsync
    (
        Bodypart bodypart,
        Species species,
        CancellationToken ct = default
    )
    {
        return !await _database.Transformations.ServersideQueryAsync
        (
            q => q.AnyAsync
            (
                tf => tf.Part == bodypart && string.Equals(tf.Species.Name.ToLower(), species.Name.ToLower()),
                ct
            )
        );
    }

    /// <summary>
    /// Gets the species from the database with the given name.
    /// </summary>
    /// <param name="speciesName">The name of the species.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    public Result<Species> GetSpeciesByName
    (
        string speciesName
    )
    {
        speciesName = speciesName.Trim();

        var species = _database.Species.SingleOrDefault
        (
            s => string.Equals(s.Name.ToLower(), speciesName.ToLower())
        );

        if (species is not null)
        {
            return species;
        }

        return new UserError("There is no species with that name in the database.");
    }

    /// <summary>
    /// Gets the species from the database with the given name.
    /// </summary>
    /// <param name="speciesName">The name of the species.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    public async Task<Result<Species>> GetSpeciesByNameAsync
    (
        string speciesName,
        CancellationToken ct = default
    )
    {
        speciesName = speciesName.Trim();

        var species = await _database.Species.ServersideQueryAsync
        (
            q => q
                .SingleOrDefaultAsync
                (
                    s => string.Equals(s.Name.ToLower(), speciesName.ToLower()),
                    ct
                )
        );

        if (species is not null)
        {
            return species;
        }

        return new UserError("There is no species with that name in the database.");
    }

    /// <summary>
    /// Determines whether or not the given species name is unique. This method is case-insensitive.
    /// </summary>
    /// <param name="speciesName">The name of the species.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>true if the name is unique; otherwise, false.</returns>
    public async Task<bool> IsSpeciesNameUniqueAsync
    (
        string speciesName,
        CancellationToken ct = default
    )
    {
        speciesName = speciesName.Trim();

        var matchingSpecies = await _database.Species.ServersideQueryAsync
        (
            q => q.Where(s => string.Equals(s.Name.ToLower(), speciesName.ToLower())),
            ct
        );

        return !matchingSpecies.Any();
    }

    /// <summary>
    /// Opts the given user into transformations on the given server.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="guild">The guild.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> OptInUserAsync
    (
        Snowflake user,
        Snowflake guild,
        CancellationToken ct = default
    )
    {
        var getProtectionResult = await GetOrCreateServerUserProtectionAsync(user, guild, ct);
        if (!getProtectionResult.IsSuccess)
        {
            return Result.FromError(getProtectionResult);
        }

        var protection = getProtectionResult.Entity;

        if (protection.HasOptedIn)
        {
            return new UserError("You're already opted into transformations.");
        }

        protection.HasOptedIn = true;

        await _database.SaveChangesAsync(ct);
        return Result.FromSuccess();
    }

    /// <summary>
    /// Opts the given user out of transformations on the given server.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="guild">The guild.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> OptOutUserAsync
    (
        Snowflake user,
        Snowflake guild,
        CancellationToken ct = default
    )
    {
        var getProtectionResult = await GetOrCreateServerUserProtectionAsync(user, guild, ct);
        if (!getProtectionResult.IsSuccess)
        {
            return Result.FromError(getProtectionResult);
        }

        var protection = getProtectionResult.Entity;

        if (!protection.HasOptedIn)
        {
            return new UserError("You're already opted out of transformations.");
        }

        protection.HasOptedIn = false;

        await _database.SaveChangesAsync(ct);
        return Result.FromSuccess();
    }

    /// <summary>
    /// Sets the default opt-in option for users on new servers.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="shouldOptIn">Whether the user should be opted by default on new servers.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetDefaultOptInAsync
    (
        Snowflake user,
        bool shouldOptIn,
        CancellationToken ct = default
    )
    {
        var getProtectionResult = await GetOrCreateGlobalUserProtectionAsync(user, ct);
        if (!getProtectionResult.IsSuccess)
        {
            return Result.FromError(getProtectionResult);
        }

        var protection = getProtectionResult.Entity;
        if (protection.DefaultOptIn == shouldOptIn)
        {
            return new UserError($"You're already opted {(shouldOptIn ? "in" : "out")} by default.");
        }

        protection.DefaultOptIn = shouldOptIn;

        await _database.SaveChangesAsync(ct);
        return Result.FromSuccess();
    }
}
