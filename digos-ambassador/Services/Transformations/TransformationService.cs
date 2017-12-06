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

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Appearances;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Transformations;

using Discord;
using Discord.Commands;

using Humanizer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Handles transformations of users and their characters.
	/// </summary>
	public class TransformationService
	{
		private readonly ContentService Content;

		private TransformationDescriptionBuilder DescriptionBuilder;

		/// <summary>
		/// Initializes a new instance of the <see cref="TransformationService"/> class.
		/// </summary>
		/// <param name="content">The content service.</param>
		public TransformationService(ContentService content)
		{
			this.Content = content;
		}

		/// <summary>
		/// Sets the description builder to use with the service.
		/// </summary>
		/// <param name="descriptionBuilder">The builder.</param>
		/// <returns>The transformation service with the given builder.</returns>
		public TransformationService WithDescriptionBuilder(TransformationDescriptionBuilder descriptionBuilder)
		{
			this.DescriptionBuilder = descriptionBuilder;
			return this;
		}

		/// <summary>
		/// Removes the given character's bodypart.
		/// </summary>
		/// <param name="global">The global database.</param>
		/// <param name="local">The database where characters and transformations are stored.</param>
		/// <param name="context">The context of the command.</param>
		/// <param name="character">The character to shift.</param>
		/// <param name="bodyPart">The bodypart to remove.</param>
		/// <returns>A shifting result which may or may not have succeeded.</returns>
		public async Task<ShiftBodypartResult> RemoveCharacterBodypartAsync
		(
			[NotNull] GlobalInfoContext global,
			[NotNull] LocalInfoContext local,
			[NotNull] ICommandContext context,
			[NotNull] Character character,
			Bodypart bodyPart
		)
		{
			var discordUser = await context.Guild.GetUserAsync(character.Owner.DiscordID);
			var canTransformResult = await CanUserTransformUserAsync(global, local, context.User, discordUser);
			if (!canTransformResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(canTransformResult);
			}

			var component = character.CurrentAppearance.Components.FirstOrDefault(c => c.Bodypart == bodyPart);
			if (component is null)
			{
				return ShiftBodypartResult.FromError(CommandError.ObjectNotFound, "The character doesn't have that bodypart.");
			}

			var transformation = component.Transformation;
			character.CurrentAppearance.Components.Remove(component);
			await local.SaveChangesAsync();

			string removeMessage = this.DescriptionBuilder.BuildRemoveMessage(character, transformation);
			return ShiftBodypartResult.FromSuccess(removeMessage);
		}

		/// <summary>
		/// Adds the given bodypart to the given character.
		/// </summary>
		/// <param name="global">The global database.</param>
		/// <param name="local">The database where characters and transformations are stored.</param>
		/// <param name="context">The context of the command.</param>
		/// <param name="character">The character to shift.</param>
		/// <param name="bodyPart">The bodypart to add.</param>
		/// <param name="species">The species of the part to add..</param>
		/// <returns>A shifting result which may or may not have succeeded.</returns>
		public async Task<ShiftBodypartResult> AddCharacterBodypartAsync
		(
			[NotNull] GlobalInfoContext global,
			[NotNull] LocalInfoContext local,
			[NotNull] ICommandContext context,
			[NotNull] Character character,
			Bodypart bodyPart,
			[NotNull] string species
		)
		{
			var discordUser = await context.Guild.GetUserAsync(character.Owner.DiscordID);
			var canTransformResult = await CanUserTransformUserAsync(global, local, context.User, discordUser);
			if (!canTransformResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(canTransformResult);
			}

			if (character.HasBodypart(bodyPart))
			{
				return ShiftBodypartResult.FromError(CommandError.ObjectNotFound, "The character already has that bodypart.");
			}

			var getSpeciesResult = await GetSpeciesByNameAsync(global, species);
			if (!getSpeciesResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(getSpeciesResult);
			}

			var getTFResult = await GetTransformationByPartAndSpeciesAsync(global, bodyPart, getSpeciesResult.Entity);
			if (!getTFResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(getTFResult);
			}

			var transformation = getTFResult.Entity;

			var component = AppearanceComponent.CreateFrom(transformation);
			character.CurrentAppearance.Components.Add(component);
			await local.SaveChangesAsync();

			string growMessage = this.DescriptionBuilder.BuildGrowMessage(character, transformation);
			return ShiftBodypartResult.FromSuccess(growMessage);
		}

		/// <summary>
		/// Shifts the given character's bodypart to the given species.
		/// </summary>
		/// <param name="global">The global database.</param>
		/// <param name="local">The database where characters and transformations are stored.</param>
		/// <param name="context">The context of the command.</param>
		/// <param name="character">The character to shift.</param>
		/// <param name="bodyPart">The bodypart to shift.</param>
		/// <param name="species">The species to shift the bodypart into.</param>
		/// <returns>A shifting result which may or may not have succeeded.</returns>
		public async Task<ShiftBodypartResult> ShiftBodypartAsync
		(
			[NotNull] GlobalInfoContext global,
			[NotNull] LocalInfoContext local,
			[NotNull] ICommandContext context,
			[NotNull] Character character,
			Bodypart bodyPart,
			[NotNull] string species
		)
		{
			var discordUser = await context.Guild.GetUserAsync(character.Owner.DiscordID);
			var canTransformResult = await CanUserTransformUserAsync(global, local, context.User, discordUser);
			if (!canTransformResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(canTransformResult);
			}

			var getSpeciesResult = await GetSpeciesByNameAsync(global, species);
			if (!getSpeciesResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(getSpeciesResult);
			}

			var getTFResult = await GetTransformationByPartAndSpeciesAsync(global, bodyPart, getSpeciesResult.Entity);
			if (!getTFResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(getTFResult);
			}

			string shiftMessage;
			AppearanceComponent currentComponent;
			var transformation = getTFResult.Entity;
			if (!character.HasBodypart(bodyPart))
			{
				currentComponent = AppearanceComponent.CreateFrom(transformation);
				character.CurrentAppearance.Components.Add(currentComponent);

				shiftMessage = this.DescriptionBuilder.BuildGrowMessage(character, transformation);
			}
			else
			{
				currentComponent = character.GetBodypart(bodyPart);
				if (currentComponent.Transformation.Species.Name.Equals(transformation.Species.Name))
				{
					return ShiftBodypartResult.FromError(CommandError.Unsuccessful, "The user's bodypart is already that form.");
				}

				currentComponent.Transformation = transformation;

				shiftMessage = this.DescriptionBuilder.BuildShiftMessage(character, transformation);
			}

			await local.SaveChangesAsync();

			return ShiftBodypartResult.FromSuccess(shiftMessage);
		}

		/// <summary>
		/// Shifts the colour of the given bodypart on the given character to the given colour.
		/// </summary>
		/// <param name="global">The global database.</param>
		/// <param name="local">The database.</param>
		/// <param name="context">The command context.</param>
		/// <param name="character">The character to shift.</param>
		/// <param name="bodyPart">The bodypart to shift.</param>
		/// <param name="colour">The colour to shift it into.</param>
		/// <returns>A shifting result which may or may not have succeeded.</returns>
		public async Task<ShiftBodypartResult> ShiftBodypartColourAsync
		(
			[NotNull] GlobalInfoContext global,
			[NotNull] LocalInfoContext local,
			[NotNull] ICommandContext context,
			[NotNull] Character character,
			Bodypart bodyPart,
			[NotNull] Colour colour
		)
		{
			var discordUser = await context.Guild.GetUserAsync(character.Owner.DiscordID);
			var canTransformResult = await CanUserTransformUserAsync(global, local, context.User, discordUser);
			if (!canTransformResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(canTransformResult);
			}

			if (!character.HasBodypart(bodyPart))
			{
				return ShiftBodypartResult.FromError(CommandError.ObjectNotFound, "The character doesn't have that bodypart.");
			}

			var currentComponent = character.GetBodypart(bodyPart);
			var originalColour = currentComponent.BaseColour;
			currentComponent.BaseColour = colour;

			await local.SaveChangesAsync();

			string shiftMessage = this.DescriptionBuilder.BuildColourShiftMessage(character, originalColour, currentComponent);
			return ShiftBodypartResult.FromSuccess(shiftMessage);
		}

		/// <summary>
		/// Shifts the pattern of the given bodypart on the given character to the given pattern with the given colour.
		/// </summary>
		/// <param name="global">The global database.</param>
		/// <param name="local">The database.</param>
		/// <param name="context">The command context.</param>
		/// <param name="character">The character to shift.</param>
		/// <param name="bodyPart">The bodypart to shift.</param>
		/// <param name="pattern">The pattern to shift the bodypart into.</param>
		/// <param name="patternColour">The colour to shift it into.</param>
		/// <returns>A shifting result which may or may not have succeeded.</returns>
		public async Task<ShiftBodypartResult> ShiftBodypartPatternAsync
		(
			[NotNull] GlobalInfoContext global,
			[NotNull] LocalInfoContext local,
			[NotNull] ICommandContext context,
			[NotNull] Character character,
			Bodypart bodyPart,
			Pattern pattern,
			[NotNull] Colour patternColour
		)
		{
			var discordUser = await context.Guild.GetUserAsync(character.Owner.DiscordID);
			var canTransformResult = await CanUserTransformUserAsync(global, local, context.User, discordUser);
			if (!canTransformResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(canTransformResult);
			}

			if (!character.HasBodypart(bodyPart))
			{
				return ShiftBodypartResult.FromError(CommandError.ObjectNotFound, "The character doesn't have that bodypart.");
			}

			var currentComponent = character.GetBodypart(bodyPart);

			var originalPattern = currentComponent.Pattern;
			var originalColour = currentComponent.BaseColour;

			currentComponent.Pattern = pattern;
			currentComponent.PatternColour = patternColour;

			await local.SaveChangesAsync();

			string shiftMessage = this.DescriptionBuilder.BuildPatternShiftMessage(character, originalPattern, originalColour, currentComponent);
			return ShiftBodypartResult.FromSuccess(shiftMessage);
		}

		/// <summary>
		/// Shifts the colour of the given bodypart's pattern on the given character to the given colour.
		/// </summary>
		/// <param name="global">The global database.</param>
		/// <param name="local">The local database.</param>
		/// <param name="context">The command context.</param>
		/// <param name="character">The character to shift.</param>
		/// <param name="bodyPart">The bodypart to shift.</param>
		/// <param name="patternColour">The colour to shift it into.</param>
		/// <returns>A shifting result which may or may not have succeeded.</returns>
		public async Task<ShiftBodypartResult> ShiftPatternColourAsync
		(
			[NotNull] GlobalInfoContext global,
			[NotNull] LocalInfoContext local,
			[NotNull] ICommandContext context,
			[NotNull] Character character,
			Bodypart bodyPart,
			[NotNull] Colour patternColour
		)
		{
			var discordUser = await context.Guild.GetUserAsync(character.Owner.DiscordID);
			var canTransformResult = await CanUserTransformUserAsync(global, local, context.User, discordUser);
			if (!canTransformResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(canTransformResult);
			}

			if (!character.HasBodypart(bodyPart))
			{
				return ShiftBodypartResult.FromError(CommandError.ObjectNotFound, "The character doesn't have that bodypart.");
			}

			var currentComponent = character.GetBodypart(bodyPart);

			if (currentComponent.PatternColour is null)
			{
				return ShiftBodypartResult.FromError(CommandError.ObjectNotFound, "The pattern doesn't have a colour to shift.");
			}

			var originalColour = currentComponent.PatternColour;
			currentComponent.PatternColour = patternColour;

			await local.SaveChangesAsync();

			string shiftMessage = this.DescriptionBuilder.BuildPatternColourShiftMessage(character, originalColour, currentComponent);
			return ShiftBodypartResult.FromSuccess(shiftMessage);
		}

		/// <summary>
		/// Determines whether or not a user is allowed to transform another user.
		/// </summary>
		/// <param name="global">The global database.</param>
		/// <param name="local">The local database.</param>
		/// <param name="invokingUser">The user trying to transform.</param>
		/// <param name="targetUser">The user being transformed.</param>
		/// <returns>A conditional determination with an attached reason if it failed.</returns>
		[Pure]
		public async Task<DetermineConditionResult> CanUserTransformUserAsync
		(
			[NotNull] GlobalInfoContext global,
			[NotNull] LocalInfoContext local,
			[NotNull] IUser invokingUser,
			[NotNull] IUser targetUser
		)
		{
			var localProtection = await GetOrCreateServerUserProtectionAsync(global, local, targetUser);
			if (!localProtection.HasOptedIn)
			{
				return DetermineConditionResult.FromError("The target hasn't opted into transformations.");
			}

			var globalProtection = await GetOrCreateGlobalUserProtectionAsync(global, targetUser);
			switch (localProtection.Type)
			{
				case ProtectionType.Blacklist:
				{
					return globalProtection.Blacklist.All(u => u.Identifier.DiscordID != invokingUser.Id)
						? DetermineConditionResult.FromSuccess()
						: DetermineConditionResult.FromError("You're on that user's blacklist.");
				}
				case ProtectionType.Whitelist:
				{
					return globalProtection.Whitelist.Any(u => u.Identifier.DiscordID == invokingUser.Id)
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
		/// <param name="context">The context of the generation.</param>
		/// <param name="character">The character to generate the description for.</param>
		/// <returns>An embed with a formatted description.</returns>
		[Pure]
		public async Task<Embed> GenerateCharacterDescriptionAsync
		(
			[NotNull] ICommandContext context,
			[NotNull] Character character
		)
		{
			var eb = new EmbedBuilder();
			eb.WithColor(Color.DarkPurple);
			eb.WithTitle($"{character.Name} {(character.Nickname is null ? string.Empty : $"\"{character.Nickname}\"")}".Trim());

			var user = await context.Client.GetUserAsync(character.Owner.DiscordID);
			eb.WithAuthor(user);

			eb.WithThumbnailUrl
			(
				!character.AvatarUrl.IsNullOrWhitespace()
					? character.AvatarUrl
					: this.Content.DefaultAvatarUri.ToString()
			);

			eb.AddField("Description", character.Description);

			string visualDescription = this.DescriptionBuilder.BuildVisualDescription(character);
			eb.WithDescription(visualDescription);

			return eb.Build();
		}

		/// <summary>
		/// Gets the available species in transformations.
		/// </summary>
		/// <param name="db">The database containing the transformations.</param>
		/// <returns>A list of the available species.</returns>
		[Pure]
		public async Task<IReadOnlyList<Species>> GetAvailableSpeciesAsync([NotNull] GlobalInfoContext db)
		{
			return await db.Species
				.Include(s => s.Parent)
				.ToListAsync();
		}

		/// <summary>
		/// Gets the available transformations for the given bodypart.
		/// </summary>
		/// <param name="db">The database containing the transformations.</param>
		/// <param name="bodyPart">The bodypart to get the transformations for.</param>
		/// <returns>A list of the available transformations..</returns>
		[Pure]
		public async Task<IReadOnlyList<Transformation>> GetAvailableTransformations
		(
			[NotNull] GlobalInfoContext db,
			Bodypart bodyPart
		)
		{
			return await db.Transformations
				.Include(tf => tf.Species)
				.Where(tf => tf.Part == bodyPart).ToListAsync();
		}

		/// <summary>
		/// Resets the given character's appearance to its default state.
		/// </summary>
		/// <param name="db">The database containing the characters.</param>
		/// <param name="character">The character to reset.</param>
		/// <returns>An entity modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> ResetCharacterFormAsync
		(
			[NotNull] LocalInfoContext db,
			[NotNull] Character character
		)
		{
			if (character.DefaultAppearance is null)
			{
				return ModifyEntityResult.FromError(CommandError.ObjectNotFound, "The character has no default appearance.");
			}

			character.CurrentAppearance = character.DefaultAppearance;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Sets the current appearance of the given character as its default appearance.
		/// </summary>
		/// <param name="db">The database containing the characters.</param>
		/// <param name="character">The character to set the default appearance of.</param>
		/// <returns>An entity modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetCurrentAppearanceAsDefaultForCharacterAsync
		(
			[NotNull] LocalInfoContext db,
			[NotNull] Character character
		)
		{
			if (character.CurrentAppearance is null)
			{
				return ModifyEntityResult.FromError(CommandError.ObjectNotFound, "The character doesn't have an altered appearance.");
			}

			character.DefaultAppearance = character.CurrentAppearance;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Sets the default protection type that the user has for transformations.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordUser">The user to set the protection for.</param>
		/// <param name="protectionType">The protection type to set.</param>
		/// <returns>An entity modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetDefaultProtectionTypeAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser discordUser,
			ProtectionType protectionType
		)
		{
			var protection = await GetOrCreateGlobalUserProtectionAsync(db, discordUser);
			if (protection.DefaultType == protectionType)
			{
				return ModifyEntityResult.FromError(CommandError.Unsuccessful, $"{protectionType.Humanize()} is already your default setting.");
			}

			protection.DefaultType = protectionType;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Sets the protection type that the user has for transformations on the given server.
		/// </summary>
		/// <param name="global">The global database.</param>
		/// <param name="local">The local database.</param>
		/// <param name="discordUser">The user to set the protection for.</param>
		/// <param name="protectionType">The protection type to set.</param>
		/// <returns>An entity modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetServerProtectionTypeAsync
		(
			[NotNull] GlobalInfoContext global,
			[NotNull] LocalInfoContext local,
			[NotNull] IUser discordUser,
			ProtectionType protectionType
		)
		{
			var protection = await GetOrCreateServerUserProtectionAsync(global, local, discordUser);
			if (protection.Type == protectionType)
			{
				return ModifyEntityResult.FromError(CommandError.Unsuccessful, $"{protectionType.Humanize()} is already your current setting.");
			}

			protection.Type = protectionType;
			await local.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Whitelists the given user, allowing them to transform the <paramref name="discordUser"/>.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordUser">The user to modify.</param>
		/// <param name="whitelistedUser">The user to add to the whitelist.</param>
		/// <returns>An entity modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> WhitelistUserAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser discordUser,
			[NotNull] IUser whitelistedUser
		)
		{
			var protection = await GetOrCreateGlobalUserProtectionAsync(db, discordUser);
			if (protection.Whitelist.Any(u => u.Identifier.DiscordID == whitelistedUser.Id))
			{
				return ModifyEntityResult.FromError(CommandError.Unsuccessful, "You've already whitelisted that user.");
			}

			var user = await db.GetOrRegisterUserAsync(whitelistedUser);
			protection.Whitelist.Add(user);
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Blacklists the given user, preventing them from transforming the <paramref name="discordUser"/>.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordUser">The user to modify.</param>
		/// <param name="blacklistedUser">The user to add to the blacklist.</param>
		/// <returns>An entity modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> BlacklistUserAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser discordUser,
			[NotNull] IUser blacklistedUser
		)
		{
			var protection = await GetOrCreateGlobalUserProtectionAsync(db, discordUser);
			if (protection.Blacklist.Any(u => u.Identifier.DiscordID == blacklistedUser.Id))
			{
				return ModifyEntityResult.FromError(CommandError.Unsuccessful, "You've already blacklisted that user.");
			}

			var user = await db.GetOrRegisterUserAsync(blacklistedUser);
			protection.Blacklist.Add(user);
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Gets or creates the global transformation protection data for the given user.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordUser">The user.</param>
		/// <returns>Global protection data for the given user.</returns>
		public async Task<GlobalUserProtection> GetOrCreateGlobalUserProtectionAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser discordUser
		)
		{
			var protection = await db.GlobalUserProtections
			.Include(p => p.User)
			.Include(p => p.Whitelist)
			.Include(p => p.Blacklist)
			.FirstOrDefaultAsync(p => p.User.Identifier.DiscordID == discordUser.Id);

			if (!(protection is null))
			{
				return protection;
			}

			var user = await db.GetOrRegisterUserAsync(discordUser);
			protection = GlobalUserProtection.CreateDefault(user);

			await db.GlobalUserProtections.AddAsync(protection);
			await db.SaveChangesAsync();

			return protection;
		}

		/// <summary>
		/// Gets or creates server-specific transformation protection data for the given user and server.
		/// </summary>
		/// <param name="global">The global database.</param>
		/// <param name="local">The local database.</param>
		/// <param name="discordUser">The user.</param>
		/// <returns>Server-specific protection data for the given user.</returns>
		public async Task<ServerUserProtection> GetOrCreateServerUserProtectionAsync
		(
			[NotNull] GlobalInfoContext global,
			[NotNull] LocalInfoContext local,
			[NotNull] IUser discordUser
		)
		{
			var protection = await local.UserProtections
			.FirstOrDefaultAsync
			(
				p => p.User.DiscordID == discordUser.Id
			);

			if (!(protection is null))
			{
				return protection;
			}

			var globalProtection = await GetOrCreateGlobalUserProtectionAsync(global, discordUser);
			protection = ServerUserProtection.CreateDefault(globalProtection);

			await local.UserProtections.AddAsync(protection);
			await local.SaveChangesAsync();

			return protection;
		}

		/// <summary>
		/// Updates the database with new or changed transformations.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <returns>An update result which may or may not have succeeded.</returns>
		public async Task<UpdateTransformationsResult> UpdateTransformationDatabase
		(
			[NotNull] GlobalInfoContext db
		)
		{
			uint addedSpecies = 0;
			uint updatedSpecies = 0;

			var bundledSpeciesResult = await this.Content.DiscoverBundledSpeciesAsync();
			if (!bundledSpeciesResult.IsSuccess)
			{
				return UpdateTransformationsResult.FromError(bundledSpeciesResult);
			}

			foreach (var species in bundledSpeciesResult.Entity.OrderBy(s => s.GetSpeciesDepth()))
			{
				if (await IsSpeciesNameUniqueAsync(db, species.Name))
				{
					// Add a new specices
					db.Species.Add(species);
					++addedSpecies;
				}
				else
				{
					// There's an existing species with this name
					var existingSpecies = (await GetSpeciesByNameAsync(db, species.Name)).Entity;

					int updatedFields = 0;

					// Update its fields with the info in the bundled species
					if (!existingSpecies.Description.Equals(species.Description, StringComparison.OrdinalIgnoreCase))
					{
						existingSpecies.Description = species.Description;
						++updatedFields;
					}

					// The extra reference equality check is due to the fact that the parent can be null
					bool shouldUpdateParent =
						(existingSpecies.Parent is null ^ species.Parent is null)
						|| (!(existingSpecies.Parent is null) && !existingSpecies.Parent.IsSameSpeciesAs(species.Parent));

					if (shouldUpdateParent)
					{
						existingSpecies.Parent = species.Parent;
						++updatedFields;
					}

					if (updatedFields > 0)
					{
						++updatedSpecies;
					}
				}

				await db.SaveChangesAsync();
			}

			uint addedTransformations = 0;
			uint updatedTransformations = 0;

			var availableSpecies = await GetAvailableSpeciesAsync(db);
			foreach (var species in availableSpecies)
			{
				var bundledTransformationsResult = await this.Content.DiscoverBundledTransformationsAsync(this, species);
				if (!bundledTransformationsResult.IsSuccess)
				{
					return UpdateTransformationsResult.FromError(bundledTransformationsResult);
				}

				foreach (var transformation in bundledTransformationsResult.Entity)
				{
					if (await IsPartAndSpeciesCombinationUniqueAsync(db, transformation.Part, transformation.Species))
					{
						// Add a new transformation
						db.Transformations.Add(transformation);
						++addedTransformations;
					}
					else
					{
						var existingTransformation = (await GetTransformationByPartAndSpeciesAsync(db, transformation.Part, transformation.Species)).Entity;

						int updatedFields = 0;

						if (!existingTransformation.Description.Equals(transformation.Description, StringComparison.OrdinalIgnoreCase))
						{
							existingTransformation.Description = transformation.Description;
							++updatedFields;
						}

						if (existingTransformation.IsNSFW != transformation.IsNSFW)
						{
							existingTransformation.IsNSFW = transformation.IsNSFW;
							++updatedFields;
						}

						if (!existingTransformation.ShiftMessage.Equals(transformation.ShiftMessage, StringComparison.OrdinalIgnoreCase))
						{
							existingTransformation.ShiftMessage = transformation.ShiftMessage;
							++updatedFields;
						}

						if (!existingTransformation.GrowMessage.Equals(transformation.GrowMessage, StringComparison.OrdinalIgnoreCase))
						{
							existingTransformation.GrowMessage = transformation.GrowMessage;
							++updatedFields;
						}

						if (!existingTransformation.SingleDescription.Equals(transformation.SingleDescription, StringComparison.OrdinalIgnoreCase))
						{
							existingTransformation.SingleDescription = transformation.SingleDescription;
							++updatedFields;
						}

						if (existingTransformation.UniformDescription != null && !existingTransformation.UniformDescription.Equals(transformation.UniformDescription, StringComparison.OrdinalIgnoreCase))
						{
							existingTransformation.UniformDescription = transformation.UniformDescription;
							++updatedFields;
						}

						if (updatedFields > 0)
						{
							++updatedTransformations;
						}
					}

					await db.SaveChangesAsync();
				}
			}

			return UpdateTransformationsResult.FromSuccess(addedSpecies, addedTransformations, updatedSpecies, updatedTransformations);
		}

		/// <summary>
		/// Gets a transformation from the database by its part and species.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="bodypart">The part.</param>
		/// <param name="species">The species.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		[Pure]
		public async Task<RetrieveEntityResult<Transformation>> GetTransformationByPartAndSpeciesAsync
		(
			[NotNull] GlobalInfoContext db,
			Bodypart bodypart,
			[NotNull] Species species
		)
		{
			var transformation = await db.Transformations
				.Include(tf => tf.DefaultBaseColour)
				.Include(tf => tf.DefaultPatternColour)
				.Include(tf => tf.Species)
				.FirstOrDefaultAsync(tf => tf.Part == bodypart && tf.Species.IsSameSpeciesAs(species));

			if (transformation is null)
			{
				return RetrieveEntityResult<Transformation>.FromError(CommandError.ObjectNotFound, "No transformation found for that combination.");
			}

			return RetrieveEntityResult<Transformation>.FromSuccess(transformation);
		}

		/// <summary>
		/// Determines whether a combination of a part and a species is a unique transformation.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="bodypart">The bodypart that is transformed.</param>
		/// <param name="species">The species to transform into.</param>
		/// <returns>true if the combination is unique; otherwise, false.</returns>
		[Pure]
		public async Task<bool> IsPartAndSpeciesCombinationUniqueAsync
		(
			[NotNull] GlobalInfoContext db,
			Bodypart bodypart,
			[NotNull] Species species
		)
		{
			return !await db.Transformations.AnyAsync(tf => tf.Part == bodypart && tf.Species.IsSameSpeciesAs(species));
		}

		/// <summary>
		/// Gets the species from the database with the given name.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="speciesName">The name of the species.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		[Pure]
		public RetrieveEntityResult<Species> GetSpeciesByName
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] string speciesName
		)
		{
			var species = db.Species.FirstOrDefault(s => s.Name.Equals(speciesName, StringComparison.OrdinalIgnoreCase));
			if (species is null)
			{
				return RetrieveEntityResult<Species>.FromError(CommandError.ObjectNotFound, "There is no species with that name in the database.");
			}

			return RetrieveEntityResult<Species>.FromSuccess(species);
		}

		/// <summary>
		/// Gets the species from the database with the given name.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="speciesName">The name of the species.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		[Pure]
		public async Task<RetrieveEntityResult<Species>> GetSpeciesByNameAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] string speciesName
		)
		{
			var species = await db.Species.FirstOrDefaultAsync(s => s.Name.Equals(speciesName, StringComparison.OrdinalIgnoreCase));
			if (species is null)
			{
				return RetrieveEntityResult<Species>.FromError(CommandError.ObjectNotFound, "There is no species with that name in the database.");
			}

			return RetrieveEntityResult<Species>.FromSuccess(species);
		}

		/// <summary>
		/// Determines whether or not the given species name is unique. This method is case-insensitive.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="speciesName">The name of the species.</param>
		/// <returns>true if the name is unique; otherwise, false.</returns>
		[Pure]
		public async Task<bool> IsSpeciesNameUniqueAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] string speciesName
		)
		{
			return !await db.Species.AnyAsync(s => s.Name.Equals(speciesName, StringComparison.OrdinalIgnoreCase));
		}
	}
}
