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

using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Permissions;
using DIGOS.Ambassador.Tests.TestBases;
using DIGOS.Ambassador.Tests.Utility;

using Discord;

using Moq;
using Xunit;
using PermissionTarget = DIGOS.Ambassador.Permissions.PermissionTarget;

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

                this._guild = guildMock.Object;
            }

            [Fact]
            public async void EmptyPermissionSetReturnsFalse()
            {
                // Set up mocked permissions
                var requiredPermission = (Permission.SetClass, PermissionTarget.Other);

                var result = await this.Permissions.HasPermissionAsync(this.Database, this._guild, this._user, requiredPermission);

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
                    ServerDiscordID = (long)this._guild.Id,
                    UserDiscordID = (long)this._user.Id
                };

                await this.Database.LocalPermissions.AddAsync(grantedPermission);
                await this.Database.SaveChangesAsync();

                Assert.True(await this.Permissions.HasPermissionAsync(this.Database, this._guild, this._user, requiredPermission));
            }

            [Fact]
            public async void GrantedOtherTargetReturnsFalseForMatchingAndSelfTarget()
            {
                var requiredPermission = (Permission.SetClass, PermissionTarget.Self);

                var grantedPermission = new LocalPermission
                {
                    Permission = Permission.SetClass,
                    Target = PermissionTarget.Other,
                    ServerDiscordID = (long)this._guild.Id,
                    UserDiscordID = (long)this._user.Id
                };

                await this.Database.LocalPermissions.AddAsync(grantedPermission);

                await this.Database.SaveChangesAsync();

                Assert.False(await this.Permissions.HasPermissionAsync(this.Database, this._guild, this._user, requiredPermission));
            }

            [Fact]
            public async void GrantedSelfTargetReturnsFalseForMatchingAndOtherTarget()
            {
                var requiredPermission = (Permission.SetClass, PermissionTarget.Other);

                var grantedPermission = new LocalPermission
                {
                    Permission = Permission.SetClass,
                    Target = PermissionTarget.Self,
                    ServerDiscordID = (long)this._guild.Id,
                    UserDiscordID = (long)this._user.Id
                };

                await this.Database.LocalPermissions.AddAsync(grantedPermission);

                await this.Database.SaveChangesAsync();

                Assert.False(await this.Permissions.HasPermissionAsync(this.Database, this._guild, this._user, requiredPermission));
            }

            [Fact]
            public async void GrantedLocalPermissionReturnsFalseIfServerIDsDiffer()
            {
                var anotherServer = MockHelper.CreateDiscordGuild(2);
                var requiredPermission = (Permission.SetClass, PermissionTarget.Self);

                var grantedPermission = new LocalPermission
                {
                    Permission = Permission.SetClass,
                    Target = PermissionTarget.Other,
                    ServerDiscordID = (long)anotherServer.Id
                };

                await this.Database.LocalPermissions.AddAsync(grantedPermission);

                await this.Database.SaveChangesAsync();

                Assert.False(await this.Permissions.HasPermissionAsync(this.Database, this._guild, this._user, requiredPermission));
            }

            [Fact]
            public async void GrantedGlobalPermissionReturnsTrueForGrantedLocal()
            {
                var requiredPermission = (Permission.SetClass, PermissionTarget.Self);

                var grantedLocalPermission = new LocalPermission
                {
                    Permission = Permission.SetClass,
                    Target = PermissionTarget.Self,
                    ServerDiscordID = (long)this._guild.Id,
                    UserDiscordID = (long)this._user.Id
                };

                var grantedGlobalPermission = new GlobalPermission
                {
                    Permission = Permission.SetClass,
                    Target = PermissionTarget.Self,
                    UserDiscordID = (long)this._user.Id
                };

                await this.Database.GlobalPermissions.AddAsync(grantedGlobalPermission);
                await this.Database.LocalPermissions.AddAsync(grantedLocalPermission);

                await this.Database.SaveChangesAsync();

                Assert.True(await this.Permissions.HasPermissionAsync(this.Database, this._guild, this._user, requiredPermission));
            }

            [Fact]
            public async void GrantedGlobalPermissionReturnsTrueForNonGrantedLocal()
            {
                var requiredPermission = (Permission.SetClass, PermissionTarget.Self);

                var grantedGlobalPermission = new GlobalPermission
                {
                    Permission = Permission.SetClass,
                    Target = PermissionTarget.Self,
                    UserDiscordID = (long)this._user.Id
                };

                await this.Database.GlobalPermissions.AddAsync(grantedGlobalPermission);
                await this.Database.SaveChangesAsync();

                Assert.True(await this.Permissions.HasPermissionAsync(this.Database, this._guild, this._user, requiredPermission));
            }

            [Fact]
            public async void GrantedGlobalPermissionReturnsTrueForGrantedLocalWithDifferingTarget()
            {
                var requiredPermission = (Permission.SetClass, PermissionTarget.Self);

                var grantedLocalPermission = new LocalPermission
                {
                    Permission = Permission.SetClass,
                    Target = PermissionTarget.Self,
                    ServerDiscordID = (long)this._guild.Id,
                    UserDiscordID = (long)this._user.Id
                };

                var grantedGlobalPermission = new GlobalPermission
                {
                    Permission = Permission.SetClass,
                    Target = PermissionTarget.Other,
                    UserDiscordID = (long)this._user.Id
                };

                await this.Database.GlobalPermissions.AddAsync(grantedGlobalPermission);
                await this.Database.LocalPermissions.AddAsync(grantedLocalPermission);

                await this.Database.SaveChangesAsync();

                Assert.True(await this.Permissions.HasPermissionAsync(this.Database, this._guild, this._user, requiredPermission));
            }
        }
    }
}
