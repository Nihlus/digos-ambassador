//
//  TransformationText.TransformationMessages.ShiftingMessages.SingleMessages.cs
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
using System.Text.Json.Serialization;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Messages
{
    public sealed partial class TransformationText
    {
        public sealed partial class TransformationMessages
        {
            public sealed partial class ShiftingMessages
            {
                /// <summary>
                /// Holds singular messages.
                /// </summary>
                public sealed class SingleMessages
                {
                    /// <summary>
                    /// Gets a list of colour shifting messages.
                    /// </summary>
                    public IReadOnlyList<string> Colour { get; init; } = new List<string>();

                    /// <summary>
                    /// Gets a list of pattern shifting messages.
                    /// </summary>
                    public IReadOnlyList<string> Pattern { get; init; } = new List<string>();

                    /// <summary>
                    /// Gets a list of pattern colour shifting messages.
                    /// </summary>
                    [JsonPropertyName("patternColour")]
                    public IReadOnlyList<string> PatternColour { get; init; } = new List<string>();
                }
            }
        }
    }
}
