//
//  TransformationText.cs
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

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Messages
{
    /// <summary>
    /// Database class for various data-driven transformation messages.
    /// </summary>
    public sealed partial class TransformationText
    {
        /// <summary>
        /// Gets a set of description messages.
        /// </summary>
        public DescriptionMessages Descriptions { get; init; } = new();

        /// <summary>
        /// Gets a set of transformation messages.
        /// </summary>
        public TransformationMessages Messages { get; init; } = new();

        /// <summary>
        /// Attempts to deserialize a <see cref="TransformationText"/> instance from the given JSON text.
        /// </summary>
        /// <param name="json">The JSON text.</param>
        /// <param name="text">The deserialized database.</param>
        /// <returns>true if the deserialization was successful; otherwise, false.</returns>
        [Pure]
        public static bool TryDeserialize(string json, [NotNullWhen(true)] out TransformationText? text)
        {
            text = null;
            try
            {
                text = JsonSerializer.Deserialize<TransformationText>(json) ?? throw new JsonException();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
