using System.Collections.Generic;
using Discord;
using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Database.UserInfo;
using DIGOS.Ambassador.Permissions;
using DIGOS.Ambassador.Tests.Database;
using Moq;
using Xunit;
using PermissionTarget = DIGOS.Ambassador.Permissions.PermissionTarget;

namespace DIGOS.Ambassador.Tests
{
	public class PermissionTests
	{
		[Fact]
		public async void EmptyPermissionSetReturnsFalse()
		{
			// Set up mocked permissions
			var requiredPermission = new RequiredPermission()
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Other,
			};

			// Set up mocked users
			var user = new User
			{
				LocalPermissions = new List<LocalPermission>()
			};

			// Set up the mocked discord server
			var guildMock = new Mock<IGuild>();

			using (var mockDbConnection = new MockedDatabase())
			{
				// Add mocked data
				mockDbConnection.AddMockedUser(user);

				using (var db = mockDbConnection.GetDatabaseContext())
				{
					Assert.False(await PermissionChecker.HasPermissionAsync(db, guildMock.Object, user, requiredPermission));
				}
			}
		}

		[Fact]
		public async void ExactlyMatchingLocalPermissionSetReturnsTrue()
		{
			const ulong serverID = 1;
			var server = new Server { DiscordGuildID = serverID };

			var requiredPermission = new RequiredPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Other,
			};

			var grantedPermission = new LocalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Other,
				Server = server
			};

			var user = new User
			{
				LocalPermissions = new List<LocalPermission> { grantedPermission }
			};

			// Set up the mocked discord server
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(s => s.Id).Returns(serverID);

			using (var mockDbConnection = new MockedDatabase())
			{
				// Add mocked data
				mockDbConnection.AddMockedUser(user);

				using (var db = mockDbConnection.GetDatabaseContext())
				{
					Assert.True(await PermissionChecker.HasPermissionAsync(db, guildMock.Object, user, requiredPermission));
				}
			}
		}

		[Fact]
		public async void GrantedOtherTargetReturnsFalseForMatchingAndSelfTarget()
		{
			const ulong serverID = 1;
			var server = new Server { DiscordGuildID = serverID };

			var requiredPermission = new RequiredPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Self,
			};

			var grantedPermission = new LocalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Other,
				Server = server
			};

			var user = new User
			{
				LocalPermissions = new List<LocalPermission> { grantedPermission }
			};

			// Set up the mocked discord server
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(s => s.Id).Returns(serverID);

			using (var mockDbConnection = new MockedDatabase())
			{
				// Add mocked data
				mockDbConnection.AddMockedUser(user);

				using (var db = mockDbConnection.GetDatabaseContext())
				{
					Assert.False(await PermissionChecker.HasPermissionAsync(db, guildMock.Object, user, requiredPermission));
				}
			}
		}

		[Fact]
		public async void GrantedSelfTargetReturnsFalseForMatchingAndOtherTarget()
		{
			const ulong serverID = 1;
			var server = new Server { DiscordGuildID = serverID };

			var requiredPermission = new RequiredPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Other,
			};

			var grantedPermission = new LocalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Self,
				Server = server
			};

			var user = new User
			{
				LocalPermissions = new List<LocalPermission> { grantedPermission }
			};

			// Set up the mocked discord server
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(s => s.Id).Returns(serverID);

			using (var mockDbConnection = new MockedDatabase())
			{
				// Add mocked data
				mockDbConnection.AddMockedUser(user);

				using (var db = mockDbConnection.GetDatabaseContext())
				{
					Assert.False(await PermissionChecker.HasPermissionAsync(db, guildMock.Object, user, requiredPermission));
				}
			}
		}

		[Fact]
		public async void GrantedLocalPermissionReturnsFalseIfServerIDsDiffer()
		{
			const ulong server1ID = 1;
			const ulong server2ID = 2;

			var server1 = new Server { DiscordGuildID = server1ID };
			var server2 = new Server { DiscordGuildID = server2ID };

			var requiredPermission = new RequiredPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Self,
			};

			var grantedPermission = new LocalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Other,
				Server = server2
			};

			var user = new User
			{
				LocalPermissions = new List<LocalPermission> { grantedPermission }
			};

			// Set up the mocked discord server
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(s => s.Id).Returns(server1ID);

			using (var mockDbConnection = new MockedDatabase())
			{
				// Add mocked data (this method cascades through the sub-entities)
				mockDbConnection.AddMockedUser(user);
				mockDbConnection.AddMockedServer(server1);

				using (var db = mockDbConnection.GetDatabaseContext())
				{
					Assert.False(await PermissionChecker.HasPermissionAsync(db, guildMock.Object, user, requiredPermission));
				}
			}
		}

		[Fact]
		public async void GrantedGlobalPermissionReturnsTrueForGrantedLocal()
		{
			const ulong serverID = 1;
			var server1 = new Server { DiscordGuildID = serverID };

			var requiredPermission = new RequiredPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Self,
			};

			var grantedLocalPermission = new LocalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Self,
				Server = server1
			};

			var user = new User
			{
				LocalPermissions = new List<LocalPermission> { grantedLocalPermission }
			};

			var grantedGlobalPermission = new GlobalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Self,
				User = user
			};

			// Set up the mocked discord server
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(s => s.Id).Returns(serverID);

			using (var mockDbConnection = new MockedDatabase())
			{
				// Add mocked data (this method cascades through the sub-entities)
				mockDbConnection.AddMockedGlobalPermission(grantedGlobalPermission);

				using (var db = mockDbConnection.GetDatabaseContext())
				{
					Assert.True(await PermissionChecker.HasPermissionAsync(db, guildMock.Object, user, requiredPermission));
				}
			}
		}

		[Fact]
		public async void GrantedGlobalPermissionReturnsTrueForNonGrantedLocal()
		{
			const ulong serverID = 1;

			var requiredPermission = new RequiredPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Self,
			};

			var user = new User
			{
				LocalPermissions = new List<LocalPermission>()
			};

			var grantedGlobalPermission = new GlobalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Self,
				User = user
			};

			// Set up the mocked current discord server
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(s => s.Id).Returns(serverID);

			using (var mockDbConnection = new MockedDatabase())
			{
				// Add mocked data (this method cascades through the sub-entities)
				mockDbConnection.AddMockedGlobalPermission(grantedGlobalPermission);

				using (var db = mockDbConnection.GetDatabaseContext())
				{
					Assert.True(await PermissionChecker.HasPermissionAsync(db, guildMock.Object, user, requiredPermission));
				}
			}
		}

		[Fact]
		public async void GrantedGlobalPermissionReturnsTrueForGrantedLocalWithDifferingTarget()
		{
			const ulong serverID = 1;
			var server = new Server { DiscordGuildID = serverID };

			var requiredPermission = new RequiredPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Self,
			};

			var grantedLocalPermission = new LocalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Self,
				Server = server
			};

			var user = new User
			{
				LocalPermissions = new List<LocalPermission> { grantedLocalPermission }
			};

			var grantedGlobalPermission = new GlobalPermission
			{
				Permission = Permission.SetClass,
				Target = PermissionTarget.Other,
				User = user
			};

			// Set up the mocked current discord server
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(s => s.Id).Returns(serverID);

			using (var mockDbConnection = new MockedDatabase())
			{
				// Add mocked data (this method cascades through the sub-entities)
				mockDbConnection.AddMockedGlobalPermission(grantedGlobalPermission);

				using (var db = mockDbConnection.GetDatabaseContext())
				{
					Assert.True(await PermissionChecker.HasPermissionAsync(db, guildMock.Object, user, requiredPermission));
				}
			}
		}
	}
}