//
//  LocalPermission.cs
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
using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Permissions;

namespace DIGOS.Ambassador.Database.Permissions
{
	/// <summary>
	/// Represents a server-specific granted permission.
	/// </summary>
	public class LocalPermission : IEquatable<LocalPermission>
	{
		/// <summary>
		/// Gets or sets the unique ID for this permission.
		/// </summary>
		public uint LocalPermissionID { get; set; }

		/// <summary>
		/// Gets or sets the granted permission.
		/// </summary>
		public Permission Permission { get; set; }

		/// <summary>
		/// Gets or sets the allowed targets.
		/// </summary>
		public PermissionTarget Target { get; set; }

		/// <summary>
		/// Gets or sets the the server that this permission has been granted on.
		/// </summary>
		public Server Server { get; set; }

		/// <inheritdoc />
		public bool Equals(LocalPermission other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return
				this.Permission == other.Permission &&
				this.Target == other.Target &&
				this.Server == other.Server;
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

			return Equals((LocalPermission)obj);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (int)this.Permission;
				hashCode = (hashCode * 397) ^ (int)this.Target;
				hashCode = (hashCode * 397) ^ (this.Server != null ? this.Server.GetHashCode() : 0);
				return hashCode;
			}
		}

		public static bool operator ==(LocalPermission left, LocalPermission right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(LocalPermission left, LocalPermission right)
		{
			return !Equals(left, right);
		}
	}
}
