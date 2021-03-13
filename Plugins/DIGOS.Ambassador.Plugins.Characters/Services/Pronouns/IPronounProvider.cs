//
//  IPronounProvider.cs
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

using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Characters.Services.Pronouns
{
    /// <summary>
    /// Represents a pronoun in its different forms.
    /// </summary>
    public interface IPronounProvider
    {
        /// <summary>
        /// Gets the family of the pronoun, that is, "I", "He", or "She".
        /// </summary>
        string Family { get; }

        /// <summary>
        /// Gets the pronoun in subject form, that is, "I" or "you". If <paramref name="withVerb"/> is true,
        /// the pronoun will include a possessive verb - that is, "I am" or "she is".
        /// </summary>
        /// <param name="withVerb">Whether or not to include the connective verb.</param>
        /// <returns>The pronoun.</returns>
        string GetSubjectForm(bool withVerb = false);

        /// <summary>
        /// Gets the pronoun in object form, that is, "me" or "you".
        /// </summary>
        /// <returns>The pronoun.</returns>
        string GetObjectForm();

        /// <summary>
        /// Gets the pronoun in possessive adjective form, that is, "my" or "yours".
        /// </summary>
        /// <returns>The pronoun.</returns>
        string GetPossessiveAdjectiveForm();

        /// <summary>
        /// Gets the pronoun in possessive form, that is, "mine" or "yours". If <paramref name="withVerb"/> is true,
        /// the pronoun will include a possessive verb - that is, "I have" or "she has".
        /// </summary>
        /// <param name="withVerb">Whether or not to include the connective verb.</param>
        /// <returns>The pronoun.</returns>
        string GetPossessiveForm(bool withVerb = false);

        /// <summary>
        /// Gets the pronoun in reflexive form, that is, "myself" or "yourself.
        /// </summary>
        /// <returns>The pronoun.</returns>
        string GetReflexiveForm();

        /// <summary>
        /// Gets the specified pronoun form.
        /// </summary>
        /// <param name="form">The form to get.</param>
        /// <returns>The pronoun.</returns>
        string GetForm(PronounForm form);
    }
}
