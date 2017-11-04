using System;
using System.Collections.Generic;
using Discord;
using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Database.UserInfo;
using DIGOS.Ambassador.Permissions;
using Moq;
using Xunit;
using static DIGOS.Ambassador.Permissions.Permission;
using static DIGOS.Ambassador.Permissions.PermissionScope;
using static DIGOS.Ambassador.Permissions.PermissionTarget;

namespace digos_ambassador.Tests
{
	public class PermissionTests
	{
		[Fact]
		public void EmptyPermissionSetReturnsFalse()
		{
			var requiredPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Other,
				Scope = Global
			};

			var user = new User
			{
				Permissions = new List<UserPermission>()
			};

			const ulong serverID = 1;
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(s => s.Id).Returns(serverID);

			Assert.False(PermissionChecker.HasPermission(guildMock.Object, user, requiredPermission));
		}

		[Fact]
		public void ExactlyMatchingPermissionSetReturnsTrue()
		{
			var requiredPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Other,
				Scope = Global
			};

			var user = new User
			{
				Permissions = new List<UserPermission> { requiredPermission }
			};

			const ulong serverID = 1;
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(s => s.Id).Returns(serverID);

			Assert.True(PermissionChecker.HasPermission(guildMock.Object, user, requiredPermission));
		}

		[Fact]
		public void GrantedOtherTargetReturnsTrueForMatchingAndSelfTarget()
		{
			var requiredPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Self,
				Scope = Global
			};

			var grantedPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Other,
				Scope = Global
			};

			var user = new User
			{
				Permissions = new List<UserPermission> { grantedPermission }
			};

			const ulong serverID = 1;
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(s => s.Id).Returns(serverID);

			Assert.True(PermissionChecker.HasPermission(guildMock.Object, user, requiredPermission));
		}

		[Fact]
		public void GrantedGlobalScopeReturnsTrueForMatchingAndLocalScope()
		{
			var requiredPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Other,
				Scope = Local
			};

			var grantedPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Other,
				Scope = Global
			};

			var user = new User
			{
				Permissions = new List<UserPermission> { grantedPermission }
			};

			const ulong serverID = 1;
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(s => s.Id).Returns(serverID);

			Assert.True(PermissionChecker.HasPermission(guildMock.Object, user, requiredPermission));
		}

		[Fact]
		public void GrantedSelfTargetReturnsFalseForMatchingAndOtherTarget()
		{
			var requiredPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Other,
				Scope = Global
			};

			var grantedPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Self,
				Scope = Global
			};

			var user = new User
			{
				Permissions = new List<UserPermission> { grantedPermission }
			};

			const ulong serverID = 1;
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(s => s.Id).Returns(serverID);

			Assert.False(PermissionChecker.HasPermission(guildMock.Object, user, requiredPermission));
		}

		[Fact]
		public void GrantedLocalScopeReturnsFalseForMatchingAndGlobalScope()
		{
			var requiredPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Other,
				Scope = Global
			};

			var grantedPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Other,
				Scope = Local
			};

			var user = new User
			{
				Permissions = new List<UserPermission> { grantedPermission }
			};

			const ulong serverID = 1;
			var guildMock = new Mock<IGuild>();
			guildMock.Setup(s => s.Id).Returns(serverID);

			Assert.False(PermissionChecker.HasPermission(guildMock.Object, user, requiredPermission));
		}

		[Fact]
		public void GrantedGlobalPermissionReturnsTrueEvenIfServerIDsDiffer()
		{
			const ulong server1ID = 1;
			const ulong server2ID = 2;

			var server1Mock = new Mock<Server>();
			server1Mock.Setup(s => s.DiscordGuildID).Returns(server1ID);

			var server2Mock = new Mock<Server>();
			server2Mock.Setup(s => s.DiscordGuildID).Returns(server2ID);

			var requiredPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Other,
				Scope = Local,
				Servers = new List<Server> { server1Mock.Object }
			};

			var grantedPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Other,
				Scope = Global,
				Servers = new List<Server> { server2Mock.Object }
			};

			var user = new User
			{
				Permissions = new List<UserPermission> { grantedPermission }
			};

			var guildMock = new Mock<IGuild>();
			guildMock.Setup(s => s.Id).Returns(server2ID);

			Assert.True(PermissionChecker.HasPermission(guildMock.Object, user, requiredPermission));
		}

		[Fact]
		public void GrantedLocalPermissionReturnsFalseIfServerIDsDiffer()
		{
			const ulong server1ID = 1;
			const ulong server2ID = 2;

			var server1Mock = new Mock<Server>();
			server1Mock.Setup(s => s.DiscordGuildID).Returns(server1ID);

			var server2Mock = new Mock<Server>();
			server2Mock.Setup(s => s.DiscordGuildID).Returns(server2ID);

			var requiredPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Other,
				Scope = Local,
				Servers = new List<Server> { server1Mock.Object }
			};

			var grantedPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Other,
				Scope = Local,
				Servers = new List<Server> { server2Mock.Object }
			};

			var user = new User
			{
				Permissions = new List<UserPermission> { grantedPermission }
			};

			var guildMock = new Mock<IGuild>();
			guildMock.Setup(s => s.Id).Returns(server1ID);

			Assert.False(PermissionChecker.HasPermission(guildMock.Object, user, requiredPermission));
		}
	}
}