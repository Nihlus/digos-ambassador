//
//  LuaScriptToken.cs
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
using DIGOS.Ambassador.Plugins.Transformations.Extensions;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Services.Lua;
using JetBrains.Annotations;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Tokens
{
    /// <summary>
    /// Represents a token which executes a named lua code script and gets replaced with the result.
    /// </summary>
    [PublicAPI]
    [TokenIdentifier("script", "sc")]
    public sealed class LuaScriptToken : ReplacableTextToken<LuaScriptToken>
    {
        private readonly LuaService _lua;

        /// <summary>
        /// Gets the name of the script to execute.
        /// </summary>
        public string ScriptName { get; private set; } = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaScriptToken"/> class.
        /// </summary>
        /// <param name="luaService">The lua execution service.</param>
        public LuaScriptToken(LuaService luaService)
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
            if (component is null)
            {
                return string.Empty;
            }

            var scriptPath = ContentServiceExtensions.GetLuaScriptPath(component.Transformation, this.ScriptName);
            var result = await _lua.ExecuteScriptAsync
            (
                scriptPath,
                (nameof(appearance), appearance),
                ("character", appearance.Character),
                (nameof(component), component)
            );

            return result.IsSuccess
                ? result.Entity
                : $"[{result.Unwrap().Message}]";
        }

        /// <inheritdoc />
        protected override LuaScriptToken Initialize(string? data)
        {
            this.ScriptName = data ?? string.Empty;
            return this;
        }
    }
}
