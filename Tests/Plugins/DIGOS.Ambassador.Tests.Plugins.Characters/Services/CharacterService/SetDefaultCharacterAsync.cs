//
//  SetDefaultCharacterAsync.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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

using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Characters.Model;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Characters;

public partial class CharacterServiceTests
{
    public class SetDefaultCharacterAsync : CharacterServiceTestBase
    {
        private readonly Character _character;

        public SetDefaultCharacterAsync()
        {
            _character = CreateCharacter();
        }

        [Fact]
        public async Task CanSetDefaultCharacter()
        {
            var result = await this.Characters.SetDefaultCharacterAsync
            (
                this.DefaultOwner,
                this.DefaultServer,
                _character
            );

            Assert.True(result.IsSuccess);

            var defaultCharacterResult = await this.Characters.GetDefaultCharacterAsync
            (
                this.DefaultOwner,
                this.DefaultServer
            );

            Assert.Same(_character, defaultCharacterResult.Entity);
        }

        [Fact]
        public async Task ReturnsErrorIfDefaultCharacterIsAlreadySetToTheSameCharacter()
        {
            await this.Characters.SetDefaultCharacterAsync
            (
                this.DefaultOwner,
                this.DefaultServer,
                _character
            );

            var result = await this.Characters.SetDefaultCharacterAsync
            (
                this.DefaultOwner,
                this.DefaultServer,
                _character
            );

            Assert.False(result.IsSuccess);

            var defaultCharacterResult = await this.Characters.GetDefaultCharacterAsync
            (
                this.DefaultOwner,
                this.DefaultServer
            );

            Assert.Same(_character, defaultCharacterResult.Entity);
        }
    }
}
