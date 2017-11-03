//
//  UserPermission.cs
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

using System;
using DIGOS.Ambassador.Permissions;

namespace DIGOS.Ambassador.Database.Permissions
{
	/// <summary>
	/// Represents a granted permission with its specified target.
	/// </summary>
	public class UserPermission : IEquatable<UserPermission>
	{
		/// <summary>
		/// Gets or sets the unique ID for this permission.
		/// </summary>
		public uint UserPermissionID { get; set; }

		/// <summary>
		/// Gets or sets the granted permission.
		/// </summary>
		public Permission Permission { get; set; }

		/// <summary>
		/// Gets or sets the allowed targets.
		/// </summary>
		public PermissionTarget Target { get; set; }

		/// <summary>
		/// Gets or sets the scope of the permission.
		/// </summary>
		public PermissionScope Scope { get; set; }

		/// <summary>
		/// Gets or sets the ID of the server that this permission was granted on.
		/// </summary>
		public ulong ServerID { get; set; }

		/// <inheritdoc />
		public bool Equals(UserPermission other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return this.Permission == other.Permission && this.Target == other.Target && this.Scope == other.Scope && this.ServerID == other.ServerID;
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((UserPermission)obj);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (int)this.Permission;
				hashCode = (hashCode * 397) ^ (int)this.Target;
				hashCode = (hashCode * 397) ^ (int)this.Scope;
				hashCode = (hashCode * 397) ^ this.ServerID.GetHashCode();
				return hashCode;
			}
		}

		public static bool operator ==(UserPermission left, UserPermission right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(UserPermission left, UserPermission right)
		{
			return !Equals(left, right);
		}
	}
}
