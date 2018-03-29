//
//  DefaultPermissions.cs
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

using System.Collections.Generic;
using System.Linq;
using DIGOS.Ambassador.Database.Permissions;
using JetBrains.Annotations;
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
			ImportCharacter,
			EditCharacter,
			TransferCharacter,
			AssumeCharacter,
			CreateRoleplay,
			DeleteRoleplay,
			JoinRoleplay,
			EditRoleplay,
			ReplayRoleplay,
			StartStopRoleplay,
			TransferRoleplay,
			KickRoleplayMember
		};

		/// <summary>
		/// Gets the default set of local permissions.
		/// </summary>
		[NotNull]
		public static IReadOnlyList<LocalPermission> DefaultPermissionSet => Permissions.Select(p => new LocalPermission { Permission = p, Target = Self }).ToList();
	}
}
