//
//  OwnedEntityService.cs
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
using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Extensions;

using Discord;
using Discord.Commands;

using Humanizer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Acts as an interface for accessing and modifying named entities owned by users.
	/// </summary>
	public class OwnedEntityService
	{
		/// <summary>
		/// Holds reserved characters which may not appear in names.
		/// </summary>
		private char[] ReservedNameCharacters = { ':' };

		/// <summary>
		/// Holds reserved names which entities may not have.
		/// </summary>
		private string[] ReservedNames = { "current" };

		/// <summary>
		/// Determines whether or not the given entity name is unique for a given set of user entities.
		/// </summary>
		/// <param name="userEntities">The entities to check.</param>
		/// <param name="entityName">The entity name to check.</param>
		/// <returns>true if the name is unique; otherwise, false.</returns>
		public async Task<bool> IsEntityNameUniqueForUserAsync
		(
			[NotNull] IQueryable<IOwnedNamedEntity> userEntities,
			[NotNull] string entityName
		)
		{
			if (await userEntities.CountAsync() <= 0)
			{
				return true;
			}

			return !await userEntities.AnyAsync(ch => ch.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Transfers ownership of the given entity to the specified user.
		/// </summary>
		/// <param name="db">The database where the entities are stored.</param>
		/// <param name="newOwner">The new owner.</param>
		/// <param name="newOwnerEntities">The entities that the user already owns.</param>
		/// <param name="entity">The entity to transfer.</param>
		/// <returns>An entity modification result, which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> TransferEntityOwnershipAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser newOwner,
			[NotNull] IQueryable<IOwnedNamedEntity> newOwnerEntities,
			[NotNull] IOwnedNamedEntity entity
		)
		{
			if (entity.IsOwner(newOwner))
			{
				return ModifyEntityResult.FromError
				(
					CommandError.Unsuccessful,
					$"That person already owns the {entity.EntityTypeDisplayName}."
						.Humanize().Transform(To.SentenceCase)
				);
			}

			if (newOwnerEntities.Any(e => e.Name.Equals(entity.Name, StringComparison.OrdinalIgnoreCase)))
			{
				return ModifyEntityResult.FromError
				(
					CommandError.MultipleMatches,
					$"That user already owns a {entity.EntityTypeDisplayName} named {entity.Name}. Please rename it first."
						.Humanize().Transform(To.SentenceCase)
				);
			}

			var newUser = await db.GetOrRegisterUserAsync(newOwner);
			entity.Owner = newUser;

			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Builds a list of the command names and aliases in a given command module, and checks that the given
		/// entity name is not one of them.
		/// </summary>
		/// <param name="commandModule">The command module to scan.</param>
		/// <param name="entityName">The name of the entity.</param>
		/// <returns>true if the name is valid; otherwise, false.</returns>
		[ContractAnnotation("entityName:null => false")]
		public CheckConditionResult IsEntityNameValid
		(
			[NotNull] ModuleInfo commandModule,
			[CanBeNull] string entityName
		)
		{
			if (entityName.IsNullOrWhitespace())
			{
				return CheckConditionResult.FromError(CommandError.ObjectNotFound, "Names cannot be empty.");
			}

			if (entityName.Any(c => this.ReservedNameCharacters.Contains(c)))
			{
				return CheckConditionResult.FromError
				(
					CommandError.UnmetPrecondition,
					$"Names may not contain any of the following characters: {this.ReservedNameCharacters.Humanize()}"
				);
			}

			if (this.ReservedNames.Any(n => n.Equals(entityName, StringComparison.OrdinalIgnoreCase)))
			{
				return CheckConditionResult.FromError
				(
					CommandError.UnmetPrecondition,
					"That is a reserved name."
				);
			}

			var submodules = commandModule.Submodules;

			var commandNames = commandModule.Commands.SelectMany(c => c.Aliases);
			commandNames = commandNames.Union(commandModule.Commands.Select(c => c.Name));

			var submoduleCommandNames = submodules.SelectMany(s => s.Commands.SelectMany(c => c.Aliases));
			submoduleCommandNames = submoduleCommandNames.Union(submodules.SelectMany(s => s.Commands.Select(c => c.Name)));

			commandNames = commandNames.Union(submoduleCommandNames);

			if (commandNames.Contains(entityName))
			{
				return CheckConditionResult.FromError(CommandError.UnmetPrecondition, "Names may not be the same as a command.");
			}

			return CheckConditionResult.FromSuccess();
		}
	}
}
