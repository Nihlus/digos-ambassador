//
//  SetCharacterIsNSFWAsync.cs
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
using DIGOS.Ambassador.Plugins.Characters.Model;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Characters
{
    public partial class CharacterServiceTests
    {
        public class SetCharacterIsNSFWAsync : CharacterServiceTestBase
        {
            private const bool IsNSFW = false;

            private Character _character = null!;

            public override async Task InitializeAsync()
            {
                _character = CreateCharacter(isNSFW: IsNSFW);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfIsNSFWIsTheSameAsTheCurrentIsNSFW()
            {
                var result = await this.Characters.SetCharacterIsNSFWAsync(_character, IsNSFW);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfIsNSFWIsAccepted()
            {
                var result = await this.Characters.SetCharacterIsNSFWAsync(_character, true);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task SetsIsNSFW()
            {
                const bool newIsNSFW = true;
                await this.Characters.SetCharacterIsNSFWAsync(_character, newIsNSFW);

                var character = this.Database.Characters.First();
                Assert.Equal(newIsNSFW, character.IsNSFW);
            }
        }
    }
}
