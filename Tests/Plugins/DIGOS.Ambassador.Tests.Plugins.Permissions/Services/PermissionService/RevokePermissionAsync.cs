//
//  RevokePermissionAsync.cs
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

using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Permissions;
using DIGOS.Ambassador.Tests.Plugins.Permissions.Data;
using Remora.Rest.Core;
using Xunit;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Permissions;

public static partial class PermissionServiceTests
{
    public class RevokePermissionFromUserAsync : PermissionServiceTestBase
    {
        private readonly Snowflake _discordGuild;
        private readonly Snowflake _discordUser;
        private readonly IPermission _permission;

        public RevokePermissionFromUserAsync()
        {
            _discordGuild = new Snowflake(0);
            _discordUser = new Snowflake(0);

            _permission = new TestPermission();
        }

        [Fact]
        public async Task RevokingAPermissionThatIsNotGrantedReturnsFalse()
        {
            var result = await this.Permissions.RevokePermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Self
            );

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task RevokingAPermissionThatIsGrantedReturnsTrue()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Self
            );

            var result = await this.Permissions.RevokePermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Self
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RevokingAPermissionThatIsGrantedActuallyRevokesIt()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Self
            );

            await this.Permissions.RevokePermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Self
            );

            var permission = this.Database.UserPermissions.First();

            Assert.False(permission.IsGranted);
        }

        [Fact]
        public async Task RevokingAPermissionWithAllTargetReturnsFalseIfNeitherSelfNorOtherAreGranted()
        {
            var result = await this.Permissions.RevokePermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.All
            );

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task RevokingAPermissionWithAllTargetReturnsTrueIfBothSelfAndOtherAreGranted()
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

            var result = await this.Permissions.RevokePermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.All
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RevokingAPermissionWithAllTargetReturnsTrueIfJustSelfIsGranted()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Self
            );

            var result = await this.Permissions.RevokePermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.All
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RevokingAPermissionWithAllTargetReturnsTrueIfJustOtherIsGranted()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Other
            );

            var result = await this.Permissions.RevokePermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.All
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RevokingAPermissionWithAllTargetActuallyRevokesBothIfBothSelfAndOtherAreGranted()
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

            await this.Permissions.RevokePermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.All
            );

            var selfPermission = this.Database.UserPermissions.First
            (
                p => p.Target == PermissionTarget.Self
            );

            Assert.False(selfPermission.IsGranted);

            var otherPermission = this.Database.UserPermissions.First
            (
                p => p.Target == PermissionTarget.Other
            );

            Assert.False(otherPermission.IsGranted);
        }

        [Fact]
        public async Task RevokingAPermissionWithAllTargetRevokesJustSelfIfJustSelfIsGranted()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Self
            );

            await this.Permissions.RevokePermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.All
            );

            var selfPermission = this.Database.UserPermissions.First
            (
                p => p.Target == PermissionTarget.Self
            );

            Assert.False(selfPermission.IsGranted);
        }

        [Fact]
        public async Task RevokingAPermissionWithAllTargetRevokesJustOtherIfJustOtherIsGranted()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Other
            );

            await this.Permissions.RevokePermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.All
            );

            var otherPermission = this.Database.UserPermissions.First
            (
                p => p.Target == PermissionTarget.Other
            );

            Assert.False(otherPermission.IsGranted);
        }
    }

    public class RevokePermissionFromRoleAsync : PermissionServiceTestBase
    {
        private readonly Snowflake _discordRole;
        private readonly IPermission _permission;

        public RevokePermissionFromRoleAsync()
        {
            _discordRole = new Snowflake(0);

            _permission = new TestPermission();
        }

        [Fact]
        public async Task RevokingAPermissionThatIsNotGrantedReturnsFalse()
        {
            var result = await this.Permissions.RevokePermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.Self
            );

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task RevokingAPermissionThatIsGrantedReturnsTrue()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.Self
            );

            var result = await this.Permissions.RevokePermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.Self
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RevokingAPermissionThatIsGrantedActuallyRevokesIt()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.Self
            );

            await this.Permissions.RevokePermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.Self
            );

            var permission = this.Database.RolePermissions.First();

            Assert.False(permission.IsGranted);
        }

        [Fact]
        public async Task RevokingAPermissionWithAllTargetReturnsFalseIfNeitherSelfNorOtherAreGranted()
        {
            var result = await this.Permissions.RevokePermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.All
            );

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task RevokingAPermissionWithAllTargetReturnsTrueIfBothSelfAndOtherAreGranted()
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

            var result = await this.Permissions.RevokePermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.All
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RevokingAPermissionWithAllTargetReturnsTrueIfJustSelfIsGranted()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.Self
            );

            var result = await this.Permissions.RevokePermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.All
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RevokingAPermissionWithAllTargetReturnsTrueIfJustOtherIsGranted()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.Other
            );

            var result = await this.Permissions.RevokePermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.All
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RevokingAPermissionWithAllTargetActuallyRevokesBothIfBothSelfAndOtherAreGranted()
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

            await this.Permissions.RevokePermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.All
            );

            var selfPermission = this.Database.RolePermissions.First
            (
                p => p.Target == PermissionTarget.Self
            );

            Assert.False(selfPermission.IsGranted);

            var otherPermission = this.Database.RolePermissions.First
            (
                p => p.Target == PermissionTarget.Other
            );

            Assert.False(otherPermission.IsGranted);
        }

        [Fact]
        public async Task RevokingAPermissionWithAllTargetRevokesJustSelfIfJustSelfIsGranted()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.Self
            );

            await this.Permissions.RevokePermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.All
            );

            var selfPermission = this.Database.RolePermissions.First
            (
                p => p.Target == PermissionTarget.Self
            );

            Assert.False(selfPermission.IsGranted);
        }

        [Fact]
        public async Task RevokingAPermissionWithAllTargetRevokesJustOtherIfJustOtherIsGranted()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.Other
            );

            await this.Permissions.RevokePermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.All
            );

            var otherPermission = this.Database.RolePermissions.First
            (
                p => p.Target == PermissionTarget.Other
            );

            Assert.False(otherPermission.IsGranted);
        }
    }
}
