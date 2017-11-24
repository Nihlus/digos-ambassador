﻿//
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Extensions;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DIGOS.Ambassador.Database.Appearances;
using Humanizer;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Handles transformations of users and their characters.
	/// </summary>
	public class TransformationService
	{
		private readonly ContentService Content;

		/// <summary>
		/// Initializes a new instance of the <see cref="TransformationService"/> class.
		/// </summary>
		/// <param name="content">The content service.</param>
		public TransformationService(ContentService content)
		{
			this.Content = content;
		}

		/// <summary>
		/// Removes the given character's bodypart.
		/// </summary>
		/// <param name="db">The database where characters and transformations are stored.</param>
		/// <param name="context">The context of the command.</param>
		/// <param name="character">The character to shift.</param>
		/// <param name="bodyPart">The bodypart to remove.</param>
		/// <returns>A shifting result which may or may not have succeeded.</returns>
		public async Task<ShiftBodypartResult> RemoveCharacterBodypartAsync(GlobalInfoContext db, SocketCommandContext context, Character character, Bodypart bodyPart)
		{
			var discordUser = context.Guild.GetUser(character.Owner.DiscordID);
			var canTransformResult = await CanUserTransformUserAsync(db, context.Guild, context.User, discordUser);
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
			await db.SaveChangesAsync();

			string removeMessage = TransformationDescriptionBuilder.BuildRemoveMessage(character, transformation);
			return ShiftBodypartResult.FromSuccess(removeMessage);
		}

		/// <summary>
		/// Adds the given bodypart to the given character.
		/// </summary>
		/// <param name="db">The database where characters and transformations are stored.</param>
		/// <param name="context">The context of the command.</param>
		/// <param name="character">The character to shift.</param>
		/// <param name="bodyPart">The bodypart to add.</param>
		/// <param name="species">The species of the part to add..</param>
		/// <returns>A shifting result which may or may not have succeeded.</returns>
		public async Task<ShiftBodypartResult> AddCharacterBodypartAsync(GlobalInfoContext db, SocketCommandContext context, Character character, Bodypart bodyPart, string species)
		{
			var discordUser = context.Guild.GetUser(character.Owner.DiscordID);
			var canTransformResult = await CanUserTransformUserAsync(db, context.Guild, context.User, discordUser);
			if (!canTransformResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(canTransformResult);
			}

			if (character.HasBodypart(bodyPart))
			{
				return ShiftBodypartResult.FromError(CommandError.ObjectNotFound, "The character already has that bodypart.");
			}

			var getSpeciesResult = await GetSpeciesByNameAsync(db, species);
			if (!getSpeciesResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(getSpeciesResult);
			}

			var getTFResult = await GetTransformationByPartAndSpeciesAsync(db, bodyPart, getSpeciesResult.Entity);
			if (!getTFResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(getTFResult);
			}

			var transformation = getTFResult.Entity;

			var component = AppearanceComponent.CreateFrom(transformation);
			character.CurrentAppearance.Components.Add(component);
			await db.SaveChangesAsync();

			string growMessage = TransformationDescriptionBuilder.BuildGrowMessage(character, transformation);
			return ShiftBodypartResult.FromSuccess(growMessage);
		}

		/// <summary>
		/// Shifts the given character's bodypart to the given species.
		/// </summary>
		/// <param name="db">The database where characters and transformations are stored.</param>
		/// <param name="context">The context of the command.</param>
		/// <param name="character">The character to shift.</param>
		/// <param name="bodyPart">The bodypart to shift.</param>
		/// <param name="species">The species to shift the bodypart into.</param>
		/// <returns>A shifting result which may or may not have succeeded.</returns>
		public async Task<ShiftBodypartResult> ShiftCharacterBodypartAsync(GlobalInfoContext db, SocketCommandContext context, Character character, Bodypart bodyPart, string species)
		{
			var discordUser = context.Guild.GetUser(character.Owner.DiscordID);
			var canTransformResult = await CanUserTransformUserAsync(db, context.Guild, context.User, discordUser);
			if (!canTransformResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(canTransformResult);
			}

			if (!character.HasBodypart(bodyPart))
			{
				return ShiftBodypartResult.FromError(CommandError.ObjectNotFound, "The character doesn't have that bodypart.");
			}

			var getSpeciesResult = await GetSpeciesByNameAsync(db, species);
			if (!getSpeciesResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(getSpeciesResult);
			}

			var getTFResult = await GetTransformationByPartAndSpeciesAsync(db, bodyPart, getSpeciesResult.Entity);
			if (!getTFResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(getTFResult);
			}

			var currentComponent = character.CurrentAppearance.Components.First(c => c.Bodypart == bodyPart);

			var transformation = getTFResult.Entity;
			currentComponent.Transformation = transformation;
			await db.SaveChangesAsync();

			string shiftMessage = TransformationDescriptionBuilder.BuildShiftMessage(character, transformation);
			return ShiftBodypartResult.FromSuccess(shiftMessage);
		}

		/// <summary>
		/// Determines whether or not a user is allowed to transform another user.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="discordServer"></param>
		/// <param name="invokingUser"></param>
		/// <param name="targetUser"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		[Pure]
		public async Task<DetermineConditionResult> CanUserTransformUserAsync(GlobalInfoContext db, IGuild discordServer, IUser invokingUser, IUser targetUser)
		{
			var localProtection = await GetOrCreateServerUserProtectionAsync(db, targetUser, discordServer);
			if (!localProtection.HasOptedIn)
			{
				return DetermineConditionResult.FromError("The target hasn't opted into transformations.");
			}

			var globalProtection = await GetOrCreateGlobalUserProtectionAsync(db, targetUser);
			switch (localProtection.Type)
			{
				case ProtectionType.Blacklist:
				{
					return globalProtection.Blacklist.All(u => u.DiscordID != invokingUser.Id)
						? DetermineConditionResult.FromSuccess()
						: DetermineConditionResult.FromError("You're on that user's blacklist.");
				}
				case ProtectionType.Whitelist:
				{
					return globalProtection.Whitelist.Any(u => u.DiscordID == invokingUser.Id)
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
		public async Task<Embed> GenerateCharacterDescriptionAsync(Character character)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the available species in transformations.
		/// </summary>
		/// <param name="db">The database containing the transformations.</param>
		/// <returns>A list of the available species.</returns>
		[Pure]
		public async Task<IReadOnlyList<Species>> GetAvailableSpeciesAsync(GlobalInfoContext db)
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
		public async Task<IReadOnlyList<Transformation>> GetAvailableTransformations(GlobalInfoContext db, Bodypart bodyPart)
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
		public async Task<ModifyEntityResult> ResetCharacterFormAsync(GlobalInfoContext db, Character character)
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
		public async Task<ModifyEntityResult> SetCurrentAppearanceAsDefaultForCharacterAsync(GlobalInfoContext db, Character character)
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
		public async Task<ModifyEntityResult> SetDefaultProtectionTypeAsync(GlobalInfoContext db, IUser discordUser, ProtectionType protectionType)
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
		/// <param name="db">The database.</param>
		/// <param name="discordUser">The user to set the protection for.</param>
		/// <param name="discordServer">The server to set the protection on.</param>
		/// <param name="protectionType">The protection type to set.</param>
		/// <returns>An entity modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetServerProtectionTypeAsync(GlobalInfoContext db, IUser discordUser, IGuild discordServer, ProtectionType protectionType)
		{
			var protection = await GetOrCreateServerUserProtectionAsync(db, discordUser, discordServer);
			if (protection.Type == protectionType)
			{
				return ModifyEntityResult.FromError(CommandError.Unsuccessful, $"{protectionType.Humanize()} is already your current setting.");
			}

			protection.Type = protectionType;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Whitelists the given user, allowing them to transform the <paramref name="discordUser"/>.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordUser">The user to modify.</param>
		/// <param name="whitelistedUser">The user to add to the whitelist.</param>
		/// <returns>An entity modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> WhitelistUserAsync(GlobalInfoContext db, IUser discordUser, IUser whitelistedUser)
		{
			var protection = await GetOrCreateGlobalUserProtectionAsync(db, discordUser);
			if (protection.Whitelist.Any(u => u.DiscordID == whitelistedUser.Id))
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
		public async Task<ModifyEntityResult> BlacklistUserAsync(GlobalInfoContext db, IUser discordUser, IUser blacklistedUser)
		{
			var protection = await GetOrCreateGlobalUserProtectionAsync(db, discordUser);
			if (protection.Blacklist.Any(u => u.DiscordID == blacklistedUser.Id))
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
		public async Task<GlobalUserProtection> GetOrCreateGlobalUserProtectionAsync(GlobalInfoContext db, IUser discordUser)
		{
			var protection = await db.GlobalUserProtections
			.Include(p => p.User)
			.Include(p => p.Whitelist)
			.Include(p => p.Blacklist)
			.FirstOrDefaultAsync(p => p.User.DiscordID == discordUser.Id);

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
		/// <param name="db">The database.</param>
		/// <param name="discordUser">The user.</param>
		/// <param name="guild">The server.</param>
		/// <returns>Server-specific protection data for the given user.</returns>
		public async Task<ServerUserProtection> GetOrCreateServerUserProtectionAsync(GlobalInfoContext db, IUser discordUser, IGuild guild)
		{
			var protection = await db.ServerUserProtections
			.Include(p => p.Server)
			.Include(p => p.User)
			.FirstOrDefaultAsync
			(
				p =>
					p.User.DiscordID == discordUser.Id && p.Server.DiscordID == guild.Id
			);

			if (!(protection is null))
			{
				return protection;
			}

			var server = await db.GetOrRegisterServerAsync(guild);
			var globalProtection = await GetOrCreateGlobalUserProtectionAsync(db, discordUser);
			protection = ServerUserProtection.CreateDefault(globalProtection, server);

			await db.ServerUserProtections.AddAsync(protection);
			await db.SaveChangesAsync();

			return protection;
		}

		/// <summary>
		/// Updates the database with new or changed transformations.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <returns>An update result which may or may not have succeeded.</returns>
		public async Task<UpdateTransformationsResult> UpdateTransformationDatabase(GlobalInfoContext db)
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

						if (!existingTransformation.UniformDescription.Equals(transformation.UniformDescription, StringComparison.OrdinalIgnoreCase))
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
		public async Task<RetrieveEntityResult<Transformation>> GetTransformationByPartAndSpeciesAsync(GlobalInfoContext db, Bodypart bodypart, Species species)
		{
			var transformation = await db.Transformations.FirstOrDefaultAsync(tf => tf.Part == bodypart && tf.Species.IsSameSpeciesAs(species));
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
		public async Task<bool> IsPartAndSpeciesCombinationUniqueAsync(GlobalInfoContext db, Bodypart bodypart, Species species)
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
		public RetrieveEntityResult<Species> GetSpeciesByName(GlobalInfoContext db, string speciesName)
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
		public async Task<RetrieveEntityResult<Species>> GetSpeciesByNameAsync(GlobalInfoContext db, string speciesName)
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
		public async Task<bool> IsSpeciesNameUniqueAsync(GlobalInfoContext db, string speciesName)
		{
			return !await db.Species.AnyAsync(s => s.Name.Equals(speciesName, StringComparison.OrdinalIgnoreCase));
		}
	}
}
