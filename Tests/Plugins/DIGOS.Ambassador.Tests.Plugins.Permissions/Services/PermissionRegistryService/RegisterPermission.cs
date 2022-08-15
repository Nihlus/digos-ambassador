//
//  RegisterPermission.cs
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

using DIGOS.Ambassador.Tests.Plugins.Permissions.Data;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Permissions;

public static partial class PermissionRegistryServiceTests
{
    public class RegisterPermission : PermissionRegistryServiceTestBase
    {
        [Fact]
        public void RegisteringPermissionByTypeReturnsTrue()
        {
            var result = this.PermissionRegistry.RegisterPermission(typeof(TestPermission), this.Services);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void RegisteringPermissionByGenericArgumentReturnsTrue()
        {
            var result = this.PermissionRegistry.RegisterPermission<TestPermission>(this.Services);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void RegisteringSamePermissionTwiceReturnsFalse()
        {
            this.PermissionRegistry.RegisterPermission(typeof(TestPermission), this.Services);
            var result = this.PermissionRegistry.RegisterPermission(typeof(TestPermission), this.Services);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void RegisteringBadlyBehavedPermissionThatThrowsDuringCreationReturnsFalse()
        {
            var result = this.PermissionRegistry.RegisterPermission
            (
                typeof(BadlyBehavedPermissionThatThrowsInConstructor),
                this.Services
            );

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void RegisteringBadlyBehavedPermissionWithSameGUIDAsAnotherPermissionReturnsFalse()
        {
            this.PermissionRegistry.RegisterPermission(typeof(TestPermission), this.Services);
            var result = this.PermissionRegistry.RegisterPermission
            (
                typeof(BadlyBehavedPermissionWithSameGUIDAsAnotherPermission),
                this.Services
            );

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void RegisteringFailingCaseUsingGenericParameterReturnsFalse()
        {
            this.PermissionRegistry.RegisterPermission<TestPermission>(this.Services);
            var result = this.PermissionRegistry.RegisterPermission<TestPermission>(this.Services);

            Assert.False(result.IsSuccess);
        }
    }
}
