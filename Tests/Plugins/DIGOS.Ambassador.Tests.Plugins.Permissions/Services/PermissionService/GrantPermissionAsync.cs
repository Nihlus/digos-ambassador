//
//  GrantPermissionAsync.cs
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
using DIGOS.Ambassador.Plugins.Permissions;
using DIGOS.Ambassador.Tests.Plugins.Permissions.Data;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Microsoft.EntityFrameworkCore;
using Xunit;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Permissions
{
    public static partial class PermissionServiceTests
    {
        public class GrantPermissionToUserAsync : PermissionServiceTestBase
        {
            private readonly IGuild _discordGuild;
            private readonly IUser _discordUser;
            private readonly IPermission _permission;

            public GrantPermissionToUserAsync()
            {
                _discordGuild = MockHelper.CreateDiscordGuild(0);
                _discordUser = MockHelper.CreateDiscordUser(0);

                _permission = new TestPermission();
            }

            [Fact]
            public async Task GrantingNewPermissionReturnsTrue()
            {
                var result = await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.Self
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task GrantingNewPermissionActuallyGrantsPermission()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.Self
                );

                var grantedPermission = this.Database.UserPermissions.Local.FirstOrDefault();

                Assert.NotNull(grantedPermission);
                Assert.Equal((long)_discordGuild.Id, grantedPermission.ServerID);
                Assert.Equal((long)_discordUser.Id, grantedPermission.UserID);
                Assert.Equal(_permission.UniqueIdentifier, grantedPermission.Permission);
                Assert.Equal(PermissionTarget.Self, grantedPermission.Target);
                Assert.True(grantedPermission.IsGranted);
            }

            [Fact]
            public async Task GrantingSamePermissionTwiceReturnsFalse()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.Self
                );

                var result = await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.Self
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task GrantingSamePermissionButWithAnotherTargetReturnsTrue()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.Self
                );

                var result = await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.Other
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task GrantingSamePermissionButWithAnotherTargetActuallyGrantsPermission()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.Self
                );

                await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.Other
                );

                var grantedPermission = this.Database.UserPermissions.Local.FirstOrDefault
                (
                    p => p.Target == PermissionTarget.Other
                );

                Assert.NotNull(grantedPermission);
                Assert.Equal((long)_discordGuild.Id, grantedPermission.ServerID);
                Assert.Equal((long)_discordUser.Id, grantedPermission.UserID);
                Assert.Equal(_permission.UniqueIdentifier, grantedPermission.Permission);
                Assert.Equal(PermissionTarget.Other, grantedPermission.Target);
                Assert.True(grantedPermission.IsGranted);
            }

            [Fact]
            public async Task GrantingPermissionWithAllTargetReturnsTrue()
            {
                var result = await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.All
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task GrantingPermissionWithAllTargetActuallyGrantsBothSelfAndOther()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.All
                );

                var grantedSelfPermission = this.Database.UserPermissions.Local.FirstOrDefault
                (
                    p => p.Target == PermissionTarget.Self
                );

                Assert.NotNull(grantedSelfPermission);
                Assert.Equal((long)_discordGuild.Id, grantedSelfPermission.ServerID);
                Assert.Equal((long)_discordUser.Id, grantedSelfPermission.UserID);
                Assert.Equal(_permission.UniqueIdentifier, grantedSelfPermission.Permission);
                Assert.Equal(PermissionTarget.Self, grantedSelfPermission.Target);
                Assert.True(grantedSelfPermission.IsGranted);

                var grantedOtherPermission = this.Database.UserPermissions.Local.FirstOrDefault
                (
                    p => p.Target == PermissionTarget.Other
                );

                Assert.NotNull(grantedOtherPermission);
                Assert.Equal((long)_discordGuild.Id, grantedOtherPermission.ServerID);
                Assert.Equal((long)_discordUser.Id, grantedOtherPermission.UserID);
                Assert.Equal(_permission.UniqueIdentifier, grantedOtherPermission.Permission);
                Assert.Equal(PermissionTarget.Other, grantedOtherPermission.Target);
                Assert.True(grantedOtherPermission.IsGranted);
            }

            [Fact]
            public async Task GrantingPermissionWithAllTargetTwiceReturnsFalse()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.All
                );

                var result = await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.All
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task GrantingPermissionWithAllTargetWhenUserHasSelfAlreadyReturnsTrue()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.Self
                );

                var result = await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.All
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task GrantingPermissionWithAllTargetWhenUserAlreadyHasSelfActuallyGrantsOther()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.Self
                );

                await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.All
                );

                var grantedSelfPermission = this.Database.UserPermissions.Local.FirstOrDefault
                (
                    p => p.Target == PermissionTarget.Self
                );

                Assert.NotNull(grantedSelfPermission);
                Assert.Equal((long)_discordGuild.Id, grantedSelfPermission.ServerID);
                Assert.Equal((long)_discordUser.Id, grantedSelfPermission.UserID);
                Assert.Equal(_permission.UniqueIdentifier, grantedSelfPermission.Permission);
                Assert.Equal(PermissionTarget.Self, grantedSelfPermission.Target);
                Assert.True(grantedSelfPermission.IsGranted);

                var grantedOtherPermission = this.Database.UserPermissions.Local.FirstOrDefault
                (
                    p => p.Target == PermissionTarget.Other
                );

                Assert.NotNull(grantedOtherPermission);
                Assert.Equal((long)_discordGuild.Id, grantedOtherPermission.ServerID);
                Assert.Equal((long)_discordUser.Id, grantedOtherPermission.UserID);
                Assert.Equal(_permission.UniqueIdentifier, grantedOtherPermission.Permission);
                Assert.Equal(PermissionTarget.Other, grantedOtherPermission.Target);
                Assert.True(grantedOtherPermission.IsGranted);
            }

            [Fact]
            public async Task GrantingPermissionWithAllTargetWhenUserAlreadyHasOtherActuallyGrantsSelf()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.Other
                );

                await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.All
                );

                var grantedSelfPermission = this.Database.UserPermissions.Local.FirstOrDefault
                (
                    p => p.Target == PermissionTarget.Self
                );

                Assert.NotNull(grantedSelfPermission);
                Assert.Equal((long)_discordGuild.Id, grantedSelfPermission.ServerID);
                Assert.Equal((long)_discordUser.Id, grantedSelfPermission.UserID);
                Assert.Equal(_permission.UniqueIdentifier, grantedSelfPermission.Permission);
                Assert.Equal(PermissionTarget.Self, grantedSelfPermission.Target);
                Assert.True(grantedSelfPermission.IsGranted);

                var grantedOtherPermission = this.Database.UserPermissions.Local.FirstOrDefault
                (
                    p => p.Target == PermissionTarget.Other
                );

                Assert.NotNull(grantedOtherPermission);
                Assert.Equal((long)_discordGuild.Id, grantedOtherPermission.ServerID);
                Assert.Equal((long)_discordUser.Id, grantedOtherPermission.UserID);
                Assert.Equal(_permission.UniqueIdentifier, grantedOtherPermission.Permission);
                Assert.Equal(PermissionTarget.Other, grantedOtherPermission.Target);
                Assert.True(grantedOtherPermission.IsGranted);
            }

            [Fact]
            public async Task GrantingPermissionWithAllTargetWhenUserHasOtherAlreadyReturnsTrue()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.Other
                );

                var result = await this.Permissions.GrantPermissionAsync
                (
                    _discordGuild,
                    _discordUser,
                    _permission,
                    PermissionTarget.All
                );

                Assert.True(result.IsSuccess);
            }
        }

        public class GrantPermissionToRoleAsync : PermissionServiceTestBase
        {
            private readonly IRole _discordRole;
            private readonly IPermission _permission;

            public GrantPermissionToRoleAsync()
            {
                _discordRole = MockHelper.CreateDiscordRole(0);

                _permission = new TestPermission();
            }

            [Fact]
            public async Task GrantingNewPermissionReturnsTrue()
            {
                var result = await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.Self
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task GrantingNewPermissionActuallyGrantsPermission()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.Self
                );

                var grantedPermission = this.Database.RolePermissions.Local.FirstOrDefault();

                Assert.NotNull(grantedPermission);
                Assert.Equal((long)_discordRole.Id, grantedPermission.RoleID);
                Assert.Equal(_permission.UniqueIdentifier, grantedPermission.Permission);
                Assert.Equal(PermissionTarget.Self, grantedPermission.Target);
                Assert.True(grantedPermission.IsGranted);
            }

            [Fact]
            public async Task GrantingSamePermissionTwiceReturnsFalse()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.Self
                );

                var result = await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.Self
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task GrantingSamePermissionButWithAnotherTargetReturnsTrue()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.Self
                );

                var result = await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.Other
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task GrantingSamePermissionButWithAnotherTargetActuallyGrantsPermission()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.Self
                );

                await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.Other
                );

                var grantedPermission = this.Database.RolePermissions.Local.FirstOrDefault
                (
                    p => p.Target == PermissionTarget.Other
                );

                Assert.NotNull(grantedPermission);
                Assert.Equal((long)_discordRole.Id, grantedPermission.RoleID);
                Assert.Equal(_permission.UniqueIdentifier, grantedPermission.Permission);
                Assert.Equal(PermissionTarget.Other, grantedPermission.Target);
                Assert.True(grantedPermission.IsGranted);
            }

            [Fact]
            public async Task GrantingPermissionWithAllTargetReturnsTrue()
            {
                var result = await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.All
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task GrantingPermissionWithAllTargetActuallyGrantsBothSelfAndOther()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.All
                );

                var grantedSelfPermission = this.Database.RolePermissions.Local.FirstOrDefault
                (
                    p => p.Target == PermissionTarget.Self
                );

                Assert.NotNull(grantedSelfPermission);
                Assert.Equal((long)_discordRole.Id, grantedSelfPermission.RoleID);
                Assert.Equal(_permission.UniqueIdentifier, grantedSelfPermission.Permission);
                Assert.Equal(PermissionTarget.Self, grantedSelfPermission.Target);
                Assert.True(grantedSelfPermission.IsGranted);

                var grantedOtherPermission = this.Database.RolePermissions.Local.FirstOrDefault
                (
                    p => p.Target == PermissionTarget.Other
                );

                Assert.NotNull(grantedOtherPermission);
                Assert.Equal((long)_discordRole.Id, grantedOtherPermission.RoleID);
                Assert.Equal(_permission.UniqueIdentifier, grantedOtherPermission.Permission);
                Assert.Equal(PermissionTarget.Other, grantedOtherPermission.Target);
                Assert.True(grantedOtherPermission.IsGranted);
            }

            [Fact]
            public async Task GrantingPermissionWithAllTargetTwiceReturnsFalse()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.All
                );

                var result = await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.All
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task GrantingPermissionWithAllTargetWhenUserHasSelfAlreadyReturnsTrue()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.Self
                );

                var result = await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.All
                );

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task GrantingPermissionWithAllTargetWhenUserAlreadyHasSelfActuallyGrantsOther()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.Self
                );

                await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.All
                );

                var grantedSelfPermission = this.Database.RolePermissions.Local.FirstOrDefault
                (
                    p => p.Target == PermissionTarget.Self
                );

                Assert.NotNull(grantedSelfPermission);
                Assert.Equal((long)_discordRole.Id, grantedSelfPermission.RoleID);
                Assert.Equal(_permission.UniqueIdentifier, grantedSelfPermission.Permission);
                Assert.Equal(PermissionTarget.Self, grantedSelfPermission.Target);
                Assert.True(grantedSelfPermission.IsGranted);

                var grantedOtherPermission = this.Database.RolePermissions.Local.FirstOrDefault
                (
                    p => p.Target == PermissionTarget.Other
                );

                Assert.NotNull(grantedOtherPermission);
                Assert.Equal((long)_discordRole.Id, grantedOtherPermission.RoleID);
                Assert.Equal(_permission.UniqueIdentifier, grantedOtherPermission.Permission);
                Assert.Equal(PermissionTarget.Other, grantedOtherPermission.Target);
                Assert.True(grantedOtherPermission.IsGranted);
            }

            [Fact]
            public async Task GrantingPermissionWithAllTargetWhenUserAlreadyHasOtherActuallyGrantsSelf()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.Other
                );

                await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.All
                );

                var grantedSelfPermission = this.Database.RolePermissions.Local.FirstOrDefault
                (
                    p => p.Target == PermissionTarget.Self
                );

                Assert.NotNull(grantedSelfPermission);
                Assert.Equal((long)_discordRole.Id, grantedSelfPermission.RoleID);
                Assert.Equal(_permission.UniqueIdentifier, grantedSelfPermission.Permission);
                Assert.Equal(PermissionTarget.Self, grantedSelfPermission.Target);
                Assert.True(grantedSelfPermission.IsGranted);

                var grantedOtherPermission = this.Database.RolePermissions.Local.FirstOrDefault
                (
                    p => p.Target == PermissionTarget.Other
                );

                Assert.NotNull(grantedOtherPermission);
                Assert.Equal((long)_discordRole.Id, grantedOtherPermission.RoleID);
                Assert.Equal(_permission.UniqueIdentifier, grantedOtherPermission.Permission);
                Assert.Equal(PermissionTarget.Other, grantedOtherPermission.Target);
                Assert.True(grantedOtherPermission.IsGranted);
            }

            [Fact]
            public async Task GrantingPermissionWithAllTargetWhenUserHasOtherAlreadyReturnsTrue()
            {
                await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.Other
                );

                var result = await this.Permissions.GrantPermissionAsync
                (
                    _discordRole,
                    _permission,
                    PermissionTarget.All
                );

                Assert.True(result.IsSuccess);
            }
        }
    }
}
