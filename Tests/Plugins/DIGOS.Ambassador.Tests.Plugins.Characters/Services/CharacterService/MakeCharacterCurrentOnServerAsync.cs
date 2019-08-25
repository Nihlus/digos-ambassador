//
//  MakeCharacterCurrentOnServerAsync.cs
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
        public class MakeCharacterCurrentOnServerAsync : CharacterServiceTestBase
        {
            private readonly ICommandContext _context;
            private readonly IGuild _guild;
            private readonly IGuildUser _owner = MockHelper.CreateDiscordGuildUser(0);

            private readonly Character _character;

            public MakeCharacterCurrentOnServerAsync()
            {
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
                mockedContext.Setup(c => c.Guild).Returns(_guild);
                mockedContext.Setup(c => c.User).Returns(_owner);

                var mockedMessage = new Mock<IUserMessage>();
                mockedMessage.Setup(m => m.Author).Returns(_owner);

                mockedContext.Setup(c => c.Message).Returns(mockedMessage.Object);

                _context = mockedContext.Object;

                _character = new Character
                {
                    Owner = new User { DiscordID = (long)_owner.Id }
                };

                this.Database.Characters.Update(_character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfCharacterIsNotCurrent()
            {
                var result = await this.Characters.MakeCharacterCurrentOnServerAsync(_context, _guild, _character);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task MakesCharacterCurrentOnCorrectServer()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(_context, _guild, _character);

                Assert.True(_character.IsCurrent);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterIsAlreadyCurrent()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(_context, _guild, _character);
                var result = await this.Characters.MakeCharacterCurrentOnServerAsync(_context, _guild, _character);

                Assert.False(result.IsSuccess);
            }
        }
    }
}
