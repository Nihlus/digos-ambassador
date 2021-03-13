//
//  ServerModerationSettings.cs
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
using Remora.Discord.Core;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Moderation.Model
{
    /// <summary>
    /// Represents settings for a Discord server.
    /// </summary>
    [Table("ServerModerationSettings", Schema = "ModerationModule")]
    public class ServerModerationSettings : EFEntity
    {
        /// <summary>
        /// Gets the server.
        /// </summary>
        [Required]
        public virtual Server Server { get; private set; } = null!;

        /// <summary>
        /// Gets the Discord ID of the channel where moderation actions are logged.
        /// </summary>
        public Snowflake? ModerationLogChannel { get; internal set; }

        /// <summary>
        /// Gets the Discord ID of the channel where events are logged, such as deleted messages, username changes, et
        /// cetera.
        /// </summary>
        public Snowflake? MonitoringChannel { get; internal set; }

        /// <summary>
        /// Gets the number of warnings a user can have before any further warnings should prompt further action.
        /// Defaults to three.
        /// </summary>
        public int WarningThreshold { get; internal set; } = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerModerationSettings"/> class.
        /// </summary>
        /// <remarks>
        /// Required by EF Core.
        /// </remarks>
        protected ServerModerationSettings()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerModerationSettings"/> class.
        /// </summary>
        /// <param name="server">The server that the settings are bound to.</param>
        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor", Justification = "Required by EF Core.")]
        public ServerModerationSettings(Server server)
        {
            this.Server = server;
        }
    }
}
