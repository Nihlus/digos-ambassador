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

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DIGOS.Ambassador.Database.Abstractions.Entities;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace DIGOS.Ambassador.Plugins.Transformations.Model
{
    /// <summary>
    /// Represents a single species (i.e, associated transformations).
    /// </summary>
    [Table("Species", Schema = "TransformationModule")]
    public class Species : IEFEntity
    {
        /// <inheritdoc />
        [YamlIgnore]
        public long ID { get; set; }

        /// <summary>
        /// Gets or sets the parent species.
        /// </summary>
        [CanBeNull]
        public virtual Species Parent { get; set; }

        /// <summary>
        /// Gets or sets the name of the species.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the species.
        /// </summary>
        [Required]
        public string Description { get; set; }

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
        [ContractAnnotation("species:null => false")]
        public bool IsSameSpeciesAs([CanBeNull] Species species)
        {
            if (species is null)
            {
                return false;
            }

            return string.Equals(this.Name, species.Name, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.Name;
        }
    }
}
