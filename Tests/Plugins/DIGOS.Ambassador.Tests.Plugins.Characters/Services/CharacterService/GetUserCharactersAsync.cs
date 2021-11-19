//
//  GetUserCharactersAsync.cs
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
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using Remora.Discord.Core;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Characters;

public partial class CharacterServiceTests
{
    public class GetUserCharactersAsync : CharacterServiceTestBase
    {
        [Fact]
        public async Task ReturnsEmptySetFromEmptyDatabase()
        {
            Assert.Empty(await this.Characters.GetUserCharactersAsync(this.DefaultServer, this.DefaultOwner));
        }

        [Fact]
        public async Task ReturnsEmptySetFromDatabaseWithCharactersWithNoMatchingOwner()
        {
            var anotherCharacter = CreateCharacter
            (
                new User(new Snowflake(1)),
                this.DefaultServer,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            );

            this.Database.Characters.Update(anotherCharacter);

            var result = await this.Characters.GetUserCharactersAsync(this.DefaultServer, this.DefaultOwner);
            Assert.Empty(result);
        }

        [Fact]
        public async Task ReturnsEmptySetFromDatabaseWithCharactersWithMatchingOwnerButNoMatchingServer()
        {
            var anotherCharacter = CreateCharacter
            (
                this.DefaultOwner,
                new Server(new Snowflake(2)),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            );

            this.Database.Characters.Update(anotherCharacter);

            var result = await this.Characters.GetUserCharactersAsync(this.DefaultServer, this.DefaultOwner);
            Assert.Empty(result);
        }

        [Fact]
        public async Task ReturnsNonEmptySetFromDatabaseWithCharactersWithMatchingOwner()
        {
            var anotherCharacter = CreateCharacter
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

            this.Database.Characters.Update(anotherCharacter);

            var result = await this.Characters.GetUserCharactersAsync(this.DefaultServer, this.DefaultOwner);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task ReturnsCorrectCharacterFromDatabase()
        {
            var anotherCharacter = CreateCharacter
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

            this.Database.Characters.Update(anotherCharacter);

            var result = await this.Characters.GetUserCharactersAsync(this.DefaultServer, this.DefaultOwner);
            Assert.Collection(result, c => Assert.Same(anotherCharacter, c));
        }

        [Fact]
        public async Task ReturnsCorrectMultipleCharactersFromDatabase()
        {
            var character1 = CreateCharacter(name: "dummy1");
            var character2 = CreateCharacter(name: "dummy2");

            var result = (await this.Characters.GetUserCharactersAsync(this.DefaultServer, this.DefaultOwner))
                .ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(character1, result);
            Assert.Contains(character2, result);
        }
    }
}
