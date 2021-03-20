//
//  SexToken.cs
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

using System.Linq;
using DIGOS.Ambassador.Plugins.Transformations.Extensions;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using JetBrains.Annotations;
using static DIGOS.Ambassador.Plugins.Transformations.Transformations.Bodypart;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Tokens
{
    /// <summary>
    /// A token that gets replaced with a character's physical sex.
    /// </summary>
    [PublicAPI]
    [TokenIdentifier("sex")]
    public sealed class SexToken : ReplaceableTextToken<SexToken>
    {
        /// <inheritdoc />
        public override string GetText(Appearance appearance, AppearanceComponent? component)
        {
            var genderedParts = appearance.Components
                .Where(c => !c.Bodypart.IsGenderNeutral())
                .Select(c => c.Bodypart)
                .ToList();

            if (!genderedParts.Any())
            {
                return "sexless";
            }

            if (genderedParts.Contains(Penis) && genderedParts.Contains(Vagina))
            {
                return "herm";
            }

            return genderedParts.Contains(Penis)
                ? "male"
                : "female";
        }

        /// <inheritdoc />
        protected override SexToken Initialize(string? data)
        {
            return this;
        }
    }
}
