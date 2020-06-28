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
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
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
using Discord;
using Discord.Commands;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.Results;
using PureAttribute = JetBrains.Annotations.PureAttribute;

namespace DIGOS.Ambassador.Plugins.Transformations.Services
{
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
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<Appearance>> GetOrCreateDefaultAppearanceAsync
        (
            Character character
        )
        {
            var getDefaultAppearance = await GetDefaultAppearanceAsync(character);
            if (getDefaultAppearance.IsSuccess)
            {
                return getDefaultAppearance;
            }

            var createDefaultAppearanceResult = await Appearance.CreateDefaultAsync(character, this);
            if (!createDefaultAppearanceResult.IsSuccess)
            {
                return RetrieveEntityResult<Appearance>.FromError(createDefaultAppearanceResult);
            }

            var defaultAppearance = createDefaultAppearanceResult.Entity;
            defaultAppearance.IsDefault = true;

            _database.Appearances.Update(defaultAppearance);

            return defaultAppearance;
        }

        /// <summary>
        /// Gets the given character's default appearance.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        private async Task<RetrieveEntityResult<Appearance>> GetDefaultAppearanceAsync(Character character)
        {
            var appearances = await _database.Appearances.UnifiedQueryAsync
            (
                q => q.Where(da => da.Character == character && da.IsDefault)
            );

            var appearance = appearances.SingleOrDefault();

            if (!(appearance is null))
            {
                return appearance;
            }

            return RetrieveEntityResult<Appearance>.FromError("The character doesn't have a default appearance.");
        }

        /// <summary>
        /// Gets the current appearance for the given character, cloning it from the default appearance if one does not
        /// exist.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<Appearance>> GetOrCreateCurrentAppearanceAsync
        (
            Character character
        )
        {
            var getCurrentAppearanceResult = await GetCurrentAppearanceAsync(character);
            if (getCurrentAppearanceResult.IsSuccess)
            {
                return getCurrentAppearanceResult;
            }

            // There's no current appearance, so we'll grab the default one and copy it
            var getDefaultAppearance = await GetOrCreateDefaultAppearanceAsync(character);
            if (!getDefaultAppearance.IsSuccess)
            {
                return RetrieveEntityResult<Appearance>.FromError(getDefaultAppearance);
            }

            var defaultAppearance = getDefaultAppearance.Entity;

            var currentAppearance = Appearance.CopyFrom(defaultAppearance);
            currentAppearance.IsCurrent = true;

            _database.Appearances.Update(currentAppearance);

            return currentAppearance;
        }

        /// <summary>
        /// Gets the given character's current appearance.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        private async Task<RetrieveEntityResult<Appearance>> GetCurrentAppearanceAsync(Character character)
        {
            var appearances = await _database.Appearances.UnifiedQueryAsync
            (
                q => q.Where(da => da.Character == character && da.IsCurrent)
            );

            var appearance = appearances.SingleOrDefault();

            if (!(appearance is null))
            {
                return appearance;
            }

            return RetrieveEntityResult<Appearance>.FromError("The character doesn't have a current appearance.");
        }

        /// <summary>
        /// Removes the given character's bodypart.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <param name="character">The character to shift.</param>
        /// <param name="bodyPart">The bodypart to remove.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <returns>A shifting result which may or may not have succeeded.</returns>
        public async Task<ShiftBodypartResult> RemoveBodypartAsync
        (
            ICommandContext context,
            Character character,
            Bodypart bodyPart,
            Chirality chirality = Chirality.Center
        )
        {
            var discordUser = await context.Guild.GetUserAsync((ulong)character.Owner.DiscordID);
            var canTransformResult = await CanUserTransformUserAsync(context.Guild, context.User, discordUser);
            if (!canTransformResult.IsSuccess)
            {
                return ShiftBodypartResult.FromError(canTransformResult);
            }

            var getAppearanceResult = await GetOrCreateCurrentAppearanceAsync(character);
            if (!getAppearanceResult.IsSuccess)
            {
                return ShiftBodypartResult.FromError(getAppearanceResult);
            }

            var appearance = getAppearanceResult.Entity;

            var bodypartRemover = new BodypartRemover
            (
                appearance,
                _descriptionBuilder
            );

            var shiftResult = await bodypartRemover.RemoveAsync(bodyPart, chirality);
            return shiftResult;
        }

