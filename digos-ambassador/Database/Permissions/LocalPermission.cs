//
//  LocalPermission.cs
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

using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Permissions;

namespace DIGOS.Ambassador.Database.Permissions
{
	/// <summary>
	/// Represents a server-specific granted permission.
	/// </summary>
	public class LocalPermission : IPermission<LocalPermission>, IEFEntity
	{
		/// <inheritdoc />
		public uint ID { get; set; }

		/// <summary>
		/// Gets or sets the the server that this permission has been granted on.
		/// </summary>
		public ulong ServerDiscordID { get; set; }

		/// <inheritdoc />
		public ulong UserDiscordID { get; set; }

		/// <inheritdoc />
		public Permission Permission { get; set; }

		/// <inheritdoc />
		public PermissionTarget Target { get; set; }

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
				this.ServerDiscordID == other.ServerDiscordID &&
				this.UserDiscordID == other.UserDiscordID;
		}

		/// <inheritdoc />
		[SuppressMessage("ReSharper", "ArrangeThisQualifier", Justification = "Used for explicit differentiation between compared objects.")]
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
		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = "Class is an entity.")]
		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (int)this.Permission;
				hashCode = (hashCode * 397) ^ (int)this.Target;
				hashCode = (hashCode * 397) ^ this.UserDiscordID.GetHashCode();
				hashCode = (hashCode * 397) ^ this.ServerDiscordID.GetHashCode();
				return hashCode;
			}
		}

		/// <summary>
		/// Compares the equality of two <see cref="LocalPermission"/> objects.
		/// </summary>
		/// <param name="left">The first object.</param>
		/// <param name="right">The second object.</param>
		/// <returns>true if the objects are equal; otherwise, false.</returns>
		public static bool operator ==(LocalPermission left, LocalPermission right)
		{
			return Equals(left, right);
		}

		/// <summary>
		/// Compares the inequality of two <see cref="LocalPermission"/> objects.
		/// </summary>
		/// <param name="left">The first object.</param>
		/// <param name="right">The second object.</param>
		/// <returns>true if the objects are equal; otherwise, false.</returns>
		public static bool operator !=(LocalPermission left, LocalPermission right)
		{
			return !Equals(left, right);
		}
	}
}
