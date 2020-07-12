//
//  UserServerStatistics.cs
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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Autorole.Model.Statistics
{
    /// <summary>
    /// Represents a set of per-server statistics for a user.
    /// </summary>
    [PublicAPI]
    [Table("UserServerStatistics", Schema = "AutoroleModule")]
    public class UserServerStatistics : EFEntity
    {
        /// <summary>
        /// Gets the server that the statistics are for.
        /// </summary>
        public virtual Server Server { get; private set; } = null!;

        /// <summary>
        /// Gets the total message count of the user in this server.
        /// </summary>
        public long? TotalMessageCount { get; internal set; }

        /// <summary>
        /// Gets the individual channel post counts of the user.
        /// </summary>
        public virtual List<UserChannelStatistics> ChannelStatistics { get; private set; } = null!;

        /// <summary>
        /// Gets the last time the user performed a tracked activity on the server.
        /// </summary>
        public DateTime? LastActivityTime { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserServerStatistics"/> class.
        /// </summary>
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized", Justification = "Initialized by EF Core.")]
        protected UserServerStatistics()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserServerStatistics"/> class.
        /// </summary>
        /// <param name="server">The server the statistics are for.</param>
        public UserServerStatistics(Server server)
        {
            this.Server = server;
            this.ChannelStatistics = new List<UserChannelStatistics>();
        }
    }
}
