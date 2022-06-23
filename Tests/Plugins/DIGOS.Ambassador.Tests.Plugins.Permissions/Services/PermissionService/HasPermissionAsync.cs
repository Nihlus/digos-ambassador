//
//  HasPermissionAsync.cs
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
    public class HasPermissionAsync : PermissionServiceTestBase
    {
        private readonly Snowflake _discordGuild = new(0);
        private readonly Snowflake _discordUser = new(1);
        private readonly Snowflake _discordRole = new(2);

        private readonly Snowflake _discordGuildOwner = new(3);

        private readonly IPermission _permission = new TestPermission();

        [Fact]
        public async Task ReturnsFalseIfPermissionIsNotGrantedToUser()
        {
            var result = await this.Permissions.HasPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Self
            );

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsTrueIfPermissionIsGrantedToRoleButNotUser()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.Self
            );

            var result = await this.Permissions.HasPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Self
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsTrueIfPermissionIsGrantedToUserButNotRole()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Self
            );

            var result = await this.Permissions.HasPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Self
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsTrueIfPermissionIsGrantedToUserAndRole()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.Self
            );

            await this.Permissions.GrantPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Self
            );

            var result = await this.Permissions.HasPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Self
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsFalseIfPermissionIsGrantedToRoleButRevokedFromUser()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordRole,
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

            var result = await this.Permissions.HasPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Self
            );

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsTrueIfPermissionIsRevokedFromRoleButGrantedToUser()
        {
            await this.Permissions.RevokePermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.Self
            );

            await this.Permissions.GrantPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Self
            );

            var result = await this.Permissions.HasPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Self
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsTrueWithAllTargetIfBothSelfAndOtherAreGrantedToUser()
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

            var result = await this.Permissions.HasPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.All
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsTrueWithAllTargetIfBothSelfAndOtherAreGrantedToRole()
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

            var result = await this.Permissions.HasPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.All
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsFalseWithAllTargetIfJustSelfIsGrantedToUser()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Self
            );

            var result = await this.Permissions.HasPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.All
            );

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsFalseWithAllTargetIfJustOtherIsGrantedToUser()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.Other
            );

            var result = await this.Permissions.HasPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.All
            );

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsFalseWithAllTargetIfJustSelfIsGrantedToRole()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.Self
            );

            var result = await this.Permissions.HasPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.All
            );

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsFalseWithAllTargetIfJustOtherIsGrantedToRole()
        {
            await this.Permissions.GrantPermissionAsync
            (
                _discordRole,
                _permission,
                PermissionTarget.Other
            );

            var result = await this.Permissions.HasPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.All
            );

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsTrueWithAllTargetIfSelfIsGrantedToUserAndOtherIsGrantedToRole()
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
                _discordRole,
                _permission,
                PermissionTarget.Other
            );

            var result = await this.Permissions.HasPermissionAsync
            (
                _discordGuild,
                _discordUser,
                _permission,
                PermissionTarget.All
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsTrueIfUserIsGuildOwner()
        {
            var result = await this.Permissions.HasPermissionAsync
            (
                _discordGuild,
                _discordGuildOwner,
                _permission,
                PermissionTarget.Self
            );

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsTrueIfUserIsGuildOwnerAndPermissionIsRevoked()
        {
            await this.Permissions.RevokePermissionAsync
            (
                _discordGuild,
                _discordGuildOwner,
                _permission,
                PermissionTarget.Self
            );

            var result = await this.Permissions.HasPermissionAsync
            (
                _discordGuild,
                _discordGuildOwner,
                _permission,
                PermissionTarget.Self
            );

            Assert.True(result.IsSuccess);
        }
    }
}
