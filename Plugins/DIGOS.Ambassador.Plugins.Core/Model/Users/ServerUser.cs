//
//  ServerUser.cs
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
using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Core.Model.Users
{
    /// <summary>
    /// Represents a join table entry for a server-user mapping.
    /// </summary>
    public class ServerUser : EFEntity
    {
        /// <summary>
        /// Gets or sets the server the user has joined.
        /// </summary>
        [Required, NotNull]
        public virtual Server Server { get; set; }

        /// <summary>
        /// Gets or sets the user that has joined the server.
        /// </summary>
        [Required, NotNull]
        public virtual User User { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerUser"/> class.
        /// </summary>
        [UsedImplicitly]
        protected ServerUser()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerUser"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="user">The user.</param>
        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor", Justification = "Required by EF proxies.")]
        public ServerUser([NotNull] Server server, [NotNull] User user)
        {
            this.Server = server;
            this.User = user;
        }
    }
}