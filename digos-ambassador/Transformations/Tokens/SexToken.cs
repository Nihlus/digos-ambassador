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
using DIGOS.Ambassador.Database.Appearances;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Extensions;
using static DIGOS.Ambassador.Transformations.Bodypart;

namespace DIGOS.Ambassador.Transformations
{
	/// <summary>
	/// A token that gets replaced with a character's physical sex.
	/// </summary>
	[TokenIdentifier("sex")]
	public class SexToken : ReplacableTextToken<SexToken>
	{
		/// <inheritdoc />
		public override string GetText(Character character, AppearanceComponent component)
		{
			var genderedParts = character.CurrentAppearance.Components
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

			if (genderedParts.Contains(Penis))
			{
				return "male";
			}

			return "female";
		}

		/// <inheritdoc />
		protected override SexToken Initialize(string data)
		{
			return this;
		}
	}
}
