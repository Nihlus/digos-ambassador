//
//  GetSass.cs
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
        public class GetSass : ContentServiceTestBase
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();
                await this.ContentService.InitializeAsync();
            }

            protected override async Task ConfigureFileSystemAsync(IFileSystem fileSystem)
            {
                var sassDirectory = UPath.Combine(UPath.Root, "Sass");
                var sfwSass = UPath.Combine(sassDirectory, "sass.txt");
                var nsfwSass = UPath.Combine(sassDirectory, "sass-nsfw.txt");

                fileSystem.CreateDirectory(sassDirectory);

                using (var sassFile = fileSystem.OpenFile(sfwSass, FileMode.Create, FileAccess.Write))
                {
                    using (var sw = new StreamWriter(sassFile))
                    {
                        await sw.WriteLineAsync("SFW Sass");
                    }
                }

                using (var sassFile = fileSystem.OpenFile(nsfwSass, FileMode.Create, FileAccess.Write))
                {
                    using (var sw = new StreamWriter(sassFile))
                    {
                        await sw.WriteLineAsync("NSFW Sass");
                    }
                }
            }

            [Fact]
            public void ReturnsTrueWhenSassIsAvailable()
            {
                var result = this.ContentService.GetSass();

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public void ReturnsTrueWhenSassIsAvailableIncludingNSFWSass()
            {
                var result = this.ContentService.GetSass(true);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public void ActuallyReturnsSomeSass()
            {
                var result = this.ContentService.GetSass();

                Assert.NotEmpty(result.Entity);
            }
        }

        public class GetSassWithOnlySFW : ContentServiceTestBase
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();
                await this.ContentService.InitializeAsync();
            }

            protected override async Task ConfigureFileSystemAsync(IFileSystem fileSystem)
            {
                var sassDirectory = UPath.Combine(UPath.Root, "Sass");
                var sfwSass = UPath.Combine(sassDirectory, "sass.txt");

                fileSystem.CreateDirectory(sassDirectory);

                using (var sassFile = fileSystem.OpenFile(sfwSass, FileMode.Create, FileAccess.Write))
                {
                    using (var sw = new StreamWriter(sassFile))
                    {
                        await sw.WriteLineAsync("SFW Sass");
                    }
                }
            }

            [Fact]
            public void ReturnsTrueWhenSassIsAvailable()
            {
                var result = this.ContentService.GetSass();

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public void ReturnsTrueWhenSFWSassIsAvailableButNotNSFWSass()
            {
                var result = this.ContentService.GetSass(true);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public void ActuallyReturnsSomeSass()
            {
                var result = this.ContentService.GetSass();

                Assert.NotEmpty(result.Entity);
            }
        }

        public class GetSassWithOnlyNSFW : ContentServiceTestBase
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();
                await this.ContentService.InitializeAsync();
            }

            protected override async Task ConfigureFileSystemAsync(IFileSystem fileSystem)
            {
                var sassDirectory = UPath.Combine(UPath.Root, "Sass");
                var sfwSass = UPath.Combine(sassDirectory, "sass.txt");

                fileSystem.CreateDirectory(sassDirectory);

                using (var sassFile = fileSystem.OpenFile(sfwSass, FileMode.Create, FileAccess.Write))
                {
                    using (var sw = new StreamWriter(sassFile))
                    {
                        await sw.WriteLineAsync("SFW Sass");
                    }
                }
            }

            [Fact]
            public void ReturnsTrueWhenSassIsAvailable()
            {
                var result = this.ContentService.GetSass();

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public void ReturnsTrueWhenNoSFWSassIsAvailableButNSFWSassIs()
            {
                var result = this.ContentService.GetSass(true);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public void ActuallyReturnsSomeSass()
            {
                var result = this.ContentService.GetSass();

                Assert.NotEmpty(result.Entity);
            }
        }

        public class GetSassWithoutContent : ContentServiceTestBase
        {
            [Fact]
            public void ReturnsFalseIfNoSassIsAvailable()
            {
                var result = this.ContentService.GetSass();

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public void ReturnsFalseIfNoSassIsAvailableIncludingNSFWSass()
            {
                var result = this.ContentService.GetSass();

                Assert.False(result.IsSuccess);
            }
        }
    }
}
