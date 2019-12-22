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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using JetBrains.Annotations;

using NotNullAttribute = JetBrains.Annotations.NotNullAttribute;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Model
{
    /// <summary>
    /// Represents server-specific settings related to the roleplaying module.
    /// </summary>
    [PublicAPI]
    [Table("ServerSettings", Schema = "RoleplayModule")]
    public class ServerRoleplaySettings : EFEntity
    {
        /// <summary>
        /// Gets the server the settings are relevant for.
        /// </summary>
        public virtual Server Server { get; private set; } = null!;

        /// <summary>
        /// Gets the channel category generated roleplay channels should be created under.
        /// </summary>
        public long? DedicatedRoleplayChannelsCategory { get; internal set; }

        /// <summary>
        /// Gets the channel that archived roleplays are exported to.
        /// </summary>
        public long? ArchiveChannel { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerRoleplaySettings"/> class.
        /// </summary>
        /// <remarks>
        /// Required by EF Core.
        /// </remarks>
        protected ServerRoleplaySettings()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerRoleplaySettings"/> class.
        /// </summary>
        /// <param name="server">The server that the settings are bound to.</param>
        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor", Justification = "Required by EF Core.")]
        public ServerRoleplaySettings(Server server)
        {
            this.Server = server;
        }
    }
}
