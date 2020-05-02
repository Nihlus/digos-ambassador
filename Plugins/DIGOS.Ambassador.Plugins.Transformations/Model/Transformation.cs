//
//  Transformation.cs
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
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Transformations.Model
{
    /// <summary>
    /// Represents an individual partial transformation.
    /// </summary>
    [PublicAPI]
    [Table("Transformations", Schema = "TransformationModule")]
    public class Transformation : IEFEntity
    {
        /// <inheritdoc />
        [YamlIgnore]
        public long ID { get; set; }

        /// <summary>
        /// Gets or sets the bodypart that this transformation affects.
        /// </summary>
        public Bodypart Part { get; set; }

        /// <summary>
        /// Gets or sets the species that this transformation belongs to.
        /// </summary>
        [Required]
        public virtual Species Species { get; set; } = null!;

        /// <summary>
        /// Gets or sets a short description of the transformation.
        /// </summary>
        [Required]
        public string Description { get; set; } = null!;

        /// <summary>
        /// Gets or sets the default base colour of the transformation.
        /// </summary>
        [YamlMember(Alias = "default_base_colour")]
        [Required]
        public virtual Colour DefaultBaseColour { get; set; } = null!;

        /// <summary>
        /// Gets or sets the default pattern of the transformation (if any).
        /// </summary>
        [YamlMember(Alias = "default_pattern")]
        public Pattern? DefaultPattern { get; set; }

        /// <summary>
        /// Gets or sets the default colour of the pattern (if any).
        /// </summary>
        [YamlMember(Alias = "default_pattern_colour")]
        public virtual Colour? DefaultPatternColour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this transformation is NSFW.
        /// </summary>
        [YamlMember(Alias = "is_nsfw")]
        public bool IsNSFW { get; set; }

        /// <summary>
        /// Gets or sets the text of the message when an existing bodypart shifts into this one.
        /// </summary>
        [YamlMember(Alias = "shift_message")]
        [Required]
        public string ShiftMessage { get; set; } = null!;

        /// <summary>
        /// Gets or sets the text of the message when this bodypart is added where none existed before.
        /// </summary>
        [YamlMember(Alias = "grow_message")]
        [Required]
        public string GrowMessage { get; set; } = null!;

        /// <summary>
        /// Gets or sets the uniform shift message, used when two chiral parts shift together.
        /// </summary>
        [YamlMember(Alias = "uniform_shift_message")]
        public string? UniformShiftMessage { get; set; }

        /// <summary>
        /// Gets or sets the uniform grow message, used when two chiral parts grow together.
        /// </summary>
        [YamlMember(Alias = "uniform_grow_message")]
        public string? UniformGrowMessage { get; set; }

        /// <summary>
        /// Gets or sets the text of the description when the species of the complementary bodyparts don't match.
        /// </summary>
        [YamlMember(Alias = "single_description")]
        [Required]
        public string SingleDescription { get; set; } = null!;

        /// <summary>
        /// Gets or sets the text of the description when the species of the complementary bodyparts match.
        /// </summary>
        [YamlMember(Alias = "uniform_description")]
        public string? UniformDescription { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Transformation"/> class.
        /// </summary>
        [UsedImplicitly]
        [SuppressMessage
        (
            "ReSharper",
            "NotNullMemberIsNotInitialized",
            Justification = "Initialized by EF Core or YML."
        )]
        public Transformation()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Transformation"/> class.
        /// </summary>
        /// <param name="species">The species the transformation belongs to.</param>
        /// <param name="description">The description of the transformation.</param>
        /// <param name="defaultBaseColour">The transformation's default base colour.</param>
        /// <param name="shiftMessage">The transformation's shift message.</param>
        /// <param name="growMessage">The transformation's grow message.</param>
        /// <param name="singleDescription">The description of a single bodypart.</param>
        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor", Justification = "Required by EF Core.")]
        public Transformation
        (
            Species species,
            string description,
            Colour defaultBaseColour,
            string shiftMessage,
            string growMessage,
            string singleDescription
        )
        {
            this.Species = species;
            this.Description = description;
            this.DefaultBaseColour = defaultBaseColour;
            this.ShiftMessage = shiftMessage;
            this.GrowMessage = growMessage;
            this.SingleDescription = singleDescription;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{this.Species.Name} - {this.Part}";
        }
    }
}
