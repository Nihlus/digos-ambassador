//
//  RemoveBodypartAsync.cs
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
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Results;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.Core;
using Xunit;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Transformations;

public partial class TransformationServiceTests
{
    public class RemoveBodypartAsync : TransformationServiceTestBase
    {
        private readonly Snowflake _guild = new Snowflake(1);

        private readonly Snowflake _owner = new Snowflake(2);
        private readonly Snowflake _invoker = new Snowflake(3);

        private Character _character = null!;
        private Appearance _appearance = null!;

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

            var character = this.CharacterDatabase.CreateProxy<Character>
            (
                owner,
                server,
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
            var getAppearanceConfigurationResult = await this.Transformations.GetOrCreateCurrentAppearanceAsync
            (
                _character
            );

            _appearance = getAppearanceConfigurationResult.Entity;
        }

        [Fact]
        public async Task ReturnsUnsuccessfulResultIfUserIsNotAllowedToTransformTarget()
        {
            await this.Transformations.BlacklistUserAsync(_owner, _invoker);

            var result = await this.Transformations.RemoveBodypartAsync
            (
                _invoker,
                _character,
                Bodypart.Face
            );

            Assert.False(result.IsSuccess);
            Assert.True(_appearance.HasComponent(Bodypart.Face, Chirality.Center));
        }

        [Fact]
        public async Task ReturnsSuccessfulResultIfCharacterDoesNotHaveBodypart()
        {
            var result = await this.Transformations.RemoveBodypartAsync
            (
                _invoker,
                _character,
                Bodypart.Wing,
                Chirality.Left
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsNoChangeIfCharacterDoesNotHaveBodypart()
        {
            var result = await this.Transformations.RemoveBodypartAsync
            (
                _invoker,
                _character,
                Bodypart.Wing,
                Chirality.Left
            );

            Assert.Equal(ShiftBodypartAction.Nothing, result.Entity.Action);
        }

        [Fact]
        public async Task ReturnsSuccessfulResultIfCharacterHasBodypart()
        {
            var result = await this.Transformations.RemoveBodypartAsync
            (
                _invoker,
                _character,
                Bodypart.Face
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RemovesCorrectBodypart()
        {
            Assert.Contains(_appearance.Components, c => c.Bodypart == Bodypart.Face);

            await this.Transformations.RemoveBodypartAsync
            (
                _invoker,
                _character,
                Bodypart.Face
            );

            Assert.DoesNotContain(_appearance.Components, c => c.Bodypart == Bodypart.Face);
        }

        [Fact]
        public async Task ReturnsShiftMessage()
        {
            var result = await this.Transformations.RemoveBodypartAsync
            (
                _invoker,
                _character,
                Bodypart.Face
            );

            Assert.NotNull(result.Entity.ShiftMessage);
        }
    }
}
