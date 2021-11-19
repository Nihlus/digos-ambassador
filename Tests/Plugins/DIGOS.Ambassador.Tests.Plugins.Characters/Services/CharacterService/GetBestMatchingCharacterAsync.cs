//
//  GetBestMatchingCharacterAsync.cs
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
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Characters;

public static partial class CharacterServiceTests
{
    public class GetBestMatchingCharacterAsync : CharacterServiceTestBase
    {
        private const string CharacterName = "Test";

        private readonly Character _character;

        public GetBestMatchingCharacterAsync()
        {
            _character = CreateCharacter
            (
                this.DefaultOwner,
                this.DefaultServer,
                CharacterName,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            );
        }

        /*
         * Unsuccessful assertions
         */

        [Fact]
        public async Task ReturnsUnsuccessfulResultIfOwnerIsNullAndNameIsNullAndNoCharacterIsCurrent()
        {
            var result = await this.Characters.GetBestMatchingCharacterAsync(this.DefaultServer, null, null);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsUnsuccessfulResultIfOwnerIsNullAndNoACharacterWithThatNameExists()
        {
            var result = await this.Characters.GetBestMatchingCharacterAsync(this.DefaultServer, null, "NonExistant");

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsUnsuccessfulResultIfNameIsNullAndOwnerDoesNotHaveACurrentCharacter()
        {
            var result = await this.Characters.GetBestMatchingCharacterAsync(this.DefaultServer, this.DefaultOwner, null);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsUnsuccessfulResultIfNameIsEmptyAndOwnerDoesNotHaveACurrentCharacter()
        {
            var result = await this.Characters.GetBestMatchingCharacterAsync(this.DefaultServer, this.DefaultOwner, string.Empty);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsUnsuccessfulResultIfOwnerIsNotNullAndNameIsNotNullAndUserDoesNotHaveACharacterWithThatName()
        {
            var result = await this.Characters.GetBestMatchingCharacterAsync(this.DefaultServer, this.DefaultOwner, "NonExistant");

            Assert.False(result.IsSuccess);
        }

        /*
         * Successful assertions
         */

        [Fact]
        public async Task ReturnsSuccessfulResultIfOwnerIsNullAndASingleCharacterWithThatNameExists()
        {
            var result = await this.Characters.GetBestMatchingCharacterAsync(this.DefaultServer, null, CharacterName);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsSuccessfulResultIfNameIsNullAndOwnerHasACurrentCharacter()
        {
            await this.Characters.MakeCharacterCurrentAsync(this.DefaultOwner, this.DefaultServer, _character);

            var result = await this.Characters.GetBestMatchingCharacterAsync(this.DefaultServer, this.DefaultOwner, null);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsSuccessfulResultIfNameIsEmptyAndOwnerHasACurrentCharacter()
        {
            await this.Characters.MakeCharacterCurrentAsync(this.DefaultOwner, this.DefaultServer, _character);

            var result = await this.Characters.GetBestMatchingCharacterAsync(this.DefaultServer, this.DefaultOwner, string.Empty);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsSuccessfulResultIfOwnerIsNotNullAndNameIsNotNullAndOwnerHasACharacterWithThatName()
        {
            var result = await this.Characters.GetBestMatchingCharacterAsync(this.DefaultServer, this.DefaultOwner, CharacterName);

            Assert.True(result.IsSuccess);
        }

        /*
         * Correctness assertions
         */

        [Fact]
        public async Task ReturnsCorrectCharacterIfOwnerIsNullAndASingleCharacterWithThatNameExists()
        {
            var result = await this.Characters.GetBestMatchingCharacterAsync(this.DefaultServer, null, CharacterName);

            Assert.Same(_character, result.Entity);
        }

        [Fact]
        public async Task ReturnsCurrentCharacterIfNameIsNullAndOwnerHasACurrentCharacter()
        {
            await this.Characters.MakeCharacterCurrentAsync(this.DefaultOwner, this.DefaultServer, _character);

            var result = await this.Characters.GetBestMatchingCharacterAsync(this.DefaultServer, this.DefaultOwner, null);

            Assert.Same(_character, result.Entity);
        }

        [Fact]
        public async Task ReturnsCurrentCharacterIfNameIsEmptyAndOwnerHasACurrentCharacter()
        {
            await this.Characters.MakeCharacterCurrentAsync(this.DefaultOwner, this.DefaultServer, _character);

            var result = await this.Characters.GetBestMatchingCharacterAsync(this.DefaultServer, this.DefaultOwner, string.Empty);

            Assert.Same(_character, result.Entity);
        }

        [Fact]
        public async Task ReturnsCorrectCharacterIfOwnerIsNotNullAndNameIsNotNullAndOwnerHasACharacterWithThatName()
        {
            var result = await this.Characters.GetBestMatchingCharacterAsync(this.DefaultServer, this.DefaultOwner, CharacterName);

            Assert.Same(_character, result.Entity);
        }
    }
}
