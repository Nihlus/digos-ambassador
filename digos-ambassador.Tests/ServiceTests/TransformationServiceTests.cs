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

using System;
using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Tests.Database;

using Discord;
using Discord.Commands;

using Moq;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591

namespace DIGOS.Ambassador.Tests.ServiceTests
{
	public class TransformationServiceTests
	{
		public class UpdateTransformationDatabaseAsync : IDisposable
		{
			private readonly GlobalInfoContext Database;
			private readonly TransformationService Transformations;

			public UpdateTransformationDatabaseAsync()
			{
				this.Database = new MockedDatabase().GetDatabaseContext();
				this.Transformations = new TransformationService(new ContentService());
			}

			[Fact]
			public async Task FindsBundledSpecies()
			{
				var result = await this.Transformations.UpdateTransformationDatabaseAsync(this.Database);

				Assert.True(result.IsSuccess);
				Assert.NotEmpty(this.Database.Species);
			}

			public void Dispose()
			{
				this.Database?.Dispose();
			}

			public Task DisposeAsync()
			{
				return Task.CompletedTask;
			}
		}

		public class IsSpeciesNameUniqueAsync : IDisposable, IAsyncLifetime
		{
			private readonly GlobalInfoContext Database;
			private readonly TransformationService Transformations;

			public IsSpeciesNameUniqueAsync()
			{
				this.Database = new MockedDatabase().GetDatabaseContext();
				this.Transformations = new TransformationService(new ContentService());
			}

			public async Task InitializeAsync()
			{
				await this.Transformations.UpdateTransformationDatabaseAsync(this.Database);
			}

			[Theory]
			[InlineData("asadasdas")]
			[InlineData("yeee ewwah")]
			public async Task ReturnsTrueForUniqueName(string name)
			{
				var result = await this.Transformations.IsSpeciesNameUniqueAsync(this.Database, name);

				Assert.True(result);
			}

			[Theory]
			[InlineData("template")]
			public async Task ReturnsFalseForNonUniqueName(string name)
			{
				var result = await this.Transformations.IsSpeciesNameUniqueAsync(this.Database, name);

				Assert.False(result);
			}

			[Theory]
			[InlineData("TEMPLATE")]
			public async Task IsCaseInsensitive(string name)
			{
				var result = await this.Transformations.IsSpeciesNameUniqueAsync(this.Database, name);

				Assert.False(result);
			}

			public void Dispose()
			{
				this.Database?.Dispose();
			}

			public Task DisposeAsync()
			{
				return Task.CompletedTask;
			}
		}

		public class GetSpeciesByNameAsync : IDisposable, IAsyncLifetime
		{
			private readonly GlobalInfoContext Database;
			private readonly TransformationService Transformations;

			public GetSpeciesByNameAsync()
			{
				this.Database = new MockedDatabase().GetDatabaseContext();
				this.Transformations = new TransformationService(new ContentService());
			}

			public async Task InitializeAsync()
			{
				await this.Transformations.UpdateTransformationDatabaseAsync(this.Database);
			}

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

			public void Dispose()
			{
				this.Database?.Dispose();
			}

			public Task DisposeAsync()
			{
				return Task.CompletedTask;
			}
		}

		public class GetSpeciesByName : IDisposable, IAsyncLifetime
		{
			private readonly GlobalInfoContext Database;
			private readonly TransformationService Transformations;

			public GetSpeciesByName()
			{
				this.Database = new MockedDatabase().GetDatabaseContext();
				this.Transformations = new TransformationService(new ContentService());
			}

			public async Task InitializeAsync()
			{
				await this.Transformations.UpdateTransformationDatabaseAsync(this.Database);
			}

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

			public void Dispose()
			{
				this.Database?.Dispose();
			}

			public Task DisposeAsync()
			{
				return Task.CompletedTask;
			}
		}

		public class IsPartAndSpeciesCombinationUniqueAsync : IDisposable, IAsyncLifetime
		{
			private readonly GlobalInfoContext Database;
			private readonly TransformationService Transformations;

			private Species TemplateSpecies;

			public IsPartAndSpeciesCombinationUniqueAsync()
			{
				this.Database = new MockedDatabase().GetDatabaseContext();
				this.Transformations = new TransformationService(new ContentService());
			}

			public async Task InitializeAsync()
			{
				await this.Transformations.UpdateTransformationDatabaseAsync(this.Database);
				this.TemplateSpecies = this.Database.Species.First(s => s.Name == "template");
			}

			[Fact]
			public async Task ReturnsTrueForUniqueCombination()
			{
				var result = await this.Transformations.IsPartAndSpeciesCombinationUniqueAsync(this.Database, Bodypart.Wings, this.TemplateSpecies);

				Assert.True(result);
			}

			[Fact]
			public async Task ReturnsFalseForNonUniqueCombinationTask()
			{
				var result = await this.Transformations.IsPartAndSpeciesCombinationUniqueAsync(this.Database, Bodypart.Face, this.TemplateSpecies);

				Assert.False(result);
			}

			public void Dispose()
			{
				this.Database?.Dispose();
			}

			public Task DisposeAsync()
			{
				return Task.CompletedTask;
			}
		}

		public class GetTransformationByPartAndSpeciesAsync : IDisposable, IAsyncLifetime
		{
			private readonly GlobalInfoContext Database;
			private readonly TransformationService Transformations;

			private Species TemplateSpecies;

			public GetTransformationByPartAndSpeciesAsync()
			{
				this.Database = new MockedDatabase().GetDatabaseContext();
				this.Transformations = new TransformationService(new ContentService());
			}

			public async Task InitializeAsync()
			{
				await this.Transformations.UpdateTransformationDatabaseAsync(this.Database);

				this.TemplateSpecies = this.Database.Species.First(s => s.Name == "template");
			}

