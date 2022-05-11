//
//  ResetCharacterFormAsync.cs
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

using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using Remora.Rest.Core;
using Xunit;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Transformations;

public partial class TransformationServiceTests
{
    public class ResetCharacterFormAsync : TransformationServiceTestBase
    {
        private readonly Snowflake _user = new(0);
        private User _owner = null!;
        private Character _character = null!;

        private Appearance _appearance = null!;

        protected override async Task InitializeTestAsync()
        {
            _owner = (await this.Users.GetOrRegisterUserAsync(_user)).Entity;

            _character = new Character
            (
                _owner,
                new Server(new Snowflake(0)),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            );

            this.CharacterDatabase.Characters.Update(_character);

            // Set up the default appearance
            var getAppearanceConfigurationResult = await this.Transformations.GetOrCreateDefaultAppearanceAsync
            (
                _character
            );

            _appearance = getAppearanceConfigurationResult.Entity;
        }

        [Fact]
        public async Task CanResetForm()
        {
            var defaultAppearance = new Appearance(_character)
            {
                Height = 256
            };

            _appearance = defaultAppearance;

            var result = await this.Transformations.ResetCharacterFormAsync(_character);

            Assert.True(result.IsSuccess);
            Assert.NotNull(_appearance);
            Assert.Equal(_appearance.Height, _appearance.Height);
        }
    }
}
