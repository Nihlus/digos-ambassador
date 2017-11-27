//
//  PossessivePronounToken.cs
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

namespace DIGOS.Ambassador.Transformations
{
	/// <summary>
	/// A token that gets replaced with a possessive pronoun
	/// </summary>
	[TokenIdentifier("possessive")]
	public class PossessivePronounToken : ReplacableTextToken<PossessivePronounToken>
	{
		/// <summary>
		/// Gets a value indicating whether the pronoun should be in its raw form, or together with a
		/// possessive verb - that is "Hers" or "She has".
		/// </summary>
		public bool UseVerb { get; private set; }

		/// <summary>
		/// Gets a value indicating whether the pronoun should be in its possessive adjective form, that is,
		/// "his" or "her".
		/// </summary>
		public bool UseAdjective { get; private set; }

		/// <inheritdoc />
		public override string GetText(Character character, Transformation transformation)
		{
			throw new System.NotImplementedException();
		}

		/// <inheritdoc />
		protected override PossessivePronounToken Initialize(string data)
		{
			if (data is null)
			{
				return this;
			}

			this.UseVerb = data.Equals("verb");
			this.UseAdjective = data.Equals("adjective");
			return this;
		}
	}
}
