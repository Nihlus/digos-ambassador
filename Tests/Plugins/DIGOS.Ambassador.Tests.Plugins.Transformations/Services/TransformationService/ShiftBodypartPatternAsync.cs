//
//  ShiftBodypartPatternAsync.cs
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
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Results;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using Remora.Discord.Core;
using Xunit;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Transformations
{
    public partial class TransformationServiceTests
    {
        public class ShiftBodypartPatternAsync : TransformationServiceTestBase
        {
            private readonly Snowflake _guild = new Snowflake(0);

            private readonly Snowflake _owner = new Snowflake(1);
            private readonly Snowflake _invoker = new Snowflake(2);

            private readonly Pattern _newPattern;
            private readonly Colour _newPatternColour;
            private Character _character = null!;

            public ShiftBodypartPatternAsync()
            {
                _newPattern = Pattern.Swirly;
                if (!Colour.TryParse("bright purple", out var patternColour))
                {
                    throw new InvalidOperationException("Bad colour.");
                }

                _newPatternColour = patternColour;
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
                var character = new Character
                (
                    owner,
                    new Server(_guild),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty
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

                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    _invoker,
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
                    _invoker,
                    _character,
                    Bodypart.Wing,
                    _newPattern,
                    _newPatternColour,
                    Chirality.Left
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfBodypartIsAlreadyThatPattern()
            {
                await this.Transformations.ShiftBodypartPatternAsync
                (
                    _invoker,
                    _character,
                    Bodypart.Face,
                    _newPattern,
                    _newPatternColour
                );

                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    _invoker,
                    _character,
                    Bodypart.Face,
                    _newPattern,
                    _newPatternColour
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsNoChangeIfBodypartIsAlreadyThatPattern()
            {
                await this.Transformations.ShiftBodypartPatternAsync
                (
                    _invoker,
                    _character,
                    Bodypart.Face,
                    _newPattern,
                    _newPatternColour
                );

                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    _invoker,
                    _character,
                    Bodypart.Face,
                    _newPattern,
                    _newPatternColour
                );

                Assert.Equal(ShiftBodypartAction.Nothing, result.Entity.Action);
            }

            [Fact]
            public async Task CanShiftBodypartPattern()
            {
                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    _invoker,
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
                    _invoker,
                    _character,
                    Bodypart.Face,
                    _newPattern,
                    _newPatternColour
                );

                var appearance = (await this.Transformations.GetOrCreateCurrentAppearanceAsync(_character)).Entity;

                var face = appearance.GetAppearanceComponent(Bodypart.Face, Chirality.Center);
                Assert.Equal(_newPattern, face.Pattern);
            }

            [Fact]
            public async Task ShiftsBodypartIntoCorrectPatternColour()
            {
                await this.Transformations.ShiftBodypartPatternAsync
                (
                    _invoker,
                    _character,
                    Bodypart.Face,
                    _newPattern,
                    _newPatternColour
                );

                var appearance = (await this.Transformations.GetOrCreateCurrentAppearanceAsync(_character)).Entity;

                var face = appearance.GetAppearanceComponent(Bodypart.Face, Chirality.Center);
                Assert.True(_newPatternColour.IsSameColourAs(face.PatternColour));
            }

            [Fact]
            public async Task ReturnsShiftMessage()
            {
                var result = await this.Transformations.ShiftBodypartPatternAsync
                (
                    _invoker,
                    _character,
                    Bodypart.Face,
                    _newPattern,
                    _newPatternColour
                );

                Assert.NotNull(result.Entity.ShiftMessage);
                Assert.NotEmpty(result.Entity.ShiftMessage);
            }
        }
    }
}
