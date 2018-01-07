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
using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Services;

using Discord.Commands;
using DIGOS.Ambassador.Transformations;
using JetBrains.Annotations;
using static DIGOS.Ambassador.Transformations.Bodypart;

namespace DIGOS.Ambassador.Database.Appearances
{
	/// <summary>
	/// Represents the physical appearance of a character.
	/// </summary>
	public class Appearance : IEFEntity
	{
		/// <inheritdoc />
		public uint ID { get; set; }

		/// <summary>
		/// Gets or sets the parts that compose this appearance.
		/// </summary>
		public List<AppearanceComponent> Components { get; set; }

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
		/// Creates a default appearance using the template species (a featureless, agendered species).
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="transformations">The transformation service.</param>
		/// <returns>A creation result which may or may not have succeeded.</returns>
		public static async Task<CreateEntityResult<Appearance>> CreateDefaultAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] TransformationService transformations
		)
		{
			var getSpeciesResult = await transformations.GetSpeciesByNameAsync(db, "template");
			if (!getSpeciesResult.IsSuccess)
			{
				return CreateEntityResult<Appearance>.FromError(CommandError.ObjectNotFound, "Could not find the default species.");
			}

			var templateSpecies = getSpeciesResult.Entity;
			var templateTransformations = new List<Transformation>();
			var templateParts = new List<Bodypart> { Face, Body, LeftArm, RightArm, LeftEye, RightEye, LeftLeg, RightLeg };

			foreach (var part in templateParts)
			{
				var getTFResult = await transformations.GetTransformationByPartAndSpeciesAsync(db, part, templateSpecies);
				if (!getTFResult.IsSuccess)
				{
					return CreateEntityResult<Appearance>.FromError(getTFResult);
				}

				templateTransformations.Add(getTFResult.Entity);
			}

			var templateComponents = templateTransformations.Select(AppearanceComponent.CreateFrom).ToList();

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
