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
using System.Reflection;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Appearances;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Users;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Utility;

using Discord;
using Discord.Commands;

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Image = DIGOS.Ambassador.Database.Data.Image;

namespace DIGOS.Ambassador.Services
{
    /// <summary>
    /// Acts as an interface for accessing and modifying user characters.
    /// </summary>
    public class CharacterService
    {
        private readonly TransformationService Transformations;

        private readonly CommandService Commands;

        private readonly OwnedEntityService OwnedEntities;

        private readonly ContentService Content;

        private readonly Dictionary<string, IPronounProvider> PronounProviders;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterService"/> class.
        /// </summary>
        /// <param name="commands">The application's command service.</param>
        /// <param name="entityService">The application's owned entity service.</param>
        /// <param name="content">The content service.</param>
        /// <param name="transformations">The transformation service.</param>
        public CharacterService(CommandService commands, OwnedEntityService entityService, ContentService content, TransformationService transformations)
        {
            this.Commands = commands;
            this.OwnedEntities = entityService;
            this.Content = content;
            this.Transformations = transformations;

            this.PronounProviders = new Dictionary<string, IPronounProvider>(new CaseInsensitiveStringEqualityComparer());
        }

        /// <summary>
        /// Discovers available pronoun providers in the assembly, adding them to the available providers.
        /// </summary>
        public void DiscoverPronounProviders()
        {
            this.PronounProviders.Clear();

            var assembly = Assembly.GetExecutingAssembly();
            var pronounProviderTypes = assembly.DefinedTypes.Where
            (
                t => t.ImplementedInterfaces.Contains(typeof(IPronounProvider))
                && t.IsClass
                && !t.IsAbstract
            );

            foreach (var type in pronounProviderTypes)
            {
                var pronounProvider = Activator.CreateInstance(type) as IPronounProvider;
                if (pronounProvider is null)
                {
                    continue;
                }

                WithPronounProvider(pronounProvider);
            }
        }

        /// <summary>
        /// Adds the given pronoun provider to the service.
        /// </summary>
        /// <param name="pronounProvider">The pronoun provider to add.</param>
        /// <returns>The service with the provider.</returns>
        [NotNull]
        public CharacterService WithPronounProvider([NotNull] IPronounProvider pronounProvider)
        {
            this.PronounProviders.Add(pronounProvider.Family, pronounProvider);
            return this;
        }

        /// <summary>
        /// Gets the pronoun provider for the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns>A pronoun provider.</returns>
        /// <exception cref="ArgumentException">Thrown if no pronoun provider exists for the character's preference.</exception>
        [NotNull]
        public virtual IPronounProvider GetPronounProvider([NotNull] Character character)
        {
            if (this.PronounProviders.ContainsKey(character.PronounProviderFamily))
            {
                return this.PronounProviders[character.PronounProviderFamily];
            }

            throw new KeyNotFoundException("No pronoun provider for that family found.");
        }

        /// <summary>
        /// Gets the available pronoun providers.
        /// </summary>
        /// <returns>An enumerator over the available pronouns.</returns>
        [NotNull]
        [ItemNotNull]
        public IEnumerable<IPronounProvider> GetAvailablePronounProviders()
        {
            return this.PronounProviders.Values;
        }

        /// <summary>
        /// This method searches for the best matching character given an owner and a name. If no owner is provided, then
        /// the global list is searched for a unique name. If no match can be found, a failed result is returned.
        /// </summary>
        /// <param name="db">The database where the characters are stored.</param>
        /// <param name="context">The command context.</param>
        /// <param name="characterOwner">The owner of the character, if any.</param>
        /// <param name="characterName">The name of the character.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public async Task<RetrieveEntityResult<Character>> GetBestMatchingCharacterAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] ICommandContext context,
            [CanBeNull] IUser characterOwner,
            [CanBeNull] string characterName
        )
        {
            if (characterOwner is null && characterName is null)
            {
                return await GetCurrentCharacterAsync(db, context, context.Message.Author);
            }

            if (characterOwner is null)
            {
                // Search the invoker's characters first
                var invoker = context.User;
                var invokerCharacterResult = await GetUserCharacterByNameAsync
                (
                    db,
                    context,
                    invoker,
                    characterName
                );

                if (invokerCharacterResult.IsSuccess)
                {
                    return invokerCharacterResult;
                }

                return await GetNamedCharacterAsync(db, characterName, context.Guild);
            }

            if (characterName.IsNullOrWhitespace())
            {
                return await GetCurrentCharacterAsync(db, context, characterOwner);
            }

            return await GetUserCharacterByNameAsync(db, context, characterOwner, characterName);
        }

