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
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Discord.Commands;
using Moq;
using Xunit;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Transformations
{
    public partial class TransformationServiceTests
    {
        public class ShiftBodypartPatternAsync : TransformationServiceTestBase
        {
            private readonly IGuild _guild;

            private readonly IUser _owner = MockHelper.CreateDiscordGuildUser(0);
            private readonly IUser _invoker = MockHelper.CreateDiscordGuildUser(1);

            private readonly Pattern _newPattern;
            private readonly Colour _newPatternColour;

            private readonly ICommandContext _context;
            private Character _character = null!;

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
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    _owner,
                    _guild
                );

                protection.Entity.HasOptedIn = true;

                // Create a test character
                var owner = (await this.Users.GetOrRegisterUserAsync(_owner)).Entity;
                var character = new Character
                (
                    owner,
                    new Server(0),
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

                var appearance = (await this.Transformations.GetOrCreateCurrentAppearanceAsync(_character)).Entity;

                var face = appearance.GetAppearanceComponent(Bodypart.Face, Chirality.Center);
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

                var appearance = (await this.Transformations.GetOrCreateCurrentAppearanceAsync(_character)).Entity;

                var face = appearance.GetAppearanceComponent(Bodypart.Face, Chirality.Center);
                Assert.True(_newPatternColour.IsSameColourAs(face.PatternColour));
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
    }
}
