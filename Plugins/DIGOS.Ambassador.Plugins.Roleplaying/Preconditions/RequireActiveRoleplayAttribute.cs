//
//  RequireActiveRoleplayAttribute.cs
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

using Remora.Commands.Conditions;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Preconditions
{
    /// <summary>
    /// Restricts the usage of a command to a channel where a roleplay is currently active. Optionally, it may also
    /// restrict the usage to the owner of that roleplay.
    /// </summary>
    public class RequireActiveRoleplayAttribute : ConditionAttribute
    {
        /// <summary>
        /// Gets a value indicating whether the invoker is also required to be the owner of the roleplay.
        /// </summary>
        public bool RequireOwner { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireActiveRoleplayAttribute"/> class.
        /// </summary>
        /// <param name="requireOwner">Whether or not it is required that the current roleplay is owned by the invoker.</param>
        public RequireActiveRoleplayAttribute(bool requireOwner = false)
        {
            this.RequireOwner = requireOwner;
        }
    }
}
