//
//  UserKink.cs
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
namespace DIGOS.Ambassador.Plugins.Kinks.Model
{
    /// <summary>
    /// Represents a user's kink, along with their preference for it.
    /// </summary>
    [PublicAPI]
    [Table("UserKinks", Schema = "KinkModule")]
    public class UserKink : EFEntity
    {
        /// <summary>
        /// Gets the user the kink belongs to.
        /// </summary>
        public virtual User User { get; private set; } = null!;

        /// <summary>
        /// Gets the kink.
        /// </summary>
        public virtual Kink Kink { get; private set; } = null!;

        /// <summary>
        /// Gets the user's preference for the kink.
        /// </summary>
        public KinkPreference Preference { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserKink"/> class.
        /// </summary>
        /// <remarks>
        /// Required by EF Core.
        /// </remarks>
        protected UserKink()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserKink"/> class.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="kink">The user's kink.</param>
        public UserKink([NotNull] User user, [NotNull] Kink kink)
        {
            this.User = user;
            this.Kink = kink;

            this.Preference = KinkPreference.NoPreference;
        }
    }
}