			[Fact]
			public async Task RetrievesCorrectBodypart()
			{
				var result = await this.Transformations.GetTransformationByPartAndSpeciesAsync(this.Database, Bodypart.Face, this.TemplateSpecies);

				Assert.True(result.IsSuccess);
				Assert.Equal(Bodypart.Face, result.Entity.Part);
				Assert.Same(this.TemplateSpecies, result.Entity.Species);
			}

			[Fact]
			public async Task ReturnsUnsuccessfulResultIfSpeciesDoesNotExist()
			{
				var nonexistantSpecies = new Species();
				var result = await this.Transformations.GetTransformationByPartAndSpeciesAsync(this.Database, Bodypart.Face, nonexistantSpecies);

				Assert.False(result.IsSuccess);
				Assert.Equal(CommandError.ObjectNotFound, result.Error);
			}

			[Fact]
			public async Task ReturnsUnsuccessfulResultIfCombinationDoesNotExist()
			{
				var result = await this.Transformations.GetTransformationByPartAndSpeciesAsync(this.Database, Bodypart.Wings, this.TemplateSpecies);

				Assert.False(result.IsSuccess);
				Assert.Equal(CommandError.ObjectNotFound, result.Error);
			}

			public void Dispose()
			{
				this.Database?.Dispose();
			}

			public Task DisposeAsync()
			{
				return Task.CompletedTask;
			}
		}

		public class GetOrCreateServerUserProtectionAsync : IDisposable, IAsyncLifetime
		{
			private readonly GlobalInfoContext Database;
			private readonly TransformationService Transformations;

			private readonly IUser User;
			private readonly IGuild Guild;

			public GetOrCreateServerUserProtectionAsync()
			{
				this.Database = new MockedDatabase().GetDatabaseContext();
				this.Transformations = new TransformationService(new ContentService());

				var mockedUser = new Mock<IUser>();
				mockedUser.Setup(u => u.Id).Returns(0);

				this.User = mockedUser.Object;

				var mockedGuild = new Mock<IGuild>();
				mockedGuild.Setup(g => g.Id).Returns(1);

				this.Guild = mockedGuild.Object;
			}

			public async Task InitializeAsync()
			{
				await this.Transformations.UpdateTransformationDatabaseAsync(this.Database);
			}

			[Fact]
			public async Task CreatesObjectIfOneDoesNotExist()
			{
				Assert.Empty(this.Database.ServerUserProtections);

				var result = await this.Transformations.GetOrCreateServerUserProtectionAsync(this.Database, this.User, this.Guild);

				Assert.NotEmpty(this.Database.ServerUserProtections);
				Assert.Same(result, this.Database.ServerUserProtections.First());
			}

			[Fact]
			public async Task CreatedObjectIsBoundToTheCorrectServer()
			{
				var result = await this.Transformations.GetOrCreateServerUserProtectionAsync(this.Database, this.User, this.Guild);

				Assert.Equal(this.Guild.Id, result.Server.DiscordID);
			}

			[Fact]
			public async Task CreatedObjectIsBoundToTheCorrectUser()
			{
				var result = await this.Transformations.GetOrCreateServerUserProtectionAsync(this.Database, this.User, this.Guild);

				Assert.Equal(this.User.Id, result.User.DiscordID);
			}

			[Fact]
			public async Task RetrievesCorrectObjectIfOneExists()
			{
				// Create an object
				var created = await this.Transformations.GetOrCreateServerUserProtectionAsync(this.Database, this.User, this.Guild);

				// Get it from the database
				var retrieved = await this.Transformations.GetOrCreateServerUserProtectionAsync(this.Database, this.User, this.Guild);

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

				var localSetting = await this.Transformations.GetOrCreateServerUserProtectionAsync(this.Database, this.User, this.Guild);

				Assert.Equal(globalSetting.DefaultOptIn, localSetting.HasOptedIn);
				Assert.Equal(globalSetting.DefaultType, localSetting.Type);
				Assert.Same(globalSetting.User, localSetting.User);
			}

			public void Dispose()
			{
				this.Database?.Dispose();
			}

			public Task DisposeAsync()
			{
				return Task.CompletedTask;
			}
		}

		public class GetOrCreateGlobalUserProtectionAsync : IDisposable, IAsyncLifetime
		{
			private readonly GlobalInfoContext Database;
			private readonly TransformationService Transformations;

			private readonly IUser User;

			public GetOrCreateGlobalUserProtectionAsync()
			{
				this.Database = new MockedDatabase().GetDatabaseContext();
				this.Transformations = new TransformationService(new ContentService());

				var mockedUser = new Mock<IUser>();
				mockedUser.Setup(u => u.Id).Returns(0);

				this.User = mockedUser.Object;
			}

			public async Task InitializeAsync()
			{
				await this.Transformations.UpdateTransformationDatabaseAsync(this.Database);
			}

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

				Assert.Equal(this.User.Id, result.User.DiscordID);
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

			public void Dispose()
			{
				this.Database?.Dispose();
			}

			public Task DisposeAsync()
			{
				return Task.CompletedTask;
			}
		}

		public class Template : IDisposable, IAsyncLifetime
		{
			private readonly GlobalInfoContext Database;
			private readonly TransformationService Transformations;

			public Template()
			{
				this.Database = new MockedDatabase().GetDatabaseContext();
				this.Transformations = new TransformationService(new ContentService());
			}

			public async Task InitializeAsync()
			{
				await this.Transformations.UpdateTransformationDatabaseAsync(this.Database);
			}

			public void Dispose()
			{
				this.Database?.Dispose();
			}

			public Task DisposeAsync()
			{
				return Task.CompletedTask;
			}
		}
	}
}
