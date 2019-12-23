//
//  ServerUserProtection.cs
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
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using JetBrains.Annotations;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Transformations.Model
{
    /// <summary>
    /// Holds protection data for a specific user on a specific server.
    /// </summary>
    [PublicAPI]
    [Table("ServerUserProtections", Schema = "TransformationModule")]
    public class ServerUserProtection : EFEntity
    {
        /// <summary>
        /// Gets the user that owns this protection data.
        /// </summary>
        [Required]
        public virtual User User { get; private set; } = null!;

        /// <summary>
        /// Gets the server that this protection data is valid on.
        /// </summary>
        [Required]
        public virtual Server Server { get; private set; } = null!;

        /// <summary>
        /// Gets the active protection type on this server.
        /// </summary>
        public ProtectionType Type { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether or not the user has opted in to transformations.
        /// </summary>
        public bool HasOptedIn { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerUserProtection"/> class.
        /// </summary>
        /// <remarks>
        /// Required by EF Core.
        /// </remarks>
        protected ServerUserProtection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerUserProtection"/> class.
        /// </summary>
        /// <param name="server">The server the user is protected on.</param>
        /// <param name="user">The user that is protected.</param>
        public ServerUserProtection(Server server, User user)
        {
            this.Server = server;
            this.User = user;

            this.Type = ProtectionType.Blacklist;
        }

        /// <summary>
        /// Creates a default server-specific protection object based on the given global protection data.
        /// </summary>
        /// <param name="globalProtection">The global protection data.</param>
        /// <param name="server">The server that the protection should be valid for.</param>
        /// <returns>A server-specific protection object.</returns>
        [Pure, NotNull]
        public static ServerUserProtection CreateDefault
        (
            [NotNull] GlobalUserProtection globalProtection,
            [NotNull] Server server
        )
        {
            return new ServerUserProtection(server, globalProtection.User)
            {
                Type = globalProtection.DefaultType,
                HasOptedIn = globalProtection.DefaultOptIn
            };
        }
    }
}
