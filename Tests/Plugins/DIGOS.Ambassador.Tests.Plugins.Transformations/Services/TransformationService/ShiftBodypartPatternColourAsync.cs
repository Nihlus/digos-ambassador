//
//  ShiftBodypartPatternColourAsync.cs
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
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Transformations
{
    public partial class TransformationServiceTests
    {
        public class ShiftBodypartPatternColourAsync : TransformationServiceTestBase
        {
            private readonly IGuild _guild;

            private readonly IUser _owner = MockHelper.CreateDiscordGuildUser(0);
            private readonly IUser _invoker = MockHelper.CreateDiscordGuildUser(1);

            private readonly Colour _newPatternColour;

            private readonly ICommandContext _context;
            private Character _character = null!;

            private Colour _originalPatternColour = null!;

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
                this.Services.GetRequiredService<PronounService>().WithPronounProvider(new TheyPronounProvider());

                // Ensure owner is opted into transformations
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    _owner,
                    _guild
                );

                protection.Entity.HasOptedIn = true;

                // Create a test character
                var owner = (await this.Users.GetOrRegisterUserAsync(_owner)).Entity;
                var character = new Character(0, owner, "Test");

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
                await this.Transformations.GetOrCreateCurrentAppearanceAsync
                (
                    _character
                );
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

                var appearance = (await this.Transformations.GetOrCreateCurrentAppearanceAsync(_character)).Entity;

                var face = appearance.GetAppearanceComponent(Bodypart.Face, Chirality.Center);
                Assert.True(_newPatternColour.IsSameColourAs(face.PatternColour));
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