        /// <summary>
        /// Removes the given character's bodypart.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <param name="character">The character to shift.</param>
        /// <param name="bodyPart">The bodypart to remove.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <returns>A shifting result which may or may not have succeeded.</returns>
        public async Task<ShiftBodypartResult> RemoveBodypartPatternAsync
        (
            ICommandContext context,
            Character character,
            Bodypart bodyPart,
            Chirality chirality = Chirality.Center
        )
        {
            var discordUser = await context.Guild.GetUserAsync((ulong)character.Owner.DiscordID);
            var canTransformResult = await CanUserTransformUserAsync(context.Guild, context.User, discordUser);
            if (!canTransformResult.IsSuccess)
            {
                return ShiftBodypartResult.FromError(canTransformResult);
            }

            var getAppearanceResult = await GetOrCreateCurrentAppearanceAsync(character);
            if (!getAppearanceResult.IsSuccess)
            {
                return ShiftBodypartResult.FromError(getAppearanceResult);
            }

            var appearance = getAppearanceResult.Entity;

            var patternRemover = new PatternRemover(appearance, _descriptionBuilder);
            var shiftResult = await patternRemover.RemoveAsync(bodyPart, chirality);

            return shiftResult;
        }

        /// <summary>
        /// Shifts the given character's bodypart to the given species.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <param name="character">The character to shift.</param>
        /// <param name="bodyPart">The bodypart to shift.</param>
        /// <param name="speciesName">The species to shift the bodypart into.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <returns>A shifting result which may or may not have succeeded.</returns>
        public async Task<ShiftBodypartResult> ShiftBodypartAsync
        (
            ICommandContext context,
            Character character,
            Bodypart bodyPart,
            string speciesName,
            Chirality chirality = Chirality.Center
        )
        {
            var discordUser = await context.Guild.GetUserAsync((ulong)character.Owner.DiscordID);
            var canTransformResult = await CanUserTransformUserAsync(context.Guild, context.User, discordUser);
            if (!canTransformResult.IsSuccess)
            {
                return ShiftBodypartResult.FromError(canTransformResult);
            }

            var getSpeciesResult = await GetSpeciesByNameAsync(speciesName);
            if (!getSpeciesResult.IsSuccess)
            {
                return ShiftBodypartResult.FromError(getSpeciesResult);
            }

            var species = getSpeciesResult.Entity;

            var getCurrentAppearance = await GetOrCreateCurrentAppearanceAsync(character);
            if (!getCurrentAppearance.IsSuccess)
            {
                return ShiftBodypartResult.FromError(getCurrentAppearance);
            }

            var appearance = getCurrentAppearance.Entity;

            var speciesShifter = new SpeciesShifter(appearance, species, this, _descriptionBuilder);
            var shiftResult = await speciesShifter.ShiftAsync(bodyPart, chirality);

            return shiftResult;
        }

