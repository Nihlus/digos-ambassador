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

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Transformations;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Handles transformations of users and their characters.
	/// </summary>
	public class TransformationService
	{
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
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Generate a complete textual description of the given character, and format it into an embed.
		/// </summary>
		/// <param name="character">The character to generate the description for.</param>
		/// <returns>An embed with a formatted description.</returns>
		public async Task<Embed> GenerateCharacterDescriptionAsync(Character character)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Gets the available species in transformations.
		/// </summary>
		/// <param name="db">The database containing the transformations.</param>
		/// <returns>A list of the available species.</returns>
		public async Task<IReadOnlyList<Species>> GetAvailableSpeciesAsync(GlobalInfoContext db)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Gets the available transformations for the given bodypart.
		/// </summary>
		/// <param name="db">The database containing the transformations.</param>
		/// <param name="bodyPart">The bodypart to get the transformations for.</param>
		/// <returns>A list of the available transformations..</returns>
		public async Task<IReadOnlyList<Transformation>> GetAvailableTransformations(GlobalInfoContext db, Bodypart bodyPart)
		{
			throw new System.NotImplementedException();
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
			throw new System.NotImplementedException();
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
			throw new System.NotImplementedException();
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
			throw new System.NotImplementedException();
		}
	}
}
