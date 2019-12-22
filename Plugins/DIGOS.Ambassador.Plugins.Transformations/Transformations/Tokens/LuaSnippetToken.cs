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
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Services.Lua;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Tokens
{
    /// <summary>
    /// Represents a token which executes an inline snippet of lua code and gets replaced with the result.
    /// </summary>
    [PublicAPI]
    [TokenIdentifier("snippet", "lua", "sn")]
    public sealed class LuaSnippetToken : ReplacableTextToken<LuaSnippetToken>
    {
        [NotNull]
        private readonly LuaService _lua;

        /// <summary>
        /// Gets the snippet of lua code to execute.
        /// </summary>
        public string Snippet { get; private set; } = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaSnippetToken"/> class.
        /// </summary>
        /// <param name="luaService">The lua execution service.</param>
        public LuaSnippetToken([NotNull] LuaService luaService)
        {
            _lua = luaService;
        }

        /// <inheritdoc />
        public override string GetText(Appearance appearance, AppearanceComponent? component)
        {
            return GetTextAsync(appearance, component).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public override async Task<string> GetTextAsync(Appearance appearance, AppearanceComponent? component)
        {
            var result = await _lua.ExecuteSnippetAsync
            (
                this.Snippet,
                (nameof(appearance), appearance),
                ("character", appearance.Character),
                (nameof(component), component)
            );

            if (!result.IsSuccess)
            {
                return $"[{result.ErrorReason}]";
            }

            return result.Entity;
        }

        /// <inheritdoc />
        protected override LuaSnippetToken Initialize(string? data)
        {
            this.Snippet = data ?? string.Empty;
            return this;
        }
    }
}
