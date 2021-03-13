//
//  PermissionState.cs
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

using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Permissions.Model
{
    /// <summary>
    /// Defines the state a recorded permission can be in.
    /// </summary>
    public enum PermissionState
    {
        /// <summary>
        /// The permission has been explicitly granted.
        /// </summary>
        Granted,

        /// <summary>
        /// The permission has been explicitly denied.
        /// </summary>
        Denied,

        /// <summary>
        /// The permission inherits superseding values, or uses the permission's default.
        /// </summary>
        InheritOrDefault
    }
}
