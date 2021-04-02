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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DIGOS.Ambassador.Core.Extensions;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Plugins.Transformations.Model.Appearances
{
    /// <summary>
    /// Represents a colour shade with an optional modifier.
    /// </summary>
    [Owned]
    [PublicAPI]
    public class Colour : IColour
    {
        /// <inheritdoc />
        public Shade Shade { get; internal set; }

        /// <inheritdoc />
        public ShadeModifier? Modifier { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Colour"/> class.
        /// </summary>
        /// <param name="shade">The colour's shade.</param>
        /// <param name="modifier">The shade modifier.</param>
        public Colour(Shade shade, ShadeModifier? modifier)
        {
            this.Shade = shade;
            this.Modifier = modifier;
        }

        /// <summary>
        /// Determines whether this colour is the same colour as the given one.
        /// </summary>
        /// <param name="other">The other colour.</param>
        /// <returns>true if the colours are the same; otherwise, false.</returns>
        [Pure]
        public bool IsSameColourAs(Colour? other)
        {
            if (other is null)
            {
                return false;
            }

            return this.Shade == other.Shade && this.Modifier == other.Modifier;
        }

        /// <summary>
        /// Attempts to parse the given input into a colour.
        /// </summary>
        /// <param name="input">The input text to parse.</param>
        /// <param name="colour">The output colour.</param>
        /// <returns>true if the parsing was successful; otherwise, false.</returns>
        [Pure]
        public static bool TryParse
        (
            string? input,
            [NotNullWhen(true)] out Colour? colour
        )
        {
            colour = null;

            if (input.IsNullOrWhitespace())
            {
                return false;
            }

            // First, break the input up into parts based on spaces
            var parts = input.Split(' ');

            // Check for a modifier
            ShadeModifier? modifier = null;
            if (Enum.TryParse(parts[0], true, out ShadeModifier realModifier))
            {
                modifier = realModifier;
                parts = parts.Skip(1).ToArray();
            }

            // Then check for a known shade
            if (!Enum.TryParse(string.Join(string.Empty, parts), true, out Shade shade))
            {
                colour = null;
                return false;
            }

            colour = new Colour(shade, modifier);
            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{(this.Modifier is null ? string.Empty : this.Modifier.Humanize())} {this.Shade.Humanize()}".Trim().Humanize(LetterCasing.LowerCase);
        }

        /// <summary>
        /// Clones this colour, creating a new unbound colour with the same settings.
        /// </summary>
        /// <returns>The cloned colour.</returns>
        [Pure, JetBrains.Annotations.NotNull]
        public Colour Clone()
        {
            return new Colour(this.Shade, this.Modifier);
        }

        /// <summary>
        /// Copies the given colour into a new colour.
        /// </summary>
        /// <param name="other">The other colour.</param>
        /// <returns>The copied colour.</returns>
        [Pure, JetBrains.Annotations.NotNull]
        public static Colour CopyFrom(Colour other)
        {
            return other.Clone();
        }
    }
}
