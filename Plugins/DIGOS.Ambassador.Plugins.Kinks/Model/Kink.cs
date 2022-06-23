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

namespace DIGOS.Ambassador.Plugins.Kinks.Model;

/// <summary>
/// Represents a sexual kink or fetish.
/// </summary>
[PublicAPI]
[Table("Kinks", Schema = "KinkModule")]
public class Kink : EFEntity, IEquatable<Kink>
{
    /// <summary>
    /// Gets the category the kink belongs to.
    /// </summary>
    public KinkCategory Category { get; private set; }

    /// <summary>
    /// Gets the F-List ID of the kink.
    /// </summary>
    public long FListID { get; private set; }

    /// <summary>
    /// Gets the name of the kink.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the full description of the kink.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Kink"/> class.
    /// </summary>
    /// <param name="name">The name of the kink.</param>
    /// <param name="description">The kink's description.</param>
    /// <param name="fListID">The F-List ID of the kink.</param>
    /// <param name="category">The kink's category.</param>
    public Kink(string name, string description, long fListID, KinkCategory category)
    {
        this.Name = name;
        this.Description = description;
        this.FListID = fListID;
        this.Category = category;
    }

    /// <inheritdoc />
    [Pure]
    public bool Equals(Kink? other)
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
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj.GetType() == this.GetType() && Equals((Kink)obj);
    }

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = "Class is an entity.")]
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (int)this.Category;
            hashCode = hashCode * 397 ^ (int)this.FListID;
            hashCode = hashCode * 397 ^ this.Name.GetHashCode();
            hashCode = hashCode * 397 ^ this.Description.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Compares the equality of two <see cref="Kink"/> objects.
    /// </summary>
    /// <param name="left">The first object.</param>
    /// <param name="right">The second object.</param>
    /// <returns>true if the objects are equal; otherwise, false.</returns>
    public static bool operator ==(Kink? left, Kink? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Compares the inequality of two <see cref="Kink"/> objects.
    /// </summary>
    /// <param name="left">The first object.</param>
    /// <param name="right">The second object.</param>
    /// <returns>true if the objects are equal; otherwise, false.</returns>
    public static bool operator !=(Kink? left, Kink? right)
    {
        return !Equals(left, right);
    }
}
