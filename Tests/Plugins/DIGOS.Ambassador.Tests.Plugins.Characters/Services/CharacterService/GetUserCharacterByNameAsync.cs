//
//  GetUserCharacterByNameAsync.cs
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
        public class GetUserCharacterByNameAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";

            private readonly ICommandContext _context;
            private readonly IGuildUser _owner = MockHelper.CreateDiscordGuildUser(0);

            private readonly Character _character;

            private readonly User _dbOwner;

            public GetUserCharacterByNameAsync()
            {
                _dbOwner = new User((long)_owner.Id);

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

                var guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.User).Returns(_owner);
                mockedContext.Setup(c => c.Guild).Returns(guild);

                var mockedMessage = new Mock<IUserMessage>();
                mockedMessage.Setup(m => m.Author).Returns(_owner);

                mockedContext.Setup(c => c.Message).Returns(mockedMessage.Object);

                _context = mockedContext.Object;

                _character = new Character(_dbOwner, CharacterName, string.Empty)
                {
                    ServerID = (long)guild.Id
                };

                this.Database.Characters.Update(_character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfOwnerDoesNotHaveACharacterWithThatName()
            {
                var result = await this.Characters.GetUserCharacterByNameAsync(_context, _dbOwner, "NonExistant");

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfOwnerHasACharacterWithThatName()
            {
                var result = await this.Characters.GetUserCharacterByNameAsync(_context, _dbOwner, CharacterName);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsCorrectCharacter()
            {
                var result = await this.Characters.GetUserCharacterByNameAsync(_context, _dbOwner, CharacterName);

                Assert.Same(_character, result.Entity);
            }
        }
    }
}
