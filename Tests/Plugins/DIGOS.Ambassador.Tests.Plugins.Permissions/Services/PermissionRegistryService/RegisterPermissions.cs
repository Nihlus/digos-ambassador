//
//  RegisterPermissions.cs
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

using System.Collections.Generic;
using System.Reflection;
using DIGOS.Ambassador.Tests.Plugins.Permissions.Data;
using Moq;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Permissions;

public static partial class PermissionRegistryServiceTests
{
    public class RegisterPermissions : PermissionRegistryServiceTestBase
    {
        [Fact]
        public void ScanningAnAssemblyWithoutPermissionsReturnsTrue()
        {
            var assemblyMock = new Mock<Assembly>();
            var assembly = assemblyMock.Object;

            var result = this.PermissionRegistry.RegisterPermissions(assembly, this.Services);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void ScanningAnAssemblyWithoutPermissionsAddsNothingToTheRegistry()
        {
            var assemblyMock = new Mock<Assembly>();
            var assembly = assemblyMock.Object;

            this.PermissionRegistry.RegisterPermissions(assembly, this.Services);

            Assert.Empty(this.PermissionRegistry.RegisteredPermissions);
        }

        [Fact]
        public void ScanningAnAssemblyWithPermissionsReturnsTrue()
        {
            Assembly assembly;
            {
                var availablePermissions = new List<TypeInfo> { typeof(TestPermission).GetTypeInfo() };

                var assemblyMock = new Mock<Assembly>();
                assemblyMock.SetupGet(a => a.DefinedTypes).Returns(availablePermissions);

                assembly = assemblyMock.Object;
            }

            var result = this.PermissionRegistry.RegisterPermissions(assembly, this.Services);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void ScanningAnAssemblyWithPermissionsAddsSomethingToTheRegistry()
        {
            Assembly assembly;
            {
                var availablePermissions = new List<TypeInfo> { typeof(TestPermission).GetTypeInfo() };

                var assemblyMock = new Mock<Assembly>();
                assemblyMock.SetupGet(a => a.DefinedTypes).Returns(availablePermissions);

                assembly = assemblyMock.Object;
            }

            this.PermissionRegistry.RegisterPermissions(assembly, this.Services);

            Assert.NotEmpty(this.PermissionRegistry.RegisteredPermissions);
        }

        [Fact]
        public void ScanningAnAssemblyWithBadlyBehavedPermissionsReturnsFalse()
        {
            Assembly assembly;
            {
                var availablePermissions = new List<TypeInfo>
                {
                    typeof(BadlyBehavedPermissionThatThrowsInConstructor).GetTypeInfo()
                };

                var assemblyMock = new Mock<Assembly>();
                assemblyMock.SetupGet(a => a.DefinedTypes).Returns(availablePermissions);

                assembly = assemblyMock.Object;
            }

            var result = this.PermissionRegistry.RegisterPermissions(assembly, this.Services);

            Assert.False(result.IsSuccess);
        }
    }
}
