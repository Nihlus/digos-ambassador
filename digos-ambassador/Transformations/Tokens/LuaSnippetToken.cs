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

using System.Threading.Tasks;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Services;

namespace DIGOS.Ambassador.Transformations
{
	/// <summary>
	/// Represents a token which executes an inline snippet of lua code and gets replaced with the result.
	/// </summary>
	[TokenIdentifier("snippet", "lua", "sn")]
	public class LuaSnippetToken : ReplacableTextToken<LuaSnippetToken>
	{
		private readonly LuaService Lua;

		/// <summary>
		/// Gets the snippet of lua code to execute.
		/// </summary>
		public string Snippet { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LuaSnippetToken"/> class.
		/// </summary>
		/// <param name="luaService">The lua execution service.</param>
		public LuaSnippetToken(LuaService luaService)
		{
			this.Lua = luaService;
		}

		/// <inheritdoc />
		public override string GetText(Character character, Transformation transformation)
		{
			return GetTextAsync(character, transformation).GetAwaiter().GetResult();
		}

		/// <inheritdoc />
		public override async Task<string> GetTextAsync(Character character, Transformation transformation)
		{
			var result = await this.Lua.ExecuteSnippetAsync
			(
				this.Snippet,
				(nameof(character), character),
				(nameof(transformation), transformation)
			);

			if (!result.IsSuccess)
			{
				return $"[{result.ErrorReason}]";
			}

			return result.Entity;
		}

		/// <inheritdoc />
		protected override LuaSnippetToken Initialize(string data)
		{
			this.Snippet = data ?? string.Empty;
			return this;
		}
	}
}
