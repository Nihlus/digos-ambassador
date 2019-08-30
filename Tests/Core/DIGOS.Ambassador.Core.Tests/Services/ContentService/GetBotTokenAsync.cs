//
//  GetBotTokenAsync.cs
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

using System.IO;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Tests.Bases;
using Xunit;
using Zio;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Core.Tests.Services.ContentService
{
    public static partial class ContentServiceTests
    {
        public class GetBotTokenAsync : ContentServiceTestBase
        {
            private readonly UPath _tokenDirectory;
            private readonly UPath _tokenPath;

            public GetBotTokenAsync()
            {
                _tokenDirectory = UPath.Combine(UPath.Root, "Discord");
                _tokenPath = UPath.Combine(_tokenDirectory, "bot.token");
            }

            [Fact]
            public async Task ReturnsFalseIfTokenFileDoesNotExist()
            {
                var result = await this.ContentService.GetBotTokenAsync();

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsFalseIfTokenFileExistsButContainsNoValidToken()
            {
                this.FileSystem.CreateDirectory(_tokenDirectory);
                this.FileSystem.CreateFile(_tokenPath).Dispose();

                var result = await this.ContentService.GetBotTokenAsync();

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsTrueIfTokenFileExistsAndContainsValidToken()
            {
                this.FileSystem.CreateDirectory(_tokenDirectory);
                using (var sw = new StreamWriter(this.FileSystem.CreateFile(_tokenPath)))
                {
                    await sw.WriteLineAsync("02f978n90908nsaf908n2908n2908n");
                }

                var result = await this.ContentService.GetBotTokenAsync();

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ActuallyReturnsToken()
            {
                this.FileSystem.CreateDirectory(_tokenDirectory);
                using (var sw = new StreamWriter(this.FileSystem.CreateFile(_tokenPath)))
                {
                    await sw.WriteLineAsync("02f978n90908nsaf908n2908n2908n");
                }

                var result = await this.ContentService.GetBotTokenAsync();

                Assert.NotNull(result.Entity);

                Assert.Equal("02f978n90908nsaf908n2908n2908n\n", result.Entity);
            }
        }
    }
}
