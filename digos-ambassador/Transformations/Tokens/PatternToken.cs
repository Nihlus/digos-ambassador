//
//  PatternToken.cs
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

using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Transformations;
using Humanizer;

namespace DIGOS.Ambassador.Transformations
{
	/// <summary>
	/// A token that gets replaced with a pattern name.
	/// </summary>
	[TokenIdentifier("pattern")]
	public class PatternToken : ReplacableTextToken<PatternToken>
	{
		/// <inheritdoc />
		public override string GetText(Character character, Transformation transformation)
		{
			var currentBodypart = character.GetBodypart(transformation.Part);
			return currentBodypart.Transformation.DefaultPattern?.ToString().Humanize();
		}

		/// <inheritdoc />
		protected override PatternToken Initialize(string data)
		{
			return this;
		}
	}
}
