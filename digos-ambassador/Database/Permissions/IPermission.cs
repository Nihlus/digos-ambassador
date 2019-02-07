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
using DIGOS.Ambassador.Permissions;

#pragma warning disable SA1402

namespace DIGOS.Ambassador.Database.Permissions
{
    /// <summary>
    /// Member interface for permissions.
    /// </summary>
    public interface IPermission
    {
        /// <summary>
        /// Gets or sets the discord ID of the user that this permission has been granted to.
        /// </summary>
        long UserDiscordID { get; set; }

        /// <summary>
        /// Gets or sets the granted permission.
        /// </summary>
        Permission Permission { get; set; }

        /// <summary>
        /// Gets or sets the allowed targets.
        /// </summary>
        PermissionTarget Target { get; set; }
    }

    /// <summary>
    /// Equation interface for permissions.
    /// </summary>
    /// <typeparam name="T">The type of the permission class.</typeparam>
    public interface IPermission<T> : IEquatable<T>, IPermission where T : IPermission<T>
    {
    }
}
