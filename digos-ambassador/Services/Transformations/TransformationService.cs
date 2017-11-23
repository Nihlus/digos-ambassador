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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Extensions;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

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
			throw new NotImplementedException();
		}

		/// <summary>
		/// Generate a complete textual description of the given character, and format it into an embed.
		/// </summary>
		/// <param name="character">The character to generate the description for.</param>
		/// <returns>An embed with a formatted description.</returns>
		public async Task<Embed> GenerateCharacterDescriptionAsync(Character character)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the available species in transformations.
		/// </summary>
		/// <param name="db">The database containing the transformations.</param>
		/// <returns>A list of the available species.</returns>
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

			character.TransformedAppearance = character.DefaultAppearance;
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
			if (character.TransformedAppearance is null)
			{
				return ModifyEntityResult.FromError(CommandError.ObjectNotFound, "The character doesn't have an altered appearance.");
			}

			character.DefaultAppearance = character.TransformedAppearance;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Sets the protection type that the user has for transformations.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="user">The user to set the protection for.</param>
		/// <param name="protectionType">The protection type to set.</param>
		/// <returns>An entity modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetUserProtectionTypeAsync(GlobalInfoContext db, IUser user, ProtectionType protectionType)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Whitelists the given user, allowing them to transform the <paramref name="user"/>.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="user">The user to modify.</param>
		/// <param name="whitelistedUser">The user to add to the whitelist.</param>
		/// <returns>An entity modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> WhitelistUserAsync(GlobalInfoContext db, IUser user, IUser whitelistedUser)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Blacklists the given user, preventing them from transforming the <paramref name="user"/>.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="user">The user to modify.</param>
		/// <param name="blacklistedUser">The user to add to the blacklist.</param>
		/// <returns>An entity modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> BlacklistUserAsync(GlobalInfoContext db, IUser user, IUser blacklistedUser)
		{
			throw new NotImplementedException();
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
					updatedFields += existingSpecies.Description
					.ExecuteBy
					(
						() => existingSpecies.Description = species.Description,
						val => !val.Equals(species.Description, StringComparison.OrdinalIgnoreCase)
					);

					// The extra reference equality check is due to the fact that val can be null
					updatedFields += existingSpecies.Parent
					.ExecuteBy
					(
						() => existingSpecies.Parent = species.Parent,
						val => val != species.Parent && !val.IsSameSpeciesAs(species.Parent)
					);

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

						updatedFields += existingTransformation.Description
						.ExecuteBy
						(
							() => existingTransformation.Description = transformation.Description,
							val => !val.Equals(transformation.Description, StringComparison.OrdinalIgnoreCase)
						);

						updatedFields += existingTransformation.IsNSFW
						.ExecuteBy
						(
							() => existingTransformation.IsNSFW = transformation.IsNSFW,
							val => val != transformation.IsNSFW
						);

						updatedFields += existingTransformation.ShiftMessage
						.ExecuteBy
						(
							() => existingTransformation.ShiftMessage = transformation.ShiftMessage,
							val => !val.Equals(transformation.ShiftMessage, StringComparison.OrdinalIgnoreCase)
						);

						updatedFields += existingTransformation.GrowMessage
						.ExecuteBy
						(
							() => existingTransformation.GrowMessage = transformation.GrowMessage,
							val => !val.Equals(transformation.GrowMessage, StringComparison.OrdinalIgnoreCase)
						);

						updatedFields += existingTransformation.SingleDescription
						.ExecuteBy
						(
							() => existingTransformation.SingleDescription = transformation.SingleDescription,
							val => !val.Equals(transformation.SingleDescription, StringComparison.OrdinalIgnoreCase)
						);

						updatedFields += existingTransformation.UniformDescription
						.ExecuteBy
						(
							() => existingTransformation.UniformDescription = transformation.UniformDescription,
							val => !val.Equals(transformation.UniformDescription, StringComparison.OrdinalIgnoreCase)
						);

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
		public async Task<bool> IsSpeciesNameUniqueAsync(GlobalInfoContext db, string speciesName)
		{
			return !await db.Species.AnyAsync(s => s.Name.Equals(speciesName, StringComparison.OrdinalIgnoreCase));
		}
	}
}
