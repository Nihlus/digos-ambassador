//
//  LuaServiceTests.cs
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

using System.Threading.Tasks;
using DIGOS.Ambassador.Services;
using Moq;
using Xunit;

namespace DIGOS.Ambassador.Tests.ServiceTests
{
    public class LuaServiceTests
    {
        private readonly LuaService _lua;

        public LuaServiceTests()
        {
            this._lua = new LuaService(new Mock<ContentService>().Object);
        }

        [Fact]
        public async Task CanExecuteSimpleCode()
        {
            const string code = "return \"test\"";

            var result = await this._lua.ExecuteSnippetAsync(code);
            Assert.True(result.IsSuccess);
            Assert.Equal("test", result.Entity);
        }

        [Fact]
        public async Task TimesOutLongRunningScripts()
        {
            const string code = "while (true) do end";

            var result = await this._lua.ExecuteSnippetAsync(code);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ScriptUsingAPINotOnWhitelistFails()
        {
            const string code = "setfenv({})";

            var result = await this._lua.ExecuteSnippetAsync(code);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ScriptUsingAPIOnWhitelistSucceeds()
        {
            const string code = "return string.lower(\"ABC\")";

            var result = await this._lua.ExecuteSnippetAsync(code);
            Assert.True(result.IsSuccess);
            Assert.Equal("abc", result.Entity);
        }

        [Fact]
        public async Task CanAccessPassedVariables()
        {
            int variable = 10;
            const string code = "return variable";

            var result = await this._lua.ExecuteSnippetAsync(code, (nameof(variable), variable));
            Assert.True(result.IsSuccess);
            Assert.Equal("10.0", result.Entity);
        }
    }
}
