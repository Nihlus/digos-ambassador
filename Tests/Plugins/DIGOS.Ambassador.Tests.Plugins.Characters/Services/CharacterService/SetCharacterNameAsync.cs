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

using System;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Plugins.Characters.CommandModules;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.TypeReaders;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Tests.TestBases;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Characters
{
    public partial class CharacterServiceTests
    {
        public class SetCharacterNameAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";
            private const string AnotherCharacterName = "Test2";

            private readonly ICommandContext _context;
            private readonly Character _character;

            public SetCharacterNameAsync()
            {
                var mockedUserObject = MockHelper.CreateDiscordGuildUser(0);

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
                    .Returns(Task.FromResult(mockedUserObject));

                var mockedGuildObject = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.User).Returns(mockedUserObject);
                mockedContext.Setup(c => c.Guild).Returns(mockedGuildObject);

                var mockedMessage = new Mock<IUserMessage>();
                mockedMessage.Setup(m => m.Author).Returns(mockedUserObject);

                mockedContext.Setup(c => c.Message).Returns(mockedMessage.Object);

                _context = mockedContext.Object;

                _character = new Character
                {
                    Name = CharacterName,
                    ServerID = (long)mockedGuildObject.Id,
                    Owner = new User { DiscordID = (long)mockedUserObject.Id }
                };

                var anotherCharacter = new Character
                {
                    Name = AnotherCharacterName,
                    ServerID = (long)mockedGuildObject.Id,
                    Owner = new User { DiscordID = (long)mockedUserObject.Id }
                };

                this.Database.Characters.Update(_character);
                this.Database.Characters.Update(anotherCharacter);
                this.Database.SaveChanges();
            }

            protected override void RegisterServices(IServiceCollection serviceCollection)
            {
                base.RegisterServices(serviceCollection);

                serviceCollection
                    .AddScoped<DiscordService>()
                    .AddScoped<UserFeedbackService>()
                    .AddScoped<InteractivityService>()
                    .AddScoped<BaseSocketClient>(p => new DiscordSocketClient())
                    .AddScoped<Random>();
            }

            public override async Task InitializeAsync()
            {
                this.Commands.AddTypeReader<Character>(new CharacterTypeReader());
                await this.Commands.AddModuleAsync<CharacterCommands>(this.Services);

                await base.InitializeAsync();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNameIsNull()
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = await this.Characters.SetCharacterNameAsync(_context, _character, null);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNameIsEmpty()
            {
                var result = await this.Characters.SetCharacterNameAsync(_context, _character, string.Empty);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterAlreadyHasThatName()
            {
                var result = await this.Characters.SetCharacterNameAsync(_context, _character, CharacterName);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNameIsNotUniqueForUser()
            {
                var result = await this.Characters.SetCharacterNameAsync(_context, _character, AnotherCharacterName);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNameIsInvalid()
            {
                var result = await this.Characters.SetCharacterNameAsync(_context, _character, "create");

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfNameIsAccepted()
            {
                var result = await this.Characters.SetCharacterNameAsync(_context, _character, "Jeff");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task SetsName()
            {
                const string validName = "Jeff";

                await this.Characters.SetCharacterNameAsync(_context, _character, validName);

                var character = this.Database.Characters.First();
                Assert.Equal(validName, character.Name);
            }
        }
    }
}
