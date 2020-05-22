//
//  GetUserCharacters.cs
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
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using MoreLinq;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Characters
{
    public partial class CharacterServiceTests
    {
        public class GetUserCharacters : CharacterServiceTestBase
        {
            [Fact]
            public void ReturnsEmptySetFromEmptyDatabase()
            {
                Assert.Empty(this.Characters.GetUserCharacters(this.DefaultOwner, this.DefaultServer));
            }

            [Fact]
            public void ReturnsEmptySetFromDatabaseWithCharactersWithNoMatchingOwner()
            {
                var anotherCharacter = new Character
                (
                    new User(1),
                    this.DefaultServer,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty
                );

                this.Database.Characters.Update(anotherCharacter);
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(this.DefaultOwner, this.DefaultServer);
                Assert.Empty(result);
            }

            [Fact]
            public void ReturnsEmptySetFromDatabaseWithCharactersWithMatchingOwnerButNoMatchingServer()
            {
                var anotherCharacter = new Character
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

                this.Database.Characters.Update(anotherCharacter);
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(this.DefaultOwner, this.DefaultServer);
                Assert.Empty(result);
            }

            [Fact]
            public void ReturnsNonEmptySetFromDatabaseWithCharactersWithMatchingOwner()
            {
                var anotherCharacter = new Character
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
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(this.DefaultOwner, this.DefaultServer);
                Assert.NotEmpty(result);
            }

            [Fact]
            public void ReturnsCorrectCharacterFromDatabase()
            {
                var anotherCharacter = new Character
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
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(this.DefaultOwner, this.DefaultServer);
                Assert.Collection(result, c => Assert.Same(anotherCharacter, c));
            }

            [Fact]
            public async Task ReturnsCorrectMultipleCharactersFromDatabase()
            {
                var character1 = await CreateCharacterAsync(name: "dummy1");
                var character2 = await CreateCharacterAsync(name: "dummy2");

                this.Database.Update(character1);
                this.Database.Update(character2);
                await this.Database.SaveChangesAsync();

                var result = this.Characters.GetUserCharacters(this.DefaultOwner, this.DefaultServer);

                Assert.Equal(2, result.Count());
                Assert.Contains(character1, result);
                Assert.Contains(character2, result);
            }
        }
    }
}
