//
//  GetCharacters.cs
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
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Characters
{
    public static partial class CharacterServiceTests
    {
        public class GetCharacters : CharacterServiceTestBase
        {
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(0);
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);

            private User _owner;

            public override async Task InitializeAsync()
            {
                _owner = (await this.Users.GetOrRegisterUserAsync(_user)).Entity;
            }

            [Fact]
            public void ReturnsNoCharactersFromEmptyDatabase()
            {
                var result = this.Characters.GetCharacters(_guild);

                Assert.Empty(result);
            }

            [Fact]
            public void ReturnsSingleCharacterFromSingleCharacterOnServer()
            {
                this.Database.Characters.Update
                (
                    new Character(_owner, "dummy", string.Empty)
                    {
                        ServerID = (long)_guild.Id
                    }
                );

                this.Database.SaveChanges();

                var result = this.Characters.GetCharacters(_guild);

                Assert.NotEmpty(result);
                Assert.Single(result);
            }

            [Fact]
            public void ReturnsNoCharacterFromSingleCharacterOnServerWhenRequestedServerIsDifferent()
            {
                this.Database.Characters.Update
                (
                    new Character(_owner, "dummy", string.Empty)
                    {
                        ServerID = 1
                    }
                );

                this.Database.SaveChanges();

                var result = this.Characters.GetCharacters(_guild);

                Assert.Empty(result);
            }

            [Fact]
            public void ReturnsCorrectCharactersFromDatabase()
            {
                this.Database.Characters.Update
                (
                    new Character(_owner, "dummy", string.Empty)
                    {
                        ServerID = 1
                    }
                );

                this.Database.Characters.Update
                (
                    new Character(_owner, "dummy", string.Empty)
                    {
                        ServerID = (long)_guild.Id
                    }
                );

                this.Database.Characters.Update
                (
                    new Character(_owner, "dummy", string.Empty)
                    {
                        ServerID = (long)_guild.Id
                    }
                );

                this.Database.SaveChanges();

                var result = this.Characters.GetCharacters(_guild);

                Assert.NotEmpty(result);
                Assert.Equal(2, result.Count());
            }
        }
    }
}
