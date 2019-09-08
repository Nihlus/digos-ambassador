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

using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
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
            private readonly IUser _owner = MockHelper.CreateDiscordUser(0);
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(1);

            private readonly User _dbOwner;

            public GetUserCharacters()
            {
                _dbOwner = new User { DiscordID = (long)_owner.Id };
            }

            [Fact]
            public void ReturnsEmptySetFromEmptyDatabase()
            {
                Assert.Empty(this.Characters.GetUserCharacters(_dbOwner, _guild));
            }

            [Fact]
            public void ReturnsEmptySetFromDatabaseWithCharactersWithNoMatchingOwner()
            {
                var character = new Character(new User { DiscordID = 1 }, "Dummy", string.Empty)
                {
                    ServerID = (long)_guild.Id
                };

                this.Database.Characters.Update(character);
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(_dbOwner, _guild);
                Assert.Empty(result);
            }

            [Fact]
            public void ReturnsNEmptySetFromDatabaseWithCharactersWithMatchingOwnerButNoMatchingServer()
            {
                var character = new Character(_dbOwner, "Dummy", string.Empty);

                this.Database.Characters.Update(character);
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(_dbOwner, _guild);
                Assert.Empty(result);
            }

            [Fact]
            public void ReturnsNonEmptySetFromDatabaseWithCharactersWithMatchingOwner()
            {
                var character = new Character(_dbOwner, "Dummy", string.Empty)
                {
                    ServerID = (long)_guild.Id
                };

                this.Database.Characters.Update(character);
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(_dbOwner, _guild);
                Assert.NotEmpty(result);
            }

            [Fact]
            public void ReturnsCorrectCharacterFromDatabase()
            {
                var character = new Character(_dbOwner, "Dummy", string.Empty)
                {
                    ServerID = (long)_guild.Id
                };

                this.Database.Characters.Update(character);
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(_dbOwner, _guild);
                Assert.Collection(result, c => Assert.Same(character, c));
            }

            [Fact]
            public void ReturnsCorrectMultipleCharactersFromDatabase()
            {
                var character1 = new Character(_dbOwner, "Dummy1", string.Empty)
                {
                    ServerID = (long)_guild.Id
                };

                var character2 = new Character(_dbOwner, "Dummy2", string.Empty)
                {
                    ServerID = (long)_guild.Id
                };

                this.Database.Characters.Update(character1);
                this.Database.Characters.Update(character2);
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(_dbOwner, _guild);
                Assert.Collection
                (
                    result,
                    c => Assert.Same(character1, c),
                    c => Assert.Same(character2, c)
                );
            }
        }
    }
}
