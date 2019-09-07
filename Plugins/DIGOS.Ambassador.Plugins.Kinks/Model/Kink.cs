//
//  Kink.cs
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
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Core.Database.Entities;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Kinks.Model
{
    /// <summary>
    /// Represents a sexual kink or fetish.
    /// </summary>
    [Table("Kinks", Schema = "KinkModule")]
    public class Kink : IEquatable<Kink>, IEFEntity
    {
        /// <inheritdoc/>
        public long ID { get; set; }

        /// <summary>
        /// Gets or sets the category the kink belongs to.
        /// </summary>
        public KinkCategory Category { get; set; }

        /// <summary>
        /// Gets or sets the F-List ID of the kink.
        /// </summary>
        public uint FListID { get; set; }

        /// <summary>
        /// Gets or sets the name of the kink.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the full description of the kink.
        /// </summary>
        public string Description { get; set; }

        /// <inheritdoc />
        [Pure]
        public bool Equals(Kink other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.Category == other.Category && this.FListID == other.FListID && string.Equals(this.Name, other.Name) && string.Equals(this.Description, other.Description);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "ArrangeThisQualifier", Justification = "Used for explicit differentiation between compared objects.")]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((Kink)obj);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = "Class is an entity.")]
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)this.Category;
                hashCode = (hashCode * 397) ^ (int)this.FListID;
                hashCode = (hashCode * 397) ^ (this.Name != null ? this.Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (this.Description != null ? this.Description.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <summary>
        /// Compares the equality of two <see cref="Kink"/> objects.
        /// </summary>
        /// <param name="left">The first object.</param>
        /// <param name="right">The second object.</param>
        /// <returns>true if the objects are equal; otherwise, false.</returns>
        public static bool operator ==([CanBeNull] Kink left, [CanBeNull] Kink right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Compares the inequality of two <see cref="Kink"/> objects.
        /// </summary>
        /// <param name="left">The first object.</param>
        /// <param name="right">The second object.</param>
        /// <returns>true if the objects are equal; otherwise, false.</returns>
        public static bool operator !=([CanBeNull] Kink left, [CanBeNull] Kink right)
        {
            return !Equals(left, right);
        }
    }
}
