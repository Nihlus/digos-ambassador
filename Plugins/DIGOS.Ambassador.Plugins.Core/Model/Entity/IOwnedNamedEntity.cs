//
//  IOwnedNamedEntity.cs
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

using DIGOS.Ambassador.Plugins.Core.Model.Users;
using JetBrains.Annotations;
using Remora.Rest.Core;

namespace DIGOS.Ambassador.Plugins.Core.Model.Entity;

/// <summary>
/// Represents an entity that is owned by a user, and has a unique name within the context of that user. The name
/// is case-insensitive.
/// </summary>
public interface IOwnedNamedEntity : IOwnedEntity
{
    /// <summary>
    /// Gets the user-unique name of the entity.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the display name of the type that the entity is (e.g, character, roleplay, etc).
    /// </summary>
    string EntityTypeDisplayName { get; }

    /// <summary>
    /// Determines whether or not the given user is the owner of the entity.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>true if the user is the owner; otherwise, false.</returns>
    [Pure]
    bool IsOwner(User user);

    /// <summary>
    /// Determines whether or not the given user ID is the owner of the entity.
    /// </summary>
    /// <param name="userID">The ID of the user.</param>
    /// <returns>true if the user is the owner; otherwise, false.</returns>
    [Pure]
    bool IsOwner(Snowflake userID);
}
