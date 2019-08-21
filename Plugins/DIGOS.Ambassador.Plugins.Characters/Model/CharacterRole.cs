//
//  CharacterRole.cs
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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;

namespace DIGOS.Ambassador.Plugins.Characters.Model
{
    /// <summary>
    /// Represents a role associated with a character, similar to a nickname.
    /// </summary>
    [Table("CharacterRoles", Schema = "CharacterModule")]
    public class CharacterRole : IEFEntity
    {
        /// <inheritdoc />
        public long ID { get; set; }

        /// <summary>
        /// Gets or sets the server that the role is on.
        /// </summary>
        [Required]
        public virtual Server Server { get; set; }

        /// <summary>
        /// Gets or sets the role ID, taken from Discord.
        /// </summary>
        public long DiscordID { get; set; }

        /// <summary>
        /// Gets or sets the access conditions of the role.
        /// </summary>
        public RoleAccess Access { get; set; }
    }
}
