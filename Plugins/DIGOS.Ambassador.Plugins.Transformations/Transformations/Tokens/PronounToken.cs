//
//  PronounToken.cs
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
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Tokens
{
    /// <summary>
    /// A token that gets replaced with a possessive pronoun.
    /// </summary>
    [TokenIdentifier("pronoun", "pr")]
    public class PronounToken : ReplacableTextToken<PronounToken>
    {
        private readonly PronounService _pronouns;

        /// <summary>
        /// Gets the form of the pronoun.
        /// </summary>
        public PronounForm Form { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PronounToken"/> class.
        /// </summary>
        /// <param name="pronouns">The character service.</param>
        public PronounToken(PronounService pronouns)
        {
            _pronouns = pronouns;
        }

        /// <inheritdoc />
        public override string GetText(Appearance appearance, AppearanceComponent component)
        {
            var character = appearance.Character;
            var pronounProvider = _pronouns.GetPronounProvider(character);

            return pronounProvider.GetForm(this.Form);
        }

        /// <inheritdoc />
        protected override PronounToken Initialize(string data)
        {
            if (data is null)
            {
                return this;
            }

            if (Enum.TryParse<PronounForm>(data, true, out var result))
            {
                this.Form = result;
            }

            return this;
        }
    }
}
