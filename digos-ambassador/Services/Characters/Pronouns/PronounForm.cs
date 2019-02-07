//
//  PronounForm.cs
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

namespace DIGOS.Ambassador.Services
{
    /// <summary>
    /// Holds different forms that a pronoun can have.
    /// </summary>
    public enum PronounForm
    {
        /// <summary>
        /// The subject form, that is, "I" or "you".
        /// </summary>
        Subject,

        /// <summary>
        /// The subject form but with a verb, that is, "I am" or "you are".
        /// </summary>
        SubjectVerb,

        /// <summary>
        /// The object form, that is, "me" or "you.
        /// </summary>
        Object,

        /// <summary>
        /// The possessive adjective form, that is, "my" or "yours"
        /// </summary>
        PossessiveAdjective,

        /// <summary>
        /// The possessive form, that is, "mine" or "yours".
        /// </summary>
        Possessive,

        /// <summary>
        /// The possessive form but with a verb, that is, "I have", or "you have".
        /// </summary>
        PossessiveVerb,

        /// <summary>
        /// The reflexive form, that is, "myself" or "yourself".
        /// </summary>
        Reflexive
    }
}
