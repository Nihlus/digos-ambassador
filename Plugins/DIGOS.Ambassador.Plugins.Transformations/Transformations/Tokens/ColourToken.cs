//
//  ColourToken.cs
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
using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Tokens
{
    /// <summary>
    /// A token that gets replaced with a colour.
    /// </summary>
    [PublicAPI]
    [TokenIdentifier("colour", "c")]
    public sealed class ColourToken : ReplacableTextToken<ColourToken>
    {
        /// <summary>
        /// Gets a value indicating whether the pattern colour should be retrieved instead of the base colour.
        /// </summary>
        public bool UsePattern { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColourToken"/> class.
        /// </summary>
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized", Justification = "Initialized by convention.")]
        public ColourToken()
        {
        }

        /// <inheritdoc />
        public override string GetText(Appearance appearance, AppearanceComponent component)
        {
            if (component is null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            if (this.UsePattern)
            {
                return component.PatternColour?.ToString() ?? string.Empty;
            }

            return component.BaseColour.ToString();
        }

        /// <inheritdoc />
        protected override ColourToken Initialize(string data)
        {
            if (data is null)
            {
                return this;
            }

            if (data == "pattern")
            {
                this.UsePattern = true;
                return this;
            }

            return this;
        }
    }
}
