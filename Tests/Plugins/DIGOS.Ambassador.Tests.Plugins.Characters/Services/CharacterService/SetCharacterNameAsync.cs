//
//  SetCharacterNameAsync.cs
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
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649
#pragma warning disable CS8625

namespace DIGOS.Ambassador.Tests.Plugins.Characters
{
    public partial class CharacterServiceTests
    {
        public class SetCharacterNameAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";
            private const string AnotherCharacterName = "Test2";

            private Character _character = null!;

            public override async Task InitializeAsync()
            {
                _character = CreateCharacter(name: CharacterName);
                CreateCharacter(name: AnotherCharacterName);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNameIsNull()
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = await this.Characters.SetCharacterNameAsync(_character, null);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNameIsEmpty()
            {
                var result = await this.Characters.SetCharacterNameAsync(_character, string.Empty);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterAlreadyHasThatName()
            {
                var result = await this.Characters.SetCharacterNameAsync(_character, CharacterName);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNameIsNotUnique()
            {
                var result = await this.Characters.SetCharacterNameAsync(_character, AnotherCharacterName);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfNameIsAccepted()
            {
                var result = await this.Characters.SetCharacterNameAsync(_character, "Jeff");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task SetsName()
            {
                const string validName = "Jeff";

                await this.Characters.SetCharacterNameAsync(_character, validName);

                var character = this.Database.Characters.First();
                Assert.Equal(validName, character.Name);
            }
        }
    }
}
