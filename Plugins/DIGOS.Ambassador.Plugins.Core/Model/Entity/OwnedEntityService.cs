//
//  OwnedEntityService.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using Humanizer;
using JetBrains.Annotations;
using Remora.Rest.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Core.Model.Entity;

/// <summary>
/// Acts as an interface for accessing and modifying named entities owned by users.
/// </summary>
public sealed class OwnedEntityService
{
    /// <summary>
    /// Holds reserved characters which may not appear in names.
    /// </summary>
    private readonly char[] _reservedNameCharacters = { ':' };

    /// <summary>
    /// Holds reserved names which entities may not have.
    /// </summary>
    private readonly string[] _reservedNames = { "current" };

    private readonly UserService _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="OwnedEntityService"/> class.
    /// </summary>
    /// <param name="users">The user service.</param>
    public OwnedEntityService(UserService users)
    {
        _users = users;
    }

    /// <summary>
    /// Determines whether or not the given entity name is unique for a given set of user entities.
    /// </summary>
    /// <param name="userEntities">The entities to check.</param>
    /// <param name="entityName">The entity name to check.</param>
    /// <returns>true if the name is unique; otherwise, false.</returns>
    [Pure]
    public static bool IsEntityNameUniqueForUser
    (
        IReadOnlyCollection<IOwnedNamedEntity> userEntities,
        string entityName
    )
    {
        if (userEntities.Count <= 0)
        {
            return true;
        }

        return !userEntities.Any(ch => string.Equals(ch.Name.ToLower(), entityName.ToLower()));
    }

    /// <summary>
    /// Transfers ownership of the given entity to the specified user.
    /// </summary>
    /// <param name="newOwnerID">The ID of the new owner.</param>
    /// <param name="newOwnerEntities">The entities that the user already owns.</param>
    /// <param name="entity">The entity to transfer.</param>
    /// <returns>An entity modification result, which may or may not have succeeded.</returns>
    public async Task<Result> TransferEntityOwnershipAsync
    (
        Snowflake newOwnerID,
        IEnumerable<IOwnedNamedEntity> newOwnerEntities,
        IOwnedNamedEntity entity
    )
    {
        var getNewOwner = await _users.GetOrRegisterUserAsync(newOwnerID);
        if (!getNewOwner.IsSuccess)
        {
            return Result.FromError(getNewOwner);
        }

        var newOwner = getNewOwner.Entity;

        if (entity.IsOwner(newOwner))
        {
            return new UserError
            (
                $"That person already owns the {entity.EntityTypeDisplayName}."
                    .Humanize().Transform(To.SentenceCase)
            );
        }

        if (newOwnerEntities.Any(e => string.Equals(e.Name.ToLower(), entity.Name.ToLower())))
        {
            return new UserError
            (
                $"That user already owns a {entity.EntityTypeDisplayName} named {entity.Name}. Please rename it first."
                    .Humanize().Transform(To.SentenceCase)
            );
        }

        entity.Owner = newOwner;

        return Result.FromSuccess();
    }

    /// <summary>
    /// Verifies that the given entity name is not contained in the given command names, nor is otherwise invalid.
    /// </summary>
    /// <param name="commandNames">The command names.</param>
    /// <param name="entityName">The name of the entity.</param>
    /// <returns>true if the name is valid; otherwise, false.</returns>
    [Pure]
    public Result IsEntityNameValid
    (
        IEnumerable<string> commandNames,
        string? entityName
    )
    {
        if (entityName.IsNullOrWhitespace())
        {
            return new UserError("Names cannot be empty.");
        }

        if (entityName.Any(c => _reservedNameCharacters.Contains(c)))
        {
            return new UserError
            (
                $"Names may not contain any of the following characters: {_reservedNameCharacters.Humanize()}"
            );
        }

        if (_reservedNames.Any(n => string.Equals(n, entityName, StringComparison.OrdinalIgnoreCase)))
        {
            return new UserError
            (
                "That is a reserved name."
            );
        }

        return commandNames.Any(entityName.Contains)
            ? new UserError("Names may not be the same as a command.")
            : Result.FromSuccess();
    }
}
