//
//  PermissionTests.cs
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
using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Permissions;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Tests.Database;

using Discord;
using Moq;
using Xunit;
using PermissionTarget = DIGOS.Ambassador.Permissions.PermissionTarget;

#pragma warning disable SA1600
#pragma warning disable CS1591

namespace DIGOS.Ambassador.Tests
{
	public class PermissionTests
	{
		[Fact]
		public async void EmptyPermissionSetReturnsFalse()
		{
			var permissionService = new PermissionService();

			// Set up mocked permissions
			var requiredPermission = (Permission.SetClass, PermissionTarget.Other);

			// Set up mocked users
			var userMock = new Mock<IUser>();

			// Set up the mocked discord server
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(g => g.OwnerId).Returns(uint.MaxValue);

			using (var mockDbConnection = new MockedDatabase())
			{
				using (var db = mockDbConnection.GetDatabaseContext())
				{
					Assert.False(await permissionService.HasPermissionAsync(db, guildMock.Object, userMock.Object, requiredPermission));
				}
			}
		}

		[Fact]
		public async void ExactlyMatchingLocalPermissionSetReturnsTrue()
		{
			const ulong serverID = 1;
			var server = new Server { DiscordID = serverID };

			const ulong userID = 0;
			var userMock = new Mock<IUser>();
			userMock.Setup(u => u.Id).Returns(userID);

			var permissionService = new PermissionService();

			var requiredPermission = (Permission.SetClass, PermissionTarget.Other);

			var grantedPermission = new LocalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Other,
				ServerDiscordID = server.DiscordID,
				UserDiscordID = userID
			};

			// Set up the mocked discord server
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(s => s.Id).Returns(serverID);

			using (var mockDbConnection = new MockedDatabase())
			{
				// Add mocked data
				mockDbConnection.AddMockedLocalPermission(grantedPermission);

				using (var db = mockDbConnection.GetDatabaseContext())
				{
					Assert.True(await permissionService.HasPermissionAsync(db, guildMock.Object, userMock.Object, requiredPermission));
				}
			}
		}

		[Fact]
		public async void GrantedOtherTargetReturnsFalseForMatchingAndSelfTarget()
		{
			const ulong serverID = 1;
			var server = new Server { DiscordID = serverID };

			const ulong userID = 0;
			var userMock = new Mock<IUser>();
			userMock.Setup(u => u.Id).Returns(userID);

			var permissionService = new PermissionService();

			var requiredPermission = (Permission.SetClass, PermissionTarget.Self);

			var grantedPermission = new LocalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Other,
				ServerDiscordID = server.DiscordID,
				UserDiscordID = userID
			};

			// Set up the mocked discord server
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(g => g.OwnerId).Returns(uint.MaxValue);
			guildMock.Setup(s => s.Id).Returns(serverID);

