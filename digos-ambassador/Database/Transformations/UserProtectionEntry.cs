//
//  UserProtectionEntry.cs
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

using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Database.Users;

namespace DIGOS.Ambassador.Database.Transformations
{
	/// <summary>
	/// Represents a protection entry, that is, a user that has been whitelisted or blacklisted by another user in the
	/// TF module.
	/// </summary>
	public class UserProtectionEntry : IEFEntity
	{
		/// <inheritdoc />
		public long ID { get; set; }

		/// <summary>
		/// Gets or sets the global protection entry that the user has been listed in.
		/// </summary>
		public GlobalUserProtection GlobalProtection { get; set; }

		/// <summary>
		/// Gets or sets the user that's listed in the global protection entry.
		/// </summary>
		public User User { get; set; }

		/// <summary>
		/// Gets or sets the type of listing.
		/// </summary>
		public ListingType Type { get; set; }
	}
}
