//
//  ServerRoleplaySettings.cs
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

using System.ComponentModel.DataAnnotations.Schema;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Model
{
    /// <summary>
    /// Represents server-specific settings related to the roleplaying module.
    /// </summary>
    [Table("ServerSettings", Schema = "RoleplayModule")]
    public class ServerRoleplaySettings : EFEntity
    {
        /// <summary>
        /// Gets or sets the server the settings are relevant for.
        /// </summary>
        public virtual Server Server { get; set; }

        /// <summary>
        /// Gets or sets the channel category generated roleplay channels should be created under.
        /// </summary>
        public long? DedicatedRoleplayChannelsCategory { get; set; }
    }
}
