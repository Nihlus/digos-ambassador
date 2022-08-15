//
//  UserPermission.cs
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
using System.ComponentModel.DataAnnotations.Schema;
using DIGOS.Ambassador.Core.Database.Entities;
using JetBrains.Annotations;
using Remora.Rest.Core;

namespace DIGOS.Ambassador.Plugins.Permissions.Model;

/// <summary>
/// Represents a record of a permission associated with a user.
/// </summary>
[PublicAPI]
[Table("UserPermissions", Schema = "PermissionModule")]
public class UserPermission : EFEntity
{
    /// <summary>
    /// Gets the Discord ID of the server that the permission is associated with.
    /// </summary>
    public Snowflake ServerID { get; private set; }

    /// <summary>
    /// Gets the user's Discord ID.
    /// </summary>
    public Snowflake UserID { get; private set; }

    /// <summary>
    /// Gets the permission's unique identifier.
    /// </summary>
    public Guid Permission { get; private set; }

    /// <summary>
    /// Gets the allowed targets for the permission.
    /// </summary>
    public PermissionTarget Target { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the permission has been granted.
    /// </summary>
    public bool IsGranted { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserPermission"/> class.
    /// </summary>
    /// <param name="serverID">The ID of the server that the permission is associated with.</param>
    /// <param name="userID">The ID of the user that the permission applies to.</param>
    /// <param name="permission">The unique identifier of the permission.</param>
    /// <param name="target">The allowed targets for the permission.</param>
    public UserPermission(Snowflake serverID, Snowflake userID, Guid permission, PermissionTarget target)
    {
        this.UserID = userID;
        this.Permission = permission;
        this.Target = target;
        this.ServerID = serverID;
    }
}
