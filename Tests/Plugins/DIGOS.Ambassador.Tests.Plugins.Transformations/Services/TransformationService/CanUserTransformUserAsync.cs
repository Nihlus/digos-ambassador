//
//  CanUserTransformUserAsync.cs
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
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Transformations
{
    public partial class TransformationServiceTests
    {
        public class CanUserTransformUserAsync : TransformationServiceTestBase
        {
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);
            private readonly IUser _targetUser = MockHelper.CreateDiscordUser(1);

            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(0);

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfTargetUserHasNotOptedIn()
            {
                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    _guild,
                    _user,
                    _targetUser
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsOnTargetUsersBlacklist()
            {
                await EnsureOptedInAsync(_targetUser);
                await this.Transformations.BlacklistUserAsync(_targetUser, _user);

                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    _guild,
                    _user,
                    _targetUser
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfTargetUserUsesWhitelistingAndUserIsNotOnWhitelist()
            {
                await EnsureOptedInAsync(_targetUser);
                await this.Transformations.SetServerProtectionTypeAsync
                (
                    _targetUser,
                    _guild,
                    ProtectionType.Whitelist
                );

                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    _guild,
                    _user,
                    _targetUser
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfTargetUserUsesWhitelistingAndUserIsOnWhitelist()
            {
                await EnsureOptedInAsync(_targetUser);
                await this.Transformations.SetServerProtectionTypeAsync
                (
                    _targetUser,
                    _guild,
                    ProtectionType.Whitelist
                );

                await this.Transformations.WhitelistUserAsync(_targetUser, _user);

                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    _guild,
                    _user,
                    _targetUser
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfUserIsNotOnTargetUsersBlacklist()
            {
                await EnsureOptedInAsync(_targetUser);

                var result = await this.Transformations.CanUserTransformUserAsync
                (
                    _guild,
                    _user,
                    _targetUser
                );

                Assert.True(result.IsSuccess);
            }

            private async Task EnsureOptedInAsync(IUser user)
            {
                var protection = await this.Transformations.GetOrCreateServerUserProtectionAsync
                (
                    user,
                    _guild
                );
                protection.Entity.HasOptedIn = true;
            }
        }
    }
}
