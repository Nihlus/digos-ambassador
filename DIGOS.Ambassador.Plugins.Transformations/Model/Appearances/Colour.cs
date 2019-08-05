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
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Database.Abstractions.Entities;
using Humanizer;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace DIGOS.Ambassador.Plugins.Transformations.Model.Appearances
{
    /// <summary>
    /// Represents a colour shade with an optional modifier.
    /// </summary>
    [Table("Colours", Schema = "TransformationModule")]
    public class Colour : IEFEntity, IColour
    {
        /// <inheritdoc />
        [YamlIgnore]
        public long ID { get; set; }

        /// <inheritdoc />
        public Shade Shade { get; set; }

        /// <inheritdoc />
        [CanBeNull]
        public ShadeModifier? Modifier { get; set; }

        /// <summary>
        /// Determines whether this colour is the same colour as the given one.
        /// </summary>
        /// <param name="other">The other colour.</param>
        /// <returns>true if the colours are the same; otherwise, false.</returns>
        [Pure]
        public bool IsSameColourAs([NotNull] Colour other)
        {
            return this.Shade == other.Shade && this.Modifier == other.Modifier;
        }

        /// <summary>
        /// Attempts to parse the given input into a colour.
        /// </summary>
        /// <param name="input">The input text to parse.</param>
        /// <param name="colour">The output colour.</param>
        /// <returns>true if the parsing was successful; otherwise, false.</returns>
        [Pure]
        [ContractAnnotation("input:null => false")]
        public static bool TryParse([CanBeNull] string input, [CanBeNull] out Colour colour)
        {
            colour = new Colour();

            if (input.IsNullOrWhitespace())
            {
                colour = null;
                return false;
            }

            // First, break the input up into parts based on spaces
            var parts = input.Split(' ');

            // Check for a modifier
            if (Enum.TryParse(parts[0], true, out ShadeModifier modifier))
            {
                colour.Modifier = modifier;
                parts = parts.Skip(1).ToArray();
            }

            // Then check for a known shade
            if (!Enum.TryParse(string.Join(string.Empty, parts), true, out Shade shade))
            {
                colour = null;
                return false;
            }

            colour.Shade = shade;
            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{(this.Modifier is null ? string.Empty : this.Modifier.Humanize())} {this.Shade.Humanize()}".Trim().Humanize(LetterCasing.LowerCase);
        }
    }
}
