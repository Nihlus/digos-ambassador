//
//  RolePermission.cs
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
using System.ComponentModel.DataAnnotations.Schema;
using DIGOS.Ambassador.Core.Database.Entities;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Permissions.Model
{
    /// <summary>
    /// Represents a record of a permission associated with a role.
    /// </summary>
    [Table("RolePermissions", Schema = "PermissionModule")]
    public class RolePermission : EFEntity
    {
        /// <summary>
        /// Gets the role's Discord ID.
        /// </summary>
        public long RoleID { get; [UsedImplicitly] private set; }

        /// <summary>
        /// Gets the permission's unique identifier.
        /// </summary>
        public Guid Permission { get; [UsedImplicitly] private set; }

        /// <summary>
        /// Gets the allowed targets for the permission.
        /// </summary>
        public PermissionTarget Target { get; [UsedImplicitly] private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the permission has been granted.
        /// </summary>
        public bool IsGranted { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RolePermission"/> class.
        /// </summary>
        /// <param name="roleID">The ID of the role that the permission applies to.</param>
        /// <param name="permission">The unique identifier of the permission.</param>
        /// <param name="target">The allowed targets for the permission.</param>
        public RolePermission(long roleID, Guid permission, PermissionTarget target)
        {
            this.RoleID = roleID;
            this.Permission = permission;
            this.Target = target;
        }
    }
}
