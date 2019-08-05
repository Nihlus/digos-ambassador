//
//  TransformationServiceTests.cs
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
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Transformations.Model;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Tests.TestBases;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Discord.Commands;

using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591

namespace DIGOS.Ambassador.Tests.ServiceTests
{
    public class TransformationServiceTests
    {
        public class UpdateTransformationDatabaseAsync : TransformationServiceTestBase
        {
            [Fact]
            public async Task FindsBundledSpecies()
            {
                var result = await this.Transformations.UpdateTransformationDatabaseAsync();

                Assert.True(result.IsSuccess);
                Assert.NotEmpty(this.Database.Species);
            }
        }

        public class IsSpeciesNameUniqueAsync : TransformationServiceTestBase
        {
            [Theory]
            [InlineData("asadasdas")]
            [InlineData("yeee ewwah")]
            public async Task ReturnsTrueForUniqueName([NotNull] string name)
            {
                var result = await this.Transformations.IsSpeciesNameUniqueAsync(name);

                Assert.True(result);
            }

            [Theory]
            [InlineData("template")]
            public async Task ReturnsFalseForNonUniqueName([NotNull] string name)
            {
                var result = await this.Transformations.IsSpeciesNameUniqueAsync(name);

                Assert.False(result);
            }

            [Theory]
            [InlineData("TEMPLATE")]
            public async Task IsCaseInsensitive([NotNull] string name)
            {
                var result = await this.Transformations.IsSpeciesNameUniqueAsync(name);

                Assert.False(result);
            }
        }

        public class GetSpeciesByNameAsync : TransformationServiceTestBase
        {
            [Fact]
            public async Task ReturnsCorrectSpeciesForGivenName()
            {
                var result = await this.Transformations.GetSpeciesByNameAsync("template");

                Assert.True(result.IsSuccess);
                Assert.Equal("template", result.Entity.Name);
            }

            [Fact]
            public async Task ReturnsUnsuccesfulResultForNonexistantName()
            {
                var result = await this.Transformations.GetSpeciesByNameAsync("aasddduaiii");

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task IsCaseInsensitive()
            {
                var result = await this.Transformations.GetSpeciesByNameAsync("TEMPLATE");

                Assert.True(result.IsSuccess);
                Assert.Equal("template", result.Entity.Name);
            }
        }

        public class GetSpeciesByName : TransformationServiceTestBase
        {
            [Fact]
            public void ReturnsCorrectSpeciesForGivenName()
            {
                var result = this.Transformations.GetSpeciesByName("template");

                Assert.True(result.IsSuccess);
                Assert.Equal("template", result.Entity.Name);
            }

            [Fact]
            public void ReturnsUnsuccesfulResultForNonexistantName()
            {
                var result = this.Transformations.GetSpeciesByName("aasddduaiii");

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public void IsCaseInsensitive()
            {
                var result = this.Transformations.GetSpeciesByName("TEMPLATE");

                Assert.True(result.IsSuccess);
                Assert.Equal("template", result.Entity.Name);
            }
        }

        public class IsPartAndSpeciesCombinationUniqueAsync : TransformationServiceTestBase
        {
            private Species _templateSpecies;

            protected override async Task InitializeTestAsync()
            {
                _templateSpecies = this.Database.Species.First(s => s.Name == "template");
            }

            [Fact]
            public async Task ReturnsTrueForUniqueCombination()
            {
                var result = await this.Transformations.IsPartAndSpeciesCombinationUniqueAsync
                (
                    Bodypart.Wings,
                    _templateSpecies
                );

                Assert.True(result);
            }

            [Fact]
            public async Task ReturnsFalseForNonUniqueCombinationTask()
            {
                var result = await this.Transformations.IsPartAndSpeciesCombinationUniqueAsync
                (
                    Bodypart.Face,
                    _templateSpecies
                );

                Assert.False(result);
            }
        }

        public class GetTransformationByPartAndSpeciesAsync : TransformationServiceTestBase
        {
            private Species _templateSpecies;

            protected override async Task InitializeTestAsync()
            {
                _templateSpecies = this.Database.Species.First(s => s.Name == "template");
            }

