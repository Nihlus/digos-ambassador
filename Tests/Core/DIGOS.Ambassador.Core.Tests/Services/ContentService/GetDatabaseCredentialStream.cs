//
//  GetDatabaseCredentialStream.cs
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
        public class GetDatabaseCredentialStream : ContentServiceTestBase
        {
            protected override async Task ConfigureFileSystemAsync(IFileSystem fileSystem)
            {
                var databaseDirectory = UPath.Combine(UPath.Root, "Database");
                var credentialsPath = UPath.Combine(databaseDirectory, "database.credentials");

                fileSystem.CreateDirectory(databaseDirectory);

                await using var sw = new StreamWriter
                (
                    fileSystem.OpenFile(credentialsPath, FileMode.Create, FileAccess.Write)
                );

                await sw.WriteLineAsync("DATABASE CREDENTIALS");
            }

            [Fact]
            public void ReturnsTrueIfDatabaseCredentialFileExists()
            {
                var result = this.ContentService.GetDatabaseCredentialStream();

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public void ActuallyReturnsTheDatabaseCredentialFileStream()
            {
                var result = this.ContentService.GetDatabaseCredentialStream();

                using var stream = new StreamReader(result.Entity);
                var content = stream.ReadToEnd();

                Assert.Equal("DATABASE CREDENTIALS\n", content);
            }
        }

        public class GetDatabaseCredentialStreamWithoutContent : ContentServiceTestBase
        {
            [Fact]
            public void ReturnsFalseIfDatabaseCredentialFileDoesNotExist()
            {
                var result = this.ContentService.GetDatabaseCredentialStream();

                Assert.False(result.IsSuccess);
            }
        }
    }
}
