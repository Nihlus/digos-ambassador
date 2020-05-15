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
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Discord.Commands;
using Moq;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Characters
{
    public static partial class CharacterServiceTests
    {
        public class GetBestMatchingCharacterAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";

            private readonly ICommandContext _context;
            private readonly IGuildUser _owner = MockHelper.CreateDiscordGuildUser(0);
            private readonly IGuild _guild;

            private readonly Character _character;
            private readonly User _user;

            public GetBestMatchingCharacterAsync()
            {
                _user = new User((long)_owner.Id);

                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                    (
                        c =>
                            c.GetUserAsync
                            (
                                It.IsAny<ulong>(),
                                CacheMode.AllowDownload,
                                null
                            )
                    )
                    .Returns(Task.FromResult(_owner));

                _guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.User).Returns(_owner);
                mockedContext.Setup(c => c.Guild).Returns(_guild);

                var mockedMessage = new Mock<IUserMessage>();
                mockedMessage.Setup(m => m.Author).Returns(_owner);

                mockedContext.Setup(c => c.Message).Returns(mockedMessage.Object);

                _context = mockedContext.Object;

                _character = new Character(new Server((long)_guild.Id), _user, CharacterName);

                this.Database.Characters.Update(_character);
                this.Database.SaveChanges();
            }

            /*
             * Unsuccessful assertions
             */

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfOwnerIsNullAndNameIsNullAndNoCharacterIsCurrent()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, null, null);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfOwnerIsNullAndNoACharacterWithThatNameExists()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, null, "NonExistant");

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsInvokersCharacterIfOwnerIsNullAndMoreThanOneCharacterWithThatNameExists()
            {
                var anotherCharacter = new Character(new Server((long)_guild.Id), new User(2), CharacterName);

                this.Database.Characters.Update(anotherCharacter);
                await this.Database.SaveChangesAsync();

                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, null, CharacterName);

                Assert.True(result.IsSuccess);
                Assert.Same(_character, result.Entity);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNameIsNullAndOwnerDoesNotHaveACurrentCharacter()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _user, null);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNameIsEmptyAndOwnerDoesNotHaveACurrentCharacter()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _user, string.Empty);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfOwnerIsNotNullAndNameIsNotNullAndUserDoesNotHaveACharacterWithThatName()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _user, "NonExistant");

                Assert.False(result.IsSuccess);
            }

            /*
             * Successful assertions
             */

            [Fact]
            public async Task ReturnsSuccessfulResultIfOwnerIsNullAndNameIsNullAndOwnerHasACurrentCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(_context, _guild, _character);

                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, null, null);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfOwnerIsNullAndASingleCharacterWithThatNameExists()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, null, CharacterName);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfNameIsNullAndOwnerHasACurrentCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(_context, _guild, _character);

                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _user, null);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfNameIsEmptyAndOwnerHasACurrentCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(_context, _guild, _character);

                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _user, string.Empty);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfOwnerIsNotNullAndNameIsNotNullAndOwnerHasACharacterWithThatName()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _user, CharacterName);

                Assert.True(result.IsSuccess);
            }

            /*
             * Correctness assertions
             */

            [Fact]
            public async Task ReturnsCurrentCharacterIfOwnerIsNullAndNameIsNullAndOwnerHasACurrentCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(_context, _guild, _character);

                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, null, null);

                Assert.Same(_character, result.Entity);
            }

            [Fact]
            public async Task ReturnsCorrectCharacterIfOwnerIsNullAndASingleCharacterWithThatNameExists()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, null, CharacterName);

                Assert.Same(_character, result.Entity);
            }

            [Fact]
            public async Task ReturnsCurrentCharacterIfNameIsNullAndOwnerHasACurrentCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(_context, _guild, _character);

                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _user, null);

                Assert.Same(_character, result.Entity);
            }

            [Fact]
            public async Task ReturnsCurrentCharacterIfNameIsEmptyAndOwnerHasACurrentCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(_context, _guild, _character);

                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _user, string.Empty);

                Assert.Same(_character, result.Entity);
            }

            [Fact]
            public async Task ReturnsCorrectCharacterIfOwnerIsNotNullAndNameIsNotNullAndOwnerHasACharacterWithThatName()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _user, CharacterName);

                Assert.Same(_character, result.Entity);
            }
        }
    }
}
