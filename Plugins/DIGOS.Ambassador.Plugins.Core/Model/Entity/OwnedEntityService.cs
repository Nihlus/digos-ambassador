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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using Discord.Commands;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Plugins.Core.Model.Entity
{
    /// <summary>
    /// Acts as an interface for accessing and modifying named entities owned by users.
    /// </summary>
    public class OwnedEntityService
    {
        /// <summary>
        /// Holds reserved characters which may not appear in names.
        /// </summary>
        private readonly char[] _reservedNameCharacters = { ':' };

        /// <summary>
        /// Holds reserved names which entities may not have.
        /// </summary>
        private readonly string[] _reservedNames = { "current" };

        /// <summary>
        /// Determines whether or not the given entity name is unique for a given set of user entities.
        /// </summary>
        /// <param name="userEntities">The entities to check.</param>
        /// <param name="entityName">The entity name to check.</param>
        /// <returns>true if the name is unique; otherwise, false.</returns>
        [Pure]
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

            return !await userEntities.AnyAsync(ch => string.Equals(ch.Name, entityName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Transfers ownership of the given entity to the specified user.
        /// </summary>
        /// <param name="db">The database where the entities are stored.</param>
        /// <param name="newOwner">The new owner.</param>
        /// <param name="newOwnerEntities">The entities that the user already owns.</param>
        /// <param name="entity">The entity to transfer.</param>
        /// <typeparam name="TContext">The database context to use.</typeparam>
        /// <returns>An entity modification result, which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> TransferEntityOwnershipAsync<TContext>
        (
            [NotNull] TContext db,
            [NotNull] User newOwner,
            [NotNull] IQueryable<IOwnedNamedEntity> newOwnerEntities,
            [NotNull] IOwnedNamedEntity entity
        )
            where TContext : DbContext
        {
            if (entity.IsOwner(newOwner))
            {
                return ModifyEntityResult.FromError
                (
                    $"That person already owns the {entity.EntityTypeDisplayName}."
                        .Humanize().Transform(To.SentenceCase)
                );
            }

            if (newOwnerEntities.Any(e => string.Equals(e.Name, entity.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return ModifyEntityResult.FromError
                (
                    $"That user already owns a {entity.EntityTypeDisplayName} named {entity.Name}. Please rename it first."
                        .Humanize().Transform(To.SentenceCase)
                );
            }

            entity.Owner = newOwner;

            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Verifies that the given entity name is not contained in the given command names, nor is otherwise invalid.
        /// </summary>
        /// <param name="commandNames">The command names.</param>
        /// <param name="entityName">The name of the entity.</param>
        /// <returns>true if the name is valid; otherwise, false.</returns>
        [Pure]
        [ContractAnnotation("entityName:null => false")]
        public DetermineConditionResult IsEntityNameValid
        (
            [NotNull] IEnumerable<string> commandNames,
            [CanBeNull] string entityName
        )
        {
            if (entityName.IsNullOrWhitespace())
            {
                return DetermineConditionResult.FromError("Names cannot be empty.");
            }

            if (entityName.Any(c => _reservedNameCharacters.Contains(c)))
            {
                return DetermineConditionResult.FromError
                (
                    $"Names may not contain any of the following characters: {_reservedNameCharacters.Humanize()}"
                );
            }

            if (_reservedNames.Any(n => string.Equals(n, entityName, StringComparison.OrdinalIgnoreCase)))
            {
                return DetermineConditionResult.FromError
                (
                    "That is a reserved name."
                );
            }

            if (commandNames.Any(entityName.Contains))
            {
                return DetermineConditionResult.FromError("Names may not be the same as a command.");
            }

            return DetermineConditionResult.FromSuccess();
        }
    }
}
