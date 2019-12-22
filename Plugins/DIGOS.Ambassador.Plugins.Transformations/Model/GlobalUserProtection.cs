//
//  GlobalUserProtection.cs
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
using System.Linq;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Transformations.Model
{
    /// <summary>
    /// Holds global protection data for a specific user.
    /// </summary>
    [PublicAPI]
    [Table("GlobalUserProtections", Schema = "TransformationModule")]
    public class GlobalUserProtection : EFEntity
    {
        /// <summary>
        /// Gets the user that owns this protection data.
        /// </summary>
        public virtual User User { get; private set; } = null!;

        /// <summary>
        /// Gets the default protection type to use on new servers.
        /// </summary>
        public ProtectionType DefaultType { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether or not the user should be opted in by default.
        /// </summary>
        public bool DefaultOptIn { get; internal set; }

        /// <summary>
        /// Gets the list of users that are listed in this protection entry.
        /// </summary>
        public virtual List<UserProtectionEntry> UserListing { get; private set; } = new List<UserProtectionEntry>();

        /// <summary>
        /// Gets the list of users that are allowed to transform the owner.
        /// </summary>
        public IEnumerable<User> Whitelist =>
            this.UserListing.Where(u => u.Type == ListingType.Whitelist).Select(u => u.User);

        /// <summary>
        /// Gets the list of users that are prohibited from transforming the owner.
        /// </summary>
        public IEnumerable<User> Blacklist =>
                this.UserListing.Where(u => u.Type == ListingType.Blacklist).Select(u => u.User);

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalUserProtection"/> class.
        /// </summary>
        /// <remarks>
        /// Required by EF Core.
        /// </remarks>
        protected GlobalUserProtection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalUserProtection"/> class.
        /// </summary>
        /// <param name="user">The protected user.</param>
        public GlobalUserProtection(User user)
        {
            this.User = user;
            this.DefaultType = ProtectionType.Blacklist;
        }
    }
}
