//
//  PronounProvider.cs
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
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Characters.Services.Pronouns
{
    /// <summary>
    /// Provides pronouns.
    /// </summary>
    [UsedImplicitly]
    public abstract class PronounProvider : IPronounProvider
    {
        /// <inheritdoc />
        public abstract string Family { get; }

        /// <inheritdoc />
        public abstract string GetSubjectForm(bool withVerb = false);

        /// <inheritdoc />
        public abstract string GetObjectForm();

        /// <inheritdoc />
        public abstract string GetPossessiveAdjectiveForm();

        /// <inheritdoc />
        public abstract string GetPossessiveForm(bool withVerb = false);

        /// <inheritdoc />
        public abstract string GetReflexiveForm();

        /// <inheritdoc />
        public string GetForm(PronounForm form)
        {
            switch (form)
            {
                case PronounForm.Subject:
                {
                    return GetSubjectForm();
                }
                case PronounForm.SubjectVerb:
                {
                    return GetSubjectForm(true);
                }
                case PronounForm.Object:
                {
                    return GetObjectForm();
                }
                case PronounForm.PossessiveAdjective:
                {
                    return GetPossessiveAdjectiveForm();
                }
                case PronounForm.Possessive:
                {
                    return GetPossessiveForm();
                }
                case PronounForm.PossessiveVerb:
                {
                    return GetPossessiveForm(true);
                }
                case PronounForm.Reflexive:
                {
                    return GetReflexiveForm();
                }
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(form), form, null);
                }
            }
        }
    }
}
