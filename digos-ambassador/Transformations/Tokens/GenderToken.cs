//
//  GenderToken.cs
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
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Services;
using static DIGOS.Ambassador.Services.Bodypart;

namespace DIGOS.Ambassador.Transformations
{
	/// <summary>
	/// A token that gets replaced with a gender.
	/// </summary>
	[TokenIdentifier("gender", "g")]
	public class GenderToken : ReplacableTextToken<GenderToken>
	{
		/// <inheritdoc />
		public override string GetText(Character character, Transformation transformation)
		{
			var genderedParts = character.CurrentAppearance.Components
				.Where(c => !BodypartUtilities.IsGenderNeutral(c.Bodypart))
				.Select(c => c.Bodypart)
				.ToList();

			if (!genderedParts.Any())
			{
				return string.Empty;
			}

			if (genderedParts.Contains(Penis) && genderedParts.Contains(Vagina))
			{
				return "herm";
			}

			if (genderedParts.Contains(Penis))
			{
				return "male";
			}

			return "female";
		}

		/// <inheritdoc />
		protected override GenderToken Initialize(string data)
		{
			return this;
		}
	}
}