        /// <summary>
        /// Shifts the colour of the given bodypart on the given character to the given colour.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="character">The character to shift.</param>
        /// <param name="bodyPart">The bodypart to shift.</param>
        /// <param name="colour">The colour to shift it into.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <returns>A shifting result which may or may not have succeeded.</returns>
        public async Task<ShiftBodypartResult> ShiftBodypartColourAsync
        (
            ICommandContext context,
            Character character,
            Bodypart bodyPart,
            Colour colour,
            Chirality chirality = Chirality.Center
        )
        {
            var discordUser = await context.Guild.GetUserAsync((ulong)character.Owner.DiscordID);
            var canTransformResult = await CanUserTransformUserAsync(context.Guild, context.User, discordUser);
            if (!canTransformResult.IsSuccess)
            {
                return ShiftBodypartResult.FromError(canTransformResult);
            }

            if (bodyPart.IsChiral() && chirality == Chirality.Center)
            {
                return ShiftBodypartResult.FromError
                (
                    $"Please specify if it's the left or right {bodyPart.Humanize().ToLower()}."
                );
            }

            var getCurrentAppearance = await GetOrCreateCurrentAppearanceAsync(character);
            if (!getCurrentAppearance.IsSuccess)
            {
                return ShiftBodypartResult.FromError(getCurrentAppearance);
            }

            var appearance = getCurrentAppearance.Entity;

            var colourShifter = new ColourShifter(appearance, colour, _descriptionBuilder);
            var shiftResult = await colourShifter.ShiftAsync(bodyPart, chirality);

            return shiftResult;
        }

        /// <summary>
        /// Shifts the pattern of the given bodypart on the given character to the given pattern with the given colour.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="character">The character to shift.</param>
        /// <param name="bodyPart">The bodypart to shift.</param>
        /// <param name="pattern">The pattern to shift the bodypart into.</param>
        /// <param name="patternColour">The colour to shift it into.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <returns>A shifting result which may or may not have succeeded.</returns>
        public async Task<ShiftBodypartResult> ShiftBodypartPatternAsync
        (
            ICommandContext context,
            Character character,
            Bodypart bodyPart,
            Pattern pattern,
            Colour patternColour,
            Chirality chirality = Chirality.Center
        )
        {
            var discordUser = await context.Guild.GetUserAsync((ulong)character.Owner.DiscordID);
            var canTransformResult = await CanUserTransformUserAsync(context.Guild, context.User, discordUser);
            if (!canTransformResult.IsSuccess)
            {
                return ShiftBodypartResult.FromError(canTransformResult);
            }

            var getAppearanceResult = await GetOrCreateCurrentAppearanceAsync(character);
            if (!getAppearanceResult.IsSuccess)
            {
                return ShiftBodypartResult.FromError(getAppearanceResult);
            }

            var appearance = getAppearanceResult.Entity;

            var patternShifter = new PatternShifter(appearance, pattern, patternColour, _descriptionBuilder);
            var shiftResult = await patternShifter.ShiftAsync(bodyPart, chirality);

            return shiftResult;
        }

        /// <summary>
        /// Shifts the colour of the given bodypart's pattern on the given character to the given colour.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="character">The character to shift.</param>
        /// <param name="bodyPart">The bodypart to shift.</param>
        /// <param name="patternColour">The colour to shift it into.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <returns>A shifting result which may or may not have succeeded.</returns>
        public async Task<ShiftBodypartResult> ShiftPatternColourAsync
        (
            ICommandContext context,
            Character character,
            Bodypart bodyPart,
            Colour patternColour,
            Chirality chirality = Chirality.Center
        )
        {
            var discordUser = await context.Guild.GetUserAsync((ulong)character.Owner.DiscordID);
            var canTransformResult = await CanUserTransformUserAsync(context.Guild, context.User, discordUser);
            if (!canTransformResult.IsSuccess)
            {
                return ShiftBodypartResult.FromError(canTransformResult);
            }

            var getAppearanceResult = await GetOrCreateCurrentAppearanceAsync(character);
            if (!getAppearanceResult.IsSuccess)
            {
                return ShiftBodypartResult.FromError(getAppearanceResult);
            }

            var appearance = getAppearanceResult.Entity;

            var patternColourShifter = new PatternColourShifter(appearance, patternColour, _descriptionBuilder);
            var shiftResult = await patternColourShifter.ShiftAsync(bodyPart, chirality);

            return shiftResult;
        }

