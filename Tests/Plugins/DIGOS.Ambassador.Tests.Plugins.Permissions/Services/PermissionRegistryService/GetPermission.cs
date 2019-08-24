//
//  GetPermission.cs
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

using DIGOS.Ambassador.Tests.Plugins.Permissions.Data;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Permissions
{
    public static partial class PermissionRegistryServiceTests
    {
        public class GetPermission : PermissionRegistryServiceTestBase
        {
            [Fact]
            public void GettingUnregisteredPermissionsByTypeReturnsFalse()
            {
                var result = this.PermissionRegistry.GetPermission(typeof(TestPermission));

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public void GettingRegisteredPermissionsByTypeReturnsTrue()
            {
                this.PermissionRegistry.RegisterPermission<TestPermission>(this.Services);

                var result = this.PermissionRegistry.GetPermission(typeof(TestPermission));

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public void GettingRegisteredPermissionsByTypeReturnsCorrectPermission()
            {
                var registered = this.PermissionRegistry.RegisterPermission<TestPermission>(this.Services).Entity;

                var result = this.PermissionRegistry.GetPermission(typeof(TestPermission));

                Assert.Equal(registered, result.Entity);
            }

            [Fact]
            public void GettingUnregisteredPermissionsByGenericParameterReturnsFalse()
            {
                var result = this.PermissionRegistry.GetPermission<TestPermission>();

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public void GettingRegisteredPermissionsByGenericParameterReturnsTrue()
            {
                this.PermissionRegistry.RegisterPermission<TestPermission>(this.Services);

                var result = this.PermissionRegistry.GetPermission<TestPermission>();

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public void GettingRegisteredPermissionsByGenericParameterReturnsCorrectPermission()
            {
                var registered = this.PermissionRegistry.RegisterPermission<TestPermission>(this.Services).Entity;

                var result = this.PermissionRegistry.GetPermission<TestPermission>();

                Assert.Equal(registered, result.Entity);
            }

            [Fact]
            public void GettingUnregisteredPermissionsByFriendlyNameReturnsFalse()
            {
                var result = this.PermissionRegistry.GetPermission("I'm not registered");

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public void GettingRegisteredPermissionsByFriendlyNameReturnsTrue()
            {
                this.PermissionRegistry.RegisterPermission<TestPermission>(this.Services);

                var result = this.PermissionRegistry.GetPermission(nameof(TestPermission));

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public void GettingRegisteredPermissionsByFriendlyNameReturnsCorrectPermission()
            {
                var registered = this.PermissionRegistry.RegisterPermission<TestPermission>(this.Services).Entity;

                var result = this.PermissionRegistry.GetPermission(nameof(TestPermission));

                Assert.Equal(registered, result.Entity);
            }
        }
    }
}
