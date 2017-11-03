//
//  PermissionChecker.cs
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

using System.Linq;
using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Database.UserInfo;

namespace DIGOS.Ambassador.Permissions
{
	/// <summary>
	/// Holds utility methods for checking permissions.
	/// </summary>
	public static class PermissionChecker
	{
		/// <summary>
		/// Determines whether or not the user has the given permission.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="permission">The permission.</param>
		/// <returns><value>true</value> if the user has the permission; otherwise, <value>false</value>.</returns>
		public static bool HasPermission(User user, UserPermission permission)
		{
			// Find any matching permissions
			var matchingPerm = user.Permissions.FirstOrDefault(p => p.Permission == permission.Permission);

			if (matchingPerm is null)
			{
				return false;
			}

			if (!(matchingPerm.Target >= permission.Target))
			{
				return false;
			}

			if (!(matchingPerm.Scope >= permission.Scope))
			{
				return false;
			}

			if (matchingPerm.Scope == PermissionScope.Local)
			{
				if (matchingPerm.ServerID != permission.ServerID)
				{
					return false;
				}
			}

			return true;
		}
	}
}
