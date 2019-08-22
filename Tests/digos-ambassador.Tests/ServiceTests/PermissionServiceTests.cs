//
//  PermissionServiceTests.cs
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

using DIGOS.Ambassador.Plugins.Permissions;
using DIGOS.Ambassador.Tests.TestBases;
using DIGOS.Ambassador.Tests.Utility;

using Discord;

using Moq;
using Xunit;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1600
#pragma warning disable CS1591

namespace DIGOS.Ambassador.Tests.ServiceTests
{
    public class PermissionServiceTests
    {
        public class HasPermissionAsync : PermissionServiceTestBase
        {
            private readonly IGuild _guild;
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);

            public HasPermissionAsync()
            {
                var guildMock = new Mock<IGuild>();
                guildMock.Setup(g => g.Id).Returns(1);
                guildMock.Setup(g => g.OwnerId).Returns(long.MaxValue);

                _guild = guildMock.Object;
            }

            [Fact]
            public async void EmptyPermissionSetReturnsFalse()
            {
                // Set up mocked permissions
                var requiredPermission = (Permission.SetClass, PermissionTarget.Other);

                var result = await this.Permissions.HasPermissionAsync(_guild, _user, requiredPermission);

                Assert.False(result);
            }

            [Fact]
            public async void ExactlyMatchingLocalPermissionSetReturnsTrue()
            {
                var requiredPermission = (Permission.SetClass, PermissionTarget.Other);

                var grantedPermission = new LocalPermission
                {
                    Permission = Permission.SetClass,
                    Target = PermissionTarget.Other,
                    ServerDiscordID = (long)_guild.Id,
                    UserDiscordID = (long)_user.Id
                };

                await this.Database.Permissions.AddAsync(grantedPermission);
                await this.Database.SaveChangesAsync();

                Assert.True(await this.Permissions.HasPermissionAsync(_guild, _user, requiredPermission));
            }

            [Fact]
            public async void GrantedOtherTargetReturnsFalseForMatchingAndSelfTarget()
            {
                var requiredPermission = (Permission.SetClass, PermissionTarget.Self);

                var grantedPermission = new LocalPermission
                {
                    Permission = Permission.SetClass,
                    Target = PermissionTarget.Other,
                    ServerDiscordID = (long)_guild.Id,
                    UserDiscordID = (long)_user.Id
                };

                await this.Database.Permissions.AddAsync(grantedPermission);

                await this.Database.SaveChangesAsync();

                Assert.False(await this.Permissions.HasPermissionAsync(_guild, _user, requiredPermission));
            }

            [Fact]
            public async void GrantedSelfTargetReturnsFalseForMatchingAndOtherTarget()
            {
                var requiredPermission = (Permission.SetClass, PermissionTarget.Other);

                var grantedPermission = new LocalPermission
                {
                    Permission = Permission.SetClass,
                    Target = PermissionTarget.Self,
                    ServerDiscordID = (long)_guild.Id,
                    UserDiscordID = (long)_user.Id
                };

                await this.Database.Permissions.AddAsync(grantedPermission);

                await this.Database.SaveChangesAsync();

                Assert.False(await this.Permissions.HasPermissionAsync(_guild, _user, requiredPermission));
            }
        }
    }
}
