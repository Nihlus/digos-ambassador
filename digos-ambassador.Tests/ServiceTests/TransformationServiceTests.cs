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
            private Species TemplateSpecies;

            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();
                this.TemplateSpecies = this.Database.Species.First(s => s.Name == "template");
            }

            [Fact]
            public async Task ReturnsTrueForUniqueCombination()
            {
                var result = await this.Transformations.IsPartAndSpeciesCombinationUniqueAsync
                (
                    this.Database,
                    Bodypart.Wings,
                    this.TemplateSpecies
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
                    this.TemplateSpecies
                );

                Assert.False(result);
            }
        }

        public class GetTransformationByPartAndSpeciesAsync : TransformationServiceTestBase
        {
            private Species TemplateSpecies;

            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();
                this.TemplateSpecies = this.Database.Species.First(s => s.Name == "template");
            }

            [Fact]
            public async Task RetrievesCorrectBodypart()
            {
                var result = await this.Transformations.GetTransformationsByPartAndSpeciesAsync
                (
                    this.Database,
                    Bodypart.Face,
                    this.TemplateSpecies
                );

                Assert.True(result.IsSuccess);
                Assert.Single(result.Entity);

                var transformation = result.Entity.First();

                Assert.Equal(Bodypart.Face, transformation.Part);
                Assert.Same(this.TemplateSpecies, transformation.Species);
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
                    this.TemplateSpecies
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }
        }

        public class GetOrCreateServerUserProtectionAsync : TransformationServiceTestBase
        {
            private readonly IUser User = MockHelper.CreateDiscordUser(0);
            private readonly IGuild Guild = MockHelper.CreateDiscordGuild(1);

            [Fact]
            public async Task CreatesObjectIfOneDoesNotExist()
            {
                Assert.Empty(this.Database.ServerUserProtections);

                var result = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this.User,
                    this.Guild
                );

                Assert.NotEmpty(this.Database.ServerUserProtections);
                Assert.Same(result, this.Database.ServerUserProtections.First());
            }

            [Fact]
            public async Task CreatedObjectIsBoundToTheCorrectServer()
            {
                var result = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this.User,
                    this.Guild
                );

                Assert.Equal((long)this.Guild.Id, result.Server.DiscordID);
            }

            [Fact]
            public async Task CreatedObjectIsBoundToTheCorrectUser()
            {
                var result = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this.User,
                    this.Guild
                );

                Assert.Equal((long)this.User.Id, result.User.DiscordID);
            }

            [Fact]
            public async Task RetrievesCorrectObjectIfOneExists()
            {
                // Create an object
                var created = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this.User,
                    this.Guild
                );

                // Get it from the database
                var retrieved = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this.User,
                    this.Guild
                );

                Assert.Same(created, retrieved);
            }

            [Fact]
            public async Task CreatedObjectRespectsGlobalDefaults()
            {
                var user = await this.Database.GetOrRegisterUserAsync(this.User);

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
                    this.User,
                    this.Guild
                );

                Assert.Equal(globalSetting.DefaultOptIn, localSetting.HasOptedIn);
                Assert.Equal(globalSetting.DefaultType, localSetting.Type);
                Assert.Same(globalSetting.User, localSetting.User);
            }
        }

        public class GetOrCreateGlobalUserProtectionAsync : TransformationServiceTestBase
        {
            private readonly IUser User = MockHelper.CreateDiscordUser(0);

            [Fact]
            public async Task CreatesObjectIfOneDoesNotExist()
            {
                Assert.Empty(this.Database.ServerUserProtections);

                var result = await this.Transformations.GetOrCreateGlobalUserProtectionAsync(this.Database, this.User);

                Assert.NotEmpty(this.Database.GlobalUserProtections);
                Assert.Same(result, this.Database.GlobalUserProtections.First());
            }

            [Fact]
            public async Task CreatedObjectIsBoundToTheCorrectUser()
            {
                var result = await this.Transformations.GetOrCreateGlobalUserProtectionAsync(this.Database, this.User);

                Assert.Equal((long)this.User.Id, result.User.DiscordID);
            }

            [Fact]
            public async Task RetrievesCorrectObjectIfOneExists()
            {
                // Create an object
                var created = await this.Transformations.GetOrCreateGlobalUserProtectionAsync(this.Database, this.User);

                // Get it from the database
                var retrieved = await this.Transformations.GetOrCreateGlobalUserProtectionAsync(this.Database, this.User);

                Assert.Same(created, retrieved);
            }
        }

        public class BlacklistUserAsync : TransformationServiceTestBase
        {
            private readonly IUser User = MockHelper.CreateDiscordUser(0);
            private readonly IUser BlacklistedUser = MockHelper.CreateDiscordUser(1);

            [Fact]
            public async Task CanBlacklistUser()
            {
                var result = await this.Transformations.BlacklistUserAsync(this.Database, this.User, this.BlacklistedUser);

                Assert.True(result.IsSuccess);
                Assert.Equal(ModifyEntityAction.Edited, result.ActionTaken);

                Assert.NotEmpty(this.Database.GlobalUserProtections.First().Blacklist);

                Assert.Equal((long)this.BlacklistedUser.Id, this.Database.GlobalUserProtections.First().Blacklist.First().DiscordID);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsAlreadyBlacklisted()
            {
                // Blacklist the user
                await this.Transformations.BlacklistUserAsync(this.Database, this.User, this.BlacklistedUser);

                // Then blacklist them again
                var result = await this.Transformations.BlacklistUserAsync(this.Database, this.User, this.BlacklistedUser);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfTargetUserIsInvokingUser()
            {
                var result = await this.Transformations.BlacklistUserAsync(this.Database, this.User, this.User);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }
        }

        public class WhitelistUserAsync : TransformationServiceTestBase
        {
            private readonly IUser User = MockHelper.CreateDiscordUser(0);
            private readonly IUser WhitelistedUser = MockHelper.CreateDiscordUser(1);

            [Fact]
            public async Task CanWhitelistUser()
            {
                var result = await this.Transformations.WhitelistUserAsync(this.Database, this.User, this.WhitelistedUser);

                Assert.True(result.IsSuccess);
                Assert.Equal(ModifyEntityAction.Edited, result.ActionTaken);

                Assert.NotEmpty(this.Database.GlobalUserProtections.First().Whitelist);
                Assert.Equal((long)this.WhitelistedUser.Id, this.Database.GlobalUserProtections.First().Whitelist.First().DiscordID);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsAlreadyWhitelisted()
            {
                // Whitelist the user
                await this.Transformations.WhitelistUserAsync(this.Database, this.User, this.WhitelistedUser);

                // Then Whitelist them again
                var result = await this.Transformations.WhitelistUserAsync(this.Database, this.User, this.WhitelistedUser);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfTargetUserIsInvokingUser()
            {
                var result = await this.Transformations.WhitelistUserAsync(this.Database, this.User, this.User);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }
        }

        public class SetServerProtectionTypeAsync : TransformationServiceTestBase
        {
            private readonly IUser User = MockHelper.CreateDiscordUser(0);
            private readonly IGuild Guild = MockHelper.CreateDiscordGuild(1);

            [Fact]
            public async Task CanSetType()
            {
                var expected = ProtectionType.Whitelist;
                var result = await this.Transformations.SetServerProtectionTypeAsync
                (
                    this.Database,
                    this.User,
                    this.Guild,
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
                    this.User,
                    this.Guild,
                    existingType
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }
        }

        public class SetDefaultProtectionTypeAsync : TransformationServiceTestBase
        {
            private readonly IUser User = MockHelper.CreateDiscordUser(0);

            [Fact]
            public async Task CanSetType()
            {
                var expected = ProtectionType.Whitelist;
                var result = await this.Transformations.SetDefaultProtectionTypeAsync
                (
                    this.Database,
                    this.User,
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
                    this.User,
                    currentType
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }
        }

        public class SetCurrentAppearanceAsDefaultForCharacterAsync : TransformationServiceTestBase
        {
            private readonly IUser User = MockHelper.CreateDiscordUser(0);
            private User Owner;
            private Character Character;

            public override async Task InitializeAsync()
            {
                this.Owner = await this.Database.GetOrRegisterUserAsync(this.User);

                this.Character = new Character { Owner = this.Owner };
                this.Database.Characters.Add(this.Character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task CanSetDefaultAppearance()
            {
                var alteredAppearance = new Appearance
                {
                    Height = 10
                };

                this.Character.CurrentAppearance = alteredAppearance;

                var result = await this.Transformations.SetCurrentAppearanceAsDefaultForCharacterAsync
                (
                    this.Database,
                    this.Character
                );

                Assert.True(result.IsSuccess);
                Assert.Equal(ModifyEntityAction.Edited, result.ActionTaken);
                Assert.NotNull(this.Character.DefaultAppearance);
                Assert.Equal(10, this.Character.DefaultAppearance.Height);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterDoesNotHaveAnAlteredAppearance()
            {
                var result = await this.Transformations.SetCurrentAppearanceAsDefaultForCharacterAsync
                (
                    this.Database,
                    this.Character
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
                Assert.Null(this.Character.DefaultAppearance);
            }
        }

        public class ResetCharacterFormAsync : TransformationServiceTestBase
        {
            private readonly IUser User = MockHelper.CreateDiscordUser(0);
            private User Owner;
            private Character Character;

            public override async Task InitializeAsync()
            {
                this.Owner = await this.Database.GetOrRegisterUserAsync(this.User);

                this.Character = new Character { Owner = this.Owner };
                this.Database.Characters.Add(this.Character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task CanResetForm()
            {
                var defaultAppearance = new Appearance
                {
                    Height = 256
                };

                this.Character.DefaultAppearance = defaultAppearance;

                var result = await this.Transformations.ResetCharacterFormAsync(this.Database, this.Character);

                Assert.True(result.IsSuccess);
                Assert.Equal(ModifyEntityAction.Edited, result.ActionTaken);
                Assert.NotNull(this.Character.CurrentAppearance);
                Assert.Equal(this.Character.DefaultAppearance.Height, this.Character.CurrentAppearance.Height);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterDoesNotHaveADefaultAppearance()
            {
                var result = await this.Transformations.ResetCharacterFormAsync(this.Database, this.Character);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
                Assert.Null(this.Character.CurrentAppearance);
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
            private readonly IUser User = MockHelper.CreateDiscordUser(0);
            private readonly IUser TargetUser = MockHelper.CreateDiscordUser(1);

            private readonly IGuild Guild = MockHelper.CreateDiscordGuild(0);

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfTargetUserHasNotOptedIn()
            {
                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    this.Database,
                    this.Guild,
                    this.User,
                    this.TargetUser
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsOnTargetUsersBlacklist()
            {
                await EnsureOptedInAsync(this.TargetUser);
                await this.Transformations.BlacklistUserAsync(this.Database, this.TargetUser, this.User);

                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    this.Database,
                    this.Guild,
                    this.User,
                    this.TargetUser
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfTargetUserUsesWhitelistingAndUserIsNotOnWhitelist()
            {
                await EnsureOptedInAsync(this.TargetUser);
                await this.Transformations.SetServerProtectionTypeAsync
                (
                    this.Database,
                    this.TargetUser,
                    this.Guild,
                    ProtectionType.Whitelist
                );

                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    this.Database,
                    this.Guild,
                    this.User,
                    this.TargetUser
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfTargetUserUsesWhitelistingAndUserIsOnWhitelist()
            {
                await EnsureOptedInAsync(this.TargetUser);
                await this.Transformations.SetServerProtectionTypeAsync
                (
                    this.Database,
                    this.TargetUser,
                    this.Guild,
                    ProtectionType.Whitelist
                );

                await this.Transformations.WhitelistUserAsync(this.Database, this.TargetUser, this.User);

                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    this.Database,
                    this.Guild,
                    this.User,
                    this.TargetUser
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfUserIsNotOnTargetUsersBlacklist()
            {
                await EnsureOptedInAsync(this.TargetUser);

                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    this.Database,
                    this.Guild,
                    this.User,
                    this.TargetUser
                );

                Assert.True(result.IsSuccess);
            }

            private async Task EnsureOptedInAsync([NotNull] IUser user)
            {
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    user,
                    this.Guild
                );
                protection.HasOptedIn = true;

                await this.Database.SaveChangesAsync();
            }
        }

        public class RemoveBodypartAsync : TransformationServiceTestBase
        {
            private readonly IGuild Guild;

            private readonly IUser Owner = MockHelper.CreateDiscordGuildUser(0);
            private readonly IUser Invoker = MockHelper.CreateDiscordGuildUser(1);

            private readonly ICommandContext Context;
            private Character Character;

            public RemoveBodypartAsync()
            {
                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                (
                    c =>
                        c.GetUserAsync
                        (
                            It.Is<ulong>(id => id == this.Owner.Id),
                            CacheMode.AllowDownload,
                            null
                        )
                )
                .Returns(Task.FromResult((IGuildUser)this.Owner));

                this.Guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(this.Guild);
                mockedContext.Setup(c => c.User).Returns(this.Invoker);

                this.Context = mockedContext.Object;

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
                    this.Owner,
                    this.Guild
                );
                protection.HasOptedIn = true;

                // Create a test character
                var owner = await this.Database.GetOrRegisterUserAsync(this.Owner);
                this.Character = new Character
                {
                    Name = "Test",
                    CurrentAppearance = (await Appearance.CreateDefaultAsync(this.Database, this.Transformations)).Entity,
                    Owner = owner
                };

                this.Database.Characters.Add(this.Character);

                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(this.Database, this.Owner, this.Invoker);

                var result = await this.Transformations.RemoveBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
                Assert.True(this.Character.HasComponent(Bodypart.Face, Chirality.Center));
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterDoesNotHaveBodypart()
            {
                var result = await this.Transformations.RemoveBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
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
                    this.Context,
                    this.Character,
                    Bodypart.Face
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task RemovesCorrectBodypart()
            {
                Assert.Contains(this.Character.CurrentAppearance.Components, c => c.Bodypart == Bodypart.Face);

                await this.Transformations.RemoveBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face
                );

                Assert.DoesNotContain(this.Character.CurrentAppearance.Components, c => c.Bodypart == Bodypart.Face);
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                var result = await this.Transformations.RemoveBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face
                );

                Assert.NotNull(result.ShiftMessage);
            }
        }

        public class AddBodypartAsync : TransformationServiceTestBase
        {
            private readonly IGuild Guild;

            private readonly IUser Owner = MockHelper.CreateDiscordGuildUser(0);
            private readonly IUser Invoker = MockHelper.CreateDiscordGuildUser(1);

            private readonly ICommandContext Context;
            private Character Character;

            public AddBodypartAsync()
            {
                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                (
                    c =>
                        c.GetUserAsync
                        (
                            It.Is<ulong>(id => id == this.Owner.Id),
                            CacheMode.AllowDownload,
                            null
                        )
                )
                .Returns(Task.FromResult((IGuildUser)this.Owner));

                this.Guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(this.Guild);
                mockedContext.Setup(c => c.User).Returns(this.Invoker);

                this.Context = mockedContext.Object;

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
                    this.Owner,
                    this.Guild
                );

                protection.HasOptedIn = true;

                // Create a test character
                var owner = await this.Database.GetOrRegisterUserAsync(this.Owner);
                this.Character = new Character
                {
                    Name = "Test",
                    CurrentAppearance = (await Appearance.CreateDefaultAsync(this.Database, this.Transformations)).Entity,
                    Owner = owner
                };

                this.Database.Characters.Add(this.Character);

                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(this.Database, this.Owner, this.Invoker);

                var result = await this.Transformations.AddBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    "template"
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
                Assert.Contains(this.Character.CurrentAppearance.Components, c => c.Bodypart == Bodypart.Face);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCharacterHasBodypart()
            {
                var result = await this.Transformations.AddBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
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
                    this.Context,
                    this.Character,
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
                    this.Context,
                    this.Character,
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
                    this.Context,
                    this.Character,
                    Bodypart.Face
                );

                // Then add it again
                var result = await this.Transformations.AddBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
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
                    this.Context,
                    this.Character,
                    Bodypart.Face
                );

                Assert.False(this.Character.HasComponent(Bodypart.Face, Chirality.Center));

                // Then add it again
                await this.Transformations.AddBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    "template"
                );

                Assert.True(this.Character.HasComponent(Bodypart.Face, Chirality.Center));
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                // Remove the face
                await this.Transformations.RemoveBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face
                );

                // Then add it again
                var result = await this.Transformations.AddBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    "template"
                );

                Assert.NotNull(result.ShiftMessage);
            }
        }

        public class ShiftBodypartAsync : TransformationServiceTestBase
        {
            private readonly IGuild Guild;

            private readonly IUser Owner = MockHelper.CreateDiscordGuildUser(0);
            private readonly IUser Invoker = MockHelper.CreateDiscordGuildUser(1);

            private readonly ICommandContext Context;
            private Character Character;

            public ShiftBodypartAsync()
            {
                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                (
                    c =>
                        c.GetUserAsync
                        (
                            It.Is<ulong>(id => id == this.Owner.Id),
                            CacheMode.AllowDownload,
                            null
                        )
                )
                .Returns(Task.FromResult((IGuildUser)this.Owner));

                this.Guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(this.Guild);
                mockedContext.Setup(c => c.User).Returns(this.Invoker);

                this.Context = mockedContext.Object;

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
                    this.Owner,
                    this.Guild
                );

                protection.HasOptedIn = true;

                // Create a test character
                var owner = await this.Database.GetOrRegisterUserAsync(this.Owner);
                this.Character = new Character
                {
                    Name = "Test",
                    DefaultAppearance = (await Appearance.CreateDefaultAsync(this.Database, this.Transformations)).Entity,
                    CurrentAppearance = (await Appearance.CreateDefaultAsync(this.Database, this.Transformations)).Entity,
                    Owner = owner,
                    PronounProviderFamily = "Feminine"
                };

                this.Database.Characters.Add(this.Character);

                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(this.Database, this.Owner, this.Invoker);

                var result = await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
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
                    this.Context,
                    this.Character,
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
                    this.Context,
                    this.Character,
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
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    "template"
                );

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task AddsBodypartIfItDoesNotAlreadyExist()
            {
                Assert.False(this.Character.HasComponent(Bodypart.Tail, Chirality.Center));

                var result = await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Tail,
                    "shark"
                );

                Assert.True(result.IsSuccess);
                Assert.True(this.Character.HasComponent(Bodypart.Tail, Chirality.Center));
            }

            [Fact]
            public async Task ShiftsBodypartIntoCorrectSpecies()
            {
                var result = await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    "shark"
                );

                Assert.True(result.IsSuccess);
                Assert.Equal("shark", this.Character.GetAppearanceComponent(Bodypart.Face, Chirality.Center).Transformation.Species.Name);
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                var result = await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
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
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    "shark"
                );

                Assert.NotEqual
                (
                    "shark",
                    this.Character.DefaultAppearance.Components.First(c => c.Bodypart == Bodypart.Face).Transformation.Species.Name
                );
            }

            [Fact]
            public async Task CanResetAppearance()
            {
                await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    "shark"
                );

                await this.Transformations.ResetCharacterFormAsync
                (
                    this.Database,
                    this.Character
                );

                Assert.NotEqual
                (
                    "shark",
                    this.Character.CurrentAppearance.Components.First(c => c.Bodypart == Bodypart.Face).Transformation.Species.Name
                );
            }

            [Fact]
            public async Task CanSetCustomDefaultAppearance()
            {
                await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    "shark"
                );

                await this.Transformations.SetCurrentAppearanceAsDefaultForCharacterAsync
                (
                    this.Database,
                    this.Character
                );

                Assert.Equal
                (
                    "shark",
                    this.Character.DefaultAppearance.Components.First(c => c.Bodypart == Bodypart.Face).Transformation.Species.Name
                );
            }

            [Fact]
            public async Task CanResetToCustomDefaultAppearance()
            {
                await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    "shark"
                );

                await this.Transformations.SetCurrentAppearanceAsDefaultForCharacterAsync
                (
                    this.Database,
                    this.Character
                );

                await this.Transformations.ShiftBodypartAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    "shark-dronie"
                );

                await this.Transformations.ResetCharacterFormAsync
                (
                    this.Database,
                    this.Character
                );

                Assert.Equal
                (
                    "shark",
                    this.Character.CurrentAppearance.Components.First(c => c.Bodypart == Bodypart.Face).Transformation.Species.Name
                );
            }
        }

        public class ShiftBodypartColourAsync : TransformationServiceTestBase
        {
            private readonly IGuild Guild;

            private readonly IUser Owner = MockHelper.CreateDiscordGuildUser(0);
            private readonly IUser Invoker = MockHelper.CreateDiscordGuildUser(1);

            private readonly Colour NewColour;

            private readonly ICommandContext Context;
            private Character Character;

            private Colour OriginalColour;

            public ShiftBodypartColourAsync()
            {
                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                (
                    c =>
                        c.GetUserAsync
                        (
                            It.Is<ulong>(id => id == this.Owner.Id),
                            CacheMode.AllowDownload,
                            null
                        )
                )
                .Returns(Task.FromResult((IGuildUser)this.Owner));

                this.Guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(this.Guild);
                mockedContext.Setup(c => c.User).Returns(this.Invoker);

                this.Context = mockedContext.Object;

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

                Colour.TryParse("bright purple", out this.NewColour);
            }

            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();

                // Ensure owner is opted into transformations
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this.Owner,
                    this.Guild
                );

                protection.HasOptedIn = true;

                // Create a test character
                var owner = await this.Database.GetOrRegisterUserAsync(this.Owner);
                this.Character = new Character
                {
                    Name = "Test",
                    CurrentAppearance = (await Appearance.CreateDefaultAsync(this.Database, this.Transformations)).Entity,
                    Owner = owner,
                    PronounProviderFamily = "They"
                };

                this.OriginalColour = this.Character.GetAppearanceComponent(Bodypart.Face, Chirality.Center).BaseColour;

                this.Database.Characters.Add(this.Character);

                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(this.Database, this.Owner, this.Invoker);

                var result = await this.Transformations.ShiftBodypartColourAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.NewColour
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
                    this.Context,
                    this.Character,
                    Bodypart.Wing,
                    this.NewColour,
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
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.OriginalColour
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
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.NewColour
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ShiftsBodypartIntoCorrectColour()
            {
                await this.Transformations.ShiftBodypartColourAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.NewColour
                );

                var face = this.Character.CurrentAppearance.Components.First(c => c.Bodypart == Bodypart.Face);
                Assert.Same(this.NewColour, face.BaseColour);
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                var result = await this.Transformations.ShiftBodypartColourAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.NewColour
                );

                Assert.NotNull(result.ShiftMessage);
                Assert.NotEmpty(result.ShiftMessage);
            }
        }

        public class ShiftBodypartPatternAsync : TransformationServiceTestBase
        {
            private readonly IGuild Guild;

            private readonly IUser Owner = MockHelper.CreateDiscordGuildUser(0);
            private readonly IUser Invoker = MockHelper.CreateDiscordGuildUser(1);

            private readonly Pattern NewPattern;
            private readonly Colour NewPatternColour;

            private readonly ICommandContext Context;
            private Character Character;

            public ShiftBodypartPatternAsync()
            {
                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                (
                    c =>
                        c.GetUserAsync
                        (
                            It.Is<ulong>(id => id == this.Owner.Id),
                            CacheMode.AllowDownload,
                            null
                        )
                )
                .Returns(Task.FromResult((IGuildUser)this.Owner));

                this.Guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(this.Guild);
                mockedContext.Setup(c => c.User).Returns(this.Invoker);

                this.Context = mockedContext.Object;

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

                this.NewPattern = Pattern.Swirly;
                Colour.TryParse("bright purple", out this.NewPatternColour);
            }

            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();

                // Ensure owner is opted into transformations
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this.Owner,
                    this.Guild
                );

                protection.HasOptedIn = true;

                // Create a test character
                var owner = await this.Database.GetOrRegisterUserAsync(this.Owner);
                this.Character = new Character
                {
                    Name = "Test",
                    CurrentAppearance = (await Appearance.CreateDefaultAsync(this.Database, this.Transformations)).Entity,
                    Owner = owner,
                    PronounProviderFamily = "They"
                };

                this.Database.Characters.Add(this.Character);

                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(this.Database, this.Owner, this.Invoker);

                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.NewPattern,
                    this.NewPatternColour
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
                    this.Context,
                    this.Character,
                    Bodypart.Wing,
                    this.NewPattern,
                    this.NewPatternColour,
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
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.NewPattern,
                    this.NewPatternColour
                );

                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.NewPattern,
                    this.NewPatternColour
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
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.NewPattern,
                    this.NewPatternColour
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ShiftsBodypartIntoCorrectPattern()
            {
                await this.Transformations.ShiftBodypartPatternAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.NewPattern,
                    this.NewPatternColour
                );

                var face = this.Character.GetAppearanceComponent(Bodypart.Face, Chirality.Center);
                Assert.Equal(this.NewPattern, face.Pattern);
            }

            [Fact]
            public async Task ShiftsBodypartIntoCorrectPatternColour()
            {
                await this.Transformations.ShiftBodypartPatternAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.NewPattern,
                    this.NewPatternColour
                );

                var face = this.Character.GetAppearanceComponent(Bodypart.Face, Chirality.Center);
                Assert.Equal(this.NewPatternColour, face.PatternColour);
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.NewPattern,
                    this.NewPatternColour
                );

                Assert.NotNull(result.ShiftMessage);
                Assert.NotEmpty(result.ShiftMessage);
            }
        }

        public class ShiftBodypartPatternColourAsync : TransformationServiceTestBase
        {
            private readonly IGuild Guild;

            private readonly IUser Owner = MockHelper.CreateDiscordGuildUser(0);
            private readonly IUser Invoker = MockHelper.CreateDiscordGuildUser(1);

            private readonly Colour NewPatternColour;

            private readonly ICommandContext Context;
            private Character Character;

            private Colour OriginalPatternColour;

            public ShiftBodypartPatternColourAsync()
            {
                var mockedGuild = new Mock<IGuild>();
                mockedGuild.Setup(g => g.Id).Returns(1);
                mockedGuild.Setup
                (
                    c =>
                        c.GetUserAsync
                        (
                            It.Is<ulong>(id => id == this.Owner.Id),
                            CacheMode.AllowDownload,
                            null
                        )
                )
                .Returns(Task.FromResult((IGuildUser)this.Owner));

                this.Guild = mockedGuild.Object;

                var mockedContext = new Mock<ICommandContext>();
                mockedContext.Setup(c => c.Guild).Returns(this.Guild);
                mockedContext.Setup(c => c.User).Returns(this.Invoker);

                this.Context = mockedContext.Object;

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

                Colour.TryParse("bright purple", out this.NewPatternColour);
            }

            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();

                // Ensure owner is opted into transformations
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    this.Database,
                    this.Owner,
                    this.Guild
                );

                protection.HasOptedIn = true;

                // Create a test character
                var owner = await this.Database.GetOrRegisterUserAsync(this.Owner);
                this.Character = new Character
                {
                    Name = "Test",
                    CurrentAppearance = (await Appearance.CreateDefaultAsync(this.Database, this.Transformations)).Entity,
                    Owner = owner,
                    PronounProviderFamily = "They"
                };

                Colour.TryParse("dull white", out this.OriginalPatternColour);
                Assert.NotNull(this.OriginalPatternColour);

                await this.Transformations.ShiftBodypartPatternAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    Pattern.Swirly,
                    this.OriginalPatternColour
                );

                this.Database.Characters.Add(this.Character);

                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(this.Database, this.Owner, this.Invoker);

                var result = await this.Transformations.ShiftPatternColourAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.NewPatternColour
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
                    this.Context,
                    this.Character,
                    Bodypart.Wing,
                    this.NewPatternColour,
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
                    this.Context,
                    this.Character,
                    Bodypart.Arm,
                    this.NewPatternColour,
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
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.NewPatternColour
                );

                var result = await this.Transformations.ShiftPatternColourAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.NewPatternColour
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
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.NewPatternColour
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ShiftsIntoCorrectColour()
            {
                await this.Transformations.ShiftPatternColourAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.NewPatternColour
                );

                var face = this.Character.GetAppearanceComponent(Bodypart.Face, Chirality.Center);
                Assert.Equal(this.NewPatternColour, face.PatternColour);
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                var result = await this.Transformations.ShiftPatternColourAsync
                (
                    this.Database,
                    this.Context,
                    this.Character,
                    Bodypart.Face,
                    this.NewPatternColour
                );

                Assert.NotNull(result.ShiftMessage);
                Assert.NotEmpty(result.ShiftMessage);
            }
        }
    }
}