            [Fact]
            public async Task RetrievesCorrectBodypart()
            {
                var result = await this.Transformations.GetTransformationsByPartAndSpeciesAsync
                (
                    Bodypart.Face,
                    _templateSpecies
                );

                Assert.True(result.IsSuccess);
                Assert.Single(result.Entity);

                var transformation = result.Entity.First();

                Assert.Equal(Bodypart.Face, transformation.Part);
                Assert.Same(_templateSpecies, transformation.Species);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSpeciesDoesNotExist()
            {
                var nonexistantSpecies = new Species();
                var result = await this.Transformations.GetTransformationsByPartAndSpeciesAsync
                (
                    Bodypart.Face,
                    nonexistantSpecies
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCombinationDoesNotExist()
            {
                var result = await this.Transformations.GetTransformationsByPartAndSpeciesAsync
                (
                    Bodypart.Wings,
                    _templateSpecies
                );

                Assert.False(result.IsSuccess);
            }
        }

        public class GetOrCreateServerUserProtectionAsync : TransformationServiceTestBase
        {
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(1);

            [Fact]
            public async Task CreatesObjectIfOneDoesNotExist()
            {
                Assert.Empty(this.Database.ServerUserProtections);

                var result = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    _user,
                    _guild
                );

                Assert.NotEmpty(this.Database.ServerUserProtections);
                Assert.Same(result.Entity, this.Database.ServerUserProtections.First());
            }

            [Fact]
            public async Task CreatedObjectIsBoundToTheCorrectServer()
            {
                var result = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    _user,
                    _guild
                );

                Assert.Equal((long)_guild.Id, result.Entity.Server.DiscordID);
            }

            [Fact]
            public async Task CreatedObjectIsBoundToTheCorrectUser()
            {
                var result = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    _user,
                    _guild
                );

                Assert.Equal((long)_user.Id, result.Entity.User.DiscordID);
            }

            [Fact]
            public async Task RetrievesCorrectObjectIfOneExists()
            {
                // Create an object
                var created = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    _user,
                    _guild
                );

                // Get it from the database
                var retrieved = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    _user,
                    _guild
                );

                Assert.Same(created.Entity, retrieved.Entity);
            }

            [Fact]
            public async Task CreatedObjectRespectsGlobalDefaults()
            {
                var user = (await this.Users.GetOrRegisterUserAsync(_user)).Entity;

                var globalSetting = new GlobalUserProtection
                {
                    DefaultOptIn = true,
                    DefaultType = ProtectionType.Whitelist,
                    User = user
                };

                this.Database.GlobalUserProtections.Update(globalSetting);
                await this.Database.SaveChangesAsync();

                var localSetting = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    _user,
                    _guild
                );

                Assert.Equal(globalSetting.DefaultOptIn, localSetting.Entity.HasOptedIn);
                Assert.Equal(globalSetting.DefaultType, localSetting.Entity.Type);
                Assert.Same(globalSetting.User, localSetting.Entity.User);
            }
        }

        public class GetOrCreateGlobalUserProtectionAsync : TransformationServiceTestBase
        {
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);

            [Fact]
            public async Task CreatesObjectIfOneDoesNotExist()
            {
                Assert.Empty(this.Database.ServerUserProtections);

                var result = await this.Transformations.GetOrCreateGlobalUserProtectionAsync(_user);

                Assert.NotEmpty(this.Database.GlobalUserProtections);
                Assert.Same(result.Entity, this.Database.GlobalUserProtections.First());
            }

            [Fact]
            public async Task CreatedObjectIsBoundToTheCorrectUser()
            {
                var result = await this.Transformations.GetOrCreateGlobalUserProtectionAsync(_user);

                Assert.Equal((long)_user.Id, result.Entity.User.DiscordID);
            }

