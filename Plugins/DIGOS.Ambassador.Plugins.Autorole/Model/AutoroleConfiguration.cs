//
//  AutoroleConfiguration.cs
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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.Bases;
using Discord;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Autorole.Model
{
    /// <summary>
    /// Represents an automatic role.
    /// </summary>
    [PublicAPI]
    [Table("Autoroles", Schema = "AutoroleModule")]
    public class AutoroleConfiguration : EFEntity
    {
        /// <summary>
        /// Gets the ID of the Discord role.
        /// </summary>
        public long DiscordRoleID { get; private set; }

        /// <summary>
        /// Gets the conditions for acquiring the role.
        /// </summary>
        public virtual List<AutoroleCondition> Conditions { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the role needs external confirmation (from a moderator, for example)
        /// to be applied after all conditions are met.
        /// </summary>
        public bool RequiresConfirmation { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the role is currently enabled; that is, whether new users meeting the
        /// conditions will have the role applied.
        /// </summary>
        public bool IsEnabled { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoroleConfiguration"/> class.
        /// </summary>
        /// <param name="discordRoleID">The ID of the discord role to assign.</param>
        public AutoroleConfiguration(long discordRoleID)
        {
            this.DiscordRoleID = discordRoleID;
            this.Conditions = new List<AutoroleCondition>();

            this.IsEnabled = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoroleConfiguration"/> class.
        /// </summary>
        /// <param name="discordRole">The Discord role.</param>
        public AutoroleConfiguration(IRole discordRole)
            : this((long)discordRole.Id)
        {
        }
    }
}
