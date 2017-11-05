//
//  DefaultPermissions.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System.Collections.Generic;
using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Database.UserInfo;
using static DIGOS.Ambassador.Permissions.Permission;
using static DIGOS.Ambassador.Permissions.PermissionTarget;

namespace DIGOS.Ambassador.Permissions
{
	/// <summary>
	/// Helper class for assigning new users their default permissions.
	/// </summary>
	public static class DefaultPermissions
	{
		private static readonly List<Permission> Permissions = new List<Permission>
		{
			EditUser,
			CreateCharacter,
			DeleteCharacter,
			ImportCharacter
		};

		/// <summary>
		/// Grants the specified user their default permissions on the given server.
		/// </summary>
		/// <param name="server">The server to grant the permissions on.</param>
		/// <param name="user">The user to grant the permissions to.</param>
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
