//
//  RequiredPermission.cs
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

namespace DIGOS.Ambassador.Permissions
{
	/// <summary>
	/// Represents a simple record class for passing around required permissions.
	/// </summary>
	public class RequiredPermission
	{
		/// <summary>
		/// Gets or sets the required permission.
		/// </summary>
		public Permission Permission { get; set; }

		/// <summary>
		/// Gets or sets the required target.
		/// </summary>
		public PermissionTarget Target { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RequiredPermission"/> class.
		/// </summary>
		public RequiredPermission()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RequiredPermission"/> class.
		/// </summary>
		/// <param name="permission">The permission that is required-</param>
		/// <param name="target">The target that is required.</param>
		public RequiredPermission(Permission permission, PermissionTarget target)
		{
			this.Permission = permission;
			this.Target = target;
		}
	}
}
