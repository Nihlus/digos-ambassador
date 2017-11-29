//
//  SpeciesToken.cs
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
using System.Linq;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Transformations;

namespace DIGOS.Ambassador.Transformations
{
	/// <summary>
	/// A token that gets replaced with a species.
	/// </summary>
	[TokenIdentifier("species", "s")]
	public class SpeciesToken : ReplacableTextToken<SpeciesToken>
	{
		/// <inheritdoc />
		public override string GetText(Character character, Transformation transformation)
		{
			var speciesShares = new Dictionary<string, int>();

			foreach (var component in character.CurrentAppearance.Components)
			{
				var speciesName = component.Transformation.Species.Name;

				if (speciesShares.ContainsKey(speciesName))
				{
					speciesShares[speciesName]++;
				}
				else
				{
					speciesShares.Add(speciesName, 1);
				}
			}

			int totalPoints = speciesShares.Values.Sum();

			// pick the species with the largest share
			var largestSpecies = speciesShares.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
			var shareByPercentage = largestSpecies.Value / (double)totalPoints;

			return $"{largestSpecies.Key}{(shareByPercentage <= 0.75 ? "-morph" : string.Empty)}";
		}

		/// <inheritdoc />
		protected override SpeciesToken Initialize(string data)
		{
			return this;
		}
	}
}