			using (var mockDbConnection = new MockedDatabase())
			{
				mockDbConnection.AddMockedLocalPermission(grantedPermission);

				using (var db = mockDbConnection.GetDatabaseContext())
				{
					Assert.False(await permissionService.HasPermissionAsync(db, guildMock.Object, userMock.Object, requiredPermission));
				}
			}
		}

		[Fact]
		public async void GrantedSelfTargetReturnsFalseForMatchingAndOtherTarget()
		{
			const ulong serverID = 1;
			var server = new Server { DiscordID = serverID };

			const ulong userID = 0;
			var userMock = new Mock<IUser>();
			userMock.Setup(u => u.Id).Returns(userID);

			var permissionService = new PermissionService();

			var requiredPermission = (Permission.SetClass, PermissionTarget.Other);

			var grantedPermission = new LocalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Self,
				ServerDiscordID = server.DiscordID,
				UserDiscordID = userID
			};

			// Set up the mocked discord server
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(g => g.OwnerId).Returns(uint.MaxValue);
			guildMock.Setup(s => s.Id).Returns(serverID);

			using (var mockDbConnection = new MockedDatabase())
			{
				// Add mocked data
				mockDbConnection.AddMockedLocalPermission(grantedPermission);

				using (var db = mockDbConnection.GetDatabaseContext())
				{
					Assert.False(await permissionService.HasPermissionAsync(db, guildMock.Object, userMock.Object, requiredPermission));
				}
			}
		}

		[Fact]
		public async void GrantedLocalPermissionReturnsFalseIfServerIDsDiffer()
		{
			const ulong server1ID = 1;
			const ulong server2ID = 2;

			var server1 = new Server { DiscordID = server1ID };
			var server2 = new Server { DiscordID = server2ID };

			const ulong userID = 0;
			var userMock = new Mock<IUser>();
			userMock.Setup(u => u.Id).Returns(userID);

			var permissionService = new PermissionService();

			var requiredPermission = (Permission.SetClass, PermissionTarget.Self);

			var grantedPermission = new LocalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Other,
				ServerDiscordID = server2.DiscordID
			};

			// Set up the mocked discord server
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(g => g.OwnerId).Returns(uint.MaxValue);
			guildMock.Setup(s => s.Id).Returns(server1ID);

			using (var mockDbConnection = new MockedDatabase())
			{
				// Add mocked data (this method cascades through the sub-entities)
				mockDbConnection.AddMockedLocalPermission(grantedPermission);
				mockDbConnection.AddMockedServer(server1);
				mockDbConnection.AddMockedServer(server2);

				using (var db = mockDbConnection.GetDatabaseContext())
				{
					Assert.False(await permissionService.HasPermissionAsync(db, guildMock.Object, userMock.Object, requiredPermission));
				}
			}
		}

		[Fact]
		public async void GrantedGlobalPermissionReturnsTrueForGrantedLocal()
		{
			const ulong serverID = 1;
			var server = new Server { DiscordID = serverID };

			const ulong userID = 0;
			var userMock = new Mock<IUser>();
			userMock.Setup(u => u.Id).Returns(userID);

			var permissionService = new PermissionService();

			var requiredPermission = (Permission.SetClass, PermissionTarget.Self);

			var grantedLocalPermission = new LocalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Self,
				ServerDiscordID = server.DiscordID,
				UserDiscordID = userID
			};

			var grantedGlobalPermission = new GlobalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Self,
				UserDiscordID = userID
			};

			// Set up the mocked discord server
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(g => g.OwnerId).Returns(uint.MaxValue);
			guildMock.Setup(s => s.Id).Returns(serverID);

			using (var mockDbConnection = new MockedDatabase())
			{
				// Add mocked data (this method cascades through the sub-entities)
				mockDbConnection.AddMockedGlobalPermission(grantedGlobalPermission);
				mockDbConnection.AddMockedLocalPermission(grantedLocalPermission);

				using (var db = mockDbConnection.GetDatabaseContext())
				{
					Assert.True(await permissionService.HasPermissionAsync(db, guildMock.Object, userMock.Object, requiredPermission));
				}
			}
		}

		[Fact]
		public async void GrantedGlobalPermissionReturnsTrueForNonGrantedLocal()
		{
			const ulong serverID = 1;

			const ulong userID = 0;
			var userMock = new Mock<IUser>();
			userMock.Setup(u => u.Id).Returns(userID);

			var permissionService = new PermissionService();

			var requiredPermission = (Permission.SetClass, PermissionTarget.Self);

			var grantedGlobalPermission = new GlobalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Self,
				UserDiscordID = userID
			};

			// Set up the mocked current discord server
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(g => g.OwnerId).Returns(uint.MaxValue);
			guildMock.Setup(s => s.Id).Returns(serverID);

			using (var mockDbConnection = new MockedDatabase())
			{
				// Add mocked data (this method cascades through the sub-entities)
				mockDbConnection.AddMockedGlobalPermission(grantedGlobalPermission);

				using (var db = mockDbConnection.GetDatabaseContext())
				{
					Assert.True(await permissionService.HasPermissionAsync(db, guildMock.Object, userMock.Object, requiredPermission));
				}
			}
		}

		[Fact]
		public async void GrantedGlobalPermissionReturnsTrueForGrantedLocalWithDifferingTarget()
		{
			const ulong serverID = 1;
			var server = new Server { DiscordID = serverID };

			const ulong userID = 0;
			var userMock = new Mock<IUser>();
			userMock.Setup(u => u.Id).Returns(userID);

			var permissionService = new PermissionService();

			var requiredPermission = (Permission.SetClass, PermissionTarget.Self);

			var grantedLocalPermission = new LocalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Self,
				ServerDiscordID = server.DiscordID,
				UserDiscordID = userID
			};

			var grantedGlobalPermission = new GlobalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Other,
				UserDiscordID = userID
			};

			// Set up the mocked current discord server
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(g => g.OwnerId).Returns(uint.MaxValue);
			guildMock.Setup(s => s.Id).Returns(serverID);

			using (var mockDbConnection = new MockedDatabase())
			{
				// Add mocked data (this method cascades through the sub-entities)
				mockDbConnection.AddMockedGlobalPermission(grantedGlobalPermission);
				mockDbConnection.AddMockedLocalPermission(grantedLocalPermission);

				using (var db = mockDbConnection.GetDatabaseContext())
				{
					Assert.True(await permissionService.HasPermissionAsync(db, guildMock.Object, userMock.Object, requiredPermission));
				}
			}
		}
	}
}
