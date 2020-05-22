//
//  TransferCharacterOwnershipAsync.cs
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
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Characters
{
    public partial class CharacterServiceTests
    {
        public class TransferCharacterOwnershipAsync : CharacterServiceTestBase
        {
            private Character _character = null!;
            private User _newOwner = null!;

            public override async Task InitializeAsync()
            {
                _character = await CreateCharacterAsync();
                _newOwner = new User(1);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfCharacterIsTransferred()
            {
                var result = await this.Characters.TransferCharacterOwnershipAsync
                (
                    _newOwner,
                    this.DefaultServer,
                    _character
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task TransfersCharacter()
            {
                await this.Characters.TransferCharacterOwnershipAsync(_newOwner, this.DefaultServer, _character);

                var character = this.Database.Characters.First();
                Assert.Equal(_newOwner, character.Owner);
            }
        }
    }
}
