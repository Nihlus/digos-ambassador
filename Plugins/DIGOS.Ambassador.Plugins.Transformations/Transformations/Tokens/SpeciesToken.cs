//
//  SpeciesToken.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Tokens;

/// <summary>
/// A token that gets replaced with a species.
/// </summary>
[PublicAPI]
[TokenIdentifier("species", "s")]
public sealed class SpeciesToken : ReplaceableTextToken<SpeciesToken>
{
    /// <inheritdoc />
    public override string GetText(Appearance appearance, AppearanceComponent? component)
    {
        var speciesShares = new Dictionary<string, int>();

        var speciesNames = appearance.Components.Select
        (
            characterComponent => characterComponent.Transformation.Species.Name
        );

        foreach (var speciesName in speciesNames)
        {
            if (speciesShares.ContainsKey(speciesName))
            {
                speciesShares[speciesName]++;
            }
            else
            {
                speciesShares.Add(speciesName, 1);
            }
        }

        if (!speciesShares.Any())
        {
            return "person";
        }

        var totalPoints = speciesShares.Values.Sum();

        // pick the species with the largest share
        var (species, componentCount) = speciesShares.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
        var shareByPercentage = componentCount / (double)totalPoints;

        return $"{species}{(shareByPercentage <= 0.50 ? "-morph" : string.Empty)}";
    }

    /// <inheritdoc />
    protected override SpeciesToken Initialize(string? data)
    {
        return this;
    }
}