            [Fact]
            public async Task RetrievesCorrectObjectIfOneExists()
            {
                // Create an object
                var created = await this.Transformations.GetOrCreateGlobalUserProtectionAsync(_user);

                // Get it from the database
                var retrieved = await this.Transformations.GetOrCreateGlobalUserProtectionAsync(_user);

                Assert.Same(created.Entity, retrieved.Entity);
            }
        }

        public class BlacklistUserAsync : TransformationServiceTestBase
        {
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);
            private readonly IUser _blacklistedUser = MockHelper.CreateDiscordUser(1);

            [Fact]
            public async Task CanBlacklistUser()
            {
                var result = await this.Transformations.BlacklistUserAsync(_user, _blacklistedUser);

                Assert.True(result.IsSuccess);
                Assert.Equal(ModifyEntityAction.Edited, result.ActionTaken);

                Assert.NotEmpty(this.Database.GlobalUserProtections.First().Blacklist);

                Assert.Equal((long)_blacklistedUser.Id, this.Database.GlobalUserProtections.First().Blacklist.First().DiscordID);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsAlreadyBlacklisted()
            {
                // Blacklist the user
                await this.Transformations.BlacklistUserAsync(_user, _blacklistedUser);

                // Then blacklist them again
                var result = await this.Transformations.BlacklistUserAsync(_user, _blacklistedUser);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfTargetUserIsInvokingUser()
            {
                var result = await this.Transformations.BlacklistUserAsync(_user, _user);

                Assert.False(result.IsSuccess);
            }
        }

        public class WhitelistUserAsync : TransformationServiceTestBase
        {
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);
            private readonly IUser _whitelistedUser = MockHelper.CreateDiscordUser(1);

            [Fact]
            public async Task CanWhitelistUser()
            {
                var result = await this.Transformations.WhitelistUserAsync(_user, _whitelistedUser);

                Assert.True(result.IsSuccess);
                Assert.Equal(ModifyEntityAction.Edited, result.ActionTaken);

                Assert.NotEmpty(this.Database.GlobalUserProtections.First().Whitelist);
                Assert.Equal((long)_whitelistedUser.Id, this.Database.GlobalUserProtections.First().Whitelist.First().DiscordID);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsAlreadyWhitelisted()
            {
                // Whitelist the user
                await this.Transformations.WhitelistUserAsync(_user, _whitelistedUser);

                // Then Whitelist them again
                var result = await this.Transformations.WhitelistUserAsync(_user, _whitelistedUser);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfTargetUserIsInvokingUser()
            {
                var result = await this.Transformations.WhitelistUserAsync(_user, _user);

                Assert.False(result.IsSuccess);
            }
        }

        public class SetServerProtectionTypeAsync : TransformationServiceTestBase
        {
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(1);

            [Fact]
            public async Task CanSetType()
            {
                var expected = ProtectionType.Whitelist;
                var result = await this.Transformations.SetServerProtectionTypeAsync
                (
                    _user,
                    _guild,
                    expected
                );

                Assert.True(result.IsSuccess);
                Assert.Equal(ModifyEntityAction.Edited, result.ActionTaken);
                Assert.Equal(expected, this.Database.ServerUserProtections.First().Type);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSameTypeIsAlreadySet()
            {
                var existingType = ProtectionType.Blacklist;
                var result = await this.Transformations.SetServerProtectionTypeAsync
                (
                    _user,
                    _guild,
                    existingType
                );

                Assert.False(result.IsSuccess);
            }
        }

        public class SetDefaultProtectionTypeAsync : TransformationServiceTestBase
        {
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);

            [Fact]
            public async Task CanSetType()
            {
                var expected = ProtectionType.Whitelist;
                var result = await this.Transformations.SetDefaultProtectionTypeAsync
                (
                    _user,
                    expected
                );

                Assert.True(result.IsSuccess);
                Assert.Equal(ModifyEntityAction.Edited, result.ActionTaken);
                Assert.Equal(expected, this.Database.GlobalUserProtections.First().DefaultType);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSameTypeIsAlreadySet()
            {
                var currentType = ProtectionType.Blacklist;
                var result = await this.Transformations.SetDefaultProtectionTypeAsync
                (
                    _user,
                    currentType
                );

                Assert.False(result.IsSuccess);
            }
        }

        public class SetCurrentAppearanceAsDefaultForCharacterAsync : TransformationServiceTestBase
        {
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);
            private User _owner;
            private Character _character;

            private AppearanceConfiguration _appearanceConfiguration;

            protected override async Task InitializeTestAsync()
            {
                _owner = (await this.Users.GetOrRegisterUserAsync(_user)).Entity;

                _character = new Character
                {
                    Owner = _owner
                };

                this.CharacterDatabase.Characters.Update(_character);
                this.CharacterDatabase.SaveChanges();

                // Set up the default appearance
                var getAppearanceConfigurationResult = await this.Transformations.GetOrCreateAppearanceConfigurationAsync
                (
                    _character
                );

                _appearanceConfiguration = getAppearanceConfigurationResult.Entity;
            }

            [Fact]
            public async Task CanSetDefaultAppearance()
            {
                var alteredAppearance = new Appearance
                {
                    Height = 10
                };

                _appearanceConfiguration.CurrentAppearance = alteredAppearance;

                var result = await this.Transformations.SetCurrentAppearanceAsDefaultForCharacterAsync
                (
                    _character
                );

                Assert.True(result.IsSuccess);
                Assert.Equal(ModifyEntityAction.Edited, result.ActionTaken);
                Assert.NotNull(_appearanceConfiguration.DefaultAppearance);
                Assert.Equal(10, _appearanceConfiguration.DefaultAppearance.Height);
            }
        }

        public class ResetCharacterFormAsync : TransformationServiceTestBase
        {
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);
            private User _owner;
            private Character _character;

            private AppearanceConfiguration _appearanceConfiguration;

            protected override async Task InitializeTestAsync()
            {
                _owner = (await this.Users.GetOrRegisterUserAsync(_user)).Entity;

                _character = new Character
                {
                    Owner = _owner
                };

                this.CharacterDatabase.Characters.Update(_character);
                this.CharacterDatabase.SaveChanges();

                // Set up the default appearance
                var getAppearanceConfigurationResult = await this.Transformations.GetOrCreateAppearanceConfigurationAsync
                (
                    _character
                );

                _appearanceConfiguration = getAppearanceConfigurationResult.Entity;
            }

            [Fact]
            public async Task CanResetForm()
            {
                var defaultAppearance = new Appearance
                {
                    Height = 256
                };

                _appearanceConfiguration.DefaultAppearance = defaultAppearance;

                var result = await this.Transformations.ResetCharacterFormAsync(_character);

                Assert.True(result.IsSuccess);
                Assert.Equal(ModifyEntityAction.Edited, result.ActionTaken);
                Assert.NotNull(_appearanceConfiguration.CurrentAppearance);
                Assert.Equal(_appearanceConfiguration.DefaultAppearance.Height, _appearanceConfiguration.CurrentAppearance.Height);
            }
        }

        public class GetAvailableTransformationsAsync : TransformationServiceTestBase
        {
            [Fact]
            public async Task ReturnsNonemptySetForExistingTransformation()
            {
                var result = await this.Transformations.GetAvailableTransformationsAsync(Bodypart.Face);

                Assert.NotEmpty(result);
            }

            [Fact]
            public async Task ReturnsEmptySetForNonExistantTransformation()
            {
                var result = await this.Transformations.GetAvailableTransformationsAsync(Bodypart.Wings);

                Assert.Empty(result);
            }
        }

        public class GetAvailableSpeciesAsync : TransformationServiceTestBase
        {
            [Fact]
            public async Task ReturnsNonEmptySetForUpdatedDatabase()
            {
                await this.Transformations.UpdateTransformationDatabaseAsync();
                var result = await this.Transformations.GetAvailableSpeciesAsync();

                Assert.NotEmpty(result);
            }
        }

        public class CanUserTransformUserAsync : TransformationServiceTestBase
        {
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);
            private readonly IUser _targetUser = MockHelper.CreateDiscordUser(1);

            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(0);

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfTargetUserHasNotOptedIn()
            {
                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    _guild,
                    _user,
                    _targetUser
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsOnTargetUsersBlacklist()
            {
                await EnsureOptedInAsync(_targetUser);
                await this.Transformations.BlacklistUserAsync(_targetUser, _user);

                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    _guild,
                    _user,
                    _targetUser
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfTargetUserUsesWhitelistingAndUserIsNotOnWhitelist()
            {
                await EnsureOptedInAsync(_targetUser);
                await this.Transformations.SetServerProtectionTypeAsync
                (
                    _targetUser,
                    _guild,
                    ProtectionType.Whitelist
                );

                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    _guild,
                    _user,
                    _targetUser
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfTargetUserUsesWhitelistingAndUserIsOnWhitelist()
            {
                await EnsureOptedInAsync(_targetUser);
                await this.Transformations.SetServerProtectionTypeAsync
                (
                    _targetUser,
                    _guild,
                    ProtectionType.Whitelist
                );

                await this.Transformations.WhitelistUserAsync(_targetUser, _user);

                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    _guild,
                    _user,
                    _targetUser
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfUserIsNotOnTargetUsersBlacklist()
            {
                await EnsureOptedInAsync(_targetUser);

                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    _guild,
                    _user,
                    _targetUser
                );

                Assert.True(result.IsSuccess);
            }

            private async Task EnsureOptedInAsync([NotNull] IUser user)
            {
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    user,
                    _guild
                );
                protection.Entity.HasOptedIn = true;

                await this.Database.SaveChangesAsync();
            }
        }

        public class RemoveBodypartAsync : TransformationServiceTestBase
        {
            private readonly IGuild _guild;

            private readonly IUser _owner = MockHelper.CreateDiscordGuildUser(0);
            private readonly IUser _invoker = MockHelper.CreateDiscordGuildUser(1);

            private readonly ICommandContext _context;
            private Character _character;

            private AppearanceConfiguration _appearanceConfiguration;

            public RemoveBodypartAsync()
            {
                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                (
                    c =>
                        c.GetUserAsync
                        (
                            It.Is<ulong>(id => id == _owner.Id),
                            CacheMode.AllowDownload,
                            null
                        )
                )
                .Returns(Task.FromResult((IGuildUser)_owner));

                _guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(_guild);
                mockedContext.Setup(c => c.User).Returns(_invoker);

                _context = mockedContext.Object;

                var services = new ServiceCollection()
                    .AddSingleton(this.Transformations)
                    .AddSingleton<CharacterService>()
                    .BuildServiceProvider();

                this.Transformations.WithDescriptionBuilder(new TransformationDescriptionBuilder(services));
            }

            protected override async Task InitializeTestAsync()
            {
                // Ensure owner is opted into transformations
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    _owner,
                    _guild
                );
                protection.Entity.HasOptedIn = true;

                // Create a test character
                var owner = (await this.Users.GetOrRegisterUserAsync(_owner)).Entity;
                var character = new Character
                {
                    Name = "Test",
                    Owner = owner
                };

                this.CharacterDatabase.Characters.Update(character);
                await this.CharacterDatabase.SaveChangesAsync();

                _character = this.CharacterDatabase.Characters.First();

                // Set up the default appearance
                var getAppearanceConfigurationResult = await this.Transformations.GetOrCreateAppearanceConfigurationAsync
                (
                    _character
                );

                _appearanceConfiguration = getAppearanceConfigurationResult.Entity;
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(_owner, _invoker);

                var result = await this.Transformations.RemoveBodypartAsync
                (
                    _context,
                    _character,
                    Bodypart.Face
                );

                Assert.False(result.IsSuccess);
                Assert.True(_appearanceConfiguration.HasComponent(Bodypart.Face, Chirality.Center));
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterDoesNotHaveBodypart()
            {
                var result = await this.Transformations.RemoveBodypartAsync
                (
                    _context,
                    _character,
                    Bodypart.Wing,
                    Chirality.Left
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfCharacterHasBodypart()
            {
                var result = await this.Transformations.RemoveBodypartAsync
                (
                    _context,
                    _character,
                    Bodypart.Face
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task RemovesCorrectBodypart()
            {
                Assert.Contains(_appearanceConfiguration.CurrentAppearance.Components, c => c.Bodypart == Bodypart.Face);

                await this.Transformations.RemoveBodypartAsync
                (
                    _context,
                    _character,
                    Bodypart.Face
                );

                Assert.DoesNotContain(_appearanceConfiguration.CurrentAppearance.Components, c => c.Bodypart == Bodypart.Face);
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                var result = await this.Transformations.RemoveBodypartAsync
                (
                    _context,
                    _character,
                    Bodypart.Face
                );

                Assert.NotNull(result.ShiftMessage);
            }
        }

        public class ShiftBodypartAsync : TransformationServiceTestBase
        {
            private readonly IGuild _guild;

            private readonly IUser _owner = MockHelper.CreateDiscordGuildUser(0);
            private readonly IUser _invoker = MockHelper.CreateDiscordGuildUser(1);

            private readonly ICommandContext _context;
            private Character _character;

            private AppearanceConfiguration _appearanceConfiguration;

            /// <inheritdoc />
            protected override void RegisterServices(IServiceCollection serviceCollection)
            {
                base.RegisterServices(serviceCollection);

                serviceCollection
                    .AddScoped<OwnedEntityService>()
                    .AddScoped<CommandService>()
                    .AddScoped<CharacterService>();
            }

            public ShiftBodypartAsync()
            {
                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                (
                    c =>
                        c.GetUserAsync
                        (
                            It.Is<ulong>(id => id == _owner.Id),
                            CacheMode.AllowDownload,
                            null
                        )
                )
                .Returns(Task.FromResult((IGuildUser)_owner));

                _guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(_guild);
                mockedContext.Setup(c => c.User).Returns(_invoker);

                _context = mockedContext.Object;

                var characterService = this.Services.GetRequiredService<CharacterService>();
                characterService.WithPronounProvider(new FemininePronounProvider());

                this.Transformations.WithDescriptionBuilder(new TransformationDescriptionBuilder(this.Services));
            }

            protected override async Task InitializeTestAsync()
            {
                // Ensure owner is opted into transformations
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    _owner,
                    _guild
                );

                protection.Entity.HasOptedIn = true;

                // Create a test character
                var owner = (await this.Users.GetOrRegisterUserAsync(_owner)).Entity;
                var character = new Character
                {
                    Name = "Test",
                    Owner = owner,
                    PronounProviderFamily = "Feminine"
                };

                this.CharacterDatabase.Characters.Update(character);
                await this.CharacterDatabase.SaveChangesAsync();

                _character = this.CharacterDatabase.Characters.First();

                // Set up the default appearance
                var getAppearanceConfigurationResult = await this.Transformations.GetOrCreateAppearanceConfigurationAsync
                (
                    _character
                );

                _appearanceConfiguration = getAppearanceConfigurationResult.Entity;
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(_owner, _invoker);

                var result = await this.Transformations.ShiftBodypartAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    "shark"
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSpeciesDoesNotExist()
            {
                var result = await this.Transformations.ShiftBodypartAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    "adadadsasd"
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSpeciesDoesNotHaveBodypart()
            {
                var result = await this.Transformations.ShiftBodypartAsync
                (
                    _context,
                    _character,
                    Bodypart.Wing,
                    "shark",
                    Chirality.Left
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfBodypartIsAlreadyThatSpecies()
            {
                var result = await this.Transformations.ShiftBodypartAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    "template"
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task AddsBodypartIfItDoesNotAlreadyExist()
            {
                Assert.False(_appearanceConfiguration.HasComponent(Bodypart.Tail, Chirality.Center));

                var result = await this.Transformations.ShiftBodypartAsync
                (
                    _context,
                    _character,
                    Bodypart.Tail,
                    "shark"
                );

                Assert.True(result.IsSuccess);
                Assert.True(_appearanceConfiguration.HasComponent(Bodypart.Tail, Chirality.Center));
            }

            [Fact]
            public async Task ShiftsBodypartIntoCorrectSpecies()
            {
                var result = await this.Transformations.ShiftBodypartAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    "shark"
                );

                Assert.True(result.IsSuccess);
                Assert.Equal("shark", _appearanceConfiguration.GetAppearanceComponent(Bodypart.Face, Chirality.Center).Transformation.Species.Name);
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                var result = await this.Transformations.ShiftBodypartAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    "shark"
                );

                Assert.NotNull(result.ShiftMessage);
                Assert.NotEmpty(result.ShiftMessage);
            }

            [Fact]
            public async Task ShiftingBodypartDoesNotAlterDefaultAppearance()
            {
                await this.Transformations.ShiftBodypartAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    "shark"
                );

                Assert.NotEqual
                (
                    "shark",
                    _appearanceConfiguration.DefaultAppearance.Components.First(c => c.Bodypart == Bodypart.Face).Transformation.Species.Name
                );
            }

            [Fact]
            public async Task CanResetAppearance()
            {
                await this.Transformations.ShiftBodypartAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    "shark"
                );

                await this.Transformations.ResetCharacterFormAsync
                (
                    _character
                );

                Assert.NotEqual
                (
                    "shark",
                    _appearanceConfiguration.CurrentAppearance.Components.First(c => c.Bodypart == Bodypart.Face).Transformation.Species.Name
                );
            }

            [Fact]
            public async Task CanSetCustomDefaultAppearance()
            {
                await this.Transformations.ShiftBodypartAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    "shark"
                );

                await this.Transformations.SetCurrentAppearanceAsDefaultForCharacterAsync
                (
                    _character
                );

                Assert.Equal
                (
                    "shark",
                    _appearanceConfiguration.DefaultAppearance.Components.First(c => c.Bodypart == Bodypart.Face).Transformation.Species.Name
                );
            }

            [Fact]
            public async Task CanResetToCustomDefaultAppearance()
            {
                await this.Transformations.ShiftBodypartAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    "shark"
                );

                await this.Transformations.SetCurrentAppearanceAsDefaultForCharacterAsync
                (
                    _character
                );

                await this.Transformations.ShiftBodypartAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    "shark-dronie"
                );

                await this.Transformations.ResetCharacterFormAsync
                (
                    _character
                );

                Assert.Equal
                (
                    "shark",
                    _appearanceConfiguration.CurrentAppearance.Components.First(c => c.Bodypart == Bodypart.Face).Transformation.Species.Name
                );
            }
        }

        public class ShiftBodypartColourAsync : TransformationServiceTestBase
        {
            private readonly IGuild _guild;

            private readonly IUser _owner = MockHelper.CreateDiscordGuildUser(0);
            private readonly IUser _invoker = MockHelper.CreateDiscordGuildUser(1);

            private readonly Colour _newColour;

            private readonly ICommandContext _context;
            private Character _character;

            private Colour _originalColour;

            private AppearanceConfiguration _appearanceConfiguration;

            public ShiftBodypartColourAsync()
            {
                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                (
                    c =>
                        c.GetUserAsync
                        (
                            It.Is<ulong>(id => id == _owner.Id),
                            CacheMode.AllowDownload,
                            null
                        )
                )
                .Returns(Task.FromResult((IGuildUser)_owner));

                _guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(_guild);
                mockedContext.Setup(c => c.User).Returns(_invoker);

                _context = mockedContext.Object;

                Colour.TryParse("bright purple", out _newColour);
            }

            /// <inheritdoc />
            protected override void RegisterServices(IServiceCollection serviceCollection)
            {
                base.RegisterServices(serviceCollection);

                serviceCollection
                    .AddScoped<OwnedEntityService>()
                    .AddScoped<CommandService>()
                    .AddScoped<CharacterService>();
            }

            protected override async Task InitializeTestAsync()
            {
                this.Services.GetRequiredService<CharacterService>().WithPronounProvider(new TheyPronounProvider());
                this.Transformations.WithDescriptionBuilder(new TransformationDescriptionBuilder(this.Services));

                // Ensure owner is opted into transformations
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    _owner,
                    _guild
                );

                protection.Entity.HasOptedIn = true;

                // Create a test character
                var owner = (await this.Users.GetOrRegisterUserAsync(_owner)).Entity;
                var character = new Character
                {
                    Name = "Test",
                    Owner = owner,
                    PronounProviderFamily = "They"
                };

                this.CharacterDatabase.Characters.Update(character);
                await this.CharacterDatabase.SaveChangesAsync();

                _character = this.CharacterDatabase.Characters.First();

                // Set up the default appearance
                var getAppearanceConfigurationResult = await this.Transformations.GetOrCreateAppearanceConfigurationAsync
                (
                    _character
                );

                _appearanceConfiguration = getAppearanceConfigurationResult.Entity;
                _originalColour = _appearanceConfiguration.GetAppearanceComponent(Bodypart.Face, Chirality.Center).BaseColour;
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(_owner, _invoker);

                var result = await this.Transformations.ShiftBodypartColourAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _newColour
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterDoesNotHaveBodypart()
            {
                var result = await this.Transformations.ShiftBodypartColourAsync
                (
                    _context,
                    _character,
                    Bodypart.Wing,
                    _newColour,
                    Chirality.Left
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfBodypartIsAlreadyThatColour()
            {
                var result = await this.Transformations.ShiftBodypartColourAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _originalColour
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task CanShiftBodypartColour()
            {
                var result = await this.Transformations.ShiftBodypartColourAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _newColour
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ShiftsBodypartIntoCorrectColour()
            {
                await this.Transformations.ShiftBodypartColourAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _newColour
                );

                var face = _appearanceConfiguration.CurrentAppearance.Components.First(c => c.Bodypart == Bodypart.Face);
                Assert.Same(_newColour, face.BaseColour);
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                var result = await this.Transformations.ShiftBodypartColourAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _newColour
                );

                Assert.NotNull(result.ShiftMessage);
                Assert.NotEmpty(result.ShiftMessage);
            }
        }

        public class ShiftBodypartPatternAsync : TransformationServiceTestBase
        {
            private readonly IGuild _guild;

            private readonly IUser _owner = MockHelper.CreateDiscordGuildUser(0);
            private readonly IUser _invoker = MockHelper.CreateDiscordGuildUser(1);

            private readonly Pattern _newPattern;
            private readonly Colour _newPatternColour;

            private readonly ICommandContext _context;
            private Character _character;

            private AppearanceConfiguration _appearanceConfiguration;

            public ShiftBodypartPatternAsync()
            {
                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                (
                    c =>
                        c.GetUserAsync
                        (
                            It.Is<ulong>(id => id == _owner.Id),
                            CacheMode.AllowDownload,
                            null
                        )
                )
                .Returns(Task.FromResult((IGuildUser)_owner));

                _guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(_guild);
                mockedContext.Setup(c => c.User).Returns(_invoker);

                _context = mockedContext.Object;

                var services = new ServiceCollection()
                    .AddSingleton<ContentService>()
                    .AddSingleton<CommandService>()
                    .AddSingleton<OwnedEntityService>()
                    .AddSingleton(this.Transformations)
                    .AddSingleton
                    (
                        s =>
                            ActivatorUtilities.CreateInstance<CharacterService>(s).WithPronounProvider
                            (
                                new TheyPronounProvider()
                            )
                    )
                    .BuildServiceProvider();

                this.Transformations.WithDescriptionBuilder(new TransformationDescriptionBuilder(services));

                _newPattern = Pattern.Swirly;
                Colour.TryParse("bright purple", out _newPatternColour);
            }

            protected override async Task InitializeTestAsync()
            {
                // Ensure owner is opted into transformations
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    _owner,
                    _guild
                );

                protection.Entity.HasOptedIn = true;

                // Create a test character
                var owner = (await this.Users.GetOrRegisterUserAsync(_owner)).Entity;
                var character = new Character
                {
                    Name = "Test",
                    Owner = owner,
                    PronounProviderFamily = "They"
                };

                this.CharacterDatabase.Characters.Update(character);
                await this.CharacterDatabase.SaveChangesAsync();

                _character = this.CharacterDatabase.Characters.First();

                // Set up the default appearance
                var getAppearanceConfigurationResult = await this.Transformations.GetOrCreateAppearanceConfigurationAsync
                (
                    _character
                );

                _appearanceConfiguration = getAppearanceConfigurationResult.Entity;

                await this.CharacterDatabase.SaveChangesAsync();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(_owner, _invoker);

                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _newPattern,
                    _newPatternColour
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterDoesNotHaveBodypart()
            {
                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    _context,
                    _character,
                    Bodypart.Wing,
                    _newPattern,
                    _newPatternColour,
                    Chirality.Left
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfBodypartIsAlreadyThatPattern()
            {
                await this.Transformations.ShiftBodypartPatternAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _newPattern,
                    _newPatternColour
                );

                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _newPattern,
                    _newPatternColour
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task CanShiftBodypartPattern()
            {
                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _newPattern,
                    _newPatternColour
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ShiftsBodypartIntoCorrectPattern()
            {
                await this.Transformations.ShiftBodypartPatternAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _newPattern,
                    _newPatternColour
                );

                var face = _appearanceConfiguration.GetAppearanceComponent(Bodypart.Face, Chirality.Center);
                Assert.Equal(_newPattern, face.Pattern);
            }

            [Fact]
            public async Task ShiftsBodypartIntoCorrectPatternColour()
            {
                await this.Transformations.ShiftBodypartPatternAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _newPattern,
                    _newPatternColour
                );

                var face = _appearanceConfiguration.GetAppearanceComponent(Bodypart.Face, Chirality.Center);
                Assert.Equal(_newPatternColour, face.PatternColour);
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _newPattern,
                    _newPatternColour
                );

                Assert.NotNull(result.ShiftMessage);
                Assert.NotEmpty(result.ShiftMessage);
            }
        }

        public class ShiftBodypartPatternColourAsync : TransformationServiceTestBase
        {
            private readonly IGuild _guild;

            private readonly IUser _owner = MockHelper.CreateDiscordGuildUser(0);
            private readonly IUser _invoker = MockHelper.CreateDiscordGuildUser(1);

            private readonly Colour _newPatternColour;

            private readonly ICommandContext _context;
            private Character _character;

            private Colour _originalPatternColour;

            private AppearanceConfiguration _appearanceConfiguration;

            public ShiftBodypartPatternColourAsync()
            {
                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                (
                    c =>
                        c.GetUserAsync
                        (
                            It.Is<ulong>(id => id == _owner.Id),
                            CacheMode.AllowDownload,
                            null
                        )
                )
                .Returns(Task.FromResult((IGuildUser)_owner));

                _guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(_guild);
                mockedContext.Setup(c => c.User).Returns(_invoker);

                _context = mockedContext.Object;

                Colour.TryParse("bright purple", out _newPatternColour);
            }

            /// <inheritdoc />
            protected override void RegisterServices(IServiceCollection serviceCollection)
            {
                base.RegisterServices(serviceCollection);

                serviceCollection
                    .AddScoped<OwnedEntityService>()
                    .AddScoped<CommandService>()
                    .AddScoped<CharacterService>();
            }

            protected override async Task InitializeTestAsync()
            {
                this.Services.GetRequiredService<CharacterService>().WithPronounProvider(new TheyPronounProvider());
                this.Transformations.WithDescriptionBuilder(new TransformationDescriptionBuilder(this.Services));

                // Ensure owner is opted into transformations
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    _owner,
                    _guild
                );

                protection.Entity.HasOptedIn = true;

                // Create a test character
                var owner = (await this.Users.GetOrRegisterUserAsync(_owner)).Entity;
                var character = new Character
                {
                    Name = "Test",
                    Owner = owner,
                    PronounProviderFamily = "They"
                };

                this.CharacterDatabase.Characters.Update(character);
                await this.CharacterDatabase.SaveChangesAsync();

                _character = this.CharacterDatabase.Characters.First();

                Colour.TryParse("dull white", out _originalPatternColour);
                Assert.NotNull(_originalPatternColour);

                await this.Transformations.ShiftBodypartPatternAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    Pattern.Swirly,
                    _originalPatternColour
                );

                // Set up the default appearance
                var getAppearanceConfigurationResult = await this.Transformations.GetOrCreateAppearanceConfigurationAsync
                (
                    _character
                );

                _appearanceConfiguration = getAppearanceConfigurationResult.Entity;
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(_owner, _invoker);

                var result = await this.Transformations.ShiftPatternColourAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _newPatternColour
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterDoesNotHaveBodypart()
            {
                var result = await this.Transformations.ShiftPatternColourAsync
                (
                    _context,
                    _character,
                    Bodypart.Wing,
                    _newPatternColour,
                    Chirality.Left
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfBodypartDoesNotHavePattern()
            {
                var result = await this.Transformations.ShiftPatternColourAsync
                (
                    _context,
                    _character,
                    Bodypart.Arm,
                    _newPatternColour,
                    Chirality.Left
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfBodypartIsAlreadyThatColour()
            {
                await this.Transformations.ShiftPatternColourAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _newPatternColour
                );

                var result = await this.Transformations.ShiftPatternColourAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _newPatternColour
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task CanShiftColour()
            {
                var result = await this.Transformations.ShiftPatternColourAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _newPatternColour
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ShiftsIntoCorrectColour()
            {
                await this.Transformations.ShiftPatternColourAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _newPatternColour
                );

                var face = _appearanceConfiguration.GetAppearanceComponent(Bodypart.Face, Chirality.Center);
                Assert.Equal(_newPatternColour, face.PatternColour);
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                var result = await this.Transformations.ShiftPatternColourAsync
                (
                    _context,
                    _character,
                    Bodypart.Face,
                    _newPatternColour
                );

                Assert.NotNull(result.ShiftMessage);
                Assert.NotEmpty(result.ShiftMessage);
            }
        }
    }
}
