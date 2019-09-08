//
//  SetCharacterNicknameAsync.cs
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
    public partial class CharacterServiceTests
    {
        public class SetCharacterNicknameAsync : CharacterServiceTestBase
        {
            private const string Nickname = "Nicke";

            private readonly IUser _user = MockHelper.CreateDiscordUser(0);

            private User _owner;
            private Character _character;

            public override async Task InitializeAsync()
            {
                _owner = (await this.Users.GetOrRegisterUserAsync(_user)).Entity;
                _character = new Character(_owner, "Dummy", string.Empty, nickname: Nickname)
                {
                    ServerID = 0,
                };

                this.Database.Characters.Update(_character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNicknameIsNull()
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = await this.Characters.SetCharacterNicknameAsync(_character, null);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNicknameIsEmpty()
            {
                var result = await this.Characters.SetCharacterNicknameAsync(_character, string.Empty);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNicknameIsTheSameAsTheCurrentNickname()
            {
                var result = await this.Characters.SetCharacterNicknameAsync(_character, Nickname);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNewNicknameIsLongerThan32Characters()
            {
                var result = await this.Characters.SetCharacterNicknameAsync(_character, new string('a', 33));

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfNicknameIsAccepted()
            {
                var result = await this.Characters.SetCharacterNicknameAsync(_character, "Bobby");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task SetsNickname()
            {
                const string newNickname = "Bobby";
                await this.Characters.SetCharacterNicknameAsync(_character, newNickname);

                var character = this.Database.Characters.First();
                Assert.Equal(newNickname, character.Nickname);
            }
        }
    }
}
