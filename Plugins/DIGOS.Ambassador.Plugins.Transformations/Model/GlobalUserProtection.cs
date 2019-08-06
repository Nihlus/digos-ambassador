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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DIGOS.Ambassador.Database.Abstractions.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Transformations.Model
{
    /// <summary>
    /// Holds global protection data for a specific user.
    /// </summary>
    [Table("GlobalUserProtections", Schema = "TransformationModule")]
    public class GlobalUserProtection : IEFEntity
    {
        /// <inheritdoc />
        public long ID { get; set; }

        /// <summary>
        /// Gets or sets the user that owns this protection data.
        /// </summary>
        [Required]
        public virtual User User { get; set; }

        /// <summary>
        /// Gets or sets the default protection type to use on new servers.
        /// </summary>
        public ProtectionType DefaultType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the user should be opted in by default.
        /// </summary>
        public bool DefaultOptIn { get; set; }

        /// <summary>
        /// Gets or sets the list of users that are listed in this protection entry.
        /// </summary>
        [NotNull]
        public virtual List<UserProtectionEntry> UserListing { get; set; } = new List<UserProtectionEntry>();

        /// <summary>
        /// Gets the list of users that are allowed to transform the owner.
        /// </summary>
        [NotNull, NotMapped]
        public IEnumerable<User> Whitelist =>
            this.UserListing.Where(u => u.Type == ListingType.Whitelist).Select(u => u.User);

        /// <summary>
        /// Gets the list of users that are prohibited from transforming the owner.
        /// </summary>
        [NotNull, NotMapped]
        public IEnumerable<User> Blacklist =>
                this.UserListing.Where(u => u.Type == ListingType.Blacklist).Select(u => u.User);

        /// <summary>
        /// Creates a default global protection object for the given user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>A default user protection object.</returns>
        [Pure]
        [NotNull]
        public static GlobalUserProtection CreateDefault([NotNull] User user)
        {
            return new GlobalUserProtection
            {
                User = user,
                DefaultType = ProtectionType.Blacklist,
                DefaultOptIn = false,
                UserListing = new List<UserProtectionEntry>()
            };
        }
    }
}
