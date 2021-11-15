//
//  Species.cs
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
using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Core.Database.Entities;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Transformations.Model
{
    /// <summary>
    /// Represents a single species (i.e, associated transformations).
    /// </summary>
    [PublicAPI]
    [Table("Species", Schema = "TransformationModule")]
    public class Species : IEFEntity
    {
        /// <inheritdoc />
        [YamlIgnore]
        public long ID { get; set; }

        /// <summary>
        /// Gets or sets the parent species.
        /// </summary>
        public virtual Species? Parent { get; set; }

        /// <summary>
        /// Gets or sets the name of the species.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the description of the species.
        /// </summary>
        public string Description { get; set; } = null!;

        /// <summary>
        /// Gets or sets the author of the species.
        /// </summary>
        public string Author { get; set; } = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="Species"/> class.
        /// </summary>
        [UsedImplicitly]
        [SuppressMessage
        (
            "ReSharper",
            "NotNullMemberIsNotInitialized",
            Justification = "Initialized by EF Core or YML."
        )]
        public Species()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Species"/> class.
        /// </summary>
        /// <param name="name">The species name.</param>
        /// <param name="description">The description of the species.</param>
        /// <param name="author">The author of the species..</param>
        public Species(string name, string description, string author)
        {
            this.Name = name;
            this.Description = description;
            this.Author = author;
        }

        /// <summary>
        /// Gets the depth of this species in the parent chain.
        /// </summary>
        /// <returns>The depth of the species.</returns>
        [Pure]
        public uint GetSpeciesDepth()
        {
            if (this.Parent is null)
            {
                return 0;
            }

            return this.Parent.GetSpeciesDepth() + 1;
        }

        /// <summary>
        /// Determines whether or not two species are the same by comparing their names.
        /// </summary>
        /// <param name="species">The species to compare with.</param>
        /// <returns>true if the species are the same; otherwise, false.</returns>
        [Pure]
        public bool IsSameSpeciesAs(Species? species)
        {
            return species is not null && string.Equals(this.Name.ToLower(), species.Name.ToLower());
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.Name;
        }
    }
}