        /// <summary>
        /// Determines whether or not a user is allowed to transform another user.
        /// </summary>
        /// <param name="discordServer">The server the users are on.</param>
        /// <param name="invokingUser">The user trying to transform.</param>
        /// <param name="targetUser">The user being transformed.</param>
        /// <returns>A conditional determination with an attached reason if it failed.</returns>
        [Pure]
        public async Task<DetermineConditionResult> CanUserTransformUserAsync
        (
            IGuild discordServer,
            IUser invokingUser,
            IUser targetUser
        )
        {
            var getLocalProtectionResult = await GetOrCreateServerUserProtectionAsync(targetUser, discordServer);
            if (!getLocalProtectionResult.IsSuccess)
            {
                return DetermineConditionResult.FromError(getLocalProtectionResult);
            }

            var localProtection = getLocalProtectionResult.Entity;

            if (!localProtection.HasOptedIn)
            {
                return DetermineConditionResult.FromError("The target hasn't opted into transformations.");
            }

            var getGlobalProtectionResult = await GetOrCreateGlobalUserProtectionAsync(targetUser);
            if (!getGlobalProtectionResult.IsSuccess)
            {
                return DetermineConditionResult.FromError(getGlobalProtectionResult);
            }

            var globalProtection = getGlobalProtectionResult.Entity;
            switch (localProtection.Type)
            {
                case ProtectionType.Blacklist:
                {
                    return globalProtection.Blacklist.All(u => u.DiscordID != (long)invokingUser.Id)
                        ? DetermineConditionResult.FromSuccess()
                        : DetermineConditionResult.FromError("You're on that user's blacklist.");
                }
                case ProtectionType.Whitelist:
                {
                    return globalProtection.Whitelist.Any(u => u.DiscordID == (long)invokingUser.Id)
                        ? DetermineConditionResult.FromSuccess()
                        : DetermineConditionResult.FromError("You're not on that user's whitelist.");
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Generate a complete textual description of the given character, and format it into an embed.
        /// </summary>
        /// <param name="character">The character to generate the description for.</param>
        /// <returns>An embed with a formatted description.</returns>
        [Pure]
        public async Task<CreateEntityResult<string>> GenerateCharacterDescriptionAsync(Character character)
        {
            var getCurrentAppearance = await GetOrCreateCurrentAppearanceAsync
            (
                character
            );

            if (!getCurrentAppearance.IsSuccess)
            {
                return CreateEntityResult<string>.FromError(getCurrentAppearance);
            }

            var currentAppearance = getCurrentAppearance.Entity;

            var visualDescription = _descriptionBuilder.BuildVisualDescription(currentAppearance);
            return CreateEntityResult<string>.FromSuccess(visualDescription);
        }

        /// <summary>
        /// Gets the available species in transformations.
        /// </summary>
        /// <returns>A list of the available species.</returns>
        [Pure]
        public async Task<IReadOnlyList<Species>> GetAvailableSpeciesAsync()
        {
            return (await _database.Species.UnifiedQueryAsync(q => q)).ToList();
        }

        /// <summary>
        /// Gets the available transformations for the given bodypart.
        /// </summary>
        /// <param name="bodyPart">The bodypart to get the transformations for.</param>
        /// <returns>A list of the available transformations..</returns>
        [Pure]
        public async Task<IReadOnlyList<Transformation>> GetAvailableTransformationsAsync(Bodypart bodyPart)
        {
            var transformations = await _database.Transformations.UnifiedQueryAsync
            (
                q => q.Where(tf => tf.Part == bodyPart)
            );

            return transformations.ToList();
        }

        /// <summary>
        /// Resets the given character's appearance to its default state.
        /// </summary>
        /// <param name="character">The character to reset.</param>
        /// <returns>An entity modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> ResetCharacterFormAsync(Character character)
        {
            var getDefaultAppearanceResult = await GetOrCreateDefaultAppearanceAsync(character);
            if (!getDefaultAppearanceResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getDefaultAppearanceResult);
            }

            var getCurrentAppearance = await GetCurrentAppearanceAsync(character);
            if (getCurrentAppearance.IsSuccess)
            {
                // Delete the existing current appearance
                _database.Appearances.Remove(getCurrentAppearance.Entity);
            }

            // Piggyback on the get/create method to clone the default appearance
            var createNewCurrentResult = await GetOrCreateCurrentAppearanceAsync(character);
            if (!createNewCurrentResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(createNewCurrentResult);
            }

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the current appearance of the given character as its default appearance.
        /// </summary>
        /// <param name="character">The character to set the default appearance of.</param>
        /// <returns>An entity modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCurrentAppearanceAsDefaultForCharacterAsync
        (
            Character character
        )
        {
            // First, erase the existing default
            var getExistingDefaultAppearance = await GetOrCreateDefaultAppearanceAsync(character);
            if (getExistingDefaultAppearance.IsSuccess)
            {
                var existingDefault = getExistingDefaultAppearance.Entity;

                _database.Appearances.Remove(existingDefault);
            }

            // Then, get the existing current appearance
            var getCurrentAppearance = await GetOrCreateCurrentAppearanceAsync(character);
            if (!getCurrentAppearance.IsSuccess)
            {
                return ModifyEntityResult.FromError(getCurrentAppearance);
            }

            var existingCurrentAppearance = getCurrentAppearance.Entity;

            // Flip the flags. Note that we don't create a new current appearance right away, because one will
            // automatically be created if one is requested.
            existingCurrentAppearance.IsDefault = true;
            existingCurrentAppearance.IsCurrent = false;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the default protection type that the user has for transformations.
        /// </summary>
        /// <param name="discordUser">The user to set the protection for.</param>
        /// <param name="protectionType">The protection type to set.</param>
        /// <returns>An entity modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetDefaultProtectionTypeAsync
        (
            IUser discordUser,
            ProtectionType protectionType
        )
        {
            var getGlobalProtectionResult = await GetOrCreateGlobalUserProtectionAsync(discordUser);
            if (!getGlobalProtectionResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getGlobalProtectionResult);
            }

            var protection = getGlobalProtectionResult.Entity;

            if (protection.DefaultType == protectionType)
            {
                return ModifyEntityResult.FromError($"{protectionType.Humanize()} is already your default setting.");
            }

            protection.DefaultType = protectionType;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the protection type that the user has for transformations on the given server.
        /// </summary>
        /// <param name="discordUser">The user to set the protection for.</param>
        /// <param name="discordServer">The server to set the protection on.</param>
        /// <param name="protectionType">The protection type to set.</param>
        /// <returns>An entity modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetServerProtectionTypeAsync
        (
            IUser discordUser,
            IGuild discordServer,
            ProtectionType protectionType
        )
        {
            var getServerProtectionResult = await GetOrCreateServerUserProtectionAsync(discordUser, discordServer);
            if (!getServerProtectionResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getServerProtectionResult);
            }

            var protection = getServerProtectionResult.Entity;

            if (protection.Type == protectionType)
            {
                return ModifyEntityResult.FromError($"{protectionType.Humanize()} is already your current setting.");
            }

            protection.Type = protectionType;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Whitelists the given user, allowing them to transform the <paramref name="discordUser"/>.
        /// </summary>
        /// <param name="discordUser">The user to modify.</param>
        /// <param name="whitelistedUser">The user to add to the whitelist.</param>
        /// <returns>An entity modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> WhitelistUserAsync
        (
            IUser discordUser,
            IUser whitelistedUser
        )
        {
            if (discordUser == whitelistedUser)
            {
                return ModifyEntityResult.FromError("You can't whitelist yourself.");
            }

            var getGlobalProtectionResult = await GetOrCreateGlobalUserProtectionAsync(discordUser);
            if (!getGlobalProtectionResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getGlobalProtectionResult);
            }

            var protection = getGlobalProtectionResult.Entity;

            if (protection.Whitelist.Any(u => u.DiscordID == (long)whitelistedUser.Id))
            {
                return ModifyEntityResult.FromError("You've already whitelisted that user.");
            }

            var protectionEntry = protection.UserListing.FirstOrDefault(u => u.User.DiscordID == (long)discordUser.Id);
            if (protectionEntry is null)
            {
                var getUserResult = await _users.GetOrRegisterUserAsync(whitelistedUser);
                if (!getUserResult.IsSuccess)
                {
                    return ModifyEntityResult.FromError(getUserResult);
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

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Blacklists the given user, preventing them from transforming the <paramref name="discordUser"/>.
        /// </summary>
        /// <param name="discordUser">The user to modify.</param>
        /// <param name="blacklistedUser">The user to add to the blacklist.</param>
        /// <returns>An entity modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> BlacklistUserAsync
        (
            IUser discordUser,
            IUser blacklistedUser
        )
        {
            if (discordUser == blacklistedUser)
            {
                return ModifyEntityResult.FromError("You can't blacklist yourself.");
            }

            var getGlobalProtectionResult = await GetOrCreateGlobalUserProtectionAsync(discordUser);
            if (!getGlobalProtectionResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getGlobalProtectionResult);
            }

            var protection = getGlobalProtectionResult.Entity;

            if (protection.Blacklist.Any(u => u.DiscordID == (long)blacklistedUser.Id))
            {
                return ModifyEntityResult.FromError("You've already blacklisted that user.");
            }

            var protectionEntry = protection.UserListing.FirstOrDefault(u => u.User.DiscordID == (long)discordUser.Id);
            if (protectionEntry is null)
            {
                var getUserResult = await _users.GetOrRegisterUserAsync(blacklistedUser);
                if (!getUserResult.IsSuccess)
                {
                    return ModifyEntityResult.FromError(getUserResult);
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

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets or creates the global transformation protection data for the given user.
        /// </summary>
        /// <param name="discordUser">The user.</param>
        /// <returns>Global protection data for the given user.</returns>
        public async Task<RetrieveEntityResult<GlobalUserProtection>> GetOrCreateGlobalUserProtectionAsync
        (
            IUser discordUser
        )
        {
            var protections = await _database.GlobalUserProtections.UnifiedQueryAsync
            (
                q => q.Where(p => p.User.DiscordID == (long)discordUser.Id)
            );

            var protection = protections.SingleOrDefault();

            if (!(protection is null))
            {
                return protection;
            }

            var getUserResult = await _users.GetOrRegisterUserAsync(discordUser);
            if (!getUserResult.IsSuccess)
            {
                return RetrieveEntityResult<GlobalUserProtection>.FromError(getUserResult);
            }

            var user = getUserResult.Entity;

            protection = _database.CreateProxy<GlobalUserProtection>(user);
            _database.GlobalUserProtections.Update(protection);

            return RetrieveEntityResult<GlobalUserProtection>.FromSuccess(protection);
        }

        /// <summary>
        /// Gets or creates server-specific transformation protection data for the given user and server.
        /// </summary>
        /// <param name="discordUser">The user.</param>
        /// <param name="guild">The server.</param>
        /// <returns>Server-specific protection data for the given user.</returns>
        public async Task<RetrieveEntityResult<ServerUserProtection>> GetOrCreateServerUserProtectionAsync
        (
            IUser discordUser,
            IGuild guild
        )
        {
            var protections = await _database.ServerUserProtections.UnifiedQueryAsync
            (
                q => q.Where(p => p.User.DiscordID == (long)discordUser.Id && p.Server.DiscordID == (long)guild.Id)
            );

            var protection = protections.SingleOrDefault();

            if (!(protection is null))
            {
                return protection;
            }

            var getServerResult = await _servers.GetOrRegisterServerAsync(guild);
            if (!getServerResult.IsSuccess)
            {
                return RetrieveEntityResult<ServerUserProtection>.FromError(getServerResult);
            }

            var server = getServerResult.Entity;

            var getGlobalProtectionResult = await GetOrCreateGlobalUserProtectionAsync(discordUser);
            if (!getGlobalProtectionResult.IsSuccess)
            {
                return RetrieveEntityResult<ServerUserProtection>.FromError(getGlobalProtectionResult);
            }

            var globalProtection = getGlobalProtectionResult.Entity;

            var newProtection = _database.CreateProxy<ServerUserProtection>(server, globalProtection.User);
            _database.ServerUserProtections.Update(newProtection);

            newProtection.Type = globalProtection.DefaultType;
            newProtection.HasOptedIn = globalProtection.DefaultOptIn;

            return newProtection;
        }

        /// <summary>
        /// Updates the database with new or changed transformations.
        /// </summary>
        /// <returns>An update result which may or may not have succeeded.</returns>
        public async Task<UpdateTransformationsResult> UpdateTransformationDatabaseAsync()
        {
            uint addedSpecies = 0;
            uint updatedSpecies = 0;

            var bundledSpeciesResult = await _content.DiscoverBundledSpeciesAsync();
            if (!bundledSpeciesResult.IsSuccess)
            {
                return UpdateTransformationsResult.FromError(bundledSpeciesResult);
            }

            foreach (var species in bundledSpeciesResult.Entity.OrderBy(s => s.GetSpeciesDepth()))
            {
                if (await IsSpeciesNameUniqueAsync(species.Name))
                {
                    // Add a new species
                    _database.Species.Update(species);
                    ++addedSpecies;
                }
                else
                {
                    // There's an existing species with this name
                    var existingSpecies = (await GetSpeciesByNameAsync(species.Name)).Entity;

                    species.ID = existingSpecies.ID;

                    var existingEntry = _database.Entry(existingSpecies);
                    existingEntry.CurrentValues.SetValues(species);

                    if (existingEntry.State == EntityState.Modified)
                    {
                        ++updatedSpecies;
                    }
                }
            }

            uint addedTransformations = 0;
            uint updatedTransformations = 0;

            var availableSpecies = await GetAvailableSpeciesAsync();
            foreach (var species in availableSpecies)
            {
                var bundledTransformationsResult = await _content.DiscoverBundledTransformationsAsync(this, species);
                if (!bundledTransformationsResult.IsSuccess)
                {
                    return UpdateTransformationsResult.FromError(bundledTransformationsResult);
                }

                foreach (var transformation in bundledTransformationsResult.Entity)
                {
                    if (await IsPartAndSpeciesCombinationUniqueAsync(transformation.Part, transformation.Species))
                    {
                        // Add a new transformation
                        _database.Transformations.Update(transformation);
                        ++addedTransformations;
                    }
                    else
                    {
                        // We just take the first one, since species can't define composite parts individually
                        var existingTransformation =
                        (
                            await GetTransformationsByPartAndSpeciesAsync
                            (
                                transformation.Part,
                                transformation.Species
                            )
                        ).Entity.First();

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

            return UpdateTransformationsResult.FromSuccess
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
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public async Task<RetrieveEntityResult<IReadOnlyList<Transformation>>> GetTransformationsByPartAndSpeciesAsync
        (
            Bodypart bodypart,
            Species species
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

            var transformations = await _database.Transformations.UnifiedQueryAsync
            (
                q => q
                    .Where(tf => bodyparts.Contains(tf.Part))
                    .Where(tf => tf.Species.Name.ToLower().Equals(species.Name.ToLower()))
            );

            var enumeratedTransformations = transformations.ToList();

            if (!enumeratedTransformations.Any())
            {
                return RetrieveEntityResult<IReadOnlyList<Transformation>>.FromError
                (
                    "No transformation found for that combination."
                );
            }

            return enumeratedTransformations;
        }

        /// <summary>
        /// Determines whether a combination of a part and a species is a unique transformation.
        /// </summary>
        /// <param name="bodypart">The bodypart that is transformed.</param>
        /// <param name="species">The species to transform into.</param>
        /// <returns>true if the combination is unique; otherwise, false.</returns>
        [Pure]
        public async Task<bool> IsPartAndSpeciesCombinationUniqueAsync
        (
            Bodypart bodypart,
            Species species
        )
        {
            return !await _database.Transformations.AsQueryable().AnyAsync
            (
                tf => tf.Part == bodypart && string.Equals(tf.Species.Name.ToLower(), species.Name.ToLower())
            );
        }

        /// <summary>
        /// Gets the species from the database with the given name.
        /// </summary>
        /// <param name="speciesName">The name of the species.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public RetrieveEntityResult<Species> GetSpeciesByName
        (
            string speciesName
        )
        {
            return GetSpeciesByNameAsync(speciesName).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the species from the database with the given name.
        /// </summary>
        /// <param name="speciesName">The name of the species.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public async Task<RetrieveEntityResult<Species>> GetSpeciesByNameAsync
        (
            string speciesName
        )
        {
            var matchingSpecies = await _database.Species.UnifiedQueryAsync
            (
                q => q.Where(s => string.Equals(s.Name.ToLower(), speciesName.ToLower()))
            );

            var species = matchingSpecies.SingleOrDefault();

            if (!(species is null))
            {
                return species;
            }

            return RetrieveEntityResult<Species>.FromError("There is no species with that name in the database.");
        }

        /// <summary>
        /// Determines whether or not the given species name is unique. This method is case-insensitive.
        /// </summary>
        /// <param name="speciesName">The name of the species.</param>
        /// <returns>true if the name is unique; otherwise, false.</returns>
        [Pure]
        public async Task<bool> IsSpeciesNameUniqueAsync
        (
            string speciesName
        )
        {
            var matchingSpecies = await _database.Species.UnifiedQueryAsync
            (
                q => q.Where(s => string.Equals(s.Name.ToLower(), speciesName.ToLower()))
            );

            return !matchingSpecies.Any();
        }

        /// <summary>
        /// Opts the given user into transformations on the given server.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="guild">The guild.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> OptInUserAsync(IUser user, IGuild guild)
        {
            var getProtectionResult = await GetOrCreateServerUserProtectionAsync(user, guild);
            if (!getProtectionResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getProtectionResult);
            }

            var protection = getProtectionResult.Entity;

            if (protection.HasOptedIn)
            {
                return ModifyEntityResult.FromError("You're already opted into transformations.");
            }

            protection.HasOptedIn = true;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Opts the given user out of transformations on the given server.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="guild">The guild.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> OptOutUserAsync(IUser user, IGuild guild)
        {
            var getProtectionResult = await GetOrCreateServerUserProtectionAsync(user, guild);
            if (!getProtectionResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getProtectionResult);
            }

            var protection = getProtectionResult.Entity;

            if (!protection.HasOptedIn)
            {
                return ModifyEntityResult.FromError("You're already opted out of transformations.");
            }

            protection.HasOptedIn = false;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the default opt-in option for users on new servers.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="shouldOptIn">Whether the user should be opted by default on new servers.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetDefaultOptInAsync(IUser user, bool shouldOptIn)
        {
            var getProtectionResult = await GetOrCreateGlobalUserProtectionAsync(user);
            if (!getProtectionResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getProtectionResult);
            }

            var protection = getProtectionResult.Entity;
            if (protection.DefaultOptIn == shouldOptIn)
            {
                return ModifyEntityResult.FromError($"You're already opted {(shouldOptIn ? "in" : "out")} by default.");
            }

            protection.DefaultOptIn = shouldOptIn;

            return ModifyEntityResult.FromSuccess();
        }
    }
}
