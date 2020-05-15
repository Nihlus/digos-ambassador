//
//  IsCharacterNameUniqueForUserAsync.cs
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

using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
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
        public class IsCharacterNameUniqueForUserAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";

            private readonly IUser _owner = MockHelper.CreateDiscordUser(0);
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(1);

            private readonly User _user;

            public IsCharacterNameUniqueForUserAsync()
            {
                _user = new User((long)_owner.Id);

                var character = new Character(new Server((long)_guild.Id), _user, CharacterName);

                this.Database.Characters.Update(character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsFalseIfUserHasACharacterWithThatName()
            {
                var result = await this.Characters.IsCharacterNameUniqueForUserAsync(_user, CharacterName, _guild);

                Assert.False(result);
            }

            [Fact]
            public async Task ReturnsTrueIfUserDoesNotHaveACharacterWithThatName()
            {
                var result = await this.Characters.IsCharacterNameUniqueForUserAsync(_user, "AnotherName", _guild);

                Assert.True(result);
            }
        }
    }
}
