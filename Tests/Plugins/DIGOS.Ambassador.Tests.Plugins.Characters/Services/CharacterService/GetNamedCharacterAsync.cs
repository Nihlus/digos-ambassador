//
//  GetNamedCharacterAsync.cs
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
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Characters
{
    public static partial class CharacterServiceTests
    {
        public class GetNamedCharacterAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(0);
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);

            private User _owner = null!;
            private Character _character = null!;

            public override async Task InitializeAsync()
            {
                _owner = (await this.Users.GetOrRegisterUserAsync(_user)).Entity;

                _character = new Character((long)_guild.Id, _owner, CharacterName);

                this.Database.Characters.Update(_character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNoCharacterWithThatNameExists()
            {
                var result = await this.Characters.GetNamedCharacterAsync("NonExistant", _guild);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfMoreThanOneCharacterWithThatNameExists()
            {
                var anotherCharacter = _character = new Character((long)_guild.Id, _owner, CharacterName);

                this.Database.Characters.Update(anotherCharacter);
                await this.Database.SaveChangesAsync();

                var result = await this.Characters.GetNamedCharacterAsync(CharacterName, _guild);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfASingleCharacterWithThatNameExists()
            {
                var result = await this.Characters.GetNamedCharacterAsync(CharacterName, _guild);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsCorrectCharacter()
            {
                var result = await this.Characters.GetNamedCharacterAsync(CharacterName, _guild);

                Assert.Same(_character, result.Entity);
            }
        }
    }
}
