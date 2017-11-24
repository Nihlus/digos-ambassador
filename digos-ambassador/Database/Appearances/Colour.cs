//
//  Colour.cs
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
using System.Linq;
using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Extensions;
using Humanizer;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace DIGOS.Ambassador.Database.Appearances
{
	/// <summary>
	/// Represents a colour shade with an optional modifier.
	/// </summary>
	public class Colour : IEFEntity
	{
		/// <inheritdoc />
		[YamlIgnore]
		public uint ID { get; set; }

		/// <summary>
		/// Gets or sets the shade of the colour.
		/// </summary>
		public Shade Shade { get; set; }

		/// <summary>
		/// Gets or sets the colour modifier.
		/// </summary>
		public ShadeModifier? Modifier { get; set; }

		/// <summary>
		/// Attempts to parse the given input into a colour.
		/// </summary>
		/// <param name="input">The input text to parse.</param>
		/// <param name="colour">The output colour.</param>
		/// <returns>true if the parsing was successful; otherwise, false.</returns>
		[Pure]
		[ContractAnnotation("input:null => false")]
		public static bool TryParse([CanBeNull] string input, out Colour colour)
		{
			colour = default;

			if (input.IsNullOrWhitespace())
			{
				return false;
			}

			var result = new Colour();

			// First, break the input up into parts based on spaces
			var parts = input.Split(" ");

			// Check for a modifier
			if (Enum.TryParse(parts[0], true, out ShadeModifier modifier))
			{
				result.Modifier = modifier;
				parts = parts.Skip(1).ToArray();
			}

			// Then check for a known shade
			if (!Enum.TryParse(string.Join(string.Empty, parts), true, out Shade shade))
			{
				return false;
			}

			result.Shade = shade;
			return true;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"{this.Modifier.Humanize()} {this.Shade.Humanize()}".Trim().Humanize(LetterCasing.LowerCase);
		}
	}
}
