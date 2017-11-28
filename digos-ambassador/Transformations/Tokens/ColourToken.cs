//
//  PronounToken.cs
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
using DIGOS.Ambassador.Database.Appearances;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Services;

namespace DIGOS.Ambassador.Transformations
{
	/// <summary>
	/// A token that gets replaced with a possessive pronoun
	/// </summary>
	[TokenIdentifier("colour")]
	public class ColourToken : ReplacableTextToken<ColourToken>
	{
		/// <summary>
		/// Gets the form of the pronoun.
		/// </summary>
		public string Part { get; private set; }

		/// <inheritdoc />
		public override string GetText(Character character, Transformation transformation)
		{
			switch (this.Part)
			{
				case "base":
				{
					return character.GetBodypart(transformation.Part).BaseColour.ToString();
				}
				case "pattern":
				{
					return character.GetBodypart(transformation.Part).PatternColour?.ToString();
				}
				default:
				{
					throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <inheritdoc />
		protected override ColourToken Initialize(string data)
		{
			if (data is null)
			{
				this.Part = "base";
				return this;
			}

			if (data.Equals("base") | data.Equals("pattern"))
			{
				this.Part = data;
			}

			return this;
		}
	}
}
