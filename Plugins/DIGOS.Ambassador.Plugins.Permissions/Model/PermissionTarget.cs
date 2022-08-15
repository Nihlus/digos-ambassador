//
//  PermissionTarget.cs
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
using System.ComponentModel;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Permissions.Model;

/// <summary>
/// Represents target permissions for commands.
/// </summary>
[PublicAPI, Flags]
public enum PermissionTarget
{
    /// <summary>
    /// Allows execution of a command on the invoking user.
    /// </summary>
    [Description("Yourself")]
    Self = 1 << 1,

    /// <summary>
    /// Allows execution of a command on other users.
    /// </summary>
    [Description("Others")]
    Other = 1 << 2,

    /// <summary>
    /// Allows execution of a command on anyone.
    /// </summary>
    All = Self | Other
}
