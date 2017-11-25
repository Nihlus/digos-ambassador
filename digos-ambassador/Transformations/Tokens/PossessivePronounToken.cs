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

namespace DIGOS.Ambassador.Transformations
{
	/// <summary>
	/// A token that gets replaced with a possessive pronoun
	/// </summary>
	[TokenIdentifier("possessive")]
	public class PossessivePronounToken : ReplacableTextToken<PossessivePronounToken>
	{
		/// <summary>
		/// Gets or sets a value indicating whether the pronoun should be in its raw form, or together with a
		/// possessive verb - that is "Her" or "She has".
		/// </summary>
		public bool UseVerb { get; set; }

		/// <inheritdoc />
		public override string GetText(Character character)
		{
			throw new System.NotImplementedException();
		}

		/// <inheritdoc />
		protected override PossessivePronounToken Initialize(string data)
		{
			this.UseVerb = data.Equals("verb");
			return this;
		}
	}
}
