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
using DIGOS.Ambassador.Database.Appearances;
using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Transformations;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace DIGOS.Ambassador.Database.Transformations
{
	/// <summary>
	/// Represents an individual partial transformation.
	/// </summary>
	public class Transformation : IEFEntity
	{
		/// <inheritdoc />
		[YamlIgnore]
		public uint ID { get; set; }

		/// <summary>
		/// Gets or sets the bodypart that this transformation affects.
		/// </summary>
		[Required]
		public Bodypart Part { get; set; }

		/// <summary>
		/// Gets or sets the species that this transformation belongs to.
		/// </summary>
		[Required]
		public Species Species { get; set; }

		/// <summary>
		/// Gets or sets a short description of the transformation.
		/// </summary>
		[Required]
		public string Description { get; set; }

		/// <summary>
		/// Gets or sets the default base colour of the transformation.
		/// </summary>
		[Required]
		[YamlMember(Alias = "default_base_colour")]
		public Colour DefaultBaseColour { get; set; }

		/// <summary>
		/// Gets or sets the default pattern of the transformation (if any).
		/// </summary>
		[YamlMember(Alias = "default_pattern")]
		public Pattern? DefaultPattern { get; set; }

		/// <summary>
		/// Gets or sets the default colour of the pattern (if any).
		/// </summary>
		[YamlMember(Alias = "default_pattern_colour")]
		public Colour DefaultPatternColour { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this transformation is NSFW.
		/// </summary>
		[Required]
		[YamlMember(Alias = "is_nsfw")]
		public bool IsNSFW { get; set; }

		/// <summary>
		/// Gets or sets the text of the message when an existing bodypart shifts into this one.
		/// </summary>
		[Required]
		[YamlMember(Alias = "shift_message")]
		public string ShiftMessage { get; set; }

		/// <summary>
		/// Gets or sets the text of the message when this bodypart is added where none existed before.
		/// </summary>
		[Required]
		[YamlMember(Alias = "grow_message")]
		public string GrowMessage { get; set; }

		/// <summary>
		/// Gets or sets the text of the description when the species of the complementary bodyparts don't match.
		/// </summary>
		[Required]
		[YamlMember(Alias = "single_description")]
		public string SingleDescription { get; set; }

		/// <summary>
		/// Gets or sets the text of the description when the species of the complementary bodyparts match.
		/// </summary>
		[YamlMember(Alias = "uniform_description")]
		[CanBeNull]
		public string UniformDescription { get; set; }
	}
}
