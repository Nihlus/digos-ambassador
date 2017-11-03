using System;
using System.Collections.Generic;
using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Database.UserInfo;
using DIGOS.Ambassador.Permissions;
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

			Assert.False(PermissionChecker.HasPermission(user, requiredPermission));
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

			Assert.True(PermissionChecker.HasPermission(user, requiredPermission));
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

			Assert.True(PermissionChecker.HasPermission(user, requiredPermission));
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

			Assert.True(PermissionChecker.HasPermission(user, requiredPermission));
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

			Assert.False(PermissionChecker.HasPermission(user, requiredPermission));
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

			Assert.False(PermissionChecker.HasPermission(user, requiredPermission));
		}

		[Fact]
		public void GrantedGlobalPermissionReturnsTrueEvenIfServerIDsDiffer()
		{
			var requiredPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Other,
				Scope = Global,
				ServerID = 1
			};

			var grantedPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Other,
				Scope = Global,
				ServerID = 2
			};

			var user = new User
			{
				Permissions = new List<UserPermission> { grantedPermission }
			};

			Assert.True(PermissionChecker.HasPermission(user, requiredPermission));
		}

		[Fact]
		public void GrantedLocalPermissionReturnsFalseIfServerIDsDiffer()
		{
			var requiredPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Other,
				Scope = Local,
				ServerID = 1
			};

			var grantedPermission = new UserPermission
			{
				Permission = SetClass,
				Target = Other,
				Scope = Local,
				ServerID = 2
			};

			var user = new User
			{
				Permissions = new List<UserPermission> { grantedPermission }
			};

			Assert.False(PermissionChecker.HasPermission(user, requiredPermission));
		}
	}
}