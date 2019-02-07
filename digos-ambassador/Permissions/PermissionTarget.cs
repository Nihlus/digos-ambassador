//
//  PermissionTarget.cs
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

namespace DIGOS.Ambassador.Permissions
{
    /// <summary>
    /// Represents target permissions for commands.
    /// </summary>
    [Flags]
    public enum PermissionTarget
    {
        /// <summary>
        /// Does not allow execution of the command.
        /// </summary>
        None = 0,

        /// <summary>
        /// Allows execution of a command exclusively on the invoking user.
        /// </summary>
        Self = 1,

        /// <summary>
        /// Allows execution of a command on other users.
        /// </summary>
        Other = 2
    }
}
