//
//  OpenLocalStream.cs
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
        public class OpenLocalStream : ContentServiceTestBase
        {
            private readonly UPath _testFile = UPath.Combine(UPath.Root, "test");
            private readonly UPath _invalidTestFile = UPath.Combine("test");

            [Fact]
            public void ReturnsFalseIfFileDoesNotExist()
            {
                var result = this.ContentService.OpenLocalStream(_testFile);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public void ReturnsFalseIfPathIsNotAbsolute()
            {
                var result = this.ContentService.OpenLocalStream(_invalidTestFile);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public void ReturnsTrueIfFileExists()
            {
                this.FileSystem.CreateFile(_testFile).Dispose();

                var result = this.ContentService.OpenLocalStream(_testFile);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public void ActuallyReturnsFile()
            {
                this.FileSystem.CreateFile(_testFile).Dispose();

                var result = this.ContentService.OpenLocalStream(_testFile);

                Assert.NotNull(result.Entity);
            }
        }
    }
}
