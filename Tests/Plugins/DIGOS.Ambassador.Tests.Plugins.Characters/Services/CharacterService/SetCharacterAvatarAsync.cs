//
//  SetCharacterAvatarAsync.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649
#pragma warning disable CS8625

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Characters;

public partial class CharacterServiceTests
{
    public class SetCharacterAvatarAsync : CharacterServiceTestBase
    {
        private const string _avatarURL = "http://fake.com/avatar.png";

        private readonly Character _character;

        public SetCharacterAvatarAsync()
        {
            _character = CreateCharacter(avatarUrl: _avatarURL);
        }

        [Fact]
        public async Task ReturnsUnsuccessfulResultIfAvatarURLIsEmpty()
        {
            var result = await this.CharacterEditor.SetCharacterAvatarAsync(_character, string.Empty);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsUnsuccessfulResultIfAvatarURLIsTheSameAsTheCurrentURL()
        {
            var result = await this.CharacterEditor.SetCharacterAvatarAsync(_character, _avatarURL);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsSuccessfulResultIfURLIsAccepted()
        {
            var result = await this.CharacterEditor.SetCharacterAvatarAsync(_character, "http://www.myfunkyavatars.com/avatar.png");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task SetsURL()
        {
            const string newURL = "http://www.myfunkyavatars.com/avatar.png";
            await this.CharacterEditor.SetCharacterAvatarAsync(_character, newURL);

            var character = this.Database.Characters.First();
            Assert.Equal(newURL, character.AvatarUrl);
        }
    }
}
