//
//  CreateCharacterRoleAsync.cs
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

using System;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Tests.TestBases;
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
        public class CreateCharacterRoleAsync : CharacterServiceTestBase
        {
            private readonly IGuild _discordGuild;
            private readonly IRole _discordRole;

            public CreateCharacterRoleAsync()
            {
                _discordGuild = MockHelper.CreateDiscordGuild(0);
                _discordRole = MockHelper.CreateDiscordRole(1, _discordGuild);
            }

            [Fact]
            public async Task CanCreateRole()
            {
                var result = await this.Characters.CreateCharacterRoleAsync
                (
                    _discordRole,
                    RoleAccess.Open
                );

                Assert.True(result.IsSuccess);
                Assert.Equal((long)_discordRole.Id, result.Entity.DiscordID);
            }

            [Fact]
            public async Task CreatedRoleHasCorrectAccess()
            {
                foreach (var enumValue in Enum.GetValues(typeof(RoleAccess)).Cast<RoleAccess>())
                {
                    var result = await this.Characters.CreateCharacterRoleAsync
                    (
                        _discordRole,
                        enumValue
                    );

                    Assert.True(result.IsSuccess);
                    Assert.Equal(enumValue, result.Entity.Access);

                    await this.Characters.DeleteCharacterRoleAsync(result.Entity);
                }
            }

            [Fact]
            public async Task CreatingRoleWhenRoleAlreadyExistsReturnsError()
            {
                await this.Characters.CreateCharacterRoleAsync
                (
                    _discordRole,
                    RoleAccess.Open
                );

                var result = await this.Characters.CreateCharacterRoleAsync
                (
                    _discordRole,
                    RoleAccess.Open
                );

                Assert.False(result.IsSuccess);
            }
        }
    }
}
