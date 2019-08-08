//
//  CharacterServiceTests.cs
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
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
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

namespace DIGOS.Ambassador.Tests.ServiceTests
{
    public class CharacterServiceTests
    {
        public class GetBestMatchingCharacterAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";

            private readonly ICommandContext _context;
            private readonly IGuildUser _owner = MockHelper.CreateDiscordGuildUser(0);
            private readonly IGuild _guild;

            private readonly Character _character;
            private readonly User _dbOwner;

            public GetBestMatchingCharacterAsync()
            {
                _dbOwner = new User { DiscordID = (long)_owner.Id };

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

                _character = new Character
                {
                    Name = CharacterName,
                    ServerID = (long)_guild.Id,
                    Owner = _dbOwner
                };

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
                var anotherCharacter = new Character
                {
                    Name = CharacterName,
                    ServerID = (long)_guild.Id,
                    Owner = new User { DiscordID = 2 }
                };

                this.Database.Characters.Update(anotherCharacter);
                await this.Database.SaveChangesAsync();

                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, null, CharacterName);

                Assert.True(result.IsSuccess);
                Assert.Same(_character, result.Entity);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNameIsNullAndOwnerDoesNotHaveACurrentCharacter()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _dbOwner, null);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNameIsEmptyAndOwnerDoesNotHaveACurrentCharacter()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _dbOwner, string.Empty);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfOwnerIsNotNullAndNameIsNotNullAndUserDoesNotHaveACharacterWithThatName()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _dbOwner, "NonExistant");

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

                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _dbOwner, null);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfNameIsEmptyAndOwnerHasACurrentCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(_context, _guild, _character);

                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _dbOwner, string.Empty);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfOwnerIsNotNullAndNameIsNotNullAndOwnerHasACharacterWithThatName()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _dbOwner, CharacterName);

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

                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _dbOwner, null);

                Assert.Same(_character, result.Entity);
            }

            [Fact]
            public async Task ReturnsCurrentCharacterIfNameIsEmptyAndOwnerHasACurrentCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(_context, _guild, _character);

                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _dbOwner, string.Empty);

                Assert.Same(_character, result.Entity);
            }

            [Fact]
            public async Task ReturnsCorrectCharacterIfOwnerIsNotNullAndNameIsNotNullAndOwnerHasACharacterWithThatName()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(_context, _dbOwner, CharacterName);

                Assert.Same(_character, result.Entity);
            }
        }

        public class GetCurrentCharacterAsync : CharacterServiceTestBase
        {
            private readonly ICommandContext _context;
            private readonly IGuild _guild;
            private readonly IGuildUser _owner = MockHelper.CreateDiscordGuildUser(0);

            private readonly Character _character;

            private readonly User _dbOwner;

            public GetCurrentCharacterAsync()
            {
                _dbOwner = new User { DiscordID = (long)_owner.Id };

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

                _character = new Character
                {
                    ServerID = (long)_guild.Id,
                    Owner = _dbOwner
                };

                this.Database.Characters.Update(_character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserDoesNotHaveAnActiveCharacter()
            {
                var result = await this.Characters.GetCurrentCharacterAsync(_context, _dbOwner);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfUserHasActiveCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(_context, _guild, _character);

                var result = await this.Characters.GetCurrentCharacterAsync(_context, _dbOwner);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsCorrectCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(_context, _guild, _character);

                var result = await this.Characters.GetCurrentCharacterAsync(_context, _dbOwner);

                Assert.Same(_character, result.Entity);
            }
        }

        public class GetNamedCharacterAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(0);
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);

            private User _owner;
            private Character _character;

            public override async Task InitializeAsync()
            {
                _owner = (await this.Users.GetOrRegisterUserAsync(_user)).Entity;

                _character = new Character
                {
                    Name = CharacterName,
                    ServerID = (long)_guild.Id,
                    Owner = _owner
                };

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
                var anotherCharacter = new Character
                {
                    Name = CharacterName,
                    ServerID = (long)_guild.Id,
                    Owner = _owner
                };

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

        public class GetCharacters : CharacterServiceTestBase
        {
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(0);
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);

            private User _owner;

            public override async Task InitializeAsync()
            {
                _owner = (await this.Users.GetOrRegisterUserAsync(_user)).Entity;
            }

            [Fact]
            public void ReturnsNoCharactersFromEmptyDatabase()
            {
                var result = this.Characters.GetCharacters(_guild);

                Assert.Empty(result);
            }

            [Fact]
            public void ReturnsSingleCharacterFromSingleCharacterOnServer()
            {
                this.Database.Characters.Update
                (
                    new Character
                    {
                        ServerID = (long)_guild.Id,
                        Owner = _owner
                    }
                );

                this.Database.SaveChanges();

                var result = this.Characters.GetCharacters(_guild);

                Assert.NotEmpty(result);
                Assert.Single(result);
            }

            [Fact]
            public void ReturnsNoCharacterFromSingleCharacterOnServerWhenRequestedServerIsDifferent()
            {
                this.Database.Characters.Update
                (
                    new Character
                    {
                        ServerID = 1,
                        Owner = _owner
                    }
                );

                this.Database.SaveChanges();

                var result = this.Characters.GetCharacters(_guild);

                Assert.Empty(result);
            }

            [Fact]
            public void ReturnsCorrectCharactersFromDatabase()
            {
                this.Database.Characters.Update
                (
                    new Character
                    {
                        ServerID = 1,
                        Owner = _owner
                    }
                );

                this.Database.Characters.Update
                (
                    new Character
                    {
                        ServerID = (long)_guild.Id,
                        Owner = _owner
                    }
                );

                this.Database.Characters.Update
                (
                    new Character
                    {
                        ServerID = (long)_guild.Id,
                        Owner = _owner
                    }
                );

                this.Database.SaveChanges();

                var result = this.Characters.GetCharacters(_guild);

                Assert.NotEmpty(result);
                Assert.Equal(2, result.Count());
            }
        }

        public class GetUserCharacterByNameAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";

            private readonly ICommandContext _context;
            private readonly IGuildUser _owner = MockHelper.CreateDiscordGuildUser(0);

            private readonly Character _character;

            private readonly User _dbOwner;

            public GetUserCharacterByNameAsync()
            {
                _dbOwner = new User { DiscordID = (long)_owner.Id };

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

                _character = new Character
                {
                    Name = CharacterName,
                    ServerID = (long)guild.Id,
                    Owner = _dbOwner
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

        public class ClearCurrentCharacterOnServerAsync : CharacterServiceTestBase
        {
            private readonly IUser _owner = MockHelper.CreateDiscordUser(0);
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(1);
            private readonly Character _character;

            private readonly User _dbOwner;

            public ClearCurrentCharacterOnServerAsync()
            {
                _dbOwner = new User { DiscordID = (long)_owner.Id };

                _character = new Character
                {
                    ServerID = (long)_guild.Id,
                    Owner = _dbOwner
                };

                this.Database.Characters.Update(_character);
                this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterIsNotCurrentOnServer()
            {
                var result = await this.Characters.ClearCurrentCharacterOnServerAsync(_dbOwner, _guild);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfCharacterIsCurrentOnServer()
            {
                _character.IsCurrent = true;
                await this.Database.SaveChangesAsync();

                var result = await this.Characters.ClearCurrentCharacterOnServerAsync(_dbOwner, _guild);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task RemovesCorrectServerFromCharacter()
            {
                _character.IsCurrent = true;
                await this.Database.SaveChangesAsync();

                await this.Characters.ClearCurrentCharacterOnServerAsync(_dbOwner, _guild);

                Assert.False(_character.IsCurrent);
            }
        }

        public class HasActiveCharacterOnServerAsync : CharacterServiceTestBase
        {
            private readonly IUser _owner = MockHelper.CreateDiscordUser(0);
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(1);

            private readonly User _dbOwner;

            public HasActiveCharacterOnServerAsync()
            {
                _dbOwner = new User { DiscordID = (long)_owner.Id };
            }

            [Fact]
            public async Task ReturnsFalseIfServerIsNull()
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                var result = await this.Characters.HasActiveCharacterOnServerAsync(_dbOwner, null);

                Assert.False(result);
            }

            [Fact]
            public async Task ReturnsFalseIfUserHasNoCharacters()
            {
                var result = await this.Characters.HasActiveCharacterOnServerAsync(_dbOwner, _guild);

                Assert.False(result);
            }

            [Fact]
            public async Task ReturnsFalseIfUserHasNoActiveCharacter()
            {
                var character = new Character
                {
                    Owner = _dbOwner,
                    ServerID = (long)_guild.Id
                };

                this.Database.Characters.Update(character);
                this.Database.SaveChanges();

                var result = await this.Characters.HasActiveCharacterOnServerAsync(_dbOwner, _guild);

                Assert.False(result);
            }

            [Fact]
            public async Task ReturnsTrueIfUserHasAnActiveCharacter()
            {
                var character = new Character
                {
                    Owner = _dbOwner,
                    ServerID = (long)_guild.Id,
                    IsCurrent = true
                };

                this.Database.Characters.Update(character);
                this.Database.SaveChanges();

                var result = await this.Characters.HasActiveCharacterOnServerAsync(_dbOwner, _guild);

                Assert.True(result);
            }
        }

        public class CreateCharacterAsync : CharacterServiceTestBase
        {
            private readonly ICommandContext _context;

            public CreateCharacterAsync()
            {
                this.Services.GetRequiredService<PronounService>().WithPronounProvider(new TheyPronounProvider());

                var mockedUser = new Mock<IUser>();
                mockedUser.Setup(u => u.Id).Returns(0);

                var mockedMessage = new Mock<IUserMessage>();
                mockedMessage.Setup(m => m.Author).Returns(mockedUser.Object);

                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Message).Returns(mockedMessage.Object);
                mockedContext.Setup(c => c.Guild).Returns(mockedGuild.Object);

                _context = mockedContext.Object;
            }

            [Fact]
            public async Task CanCreateWithNameOnly()
            {
                var result = await this.Characters.CreateCharacterAsync(_context, "Test");

                Assert.True(result.IsSuccess);
                Assert.NotEmpty(this.Database.Characters);
                Assert.Equal("Test", this.Database.Characters.First().Name);
            }
        }

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

        public class SetCharacterAvatarAsync : CharacterServiceTestBase
        {
            private const string AvatarURL = "http://fake.com/avatar.png";

            private readonly IUser _user = MockHelper.CreateDiscordUser(0);

            private User _owner;
            private Character _character;

            public override async Task InitializeAsync()
            {
                _owner = (await this.Users.GetOrRegisterUserAsync(_user)).Entity;
                _character = new Character
                {
                    AvatarUrl = AvatarURL,
                    Owner = _owner
                };

                this.Database.Characters.Update(_character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfAvatarURLIsNull()
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = await this.Characters.SetCharacterAvatarAsync(_character, null);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfAvatarURLIsEmpty()
            {
                var result = await this.Characters.SetCharacterAvatarAsync(_character, string.Empty);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfAvatarURLIsTheSameAsTheCurrentURL()
            {
                var result = await this.Characters.SetCharacterAvatarAsync(_character, AvatarURL);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfURLIsAccepted()
            {
                var result = await this.Characters.SetCharacterAvatarAsync(_character, "http://www.myfunkyavatars.com/avatar.png");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task SetsURL()
            {
                const string newURL = "http://www.myfunkyavatars.com/avatar.png";
                await this.Characters.SetCharacterAvatarAsync(_character, newURL);

                var character = this.Database.Characters.First();
                Assert.Equal(newURL, character.AvatarUrl);
            }
        }

        public class SetCharacterNicknameAsync : CharacterServiceTestBase
        {
            private const string Nickname = "Nicke";

            private readonly IUser _user = MockHelper.CreateDiscordUser(0);

            private User _owner;
            private Character _character;

            public override async Task InitializeAsync()
            {
                _owner = (await this.Users.GetOrRegisterUserAsync(_user)).Entity;
                _character = new Character
                {
                    Nickname = Nickname,
                    Owner = _owner
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

        public class SetCharacterSummaryAsync : CharacterServiceTestBase
        {
            private const string Summary = "A cool person";
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);

            private User _owner;
            private Character _character;

            public override async Task InitializeAsync()
            {
                _owner = (await this.Users.GetOrRegisterUserAsync(_user)).Entity;
                _character = new Character
                {
                    Summary = Summary,
                    Owner = _owner
                };

                this.Database.Characters.Update(_character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSummaryIsNull()
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = await this.Characters.SetCharacterSummaryAsync(_character, null);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSummaryIsEmpty()
            {
                var result = await this.Characters.SetCharacterSummaryAsync(_character, string.Empty);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSummaryIsTheSameAsTheCurrentSummary()
            {
                var result = await this.Characters.SetCharacterSummaryAsync(_character, Summary);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNewSummaryIsLongerThan240Characters()
            {
                var result = await this.Characters.SetCharacterSummaryAsync(_character, new string('a', 241));

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfSummaryIsAccepted()
            {
                var result = await this.Characters.SetCharacterSummaryAsync(_character, "Bobby");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task SetsSummary()
            {
                const string newSummary = "An uncool person";
                await this.Characters.SetCharacterSummaryAsync(_character, newSummary);

                var character = this.Database.Characters.First();
                Assert.Equal(newSummary, character.Summary);
            }
        }

        public class SetCharacterDescriptionAsync : CharacterServiceTestBase
        {
            private const string Description = "A cool person";
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);

            private User _owner;
            private Character _character;

            public override async Task InitializeAsync()
            {
                _owner = (await this.Users.GetOrRegisterUserAsync(_user)).Entity;
                _character = new Character
                {
                    Description = Description,
                    Owner = _owner
                };

                this.Database.Characters.Update(_character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfDescriptionIsNull()
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = await this.Characters.SetCharacterDescriptionAsync(_character, null);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfDescriptionIsEmpty()
            {
                var result = await this.Characters.SetCharacterDescriptionAsync(_character, string.Empty);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfDescriptionIsTheSameAsTheCurrentDescription()
            {
                var result = await this.Characters.SetCharacterDescriptionAsync(_character, Description);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfDescriptionIsAccepted()
            {
                var result = await this.Characters.SetCharacterDescriptionAsync(_character, "Bobby");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task SetsDescription()
            {
                const string newDescription = "An uncool person";
                await this.Characters.SetCharacterDescriptionAsync(_character, newDescription);

                var character = this.Database.Characters.First();
                Assert.Equal(newDescription, character.Description);
            }
        }

        public class SetCharacterPronounAsync : CharacterServiceTestBase
        {
            private const string PronounFamily = "They";
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);

            private User _owner;
            private Character _character;

            public override async Task InitializeAsync()
            {
                this.Services.GetRequiredService<PronounService>().WithPronounProvider(new TheyPronounProvider());
                this.Services.GetRequiredService<PronounService>().WithPronounProvider(new ZeHirPronounProvider());

                _owner = (await this.Users.GetOrRegisterUserAsync(_user)).Entity;
                _character = new Character
                {
                    PronounProviderFamily = PronounFamily,
                    Owner = _owner
                };

                this.Database.Characters.Update(_character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfPronounIsNull()
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = await this.Characters.SetCharacterPronounAsync(_character, null);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfPronounIsEmpty()
            {
                var result = await this.Characters.SetCharacterPronounAsync(_character, string.Empty);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfPronounIsTheSameAsTheCurrentPronoun()
            {
                var result = await this.Characters.SetCharacterPronounAsync(_character, PronounFamily);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNoMatchingPronounProviderIsFound()
            {
                var result = await this.Characters.SetCharacterPronounAsync(_character, "ahwooooga");

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfPronounIsAccepted()
            {
                var result = await this.Characters.SetCharacterPronounAsync(_character, "Ze and hir");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task SetsPronoun()
            {
                const string newPronounFamily = "Ze and hir";
                await this.Characters.SetCharacterPronounAsync(_character, newPronounFamily);

                var character = this.Database.Characters.First();
                Assert.Equal(newPronounFamily, character.PronounProviderFamily);
            }
        }

        public class SetCharacterIsNSFWAsync : CharacterServiceTestBase
        {
            private const bool IsNSFW = false;

            private readonly IUser _user = MockHelper.CreateDiscordUser(0);

            private User _owner;
            private Character _character;

            public override async Task InitializeAsync()
            {
                _owner = (await this.Users.GetOrRegisterUserAsync(_user)).Entity;
                _character = new Character
                {
                    IsNSFW = IsNSFW,
                    Owner = _owner
                };

                this.Database.Characters.Update(_character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfIsNSFWIsTheSameAsTheCurrentIsNSFW()
            {
                var result = await this.Characters.SetCharacterIsNSFWAsync(_character, IsNSFW);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfIsNSFWIsAccepted()
            {
                var result = await this.Characters.SetCharacterIsNSFWAsync(_character, true);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task SetsIsNSFW()
            {
                const bool newIsNSFW = true;
                await this.Characters.SetCharacterIsNSFWAsync(_character, newIsNSFW);

                var character = this.Database.Characters.First();
                Assert.Equal(newIsNSFW, character.IsNSFW);
            }
        }

        public class TransferCharacterOwnershipAsync : CharacterServiceTestBase
        {
            private readonly Character _character;

            private readonly IUser _originalOwner = MockHelper.CreateDiscordUser(0);
            private readonly IUser _newOwner = MockHelper.CreateDiscordUser(1);
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(2);

            private readonly User _dbOldOwner;
            private readonly User _dbNewOwner;

            public TransferCharacterOwnershipAsync()
            {
                _dbOldOwner = new User { DiscordID = (long)_originalOwner.Id };
                _dbNewOwner = new User { DiscordID = (long)_newOwner.Id };

                _character = new Character
                {
                    ServerID = (long)_guild.Id,
                    Owner = _dbOldOwner
                };

                this.Database.Characters.Update(_character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfCharacterIsTransferred()
            {
                var result = await this.Characters.TransferCharacterOwnershipAsync(_dbNewOwner, _character, _guild);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task TransfersCharacter()
            {
                await this.Characters.TransferCharacterOwnershipAsync(_dbNewOwner, _character, _guild);

                var character = this.Database.Characters.First();
                Assert.Equal((long)_newOwner.Id, character.Owner.DiscordID);
            }
        }

        public class GetUserCharacters : CharacterServiceTestBase
        {
            private readonly IUser _owner = MockHelper.CreateDiscordUser(0);
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(1);

            private readonly User _dbOwner;

            public GetUserCharacters()
            {
                _dbOwner = new User { DiscordID = (long)_owner.Id };
            }

            [Fact]
            public void ReturnsEmptySetFromEmptyDatabase()
            {
                Assert.Empty(this.Characters.GetUserCharacters(_dbOwner, _guild));
            }

            [Fact]
            public void ReturnsEmptySetFromDatabaseWithCharactersWithNoMatchingOwner()
            {
                var character = new Character
                {
                    Owner = new User { DiscordID = 1 },
                    ServerID = (long)_guild.Id
                };

                this.Database.Characters.Update(character);
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(_dbOwner, _guild);
                Assert.Empty(result);
            }

            [Fact]
            public void ReturnsNEmptySetFromDatabaseWithCharactersWithMatchingOwnerButNoMatchingServer()
            {
                var character = new Character
                {
                    Owner = _dbOwner
                };

                this.Database.Characters.Update(character);
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(_dbOwner, _guild);
                Assert.Empty(result);
            }

            [Fact]
            public void ReturnsNonEmptySetFromDatabaseWithCharactersWithMatchingOwner()
            {
                var character = new Character
                {
                    Owner = _dbOwner,
                    ServerID = (long)_guild.Id
                };

                this.Database.Characters.Update(character);
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(_dbOwner, _guild);
                Assert.NotEmpty(result);
            }

            [Fact]
            public void ReturnsCorrectCharacterFromDatabase()
            {
                var character = new Character
                {
                    Owner = _dbOwner,
                    ServerID = (long)_guild.Id
                };

                this.Database.Characters.Update(character);
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(_dbOwner, _guild);
                Assert.Collection(result, c => Assert.Same(character, c));
            }

            [Fact]
            public void ReturnsCorrectMultipleCharactersFromDatabase()
            {
                var character1 = new Character
                {
                    Owner = _dbOwner,
                    ServerID = (long)_guild.Id
                };

                var character2 = new Character
                {
                    Owner = _dbOwner,
                    ServerID = (long)_guild.Id
                };

                this.Database.Characters.Update(character1);
                this.Database.Characters.Update(character2);
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(_dbOwner, _guild);
                Assert.Collection
                (
                    result,
                    c => Assert.Same(character1, c),
                    c => Assert.Same(character2, c)
                );
            }
        }

        public class IsCharacterNameUniqueForUserAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";

            private readonly IUser _owner = MockHelper.CreateDiscordUser(0);
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(1);

            private readonly User _dbOwner;

            public IsCharacterNameUniqueForUserAsync()
            {
                _dbOwner = new User { DiscordID = (long)_owner.Id };

                var character = new Character
                {
                    Name = CharacterName,
                    Owner = _dbOwner,
                    ServerID = (long)_guild.Id
                };

                this.Database.Characters.Update(character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsFalseIfUserHasACharacterWithThatName()
            {
                var result = await this.Characters.IsCharacterNameUniqueForUserAsync(_dbOwner, CharacterName, _guild);

                Assert.False(result);
            }

            [Fact]
            public async Task ReturnsTrueIfUserDoesNotHaveACharacterWithThatName()
            {
                var result = await this.Characters.IsCharacterNameUniqueForUserAsync(_dbOwner, "AnotherName", _guild);

                Assert.True(result);
            }
        }

        public class SetDefaultCharacterForUserAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";

            private readonly IUser _owner = MockHelper.CreateDiscordUser(0);
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(1);

            private User _user;
            private Character _character;

            public override async Task InitializeAsync()
            {
                _user = (await this.Users.GetOrRegisterUserAsync(_owner)).Entity;

                _character = new Character
                {
                    Name = CharacterName,
                    Owner = _user,
                    ServerID = (long)_guild.Id
                };

                this.Database.Characters.Update(_character);
                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task CanSetDefaultCharacter()
            {
                var contextMock = new Mock<ICommandContext>();
                contextMock.Setup(c => c.Message.Author.Id).Returns(_owner.Id);
                contextMock.Setup(c => c.Guild).Returns(_guild);

                var context = contextMock.Object;

                var result = await this.Characters.SetDefaultCharacterForUserAsync
                (
                    context,
                    _character,
                    _user
                );

                Assert.True(result.IsSuccess);

                var defaultCharacterResult = await this.Characters.GetDefaultCharacterAsync
                (
                    _user,
                    _guild
                );

                Assert.Same(_character, defaultCharacterResult.Entity);
            }

            [Fact]
            public async Task ReturnsErrorIfDefaultCharacterIsAlreadySetToTheSameCharacter()
            {
                var contextMock = new Mock<ICommandContext>();
                contextMock.Setup(c => c.Message.Author.Id).Returns(_owner.Id);
                contextMock.Setup(c => c.Guild).Returns(_guild);

                var context = contextMock.Object;

                await this.Characters.SetDefaultCharacterForUserAsync
                (
                    context,
                    _character,
                    _user
                );

                var result = await this.Characters.SetDefaultCharacterForUserAsync
                (
                    context,
                    _character,
                    _user
                );

                Assert.False(result.IsSuccess);

                var defaultCharacterResult = await this.Characters.GetDefaultCharacterAsync
                (
                    _user,
                    _guild
                );

                Assert.Same(_character, defaultCharacterResult.Entity);
            }
        }

        public class ClearDefaultCharacterForUserAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";

            private readonly IUser _owner = MockHelper.CreateDiscordUser(0);
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(1);

            private User _user;
            private Character _character;

            public override async Task InitializeAsync()
            {
                _user = (await this.Users.GetOrRegisterUserAsync(_owner)).Entity;

                _character = new Character
                {
                    Name = CharacterName,
                    Owner = _user,
                    ServerID = (long)_guild.Id
                };

                this.Database.Characters.Update(_character);
                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task CanClearDefaultCharacter()
            {
                var contextMock = new Mock<ICommandContext>();
                contextMock.Setup(c => c.Message.Author.Id).Returns(_owner.Id);
                contextMock.Setup(c => c.Guild).Returns(_guild);

                var context = contextMock.Object;

                await this.Characters.SetDefaultCharacterForUserAsync
                (
                    context,
                    _character,
                    _user
                );

                var result = await this.Characters.ClearDefaultCharacterForUserAsync
                (
                    context,
                    _user
                );

                Assert.True(result.IsSuccess);

                var defaultCharacterResult = await this.Characters.GetDefaultCharacterAsync
                (
                    _user,
                    _guild
                );

                Assert.False(defaultCharacterResult.IsSuccess);
            }

            [Fact]
            public async Task ReturnsErrorIfDefaultCharacterIsNotSet()
            {
                var contextMock = new Mock<ICommandContext>();
                contextMock.Setup(c => c.Message.Author.Id).Returns(_owner.Id);
                contextMock.Setup(c => c.Guild).Returns(_guild);

                var context = contextMock.Object;

                var result = await this.Characters.ClearDefaultCharacterForUserAsync
                (
                    context,
                    _user
                );

                Assert.False(result.IsSuccess);

                var defaultCharacterResult = await this.Characters.GetDefaultCharacterAsync
                (
                    _user,
                    _guild
                );

                Assert.False(defaultCharacterResult.IsSuccess);
            }
        }

        public class CreateCharacterRoleAsync : CharacterServiceTestBase
        {
            private readonly IGuild _discordGuild;
            private readonly IRole _discordRole;

            public CreateCharacterRoleAsync()
            {
                _discordGuild = MockHelper.CreateDiscordGuild(0);
                _discordRole = MockHelper.CreateDiscordRole(1, _discordGuild);
            }

            [Fact]
            public async Task CanCreateRole()
            {
                var result = await this.Characters.CreateCharacterRoleAsync
                (
                    _discordRole,
                    RoleAccess.Open
                );

                Assert.True(result.IsSuccess);
                Assert.Equal((long)_discordRole.Id, result.Entity.DiscordID);
            }

            [Fact]
            public async Task CreatedRoleHasCorrectAccess()
            {
                foreach (var enumValue in Enum.GetValues(typeof(RoleAccess)).Cast<RoleAccess>())
                {
                    var result = await this.Characters.CreateCharacterRoleAsync
                    (
                        _discordRole,
                        enumValue
                    );

                    Assert.True(result.IsSuccess);
                    Assert.Equal(enumValue, result.Entity.Access);

                    await this.Characters.DeleteCharacterRoleAsync(result.Entity);
                }
            }

            [Fact]
            public async Task CreatingRoleWhenRoleAlreadyExistsReturnsError()
            {
                await this.Characters.CreateCharacterRoleAsync
                (
                    _discordRole,
                    RoleAccess.Open
                );

                var result = await this.Characters.CreateCharacterRoleAsync
                (
                    _discordRole,
                    RoleAccess.Open
                );

                Assert.False(result.IsSuccess);
            }
        }

        public class DeleteCharacterRoleAsync : CharacterServiceTestBase
        {
            private CharacterRole _role;

            public override async Task InitializeAsync()
            {
                var guild = MockHelper.CreateDiscordGuild(0);
                var discordRole = MockHelper.CreateDiscordRole(1, guild);

                var result = await this.Characters.CreateCharacterRoleAsync
                (
                    discordRole,
                    RoleAccess.Open
                );

                _role = result.Entity;
            }

            [Fact]
            public void StartsWithRoleInDatabase()
            {
                Assert.Same(_role, this.Database.CharacterRoles.First());
            }

            [Fact]
            public async Task CanDeleteRole()
            {
                var result = await this.Characters.DeleteCharacterRoleAsync(_role);

                Assert.True(result.IsSuccess);
                Assert.Empty(this.Database.CharacterRoles);
            }
        }

        public class GetCharacterRoleAsync : CharacterServiceTestBase
        {
            private readonly IGuild _discordGuild;
            private readonly IRole _discordRole;
            private readonly IRole _unregisteredDiscordRole;

            private CharacterRole _role;

            public GetCharacterRoleAsync()
            {
                _discordGuild = MockHelper.CreateDiscordGuild(0);
                _discordRole = MockHelper.CreateDiscordRole(1, _discordGuild);
                _unregisteredDiscordRole = MockHelper.CreateDiscordRole(2, _discordGuild);
            }

            public override async Task InitializeAsync()
            {
                var result = await this.Characters.CreateCharacterRoleAsync
                (
                    _discordRole,
                    RoleAccess.Open
                );

                _role = result.Entity;
            }

            [Fact]
            public async Task GetsCorrectRole()
            {
                var result = await this.Characters.GetCharacterRoleAsync(_discordRole);

                Assert.True(result.IsSuccess);
                Assert.Same(_role, result.Entity);
            }

            [Fact]
            public async Task ReturnsErrorIfRoleIsNotRegistered()
            {
                var result = await this.Characters.GetCharacterRoleAsync
                (
                    _unregisteredDiscordRole
                );

                Assert.False(result.IsSuccess);
            }
        }

        public class SetCharacterRoleAccessAsync : CharacterServiceTestBase
        {
            private readonly IRole _discordRole;

            private CharacterRole _role;

            public SetCharacterRoleAccessAsync()
            {
                var guild = MockHelper.CreateDiscordGuild(0);
                _discordRole = MockHelper.CreateDiscordRole(1, guild);
            }

            public override async Task InitializeAsync()
            {
                var result = await this.Characters.CreateCharacterRoleAsync
                (
                    _discordRole,
                    RoleAccess.Open
                );

                _role = result.Entity;
            }

            [Fact]
            public async Task CanSetAccess()
            {
                var getExistingRoleResult = await this.Characters.GetCharacterRoleAsync
                (
                    _discordRole
                );

                var existingRole = getExistingRoleResult.Entity;

                Assert.Equal(RoleAccess.Open, existingRole.Access);

                var result = await this.Characters.SetCharacterRoleAccessAsync
                (
                    existingRole,
                    RoleAccess.Restricted
                );

                Assert.True(result.IsSuccess);
                Assert.Equal(RoleAccess.Restricted, existingRole.Access);
            }
        }

        public class SetCharacterRoleAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";

            private readonly IUser _owner = MockHelper.CreateDiscordUser(0);
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(1);

            private Character _character;
            private CharacterRole _role;

            public override async Task InitializeAsync()
            {
                var user = (await this.Users.GetOrRegisterUserAsync(_owner)).Entity;

                _character = new Character
                {
                    Name = CharacterName,
                    Owner = user,
                    ServerID = (long)_guild.Id
                };

                this.Database.Characters.Update(_character);

                var createRoleResult = await this.Characters.CreateCharacterRoleAsync
                (
                    MockHelper.CreateDiscordRole(2, _guild),
                    RoleAccess.Open
                );

                _role = createRoleResult.Entity;

                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task CanSetCharacterRole()
            {
                var result = await this.Characters.SetCharacterRoleAsync
                (
                    _character,
                    _role
                );

                Assert.True(result.IsSuccess);
                Assert.Same(_role, _character.Role);
            }

            [Fact]
            public async Task ReturnsErrorIfCharacterAlreadyHasSameRole()
            {
                await this.Characters.SetCharacterRoleAsync
                (
                    _character,
                    _role
                );

                var result = await this.Characters.SetCharacterRoleAsync
                (
                    _character,
                    _role
                );

                Assert.False(result.IsSuccess);
            }
        }

        public class ClearCharacterRoleAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";

            private readonly IUser _owner = MockHelper.CreateDiscordUser(0);
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(1);

            private Character _character;
            private CharacterRole _role;

            public override async Task InitializeAsync()
            {
                var user = (await this.Users.GetOrRegisterUserAsync(_owner)).Entity;

                _character = new Character
                {
                    Name = CharacterName,
                    Owner = user,
                    ServerID = (long)_guild.Id
                };

                this.Database.Characters.Update(_character);

                var createRoleResult = await this.Characters.CreateCharacterRoleAsync
                (
                    MockHelper.CreateDiscordRole(2, _guild),
                    RoleAccess.Open
                );

                _role = createRoleResult.Entity;

                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task CanClearCharacterRole()
            {
                await this.Characters.SetCharacterRoleAsync
                (
                    _character,
                    _role
                );

                var result = await this.Characters.ClearCharacterRoleAsync
                (
                    _character
                );

                Assert.True(result.IsSuccess);
                Assert.Null(_character.Role);
            }

            [Fact]
            public async Task ReturnsErrorIfCharacterDoesNotHaveARole()
            {
                var result = await this.Characters.ClearCharacterRoleAsync
                (
                    _character
                );

                Assert.False(result.IsSuccess);
            }
        }
    }
}
