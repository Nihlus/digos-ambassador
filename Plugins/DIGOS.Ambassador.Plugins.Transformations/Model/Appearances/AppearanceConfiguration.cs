//
//  AppearanceConfiguration.cs
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
using System.Linq;
using DIGOS.Ambassador.Database.Abstractions.Entities;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Transformations.Extensions;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Transformations.Model.Appearances
{
    /// <summary>
    /// Represents the appearance configuration for a character.
    /// </summary>
    [Table("AppearanceConfigurations", Schema = "TransformationModule")]
    public class AppearanceConfiguration : IEFEntity
    {
        /// <inheritdoc />
        public long ID { get; set; }

        /// <summary>
        /// Gets or sets the character that the appearance configuration applies to.
        /// </summary>
        [Required, NotNull]
        public virtual Character Character { get; set; }

        /// <summary>
        /// Gets or sets the saved default appearance of the character.
        /// </summary>
        [Required, NotNull]
        public virtual Appearance DefaultAppearance { get; set; }

        /// <summary>
        /// Gets or sets the current appearance of the character.
        /// </summary>
        [Required, NotNull]
        public virtual Appearance CurrentAppearance { get; set; }

        /// <summary>
        /// Determines whether or not the character has a given bodypart in their current appearance.
        /// </summary>
        /// <param name="bodypart">The bodypart to check for.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <returns>true if the character has the bodypart; otherwise, false.</returns>
        [Pure]
        public bool HasComponent(Bodypart bodypart, Chirality chirality)
        {
            if (bodypart.IsChiral() && chirality == Chirality.Center)
            {
                throw new ArgumentException("A chiral bodypart must have its chirality specified.", nameof(bodypart));
            }

            if (!bodypart.IsChiral() && chirality != Chirality.Center)
            {
                throw new ArgumentException("A nonchiral transformation cannot have chirality.", nameof(bodypart));
            }

            if (bodypart.IsComposite())
            {
                throw new ArgumentException("The bodypart must not be a composite part.");
            }

            return this.CurrentAppearance.Components.Any(c => c.Bodypart == bodypart && c.Chirality == chirality);
        }

        /// <summary>
        /// Gets the component on the character's current appearance that matches the given bodypart.
        /// </summary>
        /// <param name="bodypart">The bodypart to get.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <returns>The appearance component of the bodypart.</returns>
        [NotNull]
        public AppearanceComponent GetAppearanceComponent(Bodypart bodypart, Chirality chirality)
        {
            if (bodypart.IsChiral() && chirality == Chirality.Center)
            {
                throw new ArgumentException("A chiral bodypart must have its chirality specified.", nameof(bodypart));
            }

            if (!bodypart.IsChiral() && chirality != Chirality.Center)
            {
                throw new ArgumentException("A nonchiral transformation cannot have chirality.", nameof(bodypart));
            }

            if (bodypart.IsComposite())
            {
                throw new ArgumentException("The bodypart must not be a composite part.");
            }

            return this.CurrentAppearance.Components.First(c => c.Bodypart == bodypart && c.Chirality == chirality);
        }

        /// <summary>
        /// Tries to retrieve the component on the character's current appearance that matches the given bodypart.
        /// </summary>
        /// <param name="bodypart">The bodypart to get.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <param name="component">The component, or null.</param>
        /// <returns>True if a component could be retrieved, otherwise, false.</returns>
        [ContractAnnotation("=> true, component:notnull; => false, component:null")]
        public bool TryGetAppearanceComponent(Bodypart bodypart, Chirality chirality, [CanBeNull] out AppearanceComponent component)
        {
            component = null;

            if (!HasComponent(bodypart, chirality))
            {
                return false;
            }

            component = GetAppearanceComponent(bodypart, chirality);
            return true;
        }
    }
}
