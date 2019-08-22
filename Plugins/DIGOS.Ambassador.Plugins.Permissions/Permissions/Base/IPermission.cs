//
//  IPermission.cs
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
using DIGOS.Ambassador.Plugins.Permissions.Model;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Permissions
{
    /// <summary>
    /// Defines the public API of a permission.
    /// </summary>
    [PublicAPI]
    public interface IPermission
    {
        /// <summary>
        /// Gets the unique identifier for this permission. This value is associated with the permission itself, and
        /// must remain constant between all instances of the permission.
        /// </summary>
        Guid UniqueIdentifier { get; }

        /// <summary>
        /// Gets the permission's friendly name, such as "CreateUser", or "EditDescription".
        /// </summary>
        [NotNull]
        string FriendlyName { get; }

        /// <summary>
        /// Gets the human-readable description of what the permission allows.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets a value indicating whether the permission is granted by default to commands targeting the invoking
        /// user.
        /// </summary>
        bool IsGrantedByDefaultToSelf { get; }

        /// <summary>
        /// Gets a value indicating whether the permission is granted by default to commands targeting other users.
        /// </summary>
        bool IsGrantedByDefaultToOthers { get; }

        /// <summary>
        /// Determines whether or not the permission is granted by default to the given target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>true if the permission is granted by default to the target; otherwise, false.</returns>
        bool IsGrantedByDefaultTo(PermissionTarget target);
    }
}
