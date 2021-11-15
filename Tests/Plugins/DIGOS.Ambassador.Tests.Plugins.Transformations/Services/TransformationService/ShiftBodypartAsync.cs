//
//  ShiftBodypartAsync.cs
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

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Transformations.Results;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Services;
using Remora.Discord.Core;
using Xunit;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Transformations
{
    public partial class TransformationServiceTests
    {
        public class ShiftBodypartAsync : TransformationServiceTestBase
        {
            private readonly Snowflake _guild = new Snowflake(1);

            private readonly Snowflake _owner = new Snowflake(2);
            private readonly Snowflake _invoker = new Snowflake(3);

            private Character _character = null!;

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
                var pronounService = this.Services.GetRequiredService<PronounService>();
                pronounService.WithPronounProvider(new FemininePronounProvider());
            }

            protected override async Task InitializeTestAsync()
            {
                // Ensure owner is opted into transformations
                await this.Transformations.OptInUserAsync
                (
                    _owner,
                    _guild
                );

                // Create a test character
                var owner = (await this.Users.GetOrRegisterUserAsync(_owner)).Entity;
                var server = (await this.Servers.GetOrRegisterServerAsync(_guild)).Entity;

                var character = new Character
                (
                    owner,
                    server,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    "They"
                );

                this.CharacterDatabase.Characters.Update(character);
                await this.CharacterDatabase.SaveChangesAsync();

                _character = this.CharacterDatabase.Characters.First();

                // Set up the default appearance
                await this.Transformations.GetOrCreateCurrentAppearanceAsync
                (
                    _character
                );
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
            {
                await this.Transformations.BlacklistUserAsync(_owner, _invoker);

                var result = await this.Transformations.ShiftBodypartAsync
                (
                    _invoker,
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
                    _invoker,
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
                    _invoker,
                    _character,
                    Bodypart.Wing,
                    "shark",
                    Chirality.Left
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfBodypartIsAlreadyThatSpecies()
            {
                var result = await this.Transformations.ShiftBodypartAsync
                (
                    _invoker,
                    _character,
                    Bodypart.Face,
                    "template"
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsNoChangeIfBodypartIsAlreadyThatSpecies()
            {
                var result = await this.Transformations.ShiftBodypartAsync
                (
                    _invoker,
                    _character,
                    Bodypart.Face,
                    "template"
                );

                Assert.Equal(ShiftBodypartAction.Nothing, result.Entity.Action);
            }

            [Fact]
            public async Task AddsBodypartIfItDoesNotAlreadyExist()
            {
                var appearance = (await this.Transformations.GetOrCreateCurrentAppearanceAsync(_character)).Entity;

                Assert.False(appearance.HasComponent(Bodypart.Tail, Chirality.Center));

                var result = await this.Transformations.ShiftBodypartAsync
                (
                    _invoker,
                    _character,
                    Bodypart.Tail,
                    "shark"
                );

                Assert.True(result.IsSuccess);
                Assert.True(appearance.HasComponent(Bodypart.Tail, Chirality.Center));
            }

            [Fact]
            public async Task ShiftsBodypartIntoCorrectSpecies()
            {
                var result = await this.Transformations.ShiftBodypartAsync
                (
                    _invoker,
                    _character,
                    Bodypart.Face,
                    "shark"
                );

                var appearance = (await this.Transformations.GetOrCreateCurrentAppearanceAsync(_character)).Entity;

                Assert.True(result.IsSuccess);
                Assert.Equal("shark", appearance.GetAppearanceComponent(Bodypart.Face, Chirality.Center).Transformation.Species.Name);
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                var result = await this.Transformations.ShiftBodypartAsync
                (
                    _invoker,
                    _character,
                    Bodypart.Face,
                    "shark"
                );

                Assert.NotNull(result.Entity.ShiftMessage);
                Assert.NotEmpty(result.Entity.ShiftMessage);
            }

            [Fact]
            public async Task ShiftingBodypartDoesNotAlterDefaultAppearance()
            {
                await this.Transformations.ShiftBodypartAsync
                (
                    _invoker,
                    _character,
                    Bodypart.Face,
                    "shark"
                );

                var appearance = (await this.Transformations.GetOrCreateDefaultAppearanceAsync(_character)).Entity;

                Assert.NotEqual
                (
                    "shark",
                    appearance.Components.First(c => c.Bodypart == Bodypart.Face).Transformation.Species.Name
                );
            }

            [Fact]
            public async Task CanResetAppearance()
            {
                await this.Transformations.ShiftBodypartAsync
                (
                    _invoker,
                    _character,
                    Bodypart.Face,
                    "shark"
                );

                await this.Transformations.ResetCharacterFormAsync
                (
                    _character
                );

                var appearance = (await this.Transformations.GetOrCreateCurrentAppearanceAsync(_character)).Entity;

                Assert.NotEqual
                (
                    "shark",
                    appearance.Components.First(c => c.Bodypart == Bodypart.Face).Transformation.Species.Name
                );
            }

            [Fact]
            public async Task CanSetCustomDefaultAppearance()
            {
                await this.Transformations.ShiftBodypartAsync
                (
                    _invoker,
                    _character,
                    Bodypart.Face,
                    "shark"
                );

                await this.Transformations.SetCurrentAppearanceAsDefaultForCharacterAsync
                (
                    _character
                );

                var appearance = (await this.Transformations.GetOrCreateCurrentAppearanceAsync(_character)).Entity;

                Assert.Equal
                (
                    "shark",
                    appearance.Components.First(c => c.Bodypart == Bodypart.Face).Transformation.Species.Name
                );
            }

            [Fact]
            public async Task CanResetToCustomDefaultAppearance()
            {
                await this.Transformations.ShiftBodypartAsync
                (
                    _invoker,
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
                    _invoker,
                    _character,
                    Bodypart.Face,
                    "shark-dronie"
                );

                await this.Transformations.ResetCharacterFormAsync
                (
                    _character
                );

                var appearance = (await this.Transformations.GetOrCreateCurrentAppearanceAsync(_character)).Entity;

                Assert.Equal
                (
                    "shark",
                    appearance.Components.First(c => c.Bodypart == Bodypart.Face).Transformation.Species.Name
                );
            }
        }
    }
}
