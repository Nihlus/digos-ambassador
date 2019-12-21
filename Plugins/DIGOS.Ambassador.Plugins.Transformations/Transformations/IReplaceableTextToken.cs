//
//  IReplaceableTextToken.cs
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

using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations
{
    /// <summary>
    /// Represents a replaceable token in text.
    /// </summary>
    [PublicAPI]
    public interface IReplaceableTextToken
    {
        /// <summary>
        /// Gets or sets the start index of the token in the text.
        /// </summary>
        int Start { get; set; }

        /// <summary>
        /// Gets or sets the length of the original token.
        /// </summary>
        int Length { get; set; }

        /// <summary>
        /// Gets the text that the token should be replaced with.
        /// </summary>
        /// <param name="appearance">The appearance that the text should be relevant for.</param>
        /// <param name="component">The component that the text originates from.</param>
        /// <returns>The text that the token should be replaced with.</returns>
        [NotNull]
        string GetText
        (
            [NotNull] Appearance appearance,
            AppearanceComponent? component
        );

        /// <summary>
        /// Gets the text that the token should be replaced with.
        /// </summary>
        /// <param name="appearance">The appearance that the text should be relevant for.</param>
        /// <param name="component">The component that the text originates from.</param>
        /// <returns>The text that the token should be replaced with.</returns>
        [NotNull, ItemNotNull]
        Task<string> GetTextAsync
        (
            [NotNull] Appearance appearance,
            AppearanceComponent? component
        );
    }
}
