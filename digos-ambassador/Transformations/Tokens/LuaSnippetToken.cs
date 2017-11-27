//
//  LuaSnippetToken.cs
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
	/// Represents a token which executes an inline snippet of lua code and gets replaced with the result.
	/// </summary>
	[TokenIdentifier("snippet")]
	public class LuaSnippetToken : ReplacableTextToken<LuaSnippetToken>
	{
		/// <summary>
		/// Gets the snippet of lua code to execute.
		/// </summary>
		public string Snippet { get; private set; }

		/// <inheritdoc />
		public override string GetText(Character character)
		{
			throw new System.NotImplementedException();
		}

		/// <inheritdoc />
		protected override LuaSnippetToken Initialize(string data)
		{
			this.Snippet = data ?? string.Empty;
			return this;
		}
	}
}
