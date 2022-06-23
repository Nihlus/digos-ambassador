//
//  ShiftBodypartColourAsync.cs
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

using System;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Results;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Services;
using Remora.Rest.Core;
using Xunit;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Transformations;

public partial class TransformationServiceTests
{
    public class ShiftBodypartColourAsync : TransformationServiceTestBase
    {
        private readonly Snowflake _guild = new(1);
        private readonly Snowflake _owner = new(2);
        private readonly Snowflake _invoker = new(3);

        private readonly Colour _newColour;
        private Character _character = null!;

        private Colour _originalColour = null!;

        public ShiftBodypartColourAsync()
        {
            if (!Colour.TryParse("bright purple", out var colour))
            {
                throw new InvalidOperationException("Bad colour.");
            }

            _newColour = colour;
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
            var getAppearanceConfigurationResult = await this.Transformations.GetOrCreateCurrentAppearanceAsync
            (
                _character
            );

            var appearance = getAppearanceConfigurationResult.Entity;
            _originalColour = appearance.GetAppearanceComponent(Bodypart.Face, Chirality.Center).BaseColour;
        }

        [Fact]
        public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
        {
            await this.Transformations.BlacklistUserAsync(_owner, _invoker);

            var result = await this.Transformations.ShiftBodypartColourAsync
            (
                _invoker,
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
                _invoker,
                _character,
                Bodypart.Wing,
                _newColour,
                Chirality.Left
            );

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsSuccessfulResultIfBodypartIsAlreadyThatColour()
        {
            var result = await this.Transformations.ShiftBodypartColourAsync
            (
                _invoker,
                _character,
                Bodypart.Face,
                _originalColour
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsNoChangeIfBodypartIsAlreadyThatColour()
        {
            var result = await this.Transformations.ShiftBodypartColourAsync
            (
                _invoker,
                _character,
                Bodypart.Face,
                _originalColour
            );

            Assert.Equal(ShiftBodypartAction.Nothing, result.Entity.Action);
        }

        [Fact]
        public async Task CanShiftBodypartColour()
        {
            var result = await this.Transformations.ShiftBodypartColourAsync
            (
                _invoker,
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
                _invoker,
                _character,
                Bodypart.Face,
                _newColour
            );

            var appearance = (await this.Transformations.GetOrCreateCurrentAppearanceAsync(_character)).Entity;

            var face = appearance.Components.First(c => c.Bodypart == Bodypart.Face);
            Assert.True(_newColour.IsSameColourAs(face.BaseColour));
        }

        [Fact]
        public async Task ReturnsShiftMessage()
        {
            var result = await this.Transformations.ShiftBodypartColourAsync
            (
                _invoker,
                _character,
                Bodypart.Face,
                _newColour
            );

            Assert.NotNull(result.Entity.ShiftMessage);
            Assert.NotEmpty(result.Entity.ShiftMessage);
        }
    }
}
