//
//  AutoroleServerSettings.cs
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

using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using Remora.Discord.Core;

namespace DIGOS.Ambassador.Plugins.Autorole.Model
{
    /// <summary>
    /// Represents a set of server-specific autorole settings.
    /// </summary>
    public class AutoroleServerSettings : EFEntity
    {
        /// <summary>
        /// Gets the server the settings are bound to.
        /// </summary>
        public virtual Server Server { get; private set; } = null!;

        /// <summary>
        /// Gets the channel that notifications about users requiring affirmation are sent.
        /// </summary>
        public Snowflake? AffirmationRequiredNotificationChannelID { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoroleServerSettings"/> class.
        /// </summary>
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized", Justification = "Initialized by EF Core.")]
        protected AutoroleServerSettings()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoroleServerSettings"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        public AutoroleServerSettings(Server server)
        {
            this.Server = server;
        }
    }
}
