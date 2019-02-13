//
//  RoleAccess.cs
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

using Discord;

namespace DIGOS.Ambassador.Database.Characters
{
    /// <summary>
    /// Represents access conditions for character roles (who can apply them and when).
    /// </summary>
    public enum RoleAccess
    {
        /// <summary>
        /// The role can be applied by anyone to a character.
        /// </summary>
        Open,

        /// <summary>
        /// The role can only be applied by people with the <see cref="GuildPermission.ManageRoles"/> permission.
        /// </summary>
        Restricted
    }
}
