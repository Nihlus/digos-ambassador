//
//  GetCharactersAsync.cs
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

using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Characters
{
    public static partial class CharacterServiceTests
    {
        public class GetCharactersAsync : CharacterServiceTestBase
        {
            [Fact]
            public async Task ReturnsNoCharactersFromEmptyDatabase()
            {
                var result = await this.Characters.GetCharactersAsync(this.DefaultServer);

                Assert.Empty(result);
            }

            [Fact]
            public async Task ReturnsSingleCharacterFromSingleCharacter()
            {
                CreateCharacter
                (
                    this.DefaultOwner,
                    this.DefaultServer,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty
                );

                var result = (await this.Characters.GetCharactersAsync(this.DefaultServer)).ToList();

                Assert.NotEmpty(result);
                Assert.Single(result);
            }

            [Fact]
            public async Task ReturnsNoCharacterFromSingleCharacterWhenRequestedServerIsDifferent()
            {
                CreateCharacter
                (
                    this.DefaultOwner,
                    new Server(2),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty
                );

                var result = await this.Characters.GetCharactersAsync(this.DefaultServer);

                Assert.Empty(result);
            }

            [Fact]
            public async Task ReturnsCorrectCharactersFromDatabase()
            {
                CreateCharacter
                (
                    this.DefaultOwner,
                    new Server(2),
                    "dummy",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty
                );

                CreateCharacter
                (
                    this.DefaultOwner,
                    this.DefaultServer,
                    "dummy1",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty
                );

                CreateCharacter
                (
                    this.DefaultOwner,
                    this.DefaultServer,
                    "dummy2",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty
                );

                var result = (await this.Characters.GetCharactersAsync(this.DefaultServer)).ToList();

                Assert.NotEmpty(result);
                Assert.Equal(2, result.Count());
            }
        }
    }
}
