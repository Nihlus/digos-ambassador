//
//  Appearance.cs
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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Plugins.Transformations.Extensions;
using DIGOS.Ambassador.Plugins.Transformations.Services;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using JetBrains.Annotations;
using static DIGOS.Ambassador.Plugins.Transformations.Transformations.Bodypart;

namespace DIGOS.Ambassador.Plugins.Transformations.Model.Appearances
{
    /// <summary>
    /// Represents the physical appearance of a character.
    /// </summary>
    [Table("Appearances", Schema = "TransformationModule")]
    public class Appearance : IEFEntity
    {
        /// <inheritdoc />
        public long ID { get; set; }

        /// <summary>
        /// Gets or sets the parts that compose this appearance.
        /// </summary>
        [NotNull, ItemNotNull]
        public virtual List<AppearanceComponent> Components { get; set; } = new List<AppearanceComponent>();

        /// <summary>
        /// Gets or sets a character's height (in meters).
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Gets or sets a character's weight (in kilograms).
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// Gets or sets how feminine or masculine a character appears to be, on a -1 to 1 scale.
        /// </summary>
        public double GenderScale { get; set; }

        /// <summary>
        /// Gets or sets how muscular a character appears to be, on a 0 to 1 scale.
        /// </summary>
        public double Muscularity { get; set; }

        /// <summary>
        /// Creates a new appearance from a source appearance.
        /// </summary>
        /// <param name="sourceAppearance">The source appearance.</param>
        /// <returns>The new appearance.</returns>
        [NotNull]
        public static Appearance CopyFrom([NotNull] Appearance sourceAppearance)
        {
            var componentCopies = sourceAppearance.Components.Select(AppearanceComponent.CopyFrom).ToList();

            var newAppearance = new Appearance
            {
                Components = componentCopies,
                Height = sourceAppearance.Height,
                Weight = sourceAppearance.Weight,
                GenderScale = sourceAppearance.GenderScale,
                Muscularity = sourceAppearance.Muscularity
            };

            return newAppearance;
        }

        /// <summary>
        /// Creates a default appearance using the template species (a featureless, agendered species).
        /// </summary>
        /// <param name="transformations">The transformation service.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public static async Task<CreateEntityResult<Appearance>> CreateDefaultAsync
        (
            [NotNull] TransformationService transformations
        )
        {
            var getSpeciesResult = await transformations.GetSpeciesByNameAsync("template");
            if (!getSpeciesResult.IsSuccess)
            {
                return CreateEntityResult<Appearance>.FromError("Could not find the default species.");
            }

            var templateSpecies = getSpeciesResult.Entity;
            var templateTransformations = new List<Transformation>();
            var templateParts = new List<Bodypart> { Head, Body, Arms, Legs };

            // Explode the composite parts into their components
            templateParts = templateParts.SelectMany(p => p.GetComposingParts()).Distinct().ToList();

            foreach (var part in templateParts)
            {
                var getTFResult = await transformations.GetTransformationsByPartAndSpeciesAsync(part, templateSpecies);
                if (!getTFResult.IsSuccess)
                {
                    // Allow skipping of missing composing parts - a composite part might not have all of them in a TF.
                    if (part.IsComposingPart())
                    {
                        continue;
                    }

                    return CreateEntityResult<Appearance>.FromError(getTFResult);
                }

                templateTransformations.AddRange(getTFResult.Entity);
            }

            var templateComponents = new List<AppearanceComponent>();
            foreach (var tf in templateTransformations)
            {
                if (tf.Part.IsChiral())
                {
                    templateComponents.AddRange(AppearanceComponent.CreateFromChiral(tf));
                }
                else
                {
                    templateComponents.Add(AppearanceComponent.CreateFrom(tf));
                }
            }

            var appearance = new Appearance
            {
                Components = templateComponents,
                Height = 1.8,
                Weight = 80,
                GenderScale = 0,
                Muscularity = 0.5
            };

            return CreateEntityResult<Appearance>.FromSuccess(appearance);
        }
    }
}
