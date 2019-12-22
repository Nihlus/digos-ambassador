﻿//
//  UserProtectionEntry.cs
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
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using JetBrains.Annotations;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Transformations.Model
{
    /// <summary>
    /// Represents a protection entry, that is, a user that has been whitelisted or blacklisted by another user in the
    /// TF module.
    /// </summary>
    [PublicAPI]
    [Table("UserProtectionEntries", Schema = "TransformationModule")]
    public class UserProtectionEntry : EFEntity
    {
        /// <summary>
        /// Gets the global protection entry that the user has been listed in.
        /// </summary>
        public virtual GlobalUserProtection GlobalProtection { get; private set; } = null!;

        /// <summary>
        /// Gets the user that's listed in the global protection entry.
        /// </summary>
        public virtual User User { get; private set; } = null!;

        /// <summary>
        /// Gets the type of listing.
        /// </summary>
        public ListingType Type { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserProtectionEntry"/> class.
        /// </summary>
        protected UserProtectionEntry()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserProtectionEntry"/> class.
        /// </summary>
        /// <param name="globalProtection">The global protection entry.</param>
        /// <param name="user">The target user.</param>
        public UserProtectionEntry(GlobalUserProtection globalProtection, User user)
        {
            this.GlobalProtection = globalProtection;
            this.User = user;
        }
    }
}
