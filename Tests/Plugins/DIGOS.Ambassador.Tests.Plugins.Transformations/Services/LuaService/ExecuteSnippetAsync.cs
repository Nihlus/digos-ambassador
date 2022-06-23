﻿//
//  ExecuteSnippetAsync.cs
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

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Plugins.Transformations.Services.Lua;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Transformations;

public class LuaServiceTests
{
    public class ExecuteSnippetAsync
    {
        private readonly LuaService _lua;

        public ExecuteSnippetAsync()
        {
            _lua = new LuaService(new ContentService(FileSystemFactory.CreateContentFileSystem()));
        }

        [Fact]
        public async Task CanExecuteSimpleCode()
        {
            const string code = "return \"test\"";

            var result = await _lua.ExecuteSnippetAsync(code);
            Assert.True(result.IsSuccess);
            Assert.Equal("test", result.Entity);
        }

        [Fact]
        public async Task TimesOutLongRunningScripts()
        {
            const string code = "while (true) do end";

            var result = await _lua.ExecuteSnippetAsync(code);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ScriptUsingAPINotOnWhitelistFails()
        {
            const string code = "setfenv({})";

            var result = await _lua.ExecuteSnippetAsync(code);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ScriptUsingAPIOnWhitelistSucceeds()
        {
            const string code = "return string.lower(\"ABC\")";

            var result = await _lua.ExecuteSnippetAsync(code);
            Assert.True(result.IsSuccess);
            Assert.Equal("abc", result.Entity);
        }

        [Fact]
        public async Task CanAccessPassedVariables()
        {
            const int variable = 10;
            const string code = "return variable";

            var result = await _lua.ExecuteSnippetAsync(code, (nameof(variable), variable));
            Assert.True(result.IsSuccess);
            Assert.Equal("10", result.Entity);
        }
    }
}
