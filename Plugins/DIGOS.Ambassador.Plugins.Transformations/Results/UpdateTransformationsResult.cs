//
//  UpdateTransformationsResult.cs
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

namespace DIGOS.Ambassador.Plugins.Transformations.Results;

/// <summary>
/// Represents an attempt to shift a part of a character's body.
/// </summary>
public class UpdateTransformationsResult
{
    /// <summary>
    /// Gets the number of added species.
    /// </summary>
    public uint SpeciesAdded { get; }

    /// <summary>
    /// Gets the number of added transformations.
    /// </summary>
    public uint TransformationsAdded { get; }

    /// <summary>
    /// Gets the number of updated species.
    /// </summary>
    public uint SpeciesUpdated { get; }

    /// <summary>
    /// Gets the number of updated transformations.
    /// </summary>
    public uint TransformationsUpdated { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTransformationsResult"/> class.
    /// </summary>
    /// <param name="speciesAdded">The number of new species added.</param>
    /// <param name="transformationsAdded">The number of new transformations added.</param>
    /// <param name="speciesUpdated">The number of existing species that were updated with new information.</param>
    /// <param name="transformationsUpdated">
    /// The number of existing transformations that were updated with new information.
    /// </param>
    public UpdateTransformationsResult
    (
        uint speciesAdded,
        uint transformationsAdded,
        uint speciesUpdated,
        uint transformationsUpdated
    )
    {
        this.SpeciesAdded = speciesAdded;
        this.TransformationsAdded = transformationsAdded;
        this.SpeciesUpdated = speciesUpdated;
        this.TransformationsUpdated = transformationsUpdated;
    }
}
