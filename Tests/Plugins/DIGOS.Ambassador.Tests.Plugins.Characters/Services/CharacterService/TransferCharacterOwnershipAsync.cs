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
using DIGOS.Ambassador.Tests.Utility;
using Discord;
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
            private readonly Character _character;

            private readonly IUser _originalOwner = MockHelper.CreateDiscordUser(0);
            private readonly IUser _newOwner = MockHelper.CreateDiscordUser(1);
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(2);

            private readonly User _dbNewOwner;

            public TransferCharacterOwnershipAsync()
            {
                var dbOldOwner = new User((long)_originalOwner.Id);
                _dbNewOwner = new User((long)_newOwner.Id);

                _character = new Character((long)_guild.Id, dbOldOwner, "Dummy");

                this.Database.Characters.Update(_character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfCharacterIsTransferred()
            {
                var result = await this.Characters.TransferCharacterOwnershipAsync(_dbNewOwner, _character, _guild);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task TransfersCharacter()
            {
                await this.Characters.TransferCharacterOwnershipAsync(_dbNewOwner, _character, _guild);

                var character = this.Database.Characters.First();
                Assert.Equal((long)_newOwner.Id, character.Owner.DiscordID);
            }
        }
    }
}
