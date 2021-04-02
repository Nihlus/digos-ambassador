//
//  FluentPronounToken.cs
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
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Tokens
{
    /// <summary>
    /// A token that gets replaced with the correct pronoun based on a fluent parsing method.
    /// </summary>
    [PublicAPI]
    [TokenIdentifier("fluent", "f")]
    public sealed class FluentPronounToken : ReplaceableTextToken<FluentPronounToken>
    {
        private readonly PronounService _pronouns;

        /// <summary>
        /// Gets the form of the pronoun.
        /// </summary>
        public PronounForm Form { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentPronounToken"/> class.
        /// </summary>
        /// <param name="pronouns">The pronoun service.</param>
        public FluentPronounToken(PronounService pronouns)
        {
            _pronouns = pronouns;
        }

        /// <inheritdoc />
        public override string GetText(Appearance appearance, AppearanceComponent? component)
        {
            var character = appearance.Character;
            var pronounProvider = _pronouns.GetPronounProvider(character);

            return pronounProvider.GetForm(this.Form);
        }

        /// <inheritdoc />
        protected override FluentPronounToken Initialize(string? data)
        {
            if (data is null)
            {
                this.Form = PronounForm.Subject;
                return this;
            }

            if (data.Equals("they", StringComparison.OrdinalIgnoreCase))
            {
                this.Form = PronounForm.Subject;
            }

            if (data.Equals("they are", StringComparison.OrdinalIgnoreCase))
            {
                this.Form = PronounForm.SubjectVerb;
            }

            if (data.Equals("them", StringComparison.OrdinalIgnoreCase))
            {
                this.Form = PronounForm.Object;
            }

            if (data.Equals("their", StringComparison.OrdinalIgnoreCase))
            {
                this.Form = PronounForm.PossessiveAdjective;
            }

            if (data.Equals("they have", StringComparison.OrdinalIgnoreCase))
            {
                this.Form = PronounForm.PossessiveVerb;
            }

            if (data.Equals("theirs", StringComparison.OrdinalIgnoreCase))
            {
                this.Form = PronounForm.Possessive;
            }

            if (data.Equals("themselves", StringComparison.OrdinalIgnoreCase))
            {
                this.Form = PronounForm.Reflexive;
            }

            return this;
        }
    }
}