        /// <summary>
        /// Gets the current character a user has assumed the form of.
        /// </summary>
        /// <param name="db">The database where the characters are stored.</param>
        /// <param name="context">The context of the user.</param>
        /// <param name="discordUser">The user to get the current character of.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public async Task<RetrieveEntityResult<Character>> GetCurrentCharacterAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] ICommandContext context,
            [NotNull] IUser discordUser
        )
        {
            if (!await HasActiveCharacterOnServerAsync(db, discordUser, context.Guild))
            {
                var isCurrentUser = context.Message.Author.Id == discordUser.Id;
                var errorMessage = isCurrentUser
                    ? "You haven't assumed a character."
                    : "The user hasn't assumed a character.";

                return RetrieveEntityResult<Character>.FromError(CommandError.ObjectNotFound, errorMessage);
            }

            var currentCharacter = await GetUserCharacters(db, discordUser, context.Guild)
            .FirstOrDefaultAsync
            (
                ch => ch.IsCurrent
            );

            if (currentCharacter is null)
            {
                return RetrieveEntityResult<Character>.FromError(CommandError.Unsuccessful, "Failed to retrieve a current character.");
            }

            return RetrieveEntityResult<Character>.FromSuccess(currentCharacter);
        }

        /// <summary>
        /// Gets a character by its given name.
        /// </summary>
        /// <param name="db">The database context where the data is stored.</param>
        /// <param name="characterName">The name of the character.</param>
        /// <param name="guild">The guild that the character is on.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public async Task<RetrieveEntityResult<Character>> GetNamedCharacterAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] string characterName,
            [NotNull] IGuild guild
        )
        {
            var guildCharacters = db.Characters.Where(ch => ch.ServerID == (long)guild.Id);
            if (await guildCharacters.CountAsync(ch => string.Equals(ch.Name, characterName, StringComparison.OrdinalIgnoreCase)) > 1)
            {
                return RetrieveEntityResult<Character>.FromError
                (
                    CommandError.MultipleMatches,
                    "There's more than one character with that name. Please specify which user it belongs to."
                );
            }

            var character = GetCharacters(db, guild).FirstOrDefault(ch => string.Equals(ch.Name, characterName, StringComparison.OrdinalIgnoreCase));

            if (character is null)
            {
                return RetrieveEntityResult<Character>.FromError(CommandError.ObjectNotFound, "No character with that name found.");
            }

            return RetrieveEntityResult<Character>.FromSuccess(character);
        }

        /// <summary>
        /// Gets the characters in the database along with their navigation properties.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="guild">The guild where the characters are.</param>
        /// <returns>A queryable set of characters.</returns>
        [NotNull]
        public IQueryable<Character> GetCharacters([NotNull] GlobalInfoContext db, IGuild guild)
        {
            return db.Characters
                .Include(c => c.Owner)
                .Include(c => c.Images)
                .Include(c => c.Role).ThenInclude(r => r.Server)
                .Include(c => c.CurrentAppearance.Components).ThenInclude(co => co.BaseColour)
                .Include(c => c.CurrentAppearance.Components).ThenInclude(co => co.PatternColour)
                .Include(c => c.CurrentAppearance.Components).ThenInclude(co => co.Transformation.Species)
                .Include(c => c.CurrentAppearance.Components).ThenInclude(co => co.Transformation.DefaultBaseColour)
                .Include(c => c.CurrentAppearance.Components).ThenInclude(co => co.Transformation.DefaultPatternColour)
                .Include(c => c.DefaultAppearance.Components).ThenInclude(co => co.Transformation.Species)
                .Include(c => c.DefaultAppearance.Components).ThenInclude(co => co.Transformation.DefaultBaseColour)
                .Include(c => c.DefaultAppearance.Components).ThenInclude(co => co.Transformation.DefaultPatternColour)
                .Where(c => c.ServerID == (long)guild.Id);
        }

        /// <summary>
        /// Gets a character belonging to a given user by a given name.
        /// </summary>
        /// <param name="db">The database where the characters are stored.</param>
        /// <param name="context">The context of the user.</param>
        /// <param name="characterOwner">The user to get the character from.</param>
        /// <param name="characterName">The name of the character.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public async Task<RetrieveEntityResult<Character>> GetUserCharacterByNameAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] ICommandContext context,
            [NotNull] IUser characterOwner,
            [NotNull] string characterName
        )
        {
            var character = await GetUserCharacters(db, characterOwner, context.Guild)
            .FirstOrDefaultAsync
            (
                ch => string.Equals(ch.Name, characterName, StringComparison.OrdinalIgnoreCase)
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
        /// Makes the given character current on the given server.
        /// </summary>
        /// <param name="db">The database where the characters are stored.</param>
        /// <param name="context">The context of the user.</param>
        /// <param name="discordServer">The server to make the character current on.</param>
        /// <param name="character">The character to make current.</param>
        /// <returns>A task that must be awaited.</returns>
        public async Task<ModifyEntityResult> MakeCharacterCurrentOnServerAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] ICommandContext context,
            [NotNull] IGuild discordServer,
            [NotNull] Character character
        )
        {
            var user = await context.Guild.GetUserAsync((ulong)character.Owner.DiscordID);

            if (character.IsCurrent)
            {
                return ModifyEntityResult.FromError(CommandError.MultipleMatches, "The character is already current on the server.");
            }

            await ClearCurrentCharacterOnServerAsync(db, user, discordServer);

            character.IsCurrent = true;

            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
        }

        /// <summary>
        /// Clears any current characters in the server from the given user.
        /// </summary>
        /// <param name="db">The database where the characters are stored.</param>
        /// <param name="discordUser">The user to clear the characters from.</param>
        /// <param name="discordServer">The server to clear the characters on.</param>
        /// <returns>A task that must be awaited.</returns>
        public async Task<ModifyEntityResult> ClearCurrentCharacterOnServerAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] IUser discordUser,
            [NotNull] IGuild discordServer
        )
        {
            if (!await HasActiveCharacterOnServerAsync(db, discordUser, discordServer))
            {
                return ModifyEntityResult.FromError(CommandError.ObjectNotFound, "There's no current character on this server.");
            }

            var currentCharactersOnServer = GetUserCharacters(db, discordUser, discordServer).Where(ch => ch.IsCurrent);

            await currentCharactersOnServer.ForEachAsync
            (
                ch => ch.IsCurrent = false
            );

            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
        }

        /// <summary>
        /// Determines whether or not the given user has an active character on the given server.
        /// </summary>
        /// <param name="db">The database where the characters are stored.</param>
        /// <param name="discordUser">The user to check.</param>
        /// <param name="discordServer">The server to check.</param>
        /// <returns>true if the user has an active character on the server; otherwise, false.</returns>
        [Pure, ContractAnnotation("discordServer:null => false")]
        public async Task<bool> HasActiveCharacterOnServerAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] IUser discordUser,
            [CanBeNull] IGuild discordServer
        )
        {
            if (discordServer is null)
            {
                // TODO: Allow users to assume characters in DMs
                return false;
            }

            var userCharacters = GetUserCharacters(db, discordUser, discordServer);

            return await userCharacters
            .AnyAsync
            (
                c => c.IsCurrent
            );
        }

        /// <summary>
        /// Creates a character with the given name and default settings.
        /// </summary>
        /// <param name="db">The database where the characters are stored.</param>
        /// <param name="context">The context of the command.</param>
        /// <param name="characterName">The name of the character.</param>
        /// <returns>A creation result which may or may not have been successful.</returns>
        public async Task<CreateEntityResult<Character>> CreateCharacterAsync([NotNull] GlobalInfoContext db, [NotNull] ICommandContext context, [NotNull] string characterName)
        {
            return await CreateCharacterAsync(db, context, characterName, this.Content.DefaultAvatarUri.ToString(), null, null, null);
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
            [NotNull] ICommandContext context,
            [NotNull] string characterName,
            [NotNull] string characterAvatarUrl,
            [CanBeNull] string characterNickname,
            [CanBeNull] string characterSummary,
            [CanBeNull] string characterDescription
        )
        {
            var getOwnerResult = await db.GetOrRegisterUserAsync(context.Message.Author);
            if (!getOwnerResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(getOwnerResult);
            }

            var owner = getOwnerResult.Entity;

            var character = new Character
            {
                Owner = owner,
                ServerID = (long)context.Guild.Id
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

            if (!(characterNickname is null))
            {
                modifyEntityResult = await SetCharacterNicknameAsync(db, character, characterNickname);
                if (!modifyEntityResult.IsSuccess)
                {
                    return CreateEntityResult<Character>.FromError(modifyEntityResult);
                }
            }

            characterSummary = characterSummary ?? "No summary set.";
            modifyEntityResult = await SetCharacterSummaryAsync(db, character, characterSummary);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            characterDescription = characterDescription ?? "No description set.";
            modifyEntityResult = await SetCharacterDescriptionAsync(db, character, characterDescription);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            var defaultPronounFamilyName = this.PronounProviders.FirstOrDefault(p => p.Value is TheyPronounProvider).Value?.Family ?? new TheyPronounProvider().Family;
            modifyEntityResult = await SetCharacterPronounAsync(db, character, defaultPronounFamilyName);
            if (!modifyEntityResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(modifyEntityResult);
            }

            var getDefaultAppearanceResult = await Appearance.CreateDefaultAsync(db, this.Transformations);
            if (!getDefaultAppearanceResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(getDefaultAppearanceResult);
            }

            var defaultAppearance = getDefaultAppearanceResult.Entity;
            character.DefaultAppearance = defaultAppearance;

            // The default and current appearances must be different objects, or the end up pointing to the same
            // database record, which is not desired.
            var getCurrentAppearanceResult = await Appearance.CreateDefaultAsync(db, this.Transformations);
            if (!getCurrentAppearanceResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(getCurrentAppearanceResult);
            }

            var currentAppearance = getCurrentAppearanceResult.Entity;
            character.CurrentAppearance = currentAppearance;

            owner.Characters.Add(character);
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
        /// Sets the default character of a user.
        /// </summary>
        /// <param name="db">The database containing the characters.</param>
        /// <param name="context">The context of the operation.</param>
        /// <param name="newDefaultCharacter">The new default character.</param>
        /// <param name="targetUser">The user to set the default character of.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetDefaultCharacterForUserAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] ICommandContext context,
            [NotNull] Character newDefaultCharacter,
            [NotNull] User targetUser
        )
        {
            var isCurrentUser = context.Message.Author.Id == (ulong)newDefaultCharacter.Owner.DiscordID;
            var isSameCharacter = targetUser.DefaultCharacter?.Name == newDefaultCharacter.Name;
            if (isSameCharacter)
            {
                var errorMessage = isCurrentUser
                    ? "That's already your default character."
                    : "That's already the user's default character.";

                return ModifyEntityResult.FromError(CommandError.UnmetPrecondition, errorMessage);
            }

            targetUser.DefaultCharacter = newDefaultCharacter;
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
        }

        /// <summary>
        /// Clears the default character from the given user.
        /// </summary>
        /// <param name="db">The database containing the characters.</param>
        /// <param name="context">The context of the operation.</param>
        /// <param name="targetUser">The user to clear the default character of.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> ClearDefaultCharacterForUserAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] ICommandContext context,
            [NotNull] User targetUser
        )
        {
            var isCurrentUser = context.Message.Author.Id == (ulong)targetUser.DiscordID;
            if (targetUser.DefaultCharacter is null)
            {
                var errorMessage = isCurrentUser
                    ? "You don't have a default character."
                    : "That user doesn't have a default character.";

                return ModifyEntityResult.FromError(CommandError.ObjectNotFound, errorMessage);
            }

            targetUser.DefaultCharacter = null;
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
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
            [NotNull] ICommandContext context,
            [NotNull] Character character,
            [NotNull] string newCharacterName
        )
        {
            var isCurrentUser = context.Message.Author.Id == (ulong)character.Owner.DiscordID;
            if (string.IsNullOrWhiteSpace(newCharacterName))
            {
                return ModifyEntityResult.FromError(CommandError.BadArgCount, "You need to provide a name.");
            }

            if (string.Equals(character.Name, newCharacterName, StringComparison.OrdinalIgnoreCase))
            {
                return ModifyEntityResult.FromError(CommandError.Unsuccessful, "The character already has that name.");
            }

            if (newCharacterName.Contains("\""))
            {
                return ModifyEntityResult.FromError(CommandError.Unsuccessful, "The name may not contain double quotes.");
            }

            if (!await IsCharacterNameUniqueForUserAsync(db, context.Message.Author, newCharacterName, context.Guild))
            {
                var errorMessage = isCurrentUser
                    ? "You already have a character with that name."
                    : "The user already has a character with that name.";

                return ModifyEntityResult.FromError(CommandError.MultipleMatches, errorMessage);
            }

            var commandModule = this.Commands.Modules.FirstOrDefault(m => m.Name == "character");
            if (!(commandModule is null))
            {
                var validNameResult = this.OwnedEntities.IsEntityNameValid(commandModule, newCharacterName);
                if (!validNameResult.IsSuccess)
                {
                    return ModifyEntityResult.FromError(validNameResult);
                }
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
                return ModifyEntityResult.FromError(CommandError.BadArgCount, "You need to provide a new avatar url.");
            }

            if (character.AvatarUrl == newCharacterAvatarUrl)
            {
                return ModifyEntityResult.FromError(CommandError.Unsuccessful, "The character's avatar is already set to that URL.");
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

            if (character.Nickname == newCharacterNickname)
            {
                return ModifyEntityResult.FromError(CommandError.Unsuccessful, "The character already has that nickname.");
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

            if (character.Summary == newCharacterSummary)
            {
                return ModifyEntityResult.FromError(CommandError.Unsuccessful, "That's already the character's summary.");
            }

            if (newCharacterSummary.Length > 240)
            {
                return ModifyEntityResult.FromError(CommandError.Unsuccessful, "The summary is too long. It can be at most 240 characters.");
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

            if (character.Description == newCharacterDescription)
            {
                return ModifyEntityResult.FromError(CommandError.Unsuccessful, "The character already has that description.");
            }

            character.Description = newCharacterDescription;
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
        }

        /// <summary>
        /// Sets the preferred pronoun for the given character.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="character">The character.</param>
        /// <param name="pronounFamily">The pronoun family.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterPronounAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] Character character,
            [NotNull] string pronounFamily
        )
        {
            if (pronounFamily.IsNullOrWhitespace())
            {
                return ModifyEntityResult.FromError(CommandError.BadArgCount, "You need to provide a pronoun family.");
            }

            if (!this.PronounProviders.ContainsKey(pronounFamily))
            {
                return ModifyEntityResult.FromError(CommandError.ObjectNotFound, "Could not find a pronoun provider for that family.");
            }

            if (character.PronounProviderFamily == pronounFamily)
            {
                return ModifyEntityResult.FromError(CommandError.Unsuccessful, "The character is already using that pronoun set.");
            }

            var pronounProvider = this.PronounProviders[pronounFamily];
            character.PronounProviderFamily = pronounProvider.Family;

            await db.SaveChangesAsync();
            return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
        }

        /// <summary>
        /// Sets whether or not a character is NSFW.
        /// </summary>
        /// <param name="db">The database where the characters are stored.</param>
        /// <param name="character">The character to edit.</param>
        /// <param name="isNSFW">Whether or not the character is NSFW</param>
        /// <returns>A task that must be awaited.</returns>
        public async Task<ModifyEntityResult> SetCharacterIsNSFWAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] Character character,
            bool isNSFW
        )
        {
            if (character.IsNSFW == isNSFW)
            {
                var message = character.IsNSFW
                    ? "The character is already NSFW."
                    : "The character is alreadu SFW.";

                return ModifyEntityResult.FromError(CommandError.Unsuccessful, message);
            }

            character.IsNSFW = isNSFW;
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
        }

        /// <summary>
        /// Transfers ownership of the named character to the specified user.
        /// </summary>
        /// <param name="db">The database where the characters are stored.</param>
        /// <param name="newOwner">The new owner.</param>
        /// <param name="character">The character to transfer.</param>
        /// <param name="guild">The guild to scope the character search to.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> TransferCharacterOwnershipAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] IUser newOwner,
            [NotNull] Character character,
            [NotNull] IGuild guild
        )
        {
            var newOwnerCharacters = GetUserCharacters(db, newOwner, guild);
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
        /// <param name="guild">The guild to get the user's characters on.</param>
        /// <returns>A queryable list of characters belonging to the user.</returns>
        [Pure]
        [NotNull]
        [ItemNotNull]
        public IQueryable<Character> GetUserCharacters
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] IUser discordUser,
            [NotNull] IGuild guild
        )
        {
            var characters = GetCharacters(db, guild).Where(ch => ch.Owner.DiscordID == (long)discordUser.Id);
            return characters;
        }

        /// <summary>
        /// Determines whether or not the given character name is unique for a given user.
        /// </summary>
        /// <param name="db">The database where the characters are stored.</param>
        /// <param name="discordUser">The user to check.</param>
        /// <param name="characterName">The character name to check.</param>
        /// <param name="guild">The guild to scope the character search to.</param>
        /// <returns>true if the name is unique; otherwise, false.</returns>
        [Pure]
        public async Task<bool> IsCharacterNameUniqueForUserAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] IUser discordUser,
            [NotNull] string characterName,
            [NotNull] IGuild guild
        )
        {
            var userCharacters = GetUserCharacters(db, discordUser, guild);
            return await this.OwnedEntities.IsEntityNameUniqueForUserAsync(userCharacters, characterName);
        }

        /// <summary>
        /// Adds the given image with the given metadata to the given character.
        /// </summary>
        /// <param name="db">The database where the characters and images are stored.</param>
        /// <param name="character">The character to add the image to.</param>
        /// <param name="imageName">The name of the image.</param>
        /// <param name="imageUrl">The url of the image.</param>
        /// <param name="imageCaption">The caption of the image.</param>
        /// <param name="isNSFW">Whether or not the image is NSFW</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> AddImageToCharacterAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] Character character,
            [NotNull] string imageName,
            [NotNull] string imageUrl,
            [CanBeNull] string imageCaption = null,
            bool isNSFW = false
        )
        {
            bool isImageNameUnique = !character.Images.Any(i => string.Equals(i.Name, imageName, StringComparison.OrdinalIgnoreCase));
            if (!isImageNameUnique)
            {
                return ModifyEntityResult.FromError(CommandError.MultipleMatches, "The character already has an image with that name.");
            }

            var image = new Image
            {
                Name = imageName,
                Caption = imageCaption,
                Url = imageUrl,
                IsNSFW = isNSFW
            };

            character.Images.Add(image);
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess(ModifyEntityAction.Added);
        }

        /// <summary>
        /// Removes the named image from the given character.
        /// </summary>
        /// <param name="db">The database where the characters and images are stored.</param>
        /// <param name="character">The character to remove the image from.</param>
        /// <param name="imageName">The name of the image.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> RemoveImageFromCharacterAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] Character character,
            [NotNull] string imageName
        )
        {
            bool hasNamedImage = character.Images.Any(i => string.Equals(i.Name, imageName, StringComparison.OrdinalIgnoreCase));
            if (!hasNamedImage)
            {
                return ModifyEntityResult.FromError(CommandError.MultipleMatches, "The character has no image with that name.");
            }

            character.Images.RemoveAll(i => string.Equals(i.Name, imageName, StringComparison.OrdinalIgnoreCase));
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess(ModifyEntityAction.Added);
        }

        /// <summary>
        /// Creates a new template character with a given appearance.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="context">The context of the command.</param>
        /// <param name="characterName">The name of the new character.</param>
        /// <param name="appearance">The appearance that the new character should have.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<Character>> CreateCharacterFromAppearanceAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] ICommandContext context,
            [NotNull] string characterName,
            [NotNull] Appearance appearance
        )
        {
            var createCharacterResult = await CreateCharacterAsync(db, context, characterName);
            if (!createCharacterResult.IsSuccess)
            {
                return createCharacterResult;
            }

            var newCharacter = createCharacterResult.Entity;
            newCharacter.DefaultAppearance = appearance;

            await db.SaveChangesAsync();

            var getCharacterResult = await GetUserCharacterByNameAsync(db, context, context.Message.Author, characterName);
            if (!getCharacterResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(getCharacterResult);
            }

            return CreateEntityResult<Character>.FromSuccess(getCharacterResult.Entity);
        }

        /// <summary>
        /// Creates a new character role from the given Discord role and access condition.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="role">The discord role.</param>
        /// <param name="access">The access conditions.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<CharacterRole>> CreateCharacterRoleAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] IRole role,
            RoleAccess access
        )
        {
            var getExistingRoleResult = await GetCharacterRoleAsync(db, role);
            if (getExistingRoleResult.IsSuccess)
            {
                return CreateEntityResult<CharacterRole>.FromError
                (
                    CommandError.MultipleMatches,
                    "That role is already registered as a character role."
                );
            }

            var server = await db.GetOrRegisterServerAsync(role.Guild);

            var characterRole = new CharacterRole
            {
                Server = server,
                DiscordID = (long)role.Id,
                Access = access
            };

            await db.CharacterRoles.AddAsync(characterRole);
            await db.SaveChangesAsync();

            return CreateEntityResult<CharacterRole>.FromSuccess(characterRole);
        }

        /// <summary>
        /// Deletes the character role for the given Discord role.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="role">The character role.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<DeleteEntityResult> DeleteCharacterRoleAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] CharacterRole role
        )
        {
            db.CharacterRoles.Remove(role);
            await db.SaveChangesAsync();

            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets an existing character role from the database.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="role">The discord role.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<CharacterRole>> GetCharacterRoleAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] IRole role
        )
        {
            var characterRole = await db.CharacterRoles
                .Include(r => r.Server)
                .FirstOrDefaultAsync(r => r.Server.DiscordID == (long)role.Guild.Id && r.DiscordID == (long)role.Id);

            if (characterRole is null)
            {
                return RetrieveEntityResult<CharacterRole>.FromError
                (
                    CommandError.ObjectNotFound,
                    "That role is not registered as a character role."
                );
            }

            return RetrieveEntityResult<CharacterRole>.FromSuccess(characterRole);
        }

        /// <summary>
        /// Sets the access conditions for the given character role.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="role">The character role.</param>
        /// <param name="access">The access conditions.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterRoleAccessAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] CharacterRole role,
            RoleAccess access
        )
        {
            if (role.Access == access)
            {
                return ModifyEntityResult.FromError
                (
                    CommandError.Unsuccessful,
                    "The role already has those access conditions."
                );
            }

            role.Access = access;
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
        }

        /// <summary>
        /// Sets the custom role of a character.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="character">The character.</param>
        /// <param name="characterRole">The role to set.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterRoleAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] Character character,
            [NotNull] CharacterRole characterRole
        )
        {
            if (character.Role == characterRole)
            {
                return ModifyEntityResult.FromError
                (
                    CommandError.Unsuccessful,
                    "The character already has that role."
                );
            }

            character.Role = characterRole;

            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
        }

        /// <summary>
        /// Clears the custom role of a character.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="character">The character.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> ClearCharacterRoleAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] Character character
        )
        {
            if (character.Role is null)
            {
                return ModifyEntityResult.FromError
                (
                    CommandError.Unsuccessful,
                    "The character doesn't have a role set."
                );
            }

            character.Role = null;

            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
        }
    }
}
