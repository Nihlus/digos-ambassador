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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Users;
using DIGOS.Ambassador.Modules;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Interactivity;
using DIGOS.Ambassador.Tests.TestBases;
using DIGOS.Ambassador.Tests.Utility;
using DIGOS.Ambassador.TypeReaders;

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
        public class WithPronounProvider : CharacterServiceTestBase
        {
            [Fact]
            public void AddsCorrectProvider()
            {
                var provider = new TheyPronounProvider();
                this.Characters.WithPronounProvider(provider);

                Assert.Collection(this.Characters.GetAvailablePronounProviders(), p => Assert.Same(provider, p));
            }
        }

        public class GetPronounProvider : CharacterServiceTestBase
        {
            private readonly Character Character;

            public GetPronounProvider()
            {
                this.Character = new Character
                {
                    PronounProviderFamily = new TheyPronounProvider().Family
                };
            }

            [Fact]
            public void ThrowsIfNoMatchingProviderIsFound()
            {
                Assert.Throws<KeyNotFoundException>(() => this.Characters.GetPronounProvider(this.Character));
            }

            [Fact]
            public void ReturnsCorrectProvider()
            {
                var expected = new TheyPronounProvider();
                this.Characters.WithPronounProvider(expected);

                var actual = this.Characters.GetPronounProvider(this.Character);

                Assert.Same(expected, actual);
            }
        }

        public class GetAvailablePronounProviders : CharacterServiceTestBase
        {
            [Fact]
            public void ReturnsEmptySetWhenNoProvidersHaveBeenAdded()
            {
                Assert.Empty(this.Characters.GetAvailablePronounProviders());
            }

            [Fact]
            public void ReturnsNonEmptySetWhenProvidersHaveBeenAdded()
            {
                var provider = new TheyPronounProvider();
                this.Characters.WithPronounProvider(provider);

                Assert.NotEmpty(this.Characters.GetAvailablePronounProviders());
            }
        }

        public class GetBestMatchingCharacterAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";

            private readonly ICommandContext Context;
            private readonly IGuildUser Owner = MockHelper.CreateDiscordGuildUser(0);
            private readonly IGuild Guild;

            private readonly Character Character;

            public GetBestMatchingCharacterAsync()
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
                    .Returns(Task.FromResult(this.Owner));

                this.Guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.User).Returns(this.Owner);
                mockedContext.Setup(c => c.Guild).Returns(this.Guild);

                var mockedMessage = new Mock<IUserMessage>();
                mockedMessage.Setup(m => m.Author).Returns(this.Owner);

                mockedContext.Setup(c => c.Message).Returns(mockedMessage.Object);

                this.Context = mockedContext.Object;

                this.Character = new Character
                {
                    Name = CharacterName,
                    ServerID = (long)this.Guild.Id,
                    Owner = new User { DiscordID = (long)this.Owner.Id },
                };

                this.Database.Characters.Add(this.Character);
                this.Database.SaveChanges();
            }

            /*
             * Unsuccessful assertions
             */

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfOwnerIsNullAndNameIsNullAndNoCharacterIsCurrent()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(this.Database, this.Context, null, null);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfOwnerIsNullAndNoACharacterWithThatNameExists()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(this.Database, this.Context, null, "NonExistant");

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfOwnerIsNullAndMoreThanOneCharacterWithThatNameExists()
            {
                var anotherCharacter = new Character
                {
                    Name = CharacterName,
                    ServerID = (long)this.Guild.Id,
                    Owner = new User { DiscordID = 2 },
                };

                await this.Database.Characters.AddAsync(anotherCharacter);
                await this.Database.SaveChangesAsync();

                var result = await this.Characters.GetBestMatchingCharacterAsync(this.Database, this.Context, null, CharacterName);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.MultipleMatches, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNameIsNullAndOwnerDoesNotHaveACurrentCharacter()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(this.Database, this.Context, this.Owner, null);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNameIsEmptyAndOwnerDoesNotHaveACurrentCharacter()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(this.Database, this.Context, this.Owner, string.Empty);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfOwnerIsNotNullAndNameIsNotNullAndUserDoesNotHaveACharacterWithThatName()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(this.Database, this.Context, this.Owner, "NonExistant");

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            /*
             * Successful assertions
             */

            [Fact]
            public async Task ReturnsSuccessfulResultIfOwnerIsNullAndNameIsNullAndOwnerHasACurrentCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(this.Database, this.Context, this.Guild, this.Character);

                var result = await this.Characters.GetBestMatchingCharacterAsync(this.Database, this.Context, null, null);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfOwnerIsNullAndASingleCharacterWithThatNameExists()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(this.Database, this.Context, null, CharacterName);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfNameIsNullAndOwnerHasACurrentCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(this.Database, this.Context, this.Guild, this.Character);

                var result = await this.Characters.GetBestMatchingCharacterAsync(this.Database, this.Context, this.Owner, null);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfNameIsEmptyAndOwnerHasACurrentCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(this.Database, this.Context, this.Guild, this.Character);

                var result = await this.Characters.GetBestMatchingCharacterAsync(this.Database, this.Context, this.Owner, string.Empty);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfOwnerIsNotNullAndNameIsNotNullAndOwnerHasACharacterWithThatName()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(this.Database, this.Context, this.Owner, CharacterName);

                Assert.True(result.IsSuccess);
            }

            /*
             * Correctness assertions
             */

            [Fact]
            public async Task ReturnsCurrentCharacterIfOwnerIsNullAndNameIsNullAndOwnerHasACurrentCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(this.Database, this.Context, this.Guild, this.Character);

                var result = await this.Characters.GetBestMatchingCharacterAsync(this.Database, this.Context, null, null);

                Assert.Same(this.Character, result.Entity);
            }

            [Fact]
            public async Task ReturnsCorrectCharacterIfOwnerIsNullAndASingleCharacterWithThatNameExists()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(this.Database, this.Context, null, CharacterName);

                Assert.Same(this.Character, result.Entity);
            }

            [Fact]
            public async Task ReturnsCurrentCharacterIfNameIsNullAndOwnerHasACurrentCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(this.Database, this.Context, this.Guild, this.Character);

                var result = await this.Characters.GetBestMatchingCharacterAsync(this.Database, this.Context, this.Owner, null);

                Assert.Same(this.Character, result.Entity);
            }

            [Fact]
            public async Task ReturnsCurrentCharacterIfNameIsEmptyAndOwnerHasACurrentCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(this.Database, this.Context, this.Guild, this.Character);

                var result = await this.Characters.GetBestMatchingCharacterAsync(this.Database, this.Context, this.Owner, string.Empty);

                Assert.Same(this.Character, result.Entity);
            }

            [Fact]
            public async Task ReturnsCorrectCharacterIfOwnerIsNotNullAndNameIsNotNullAndOwnerHasACharacterWithThatName()
            {
                var result = await this.Characters.GetBestMatchingCharacterAsync(this.Database, this.Context, this.Owner, CharacterName);

                Assert.Same(this.Character, result.Entity);
            }
        }

        public class GetCurrentCharacterAsync : CharacterServiceTestBase
        {
            private readonly ICommandContext Context;
            private readonly IGuild Guild;
            private readonly IGuildUser Owner = MockHelper.CreateDiscordGuildUser(0);

            private readonly Character Character;

            public GetCurrentCharacterAsync()
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
                .Returns(Task.FromResult(this.Owner));

                this.Guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.User).Returns(this.Owner);
                mockedContext.Setup(c => c.Guild).Returns(this.Guild);

                var mockedMessage = new Mock<IUserMessage>();
                mockedMessage.Setup(m => m.Author).Returns(this.Owner);

                mockedContext.Setup(c => c.Message).Returns(mockedMessage.Object);

                this.Context = mockedContext.Object;

                this.Character = new Character
                {
                    ServerID = (long)this.Guild.Id,
                    Owner = new User { DiscordID = (long)this.Owner.Id },
                };

                this.Database.Characters.Add(this.Character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserDoesNotHaveAnActiveCharacter()
            {
                var result = await this.Characters.GetCurrentCharacterAsync(this.Database, this.Context, this.Owner);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfUserHasActiveCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(this.Database, this.Context, this.Guild, this.Character);

                var result = await this.Characters.GetCurrentCharacterAsync(this.Database, this.Context, this.Owner);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsCorrectCharacter()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(this.Database, this.Context, this.Guild, this.Character);

                var result = await this.Characters.GetCurrentCharacterAsync(this.Database, this.Context, this.Owner);

                Assert.Same(this.Character, result.Entity);
            }
        }

        public class GetNamedCharacterAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";
            private readonly IGuild Guild = MockHelper.CreateDiscordGuild(0);
            private readonly IUser User = MockHelper.CreateDiscordUser(0);

            private User Owner;

            private Character Character;

            public override async Task InitializeAsync()
            {
                this.Owner = (await this.Database.GetOrRegisterUserAsync(this.User)).Entity;

                this.Character = new Character
                {
                    Name = CharacterName,
                    ServerID = (long)this.Guild.Id,
                    Owner = this.Owner
                };

                this.Database.Characters.Add(this.Character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNoCharacterWithThatNameExists()
            {
                var result = await this.Characters.GetNamedCharacterAsync(this.Database, "NonExistant", this.Guild);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfMoreThanOneCharacterWithThatNameExists()
            {
                var anotherCharacter = new Character
                {
                    Name = CharacterName,
                    ServerID = (long)this.Guild.Id,
                    Owner = this.Owner
                };

                await this.Database.Characters.AddAsync(anotherCharacter);
                await this.Database.SaveChangesAsync();

                var result = await this.Characters.GetNamedCharacterAsync(this.Database, CharacterName, this.Guild);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.MultipleMatches, result.Error);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfASingleCharacterWithThatNameExists()
            {
                var result = await this.Characters.GetNamedCharacterAsync(this.Database, CharacterName, this.Guild);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsCorrectCharacter()
            {
                var result = await this.Characters.GetNamedCharacterAsync(this.Database, CharacterName, this.Guild);

                Assert.Same(this.Character, result.Entity);
            }
        }

        public class GetCharacters : CharacterServiceTestBase
        {
            private readonly IGuild Guild = MockHelper.CreateDiscordGuild(0);
            private readonly IUser User = MockHelper.CreateDiscordUser(0);

            private User Owner;

            public override async Task InitializeAsync()
            {
                this.Owner = (await this.Database.GetOrRegisterUserAsync(this.User)).Entity;
            }

            [Fact]
            public void ReturnsNoCharactersFromEmptyDatabase()
            {
                var result = this.Characters.GetCharacters(this.Database, this.Guild);

                Assert.Empty(result);
            }

            [Fact]
            public void ReturnsSingleCharacterFromSingleCharacterOnServer()
            {
                this.Database.Characters.Add(new Character { ServerID = (long)this.Guild.Id, Owner = this.Owner });
                this.Database.SaveChanges();

                var result = this.Characters.GetCharacters(this.Database, this.Guild);

                Assert.NotEmpty(result);
                Assert.Single(result);
            }

            [Fact]
            public void ReturnsNoCharacterFromSingleCharacterOnServerWhenRequestedServerIsDifferent()
            {
                this.Database.Characters.Add(new Character { ServerID = 1, Owner = this.Owner });
                this.Database.SaveChanges();

                var result = this.Characters.GetCharacters(this.Database, this.Guild);

                Assert.Empty(result);
            }

            [Fact]
            public void ReturnsCorrectCharactersFromDatabase()
            {
                this.Database.Characters.Add(new Character { ServerID = 1, Owner = this.Owner });
                this.Database.Characters.Add(new Character { ServerID = (long)this.Guild.Id, Owner = this.Owner });
                this.Database.Characters.Add(new Character { ServerID = (long)this.Guild.Id, Owner = this.Owner });
                this.Database.SaveChanges();

                var result = this.Characters.GetCharacters(this.Database, this.Guild);

                Assert.NotEmpty(result);
                Assert.Equal(2, result.Count());
            }
        }

        public class GetUserCharacterByNameAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";

            private readonly ICommandContext Context;
            private readonly IGuildUser Owner = MockHelper.CreateDiscordGuildUser(0);

            private readonly Character Character;

            public GetUserCharacterByNameAsync()
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
                    .Returns(Task.FromResult(this.Owner));

                var guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.User).Returns(this.Owner);
                mockedContext.Setup(c => c.Guild).Returns(guild);

                var mockedMessage = new Mock<IUserMessage>();
                mockedMessage.Setup(m => m.Author).Returns(this.Owner);

                mockedContext.Setup(c => c.Message).Returns(mockedMessage.Object);

                this.Context = mockedContext.Object;

                this.Character = new Character
                {
                    Name = CharacterName,
                    ServerID = (long)guild.Id,
                    Owner = new User { DiscordID = (long)this.Owner.Id },
                };

                this.Database.Characters.Add(this.Character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfOwnerDoesNotHaveACharacterWithThatName()
            {
                var result = await this.Characters.GetUserCharacterByNameAsync(this.Database, this.Context, this.Owner, "NonExistant");

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfOwnerHasACharacterWithThatName()
            {
                var result = await this.Characters.GetUserCharacterByNameAsync(this.Database, this.Context, this.Owner, CharacterName);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsCorrectCharacter()
            {
                var result = await this.Characters.GetUserCharacterByNameAsync(this.Database, this.Context, this.Owner, CharacterName);

                Assert.Same(this.Character, result.Entity);
            }
        }

        public class MakeCharacterCurrentOnServerAsync : CharacterServiceTestBase
        {
            private readonly ICommandContext Context;
            private readonly IGuild Guild;
            private readonly IGuildUser Owner = MockHelper.CreateDiscordGuildUser(0);

            private readonly Character Character;

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
                .Returns(Task.FromResult(this.Owner));

                this.Guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(this.Guild);

                var mockedMessage = new Mock<IUserMessage>();
                mockedMessage.Setup(m => m.Author).Returns(this.Owner);

                mockedContext.Setup(c => c.Message).Returns(mockedMessage.Object);

                this.Context = mockedContext.Object;

                this.Character = new Character
                {
                    Owner = new User { DiscordID = (long)this.Owner.Id },
                };

                this.Database.Characters.Add(this.Character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfCharacterIsNotCurrent()
            {
                var result = await this.Characters.MakeCharacterCurrentOnServerAsync(this.Database, this.Context, this.Guild, this.Character);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task MakesCharacterCurrentOnCorrectServer()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(this.Database, this.Context, this.Guild, this.Character);

                Assert.True(this.Character.IsCurrent);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterIsAlreadyCurrent()
            {
                await this.Characters.MakeCharacterCurrentOnServerAsync(this.Database, this.Context, this.Guild, this.Character);
                var result = await this.Characters.MakeCharacterCurrentOnServerAsync(this.Database, this.Context, this.Guild, this.Character);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.MultipleMatches, result.Error);
            }
        }

        public class ClearCurrentCharacterOnServerAsync : CharacterServiceTestBase
        {
            private readonly IUser Owner = MockHelper.CreateDiscordUser(0);
            private readonly IGuild Guild = MockHelper.CreateDiscordGuild(1);
            private readonly Character Character;

            public ClearCurrentCharacterOnServerAsync()
            {
                this.Character = new Character
                {
                    ServerID = (long)this.Guild.Id,
                    Owner = new User { DiscordID = (long)this.Owner.Id },
                };

                this.Database.Characters.Add(this.Character);
                this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterIsNotCurrentOnServer()
            {
                var result = await this.Characters.ClearCurrentCharacterOnServerAsync(this.Database, this.Owner, this.Guild);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfCharacterIsCurrentOnServer()
            {
                this.Character.IsCurrent = true;
                await this.Database.SaveChangesAsync();

                var result = await this.Characters.ClearCurrentCharacterOnServerAsync(this.Database, this.Owner, this.Guild);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task RemovesCorrectServerFromCharacter()
            {
                this.Character.IsCurrent = true;
                await this.Database.SaveChangesAsync();

                await this.Characters.ClearCurrentCharacterOnServerAsync(this.Database, this.Owner, this.Guild);

                Assert.False(this.Character.IsCurrent);
            }
        }

        public class HasActiveCharacterOnServerAsync : CharacterServiceTestBase
        {
            private readonly IUser Owner = MockHelper.CreateDiscordUser(0);
            private readonly IGuild Guild = MockHelper.CreateDiscordGuild(1);

            [Fact]
            public async Task ReturnsFalseIfServerIsNull()
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                var result = await this.Characters.HasActiveCharacterOnServerAsync(this.Database, this.Owner, null);

                Assert.False(result);
            }

            [Fact]
            public async Task ReturnsFalseIfUserHasNoCharacters()
            {
                var result = await this.Characters.HasActiveCharacterOnServerAsync(this.Database, this.Owner, this.Guild);

                Assert.False(result);
            }

            [Fact]
            public async Task ReturnsFalseIfUserHasNoActiveCharacter()
            {
                var character = new Character
                {
                    Owner = new User { DiscordID = (long)this.Owner.Id },
                    ServerID = (long)this.Guild.Id
                };

                this.Database.Characters.Add(character);
                this.Database.SaveChanges();

                var result = await this.Characters.HasActiveCharacterOnServerAsync(this.Database, this.Owner, this.Guild);

                Assert.False(result);
            }

            [Fact]
            public async Task ReturnsTrueIfUserHasAnActiveCharacter()
            {
                var character = new Character
                {
                    Owner = new User { DiscordID = (long)this.Owner.Id },
                    ServerID = (long)this.Guild.Id,
                    IsCurrent = true
                };

                this.Database.Characters.Add(character);
                this.Database.SaveChanges();

                var result = await this.Characters.HasActiveCharacterOnServerAsync(this.Database, this.Owner, this.Guild);

                Assert.True(result);
            }
        }

        public class CreateCharacterAsync : CharacterServiceTestBase
        {
            private readonly ICommandContext Context;

            public CreateCharacterAsync()
            {
                this.Characters.WithPronounProvider(new TheyPronounProvider());

                var mockedUser = new Mock<IUser>();
                mockedUser.Setup(u => u.Id).Returns(0);

                var mockedMessage = new Mock<IUserMessage>();
                mockedMessage.Setup(m => m.Author).Returns(mockedUser.Object);

                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Message).Returns(mockedMessage.Object);
                mockedContext.Setup(c => c.Guild).Returns(mockedGuild.Object);

                this.Context = mockedContext.Object;
            }

            [Fact]
            public async Task CanCreateWithNameOnly()
            {
                var result = await this.Characters.CreateCharacterAsync(this.Database, this.Context, "Test");

                Assert.True(result.IsSuccess);
                Assert.NotEmpty(this.Database.Characters);
                Assert.Equal("Test", this.Database.Characters.First().Name);
            }
        }

        public class SetCharacterNameAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";
            private const string AnotherCharacterName = "Test2";

            private readonly ICommandContext Context;
            private readonly Character Character;

            private IServiceProvider Services;

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

                this.Context = mockedContext.Object;

                this.Character = new Character
                {
                    Name = CharacterName,
                    ServerID = (long)mockedGuildObject.Id,
                    Owner = new User { DiscordID = (long)mockedUserObject.Id },
                };

                var anotherCharacter = new Character
                {
                    Name = AnotherCharacterName,
                    ServerID = (long)mockedGuildObject.Id,
                    Owner = new User { DiscordID = (long)mockedUserObject.Id },
                };

                this.Database.Characters.Add(this.Character);
                this.Database.Characters.Add(anotherCharacter);
                this.Database.SaveChanges();
            }

            public override async Task InitializeAsync()
            {
                var client = new DiscordSocketClient();

                this.Services = new ServiceCollection()
                    .AddSingleton(this.Database)
                    .AddSingleton<ContentService>()
                    .AddSingleton<CommandService>()
                    .AddSingleton<DiscordService>()
                    .AddSingleton<UserFeedbackService>()
                    .AddSingleton<OwnedEntityService>()
                    .AddSingleton<TransformationService>()
                    .AddSingleton<InteractivityService>()
                    .AddSingleton<CharacterService>()
                    .AddSingleton(client)
                    .AddSingleton<BaseSocketClient>(client)
                    .BuildServiceProvider();

                this.Commands.AddTypeReader<Character>(new CharacterTypeReader());
                await this.Commands.AddModuleAsync<CharacterCommands>(this.Services);

                await base.InitializeAsync();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNameIsNull()
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = await this.Characters.SetCharacterNameAsync(this.Database, this.Context, this.Character, null);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.BadArgCount, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNameIsEmpty()
            {
                var result = await this.Characters.SetCharacterNameAsync(this.Database, this.Context, this.Character, string.Empty);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.BadArgCount, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterAlreadyHasThatName()
            {
                var result = await this.Characters.SetCharacterNameAsync(this.Database, this.Context, this.Character, CharacterName);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNameIsNotUniqueForUser()
            {
                var result = await this.Characters.SetCharacterNameAsync(this.Database, this.Context, this.Character, AnotherCharacterName);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.MultipleMatches, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNameIsInvalid()
            {
                var result = await this.Characters.SetCharacterNameAsync(this.Database, this.Context, this.Character, "create");

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfNameIsAccepted()
            {
                var result = await this.Characters.SetCharacterNameAsync(this.Database, this.Context, this.Character, "Jeff");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task SetsName()
            {
                const string validName = "Jeff";

                await this.Characters.SetCharacterNameAsync(this.Database, this.Context, this.Character, validName);

                var character = this.Database.Characters.First();
                Assert.Equal(validName, character.Name);
            }
        }

        public class SetCharacterAvatarAsync : CharacterServiceTestBase
        {
            private const string AvatarURL = "http://fake.com/avatar.png";

            private readonly IUser User = MockHelper.CreateDiscordUser(0);

            private User Owner;
            private Character Character;

            public override async Task InitializeAsync()
            {
                this.Owner = (await this.Database.GetOrRegisterUserAsync(this.User)).Entity;
                this.Character = new Character { AvatarUrl = AvatarURL, Owner = this.Owner };

                this.Database.Characters.Add(this.Character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfAvatarURLIsNull()
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = await this.Characters.SetCharacterAvatarAsync(this.Database, this.Character, null);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.BadArgCount, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfAvatarURLIsEmpty()
            {
                var result = await this.Characters.SetCharacterAvatarAsync(this.Database, this.Character, string.Empty);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.BadArgCount, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfAvatarURLIsTheSameAsTheCurrentURL()
            {
                var result = await this.Characters.SetCharacterAvatarAsync(this.Database, this.Character, AvatarURL);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfURLIsAccepted()
            {
                var result = await this.Characters.SetCharacterAvatarAsync(this.Database, this.Character, "http://www.myfunkyavatars.com/avatar.png");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task SetsURL()
            {
                const string newURL = "http://www.myfunkyavatars.com/avatar.png";
                await this.Characters.SetCharacterAvatarAsync(this.Database, this.Character, newURL);

                var character = this.Database.Characters.First();
                Assert.Equal(newURL, character.AvatarUrl);
            }
        }

        public class SetCharacterNicknameAsync : CharacterServiceTestBase
        {
            private const string Nickname = "Nicke";

            private readonly IUser User = MockHelper.CreateDiscordUser(0);

            private User Owner;
            private Character Character;

            public override async Task InitializeAsync()
            {
                this.Owner = (await this.Database.GetOrRegisterUserAsync(this.User)).Entity;
                this.Character = new Character { Nickname = Nickname, Owner = this.Owner };

                this.Database.Characters.Add(this.Character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNicknameIsNull()
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = await this.Characters.SetCharacterNicknameAsync(this.Database, this.Character, null);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.BadArgCount, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNicknameIsEmpty()
            {
                var result = await this.Characters.SetCharacterNicknameAsync(this.Database, this.Character, string.Empty);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.BadArgCount, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNicknameIsTheSameAsTheCurrentNickname()
            {
                var result = await this.Characters.SetCharacterNicknameAsync(this.Database, this.Character, Nickname);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNewNicknameIsLongerThan32Characters()
            {
                var result = await this.Characters.SetCharacterNicknameAsync(this.Database, this.Character, new string('a', 33));

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfNicknameIsAccepted()
            {
                var result = await this.Characters.SetCharacterNicknameAsync(this.Database, this.Character, "Bobby");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task SetsNickname()
            {
                const string newNickname = "Bobby";
                await this.Characters.SetCharacterNicknameAsync(this.Database, this.Character, newNickname);

                var character = this.Database.Characters.First();
                Assert.Equal(newNickname, character.Nickname);
            }
        }

        public class SetCharacterSummaryAsync : CharacterServiceTestBase
        {
            private const string Summary = "A cool person";
            private readonly IUser User = MockHelper.CreateDiscordUser(0);

            private User Owner;
            private Character Character;

            public override async Task InitializeAsync()
            {
                this.Owner = (await this.Database.GetOrRegisterUserAsync(this.User)).Entity;
                this.Character = new Character { Summary = Summary, Owner = this.Owner };

                this.Database.Characters.Add(this.Character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSummaryIsNull()
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = await this.Characters.SetCharacterSummaryAsync(this.Database, this.Character, null);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.BadArgCount, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSummaryIsEmpty()
            {
                var result = await this.Characters.SetCharacterSummaryAsync(this.Database, this.Character, string.Empty);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.BadArgCount, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSummaryIsTheSameAsTheCurrentSummary()
            {
                var result = await this.Characters.SetCharacterSummaryAsync(this.Database, this.Character, Summary);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNewSummaryIsLongerThan240Characters()
            {
                var result = await this.Characters.SetCharacterSummaryAsync(this.Database, this.Character, new string('a', 241));

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfSummaryIsAccepted()
            {
                var result = await this.Characters.SetCharacterSummaryAsync(this.Database, this.Character, "Bobby");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task SetsSummary()
            {
                const string newSummary = "An uncool person";
                await this.Characters.SetCharacterSummaryAsync(this.Database, this.Character, newSummary);

                var character = this.Database.Characters.First();
                Assert.Equal(newSummary, character.Summary);
            }
        }

        public class SetCharacterDescriptionAsync : CharacterServiceTestBase
        {
            private const string Description = "A cool person";
            private readonly IUser User = MockHelper.CreateDiscordUser(0);

            private User Owner;
            private Character Character;

            public override async Task InitializeAsync()
            {
                this.Owner = (await this.Database.GetOrRegisterUserAsync(this.User)).Entity;
                this.Character = new Character { Description = Description, Owner = this.Owner };

                this.Database.Characters.Add(this.Character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfDescriptionIsNull()
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = await this.Characters.SetCharacterDescriptionAsync(this.Database, this.Character, null);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.BadArgCount, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfDescriptionIsEmpty()
            {
                var result = await this.Characters.SetCharacterDescriptionAsync(this.Database, this.Character, string.Empty);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.BadArgCount, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfDescriptionIsTheSameAsTheCurrentDescription()
            {
                var result = await this.Characters.SetCharacterDescriptionAsync(this.Database, this.Character, Description);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfDescriptionIsAccepted()
            {
                var result = await this.Characters.SetCharacterDescriptionAsync(this.Database, this.Character, "Bobby");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task SetsDescription()
            {
                const string newDescription = "An uncool person";
                await this.Characters.SetCharacterDescriptionAsync(this.Database, this.Character, newDescription);

                var character = this.Database.Characters.First();
                Assert.Equal(newDescription, character.Description);
            }
        }

        public class SetCharacterPronounAsync : CharacterServiceTestBase
        {
            private const string PronounFamily = "They";
            private readonly IUser User = MockHelper.CreateDiscordUser(0);

            private User Owner;
            private Character Character;

            public override async Task InitializeAsync()
            {
                this.Characters.WithPronounProvider(new TheyPronounProvider());
                this.Characters.WithPronounProvider(new ZeHirPronounProvider());

                this.Owner = (await this.Database.GetOrRegisterUserAsync(this.User)).Entity;
                this.Character = new Character { PronounProviderFamily = PronounFamily, Owner = this.Owner };

                this.Database.Characters.Add(this.Character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfPronounIsNull()
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = await this.Characters.SetCharacterPronounAsync(this.Database, this.Character, null);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.BadArgCount, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfPronounIsEmpty()
            {
                var result = await this.Characters.SetCharacterPronounAsync(this.Database, this.Character, string.Empty);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.BadArgCount, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfPronounIsTheSameAsTheCurrentPronoun()
            {
                var result = await this.Characters.SetCharacterPronounAsync(this.Database, this.Character, PronounFamily);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNoMatchingPronounProviderIsFound()
            {
                var result = await this.Characters.SetCharacterPronounAsync(this.Database, this.Character, "ahwooooga");

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfPronounIsAccepted()
            {
                var result = await this.Characters.SetCharacterPronounAsync(this.Database, this.Character, "Ze and hir");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task SetsPronoun()
            {
                const string newPronounFamily = "Ze and hir";
                await this.Characters.SetCharacterPronounAsync(this.Database, this.Character, newPronounFamily);

                var character = this.Database.Characters.First();
                Assert.Equal(newPronounFamily, character.PronounProviderFamily);
            }
        }

        public class SetCharacterIsNSFWAsync : CharacterServiceTestBase
        {
            private const bool IsNSFW = false;

            private readonly IUser User = MockHelper.CreateDiscordUser(0);

            private User Owner;
            private Character Character;

            public override async Task InitializeAsync()
            {
                this.Owner = (await this.Database.GetOrRegisterUserAsync(this.User)).Entity;
                this.Character = new Character { IsNSFW = IsNSFW, Owner = this.Owner };

                this.Database.Characters.Add(this.Character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfIsNSFWIsTheSameAsTheCurrentIsNSFW()
            {
                var result = await this.Characters.SetCharacterIsNSFWAsync(this.Database, this.Character, IsNSFW);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfIsNSFWIsAccepted()
            {
                var result = await this.Characters.SetCharacterIsNSFWAsync(this.Database, this.Character, true);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task SetsIsNSFW()
            {
                const bool newIsNSFW = true;
                await this.Characters.SetCharacterIsNSFWAsync(this.Database, this.Character, newIsNSFW);

                var character = this.Database.Characters.First();
                Assert.Equal(newIsNSFW, character.IsNSFW);
            }
        }

        public class TransferCharacterOwnershipAsync : CharacterServiceTestBase
        {
            private readonly Character Character;

            private readonly IUser OriginalOwner = MockHelper.CreateDiscordUser(0);
            private readonly IUser NewOwner = MockHelper.CreateDiscordUser(1);
            private readonly IGuild Guild = MockHelper.CreateDiscordGuild(2);

            public TransferCharacterOwnershipAsync()
            {
                this.Character = new Character
                {
                    ServerID = (long)this.Guild.Id,
                    Owner = new User { DiscordID = (long)this.OriginalOwner.Id }
                };

                this.Database.Characters.Add(this.Character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfCharacterIsTransferred()
            {
                var result = await this.Characters.TransferCharacterOwnershipAsync(this.Database, this.NewOwner, this.Character, this.Guild);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task TransfersCharacter()
            {
                await this.Characters.TransferCharacterOwnershipAsync(this.Database, this.NewOwner, this.Character, this.Guild);

                var character = this.Database.Characters.First();
                Assert.Equal((long)this.NewOwner.Id, character.Owner.DiscordID);
            }
        }

        public class GetUserCharacters : CharacterServiceTestBase
        {
            private readonly IUser Owner = MockHelper.CreateDiscordUser(0);
            private readonly IGuild Guild = MockHelper.CreateDiscordGuild(1);

            [Fact]
            public void ReturnsEmptySetFromEmptyDatabase()
            {
                Assert.Empty(this.Characters.GetUserCharacters(this.Database, this.Owner, this.Guild));
            }

            [Fact]
            public void ReturnsEmptySetFromDatabaseWithCharactersWithNoMatchingOwner()
            {
                var character = new Character
                {
                    Owner = new User { DiscordID = 1 },
                    ServerID = (long)this.Guild.Id
                };

                this.Database.Characters.Add(character);
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(this.Database, this.Owner, this.Guild);
                Assert.Empty(result);
            }

            [Fact]
            public void ReturnsNEmptySetFromDatabaseWithCharactersWithMatchingOwnerButNoMatchingServer()
            {
                var character = new Character
                {
                    Owner = new User { DiscordID = (long)this.Owner.Id }
                };

                this.Database.Characters.Add(character);
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(this.Database, this.Owner, this.Guild);
                Assert.Empty(result);
            }

            [Fact]
            public void ReturnsNonEmptySetFromDatabaseWithCharactersWithMatchingOwner()
            {
                var character = new Character
                {
                    Owner = new User { DiscordID = (long)this.Owner.Id },
                    ServerID = (long)this.Guild.Id
                };

                this.Database.Characters.Add(character);
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(this.Database, this.Owner, this.Guild);
                Assert.NotEmpty(result);
            }

            [Fact]
            public void ReturnsCorrectCharacterFromDatabase()
            {
                var character = new Character
                {
                    Owner = new User { DiscordID = (long)this.Owner.Id },
                    ServerID = (long)this.Guild.Id
                };

                this.Database.Characters.Add(character);
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(this.Database, this.Owner, this.Guild);
                Assert.Collection(result, c => Assert.Same(character, c));
            }

            [Fact]
            public void ReturnsCorrectMultipleCharactersFromDatabase()
            {
                var character1 = new Character
                {
                    Owner = new User { DiscordID = (long)this.Owner.Id },
                    ServerID = (long)this.Guild.Id
                };

                var character2 = new Character
                {
                    Owner = new User { DiscordID = (long)this.Owner.Id },
                    ServerID = (long)this.Guild.Id
                };

                this.Database.Characters.Add(character1);
                this.Database.Characters.Add(character2);
                this.Database.SaveChanges();

                var result = this.Characters.GetUserCharacters(this.Database, this.Owner, this.Guild);
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

            private readonly IUser Owner = MockHelper.CreateDiscordUser(0);
            private readonly IGuild Guild = MockHelper.CreateDiscordGuild(1);

            public IsCharacterNameUniqueForUserAsync()
            {
                var character = new Character
                {
                    Name = CharacterName,
                    Owner = new User { DiscordID = (long)this.Owner.Id },
                    ServerID = (long)this.Guild.Id
                };

                this.Database.Characters.Add(character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsFalseIfUserHasACharacterWithThatName()
            {
                var result = await this.Characters.IsCharacterNameUniqueForUserAsync(this.Database, this.Owner, CharacterName, this.Guild);

                Assert.False(result);
            }

            [Fact]
            public async Task ReturnsTrueIfUserDoesNotHaveACharacterWithThatName()
            {
                var result = await this.Characters.IsCharacterNameUniqueForUserAsync(this.Database, this.Owner, "AnotherName", this.Guild);

                Assert.True(result);
            }
        }

        public class SetDefaultCharacterForUserAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";

            private readonly IUser Owner = MockHelper.CreateDiscordUser(0);
            private readonly IGuild Guild = MockHelper.CreateDiscordGuild(1);

            private User User;
            private Character Character;

            public override async Task InitializeAsync()
            {
                this.User = (await this.Database.GetOrRegisterUserAsync(this.Owner)).Entity;

                this.Character = new Character
                {
                    Name = CharacterName,
                    Owner = this.User,
                    ServerID = (long)this.Guild.Id
                };

                this.Database.Characters.Add(this.Character);
                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task CanSetDefaultCharacter()
            {
                var contextMock = new Mock<ICommandContext>();
                contextMock.Setup(c => c.Message.Author.Id).Returns(this.Owner.Id);

                var context = contextMock.Object;

                var result = await this.Characters.SetDefaultCharacterForUserAsync
                (
                    this.Database,
                    context,
                    this.Character,
                    this.User
                );

                Assert.True(result.IsSuccess);
                Assert.Same(this.Character, this.User.DefaultCharacter);
            }

            [Fact]
            public async Task ReturnsErrorIfDefaultCharacterIsAlreadySetToTheSameCharacter()
            {
                var contextMock = new Mock<ICommandContext>();
                contextMock.Setup(c => c.Message.Author.Id).Returns(this.Owner.Id);

                var context = contextMock.Object;

                await this.Characters.SetDefaultCharacterForUserAsync
                (
                    this.Database,
                    context,
                    this.Character,
                    this.User
                );

                var result = await this.Characters.SetDefaultCharacterForUserAsync
                (
                    this.Database,
                    context,
                    this.Character,
                    this.User
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
                Assert.Same(this.Character, this.User.DefaultCharacter);
            }
        }

        public class ClearDefaultCharacterForUserAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";

            private readonly IUser Owner = MockHelper.CreateDiscordUser(0);
            private readonly IGuild Guild = MockHelper.CreateDiscordGuild(1);

            private User User;
            private Character Character;

            public override async Task InitializeAsync()
            {
                this.User = (await this.Database.GetOrRegisterUserAsync(this.Owner)).Entity;

                this.Character = new Character
                {
                    Name = CharacterName,
                    Owner = this.User,
                    ServerID = (long)this.Guild.Id
                };

                this.Database.Characters.Add(this.Character);
                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task CanClearDefaultCharacter()
            {
                var contextMock = new Mock<ICommandContext>();
                contextMock.Setup(c => c.Message.Author.Id).Returns(this.Owner.Id);

                var context = contextMock.Object;

                await this.Characters.SetDefaultCharacterForUserAsync
                (
                    this.Database,
                    context,
                    this.Character,
                    this.User
                );

                var result = await this.Characters.ClearDefaultCharacterForUserAsync
                (
                    this.Database,
                    context,
                    this.User
                );

                Assert.True(result.IsSuccess);
                Assert.Null(this.User.DefaultCharacter);
            }

            [Fact]
            public async Task ReturnsErrorIfDefaultCharacterIsNotSet()
            {
                var contextMock = new Mock<ICommandContext>();
                contextMock.Setup(c => c.Message.Author.Id).Returns(this.Owner.Id);

                var context = contextMock.Object;

                var result = await this.Characters.ClearDefaultCharacterForUserAsync
                (
                    this.Database,
                    context,
                    this.User
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
                Assert.Null(this.User.DefaultCharacter);
            }
        }

        public class AddImageToCharacterAsync : CharacterServiceTestBase
        {
        }

        public class RemoveImageFromCharacterAsync : CharacterServiceTestBase
        {
        }

        public class CreateCharacterFromAppearanceAsync : CharacterServiceTestBase
        {
        }
    }
}
