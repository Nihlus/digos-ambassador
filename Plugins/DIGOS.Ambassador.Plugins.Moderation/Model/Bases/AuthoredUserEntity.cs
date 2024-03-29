//
//  AuthoredUserEntity.cs
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
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Moderation.Model.Bases;

/// <summary>
/// Represents an entity authored by a user at a specific time.
/// </summary>
public abstract class AuthoredUserEntity : EFEntity
{
    /// <summary>
    /// Gets the server that the entity was authored on.
    /// </summary>
    public virtual Server Server { get; private set; } = null!;

    /// <summary>
    /// Gets the user that the entity is associated with.
    /// </summary>
    public virtual User User { get; private set; } = null!;

    /// <summary>
    /// Gets the user that created the entity.
    /// </summary>
    public virtual User Author { get; private set; } = null!;

    /// <summary>
    /// Gets the time at which the entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthoredUserEntity"/> class.
    /// </summary>
    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    protected AuthoredUserEntity()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthoredUserEntity"/> class.
    /// </summary>
    /// <param name="server">The server that the entity was authored on.</param>
    /// <param name="user">The user that the entity is associated with.</param>
    /// <param name="author">The user that created the entity.</param>
    protected AuthoredUserEntity(Server server, User user, User author)
    {
        this.Server = server;
        this.User = user;
        this.Author = author;

        this.CreatedAt = DateTimeOffset.UtcNow;
    }
}
