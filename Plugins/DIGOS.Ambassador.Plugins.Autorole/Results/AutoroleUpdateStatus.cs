//
//  AutoroleUpdateStatus.cs
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

namespace DIGOS.Ambassador.Plugins.Autorole.Results;

/// <summary>
/// Enumerates various types of status returns from an autorole update.
/// </summary>
public enum AutoroleUpdateStatus
{
    /// <summary>
    /// The autorole is disabled.
    /// </summary>
    Disabled,

    /// <summary>
    /// The autorole is unconditional (i.e, it has no configured conditions). This is not allowed for safety
    /// reasons.
    /// </summary>
    Unconditional,

    /// <summary>
    /// The user did not qualify for the autorole.
    /// </summary>
    Unqualified,

    /// <summary>
    /// The user qualified for the autorole, but already had it.
    /// </summary>
    Unchanged,

    /// <summary>
    /// The user qualified for the autorole, and it was assigned to them.
    /// </summary>
    Applied,

    /// <summary>
    /// The user had the autorole, but no longer qualified, and it was removed.
    /// </summary>
    Removed,

    /// <summary>
    /// The user qualified for the autorole, but it requires manual affirmation before it can be assigned.
    /// </summary>
    RequiresAffirmation
}
