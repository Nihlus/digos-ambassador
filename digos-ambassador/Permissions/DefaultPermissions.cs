using System.Collections.Generic;
using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Database.UserInfo;
using static DIGOS.Ambassador.Permissions.Permission;
using static DIGOS.Ambassador.Permissions.PermissionTarget;

namespace DIGOS.Ambassador.Permissions
{
	public static class DefaultPermissions
	{
		private static readonly List<Permission> Permissions = new List<Permission>
		{
			EditUser,
			CreateCharacter,
			DeleteCharacter,
			ImportCharacter
		};

		public static void Grant(Server server, User user)
		{
			foreach (var permission in Permissions)
			{
				user.LocalPermissions.Add(new LocalPermission
				{
					Permission = permission,
					Target = Self,
					Server = server
				});
			}
		}
	}
}
