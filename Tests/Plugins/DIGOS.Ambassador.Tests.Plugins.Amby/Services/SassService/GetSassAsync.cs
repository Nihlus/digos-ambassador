//
//  GetSassAsync.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using DIGOS.Ambassador.Tests.Plugins.Amby.Bases;
using Xunit;
using Zio;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Amby.Services.SassService;

public static class SassServiceTests
{
    public class GetSassAsync : SassServiceTestBase
    {
        protected override async Task ConfigureFileSystemAsync(IFileSystem fileSystem)
        {
            var sassDirectory = UPath.Combine(UPath.Root, "Sass");
            var sfwSass = UPath.Combine(sassDirectory, "sass.txt");
            var nsfwSass = UPath.Combine(sassDirectory, "sass-nsfw.txt");

            fileSystem.CreateDirectory(sassDirectory);

            await using (var sassFile = fileSystem.OpenFile(sfwSass, FileMode.Create, FileAccess.Write))
            {
                await using var sw = new StreamWriter(sassFile);
                await sw.WriteLineAsync("SFW Sass");
            }

            await using (var sassFile = fileSystem.OpenFile(nsfwSass, FileMode.Create, FileAccess.Write))
            {
                await using var sw = new StreamWriter(sassFile);
                await sw.WriteLineAsync("NSFW Sass");
            }
        }

        [Fact]
        public async Task ReturnsTrueWhenSassIsAvailable()
        {
            var result = await this.SassService.GetSassAsync();

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsTrueWhenSassIsAvailableIncludingNSFWSass()
        {
            var result = await this.SassService.GetSassAsync(true);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ActuallyReturnsSomeSass()
        {
            var result = await this.SassService.GetSassAsync();

            Assert.NotEmpty(result.Entity);
        }
    }

    public class GetSassWithoutContent : SassServiceTestBase
    {
        [Fact]
        public async Task ReturnsFalseIfNoSassIsAvailable()
        {
            var result = await this.SassService.GetSassAsync();

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsFalseIfNoSassIsAvailableIncludingNSFWSass()
        {
            var result = await this.SassService.GetSassAsync();

            Assert.False(result.IsSuccess);
        }
    }
}
