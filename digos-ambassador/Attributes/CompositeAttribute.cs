//
//  CompositeAttribute.cs
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
using System.Collections.Generic;
using DIGOS.Ambassador.Transformations;

namespace DIGOS.Ambassador.Attributes
{
    /// <summary>
    /// An attribute which marks a bodypart as a composite part.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class CompositeAttribute : Attribute
    {
        /// <summary>
        /// Gets the list of parts that compose this part.
        /// </summary>
        public IReadOnlyList<Bodypart> ComposingParts { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeAttribute"/> class.
        /// </summary>
        /// <param name="composingParts">The parts that compose this part.</param>
        public CompositeAttribute(params Bodypart[] composingParts)
        {
            this.ComposingParts = composingParts;
        }
    }
}
