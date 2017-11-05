using System.Collections.Generic;
using digos_ambassador.Tests.Database;
using Discord;
using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Database.UserInfo;
using DIGOS.Ambassador.Permissions;
using Moq;
using Xunit;
using static DIGOS.Ambassador.Permissions.Permission;
using static DIGOS.Ambassador.Permissions.PermissionTarget;

namespace digos_ambassador.Tests
{
	public class PermissionTests
	{
		[Fact]
		public async void EmptyPermissionSetReturnsFalse()
		{
			// Set up mocked permissions
			var requiredPermission = new RequiredPermission()
			{
				Permission = SetClass,
				Target = Other,
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
				Permission = SetClass,
				Target = Other,
			};

			var grantedPermission = new LocalPermission
			{
				Permission = SetClass,
				Target = Other,
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
				Permission = SetClass,
				Target = Self,
			};

			var grantedPermission = new LocalPermission
			{
				Permission = SetClass,
				Target = Other,
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
				Permission = SetClass,
				Target = Other,
			};

			var grantedPermission = new LocalPermission
			{
				Permission = SetClass,
				Target = Self,
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
				Permission = SetClass,
				Target = Self,
			};

			var grantedPermission = new LocalPermission
			{
				Permission = SetClass,
				Target = Other,
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
				Permission = SetClass,
				Target = Self,
			};

			var grantedLocalPermission = new LocalPermission
			{
				Permission = SetClass,
				Target = Self,
				Server = server1
			};

			var user = new User
			{
				LocalPermissions = new List<LocalPermission> { grantedLocalPermission }
			};

			var grantedGlobalPermission = new GlobalPermission
			{
				Permission = SetClass,
				Target = Self,
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
				Permission = SetClass,
				Target = Self,
			};

			var user = new User
			{
				LocalPermissions = new List<LocalPermission>()
			};

			var grantedGlobalPermission = new GlobalPermission
			{
				Permission = SetClass,
				Target = Self,
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
				Permission = SetClass,
				Target = Self,
			};

			var grantedLocalPermission = new LocalPermission
			{
				Permission = SetClass,
				Target = Self,
				Server = server
			};

			var user = new User
			{
				LocalPermissions = new List<LocalPermission> { grantedLocalPermission }
			};

			var grantedGlobalPermission = new GlobalPermission
			{
				Permission = SetClass,
				Target = Other,
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