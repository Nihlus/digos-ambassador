//
//  SetCharacterRoleAccessAsync.cs
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
        public class SetCharacterRoleAccessAsync : CharacterServiceTestBase
        {
            private readonly IRole _discordRole;

            public SetCharacterRoleAccessAsync()
            {
                var guild = MockHelper.CreateDiscordGuild(0);
                _discordRole = MockHelper.CreateDiscordRole(1, guild);
            }

            public override async Task InitializeAsync()
            {
                await this.Characters.CreateCharacterRoleAsync
                (
                    _discordRole,
                    RoleAccess.Open
                );
            }

            [Fact]
            public async Task CanSetAccess()
            {
                var getExistingRoleResult = await this.Characters.GetCharacterRoleAsync
                (
                    _discordRole
                );

                var existingRole = getExistingRoleResult.Entity;

                Assert.Equal(RoleAccess.Open, existingRole.Access);

                var result = await this.Characters.SetCharacterRoleAccessAsync
                (
                    existingRole,
                    RoleAccess.Restricted
                );

                Assert.True(result.IsSuccess);
                Assert.Equal(RoleAccess.Restricted, existingRole.Access);
            }
        }
    }
}
