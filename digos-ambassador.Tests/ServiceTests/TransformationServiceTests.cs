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

using DIGOS.Ambassador.Database.Appearances;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Database.Users;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Tests.TestBases;
using DIGOS.Ambassador.Tests.Utility;
using DIGOS.Ambassador.Transformations;

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
            public override Task InitializeAsync()
            {
                // Let tests initialize the transformation database.
                return Task.CompletedTask;
            }

            [Fact]
            public async Task FindsBundledSpecies()
            {
                var result = await this.Transformations.UpdateTransformationDatabaseAsync(this.Database);

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
                var result = await this.Transformations.IsSpeciesNameUniqueAsync(this.Database, name);

                Assert.True(result);
            }

            [Theory]
            [InlineData("template")]
            public async Task ReturnsFalseForNonUniqueName([NotNull] string name)
            {
                var result = await this.Transformations.IsSpeciesNameUniqueAsync(this.Database, name);

                Assert.False(result);
            }

            [Theory]
            [InlineData("TEMPLATE")]
            public async Task IsCaseInsensitive([NotNull] string name)
            {
                var result = await this.Transformations.IsSpeciesNameUniqueAsync(this.Database, name);

                Assert.False(result);
            }
        }

        public class GetSpeciesByNameAsync : TransformationServiceTestBase
        {
            [Fact]
            public async Task ReturnsCorrectSpeciesForGivenName()
            {
                var result = await this.Transformations.GetSpeciesByNameAsync(this.Database, "template");

                Assert.True(result.IsSuccess);
                Assert.Equal("template", result.Entity.Name);
            }

            [Fact]
            public async Task ReturnsUnsuccesfulResultForNonexistantName()
            {
                var result = await this.Transformations.GetSpeciesByNameAsync(this.Database, "aasddduaiii");

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task IsCaseInsensitive()
            {
                var result = await this.Transformations.GetSpeciesByNameAsync(this.Database, "TEMPLATE");

                Assert.True(result.IsSuccess);
                Assert.Equal("template", result.Entity.Name);
            }
        }

        public class GetSpeciesByName : TransformationServiceTestBase
        {
            [Fact]
            public void ReturnsCorrectSpeciesForGivenName()
            {
                var result = this.Transformations.GetSpeciesByName(this.Database, "template");

                Assert.True(result.IsSuccess);
                Assert.Equal("template", result.Entity.Name);
            }

            [Fact]
            public void ReturnsUnsuccesfulResultForNonexistantName()
            {
                var result = this.Transformations.GetSpeciesByName(this.Database, "aasddduaiii");

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public void IsCaseInsensitive()
            {
                var result = this.Transformations.GetSpeciesByName(this.Database, "TEMPLATE");

                Assert.True(result.IsSuccess);
                Assert.Equal("template", result.Entity.Name);
            }
        }

        public class IsPartAndSpeciesCombinationUniqueAsync : TransformationServiceTestBase
        {
            private Species _templateSpecies;

            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();
                this._templateSpecies = this.Database.Species.First(s => s.Name == "template");
            }

            [Fact]
            public async Task ReturnsTrueForUniqueCombination()
            {
                var result = await this.Transformations.IsPartAndSpeciesCombinationUniqueAsync
                (
                    this.Database,
                    Bodypart.Wings,
                    this._templateSpecies
                );

                Assert.True(result);
            }

            [Fact]
            public async Task ReturnsFalseForNonUniqueCombinationTask()
            {
                var result = await this.Transformations.IsPartAndSpeciesCombinationUniqueAsync
                (
                    this.Database,
                    Bodypart.Face,
                    this._templateSpecies
                );

                Assert.False(result);
            }
        }

        public class GetTransformationByPartAndSpeciesAsync : TransformationServiceTestBase
        {
            private Species _templateSpecies;

            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();
                this._templateSpecies = this.Database.Species.First(s => s.Name == "template");
            }

            [Fact]
            public async Task RetrievesCorrectBodypart()
            {
                var result = await this.Transformations.GetTransformationsByPartAndSpeciesAsync
                (
                    this.Database,
                    Bodypart.Face,
                    this._templateSpecies
                );

                Assert.True(result.IsSuccess);
                Assert.Single(result.Entity);

                var transformation = result.Entity.First();

                Assert.Equal(Bodypart.Face, transformation.Part);
                Assert.Same(this._templateSpecies, transformation.Species);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSpeciesDoesNotExist()
            {
                var nonexistantSpecies = new Species();
                var result = await this.Transformations.GetTransformationsByPartAndSpeciesAsync
                (
                    this.Database,
                    Bodypart.Face,
                    nonexistantSpecies
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCombinationDoesNotExist()
            {
                var result = await this.Transformations.GetTransformationsByPartAndSpeciesAsync
                (
                    this.Database,
                    Bodypart.Wings,
                    this._templateSpecies
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
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
                    this.Database,
                    this._user,
                    this._guild
                );

                Assert.NotEmpty(this.Database.ServerUserProtections);
                Assert.Same(result.Entity, this.Database.ServerUserProtections.First());
            }

            [Fact]
            public async Task CreatedObjectIsBoundToTheCorrectServer()
            {
                var result = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this._user,
                    this._guild
                );

                Assert.Equal((long)this._guild.Id, result.Entity.Server.DiscordID);
            }

            [Fact]
            public async Task CreatedObjectIsBoundToTheCorrectUser()
            {
                var result = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this._user,
                    this._guild
                );

                Assert.Equal((long)this._user.Id, result.Entity.User.DiscordID);
            }

            [Fact]
            public async Task RetrievesCorrectObjectIfOneExists()
            {
                // Create an object
                var created = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this._user,
                    this._guild
                );

                // Get it from the database
                var retrieved = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this._user,
                    this._guild
                );

                Assert.Same(created.Entity, retrieved.Entity);
            }

            [Fact]
            public async Task CreatedObjectRespectsGlobalDefaults()
            {
                var user = (await this.Database.GetOrRegisterUserAsync(this._user)).Entity;

                var globalSetting = new GlobalUserProtection
                {
                    DefaultOptIn = true,
                    DefaultType = ProtectionType.Whitelist,
                    User = user
                };

                this.Database.GlobalUserProtections.Add(globalSetting);
                await this.Database.SaveChangesAsync();

                var localSetting = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this._user,
                    this._guild
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

                var result = await this.Transformations.GetOrCreateGlobalUserProtectionAsync(this.Database, this._user);

                Assert.NotEmpty(this.Database.GlobalUserProtections);
                Assert.Same(result.Entity, this.Database.GlobalUserProtections.First());
            }

            [Fact]
            public async Task CreatedObjectIsBoundToTheCorrectUser()
            {
                var result = await this.Transformations.GetOrCreateGlobalUserProtectionAsync(this.Database, this._user);

                Assert.Equal((long)this._user.Id, result.Entity.User.DiscordID);
            }

            [Fact]
            public async Task RetrievesCorrectObjectIfOneExists()
            {
                // Create an object
                var created = await this.Transformations.GetOrCreateGlobalUserProtectionAsync(this.Database, this._user);

                // Get it from the database
                var retrieved = await this.Transformations.GetOrCreateGlobalUserProtectionAsync(this.Database, this._user);

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
                var result = await this.Transformations.BlacklistUserAsync(this.Database, this._user, this._blacklistedUser);

                Assert.True(result.IsSuccess);
                Assert.Equal(ModifyEntityAction.Edited, result.ActionTaken);

                Assert.NotEmpty(this.Database.GlobalUserProtections.First().Blacklist);

                Assert.Equal((long)this._blacklistedUser.Id, this.Database.GlobalUserProtections.First().Blacklist.First().DiscordID);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsAlreadyBlacklisted()
            {
                // Blacklist the user
                await this.Transformations.BlacklistUserAsync(this.Database, this._user, this._blacklistedUser);

                // Then blacklist them again
                var result = await this.Transformations.BlacklistUserAsync(this.Database, this._user, this._blacklistedUser);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfTargetUserIsInvokingUser()
            {
                var result = await this.Transformations.BlacklistUserAsync(this.Database, this._user, this._user);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }
        }

        public class WhitelistUserAsync : TransformationServiceTestBase
        {
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);
            private readonly IUser _whitelistedUser = MockHelper.CreateDiscordUser(1);

            [Fact]
            public async Task CanWhitelistUser()
            {
                var result = await this.Transformations.WhitelistUserAsync(this.Database, this._user, this._whitelistedUser);

                Assert.True(result.IsSuccess);
                Assert.Equal(ModifyEntityAction.Edited, result.ActionTaken);

                Assert.NotEmpty(this.Database.GlobalUserProtections.First().Whitelist);
                Assert.Equal((long)this._whitelistedUser.Id, this.Database.GlobalUserProtections.First().Whitelist.First().DiscordID);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsAlreadyWhitelisted()
            {
                // Whitelist the user
                await this.Transformations.WhitelistUserAsync(this.Database, this._user, this._whitelistedUser);

                // Then Whitelist them again
                var result = await this.Transformations.WhitelistUserAsync(this.Database, this._user, this._whitelistedUser);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfTargetUserIsInvokingUser()
            {
                var result = await this.Transformations.WhitelistUserAsync(this.Database, this._user, this._user);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
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
                    this.Database,
                    this._user,
                    this._guild,
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
                    this.Database,
                    this._user,
                    this._guild,
                    existingType
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
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
                    this.Database,
                    this._user,
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
                    this.Database,
                    this._user,
                    currentType
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }
        }

        public class SetCurrentAppearanceAsDefaultForCharacterAsync : TransformationServiceTestBase
        {
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);
            private User _owner;
            private Character _character;

            public override async Task InitializeAsync()
            {
                this._owner = (await this.Database.GetOrRegisterUserAsync(this._user)).Entity;

                this._character = new Character
                {
                    Owner = this._owner,
                    CurrentAppearance = new Appearance(),
                    DefaultAppearance = new Appearance()
                };

                this.Database.Characters.Add(this._character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task CanSetDefaultAppearance()
            {
                var alteredAppearance = new Appearance
                {
                    Height = 10
                };

                this._character.CurrentAppearance = alteredAppearance;

                var result = await this.Transformations.SetCurrentAppearanceAsDefaultForCharacterAsync
                (
                    this.Database,
                    this._character
                );

                Assert.True(result.IsSuccess);
                Assert.Equal(ModifyEntityAction.Edited, result.ActionTaken);
                Assert.NotNull(this._character.DefaultAppearance);
                Assert.Equal(10, this._character.DefaultAppearance.Height);
            }
        }

        public class ResetCharacterFormAsync : TransformationServiceTestBase
        {
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);
            private User _owner;
            private Character _character;

            public override async Task InitializeAsync()
            {
                this._owner = (await this.Database.GetOrRegisterUserAsync(this._user)).Entity;

                this._character = new Character
                {
                    Owner = this._owner,
                    CurrentAppearance = new Appearance()
                };

                this.Database.Characters.Add(this._character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task CanResetForm()
            {
                var defaultAppearance = new Appearance
                {
                    Height = 256
                };

                this._character.DefaultAppearance = defaultAppearance;

                var result = await this.Transformations.ResetCharacterFormAsync(this.Database, this._character);

                Assert.True(result.IsSuccess);
                Assert.Equal(ModifyEntityAction.Edited, result.ActionTaken);
                Assert.NotNull(this._character.CurrentAppearance);
                Assert.Equal(this._character.DefaultAppearance.Height, this._character.CurrentAppearance.Height);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterDoesNotHaveADefaultAppearance()
            {
                var result = await this.Transformations.ResetCharacterFormAsync(this.Database, this._character);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }
        }

        public class GetAvailableTransformationsAsync : TransformationServiceTestBase
        {
            [Fact]
            public async Task ReturnsNonemptySetForExistingTransformation()
            {
                var result = await this.Transformations.GetAvailableTransformationsAsync(this.Database, Bodypart.Face);

                Assert.NotEmpty(result);
            }

            [Fact]
            public async Task ReturnsEmptySetForNonExistantTransformation()
            {
                var result = await this.Transformations.GetAvailableTransformationsAsync(this.Database, Bodypart.Wings);

                Assert.Empty(result);
            }
        }

        public class GetAvailableSpeciesAsync : TransformationServiceTestBase
        {
            public override Task InitializeAsync()
            {
                return Task.CompletedTask;
            }

            [Fact]
            public async Task ReturnsNonEmptySetForUpdatedDatabase()
            {
                await this.Transformations.UpdateTransformationDatabaseAsync(this.Database);
                var result = await this.Transformations.GetAvailableSpeciesAsync(this.Database);

                Assert.NotEmpty(result);
            }

            [Fact]
            public async Task ReturnsEmptySetForEmptyDatabase()
            {
                var result = await this.Transformations.GetAvailableSpeciesAsync(this.Database);

                Assert.Empty(result);
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
                    this.Database,
                    this._guild,
                    this._user,
                    this._targetUser
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsOnTargetUsersBlacklist()
            {
                await EnsureOptedInAsync(this._targetUser);
                await this.Transformations.BlacklistUserAsync(this.Database, this._targetUser, this._user);

                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    this.Database,
                    this._guild,
                    this._user,
                    this._targetUser
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfTargetUserUsesWhitelistingAndUserIsNotOnWhitelist()
            {
                await EnsureOptedInAsync(this._targetUser);
                await this.Transformations.SetServerProtectionTypeAsync
                (
                    this.Database,
                    this._targetUser,
                    this._guild,
                    ProtectionType.Whitelist
                );

                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    this.Database,
                    this._guild,
                    this._user,
                    this._targetUser
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfTargetUserUsesWhitelistingAndUserIsOnWhitelist()
            {
                await EnsureOptedInAsync(this._targetUser);
                await this.Transformations.SetServerProtectionTypeAsync
                (
                    this.Database,
                    this._targetUser,
                    this._guild,
                    ProtectionType.Whitelist
                );

                await this.Transformations.WhitelistUserAsync(this.Database, this._targetUser, this._user);

                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    this.Database,
                    this._guild,
                    this._user,
                    this._targetUser
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfUserIsNotOnTargetUsersBlacklist()
            {
                await EnsureOptedInAsync(this._targetUser);

                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    this.Database,
                    this._guild,
                    this._user,
                    this._targetUser
                );

                Assert.True(result.IsSuccess);
            }

            private async Task EnsureOptedInAsync([NotNull] IUser user)
            {
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    user,
                    this._guild
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

            public RemoveBodypartAsync()
            {
                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                (
                    c =>
                        c.GetUserAsync
                        (
                            It.Is<ulong>(id => id == this._owner.Id),
                            CacheMode.AllowDownload,
                            null
                        )
                )
                .Returns(Task.FromResult((IGuildUser)this._owner));

                this._guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(this._guild);
                mockedContext.Setup(c => c.User).Returns(this._invoker);

                this._context = mockedContext.Object;

                var services = new ServiceCollection()
                    .AddSingleton(this.Transformations)
                    .AddSingleton<CharacterService>()
                    .BuildServiceProvider();

                this.Transformations.WithDescriptionBuilder(new TransformationDescriptionBuilder(services));
            }

            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();

                // Ensure owner is opted into transformations
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this._owner,
                    this._guild
                );
                protection.Entity.HasOptedIn = true;

                // Create a test character
                var owner = (await this.Database.GetOrRegisterUserAsync(this._owner)).Entity;
                this._character = new Character
                {
                    Name = "Test",
                    CurrentAppearance = (await Appearance.CreateDefaultAsync(this.Database, this.Transformations)).Entity,
                    Owner = owner
                };

                this.Database.Characters.Add(this._character);

                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(this.Database, this._owner, this._invoker);

                var result = await this.Transformations.RemoveBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
                Assert.True(this._character.HasComponent(Bodypart.Face, Chirality.Center));
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterDoesNotHaveBodypart()
            {
                var result = await this.Transformations.RemoveBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Wing,
                    Chirality.Left
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfCharacterHasBodypart()
            {
                var result = await this.Transformations.RemoveBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task RemovesCorrectBodypart()
            {
                Assert.Contains(this._character.CurrentAppearance.Components, c => c.Bodypart == Bodypart.Face);

                await this.Transformations.RemoveBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face
                );

                Assert.DoesNotContain(this._character.CurrentAppearance.Components, c => c.Bodypart == Bodypart.Face);
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                var result = await this.Transformations.RemoveBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face
                );

                Assert.NotNull(result.ShiftMessage);
            }
        }

        public class AddBodypartAsync : TransformationServiceTestBase
        {
            private readonly IGuild _guild;

            private readonly IUser _owner = MockHelper.CreateDiscordGuildUser(0);
            private readonly IUser _invoker = MockHelper.CreateDiscordGuildUser(1);

            private readonly ICommandContext _context;
            private Character _character;

            public AddBodypartAsync()
            {
                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                (
                    c =>
                        c.GetUserAsync
                        (
                            It.Is<ulong>(id => id == this._owner.Id),
                            CacheMode.AllowDownload,
                            null
                        )
                )
                .Returns(Task.FromResult((IGuildUser)this._owner));

                this._guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(this._guild);
                mockedContext.Setup(c => c.User).Returns(this._invoker);

                this._context = mockedContext.Object;

                var services = new ServiceCollection()
                    .AddSingleton(this.Transformations)
                    .AddSingleton<CharacterService>()
                    .BuildServiceProvider();

                this.Transformations.WithDescriptionBuilder(new TransformationDescriptionBuilder(services));
            }

            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();

                // Ensure owner is opted into transformations
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this._owner,
                    this._guild
                );

                protection.Entity.HasOptedIn = true;

                // Create a test character
                var owner = (await this.Database.GetOrRegisterUserAsync(this._owner)).Entity;
                this._character = new Character
                {
                    Name = "Test",
                    CurrentAppearance = (await Appearance.CreateDefaultAsync(this.Database, this.Transformations)).Entity,
                    Owner = owner
                };

                this.Database.Characters.Add(this._character);

                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(this.Database, this._owner, this._invoker);

                var result = await this.Transformations.AddBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    "template"
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
                Assert.Contains(this._character.CurrentAppearance.Components, c => c.Bodypart == Bodypart.Face);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterHasBodypart()
            {
                var result = await this.Transformations.AddBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    "template"
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSpeciesDoesNotExist()
            {
                var result = await this.Transformations.AddBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    "aaaa"
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSpeciesDoesNotHaveBodypart()
            {
                var result = await this.Transformations.AddBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Wing,
                    "template",
                    Chirality.Left
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfCharacterDoesNotHaveBodypart()
            {
                // Remove the face
                await this.Transformations.RemoveBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face
                );

                // Then add it again
                var result = await this.Transformations.AddBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    "template"
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task AddsCorrectBodypart()
            {
                // Remove the face
                await this.Transformations.RemoveBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face
                );

                Assert.False(this._character.HasComponent(Bodypart.Face, Chirality.Center));

                // Then add it again
                await this.Transformations.AddBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    "template"
                );

                Assert.True(this._character.HasComponent(Bodypart.Face, Chirality.Center));
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                // Remove the face
                await this.Transformations.RemoveBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face
                );

                // Then add it again
                var result = await this.Transformations.AddBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    "template"
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

            public ShiftBodypartAsync()
            {
                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                (
                    c =>
                        c.GetUserAsync
                        (
                            It.Is<ulong>(id => id == this._owner.Id),
                            CacheMode.AllowDownload,
                            null
                        )
                )
                .Returns(Task.FromResult((IGuildUser)this._owner));

                this._guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(this._guild);
                mockedContext.Setup(c => c.User).Returns(this._invoker);

                this._context = mockedContext.Object;

                var services = new ServiceCollection()
                    .AddSingleton(this.Transformations)
                    .AddSingleton<ContentService>()
                    .AddSingleton<OwnedEntityService>()
                    .AddSingleton<CommandService>()
                    .AddSingleton<CharacterService>()
                    .BuildServiceProvider();

                var characterService = services.GetRequiredService<CharacterService>();
                characterService.WithPronounProvider(new FemininePronounProvider());

                this.Transformations.WithDescriptionBuilder(new TransformationDescriptionBuilder(services));
            }

            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();

                // Ensure owner is opted into transformations
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this._owner,
                    this._guild
                );

                protection.Entity.HasOptedIn = true;

                // Create a test character
                var owner = (await this.Database.GetOrRegisterUserAsync(this._owner)).Entity;
                this._character = new Character
                {
                    Name = "Test",
                    DefaultAppearance = (await Appearance.CreateDefaultAsync(this.Database, this.Transformations)).Entity,
                    CurrentAppearance = (await Appearance.CreateDefaultAsync(this.Database, this.Transformations)).Entity,
                    Owner = owner,
                    PronounProviderFamily = "Feminine"
                };

                this.Database.Characters.Add(this._character);

                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(this.Database, this._owner, this._invoker);

                var result = await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    "shark"
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSpeciesDoesNotExist()
            {
                var result = await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    "adadadsasd"
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSpeciesDoesNotHaveBodypart()
            {
                var result = await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Wing,
                    "shark",
                    Chirality.Left
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfBodypartIsAlreadyThatSpecies()
            {
                var result = await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    "template"
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task AddsBodypartIfItDoesNotAlreadyExist()
            {
                Assert.False(this._character.HasComponent(Bodypart.Tail, Chirality.Center));

                var result = await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Tail,
                    "shark"
                );

                Assert.True(result.IsSuccess);
                Assert.True(this._character.HasComponent(Bodypart.Tail, Chirality.Center));
            }

            [Fact]
            public async Task ShiftsBodypartIntoCorrectSpecies()
            {
                var result = await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    "shark"
                );

                Assert.True(result.IsSuccess);
                Assert.Equal("shark", this._character.GetAppearanceComponent(Bodypart.Face, Chirality.Center).Transformation.Species.Name);
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                var result = await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
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
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    "shark"
                );

                Assert.NotEqual
                (
                    "shark",
                    this._character.DefaultAppearance.Components.First(c => c.Bodypart == Bodypart.Face).Transformation.Species.Name
                );
            }

            [Fact]
            public async Task CanResetAppearance()
            {
                await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    "shark"
                );

                await this.Transformations.ResetCharacterFormAsync
                (
                    this.Database,
                    this._character
                );

                Assert.NotEqual
                (
                    "shark",
                    this._character.CurrentAppearance.Components.First(c => c.Bodypart == Bodypart.Face).Transformation.Species.Name
                );
            }

            [Fact]
            public async Task CanSetCustomDefaultAppearance()
            {
                await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    "shark"
                );

                await this.Transformations.SetCurrentAppearanceAsDefaultForCharacterAsync
                (
                    this.Database,
                    this._character
                );

                Assert.Equal
                (
                    "shark",
                    this._character.DefaultAppearance.Components.First(c => c.Bodypart == Bodypart.Face).Transformation.Species.Name
                );
            }

            [Fact]
            public async Task CanResetToCustomDefaultAppearance()
            {
                await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    "shark"
                );

                await this.Transformations.SetCurrentAppearanceAsDefaultForCharacterAsync
                (
                    this.Database,
                    this._character
                );

                await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    "shark-dronie"
                );

                await this.Transformations.ResetCharacterFormAsync
                (
                    this.Database,
                    this._character
                );

                Assert.Equal
                (
                    "shark",
                    this._character.CurrentAppearance.Components.First(c => c.Bodypart == Bodypart.Face).Transformation.Species.Name
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

            public ShiftBodypartColourAsync()
            {
                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                (
                    c =>
                        c.GetUserAsync
                        (
                            It.Is<ulong>(id => id == this._owner.Id),
                            CacheMode.AllowDownload,
                            null
                        )
                )
                .Returns(Task.FromResult((IGuildUser)this._owner));

                this._guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(this._guild);
                mockedContext.Setup(c => c.User).Returns(this._invoker);

                this._context = mockedContext.Object;

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

                Colour.TryParse("bright purple", out this._newColour);
            }

            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();

                // Ensure owner is opted into transformations
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this._owner,
                    this._guild
                );

                protection.Entity.HasOptedIn = true;

                // Create a test character
                var owner = (await this.Database.GetOrRegisterUserAsync(this._owner)).Entity;
                this._character = new Character
                {
                    Name = "Test",
                    CurrentAppearance = (await Appearance.CreateDefaultAsync(this.Database, this.Transformations)).Entity,
                    Owner = owner,
                    PronounProviderFamily = "They"
                };

                this._originalColour = this._character.GetAppearanceComponent(Bodypart.Face, Chirality.Center).BaseColour;

                this.Database.Characters.Add(this._character);

                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(this.Database, this._owner, this._invoker);

                var result = await this.Transformations.ShiftBodypartColourAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._newColour
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterDoesNotHaveBodypart()
            {
                var result = await this.Transformations.ShiftBodypartColourAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Wing,
                    this._newColour,
                    Chirality.Left
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfBodypartIsAlreadyThatColour()
            {
                var result = await this.Transformations.ShiftBodypartColourAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._originalColour
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task CanShiftBodypartColour()
            {
                var result = await this.Transformations.ShiftBodypartColourAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._newColour
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ShiftsBodypartIntoCorrectColour()
            {
                await this.Transformations.ShiftBodypartColourAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._newColour
                );

                var face = this._character.CurrentAppearance.Components.First(c => c.Bodypart == Bodypart.Face);
                Assert.Same(this._newColour, face.BaseColour);
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                var result = await this.Transformations.ShiftBodypartColourAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._newColour
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

            public ShiftBodypartPatternAsync()
            {
                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                (
                    c =>
                        c.GetUserAsync
                        (
                            It.Is<ulong>(id => id == this._owner.Id),
                            CacheMode.AllowDownload,
                            null
                        )
                )
                .Returns(Task.FromResult((IGuildUser)this._owner));

                this._guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(this._guild);
                mockedContext.Setup(c => c.User).Returns(this._invoker);

                this._context = mockedContext.Object;

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

                this._newPattern = Pattern.Swirly;
                Colour.TryParse("bright purple", out this._newPatternColour);
            }

            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();

                // Ensure owner is opted into transformations
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this._owner,
                    this._guild
                );

                protection.Entity.HasOptedIn = true;

                // Create a test character
                var owner = (await this.Database.GetOrRegisterUserAsync(this._owner)).Entity;
                this._character = new Character
                {
                    Name = "Test",
                    CurrentAppearance = (await Appearance.CreateDefaultAsync(this.Database, this.Transformations)).Entity,
                    Owner = owner,
                    PronounProviderFamily = "They"
                };

                this.Database.Characters.Add(this._character);

                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(this.Database, this._owner, this._invoker);

                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._newPattern,
                    this._newPatternColour
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterDoesNotHaveBodypart()
            {
                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Wing,
                    this._newPattern,
                    this._newPatternColour,
                    Chirality.Left
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfBodypartIsAlreadyThatPattern()
            {
                await this.Transformations.ShiftBodypartPatternAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._newPattern,
                    this._newPatternColour
                );

                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._newPattern,
                    this._newPatternColour
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task CanShiftBodypartPattern()
            {
                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._newPattern,
                    this._newPatternColour
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ShiftsBodypartIntoCorrectPattern()
            {
                await this.Transformations.ShiftBodypartPatternAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._newPattern,
                    this._newPatternColour
                );

                var face = this._character.GetAppearanceComponent(Bodypart.Face, Chirality.Center);
                Assert.Equal(this._newPattern, face.Pattern);
            }

            [Fact]
            public async Task ShiftsBodypartIntoCorrectPatternColour()
            {
                await this.Transformations.ShiftBodypartPatternAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._newPattern,
                    this._newPatternColour
                );

                var face = this._character.GetAppearanceComponent(Bodypart.Face, Chirality.Center);
                Assert.Equal(this._newPatternColour, face.PatternColour);
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._newPattern,
                    this._newPatternColour
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

            public ShiftBodypartPatternColourAsync()
            {
                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                (
                    c =>
                        c.GetUserAsync
                        (
                            It.Is<ulong>(id => id == this._owner.Id),
                            CacheMode.AllowDownload,
                            null
                        )
                )
                .Returns(Task.FromResult((IGuildUser)this._owner));

                this._guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(this._guild);
                mockedContext.Setup(c => c.User).Returns(this._invoker);

                this._context = mockedContext.Object;

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

                Colour.TryParse("bright purple", out this._newPatternColour);
            }

            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();

                // Ensure owner is opted into transformations
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this._owner,
                    this._guild
                );

                protection.Entity.HasOptedIn = true;

                // Create a test character
                var owner = (await this.Database.GetOrRegisterUserAsync(this._owner)).Entity;
                this._character = new Character
                {
                    Name = "Test",
                    CurrentAppearance = (await Appearance.CreateDefaultAsync(this.Database, this.Transformations)).Entity,
                    Owner = owner,
                    PronounProviderFamily = "They"
                };

                Colour.TryParse("dull white", out this._originalPatternColour);
                Assert.NotNull(this._originalPatternColour);

                await this.Transformations.ShiftBodypartPatternAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    Pattern.Swirly,
                    this._originalPatternColour
                );

                this.Database.Characters.Add(this._character);

                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(this.Database, this._owner, this._invoker);

                var result = await this.Transformations.ShiftPatternColourAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._newPatternColour
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterDoesNotHaveBodypart()
            {
                var result = await this.Transformations.ShiftPatternColourAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Wing,
                    this._newPatternColour,
                    Chirality.Left
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfBodypartDoesNotHavePattern()
            {
                var result = await this.Transformations.ShiftPatternColourAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Arm,
                    this._newPatternColour,
                    Chirality.Left
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfBodypartIsAlreadyThatColour()
            {
                await this.Transformations.ShiftPatternColourAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._newPatternColour
                );

                var result = await this.Transformations.ShiftPatternColourAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._newPatternColour
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task CanShiftColour()
            {
                var result = await this.Transformations.ShiftPatternColourAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._newPatternColour
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ShiftsIntoCorrectColour()
            {
                await this.Transformations.ShiftPatternColourAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._newPatternColour
                );

                var face = this._character.GetAppearanceComponent(Bodypart.Face, Chirality.Center);
                Assert.Equal(this._newPatternColour, face.PatternColour);
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                var result = await this.Transformations.ShiftPatternColourAsync
                (
                    this.Database,
                    this._context,
                    this._character,
                    Bodypart.Face,
                    this._newPatternColour
                );

                Assert.NotNull(result.ShiftMessage);
                Assert.NotEmpty(result.ShiftMessage);
            }
        }
    }
}
