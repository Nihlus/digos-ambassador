//
//  GlobalUserProtection.cs
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
using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Database.Users;
using DIGOS.Ambassador.Services;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Database.Transformations
{
	/// <summary>
	/// Holds global protection data for a specific user.
	/// </summary>
	public class GlobalUserProtection : IEFEntity
	{
		/// <inheritdoc />
		public uint ID { get; set; }

		/// <summary>
		/// Gets or sets the user that owns this protection data.
		/// </summary>
		public User User { get; set; }

		/// <summary>
		/// Gets or sets the default protection type to use on new servers.
		/// </summary>
		public ProtectionType DefaultType { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the user should be opted in by default.
		/// </summary>
		public bool DefaultOptIn { get; set; }

		/// <summary>
		/// Gets or sets the list of users that are allowed to transform the owner.
		/// </summary>
		public List<User> Whitelist { get; set; }

		/// <summary>
		/// Gets or sets the list of users that are prohibited from transforming the owner.
		/// </summary>
		public List<User> Blacklist { get; set; }

		/// <summary>
		/// Creates a default global protection object for the given user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns>A default user protection object.</returns>
		[Pure]
		[NotNull]
		public static GlobalUserProtection CreateDefault([NotNull] User user)
		{
			return new GlobalUserProtection
			{
				User = user,
				DefaultType = ProtectionType.Blacklist,
				DefaultOptIn = false,
				Whitelist = new List<User>(),
				Blacklist = new List<User>()
			};
		}
	}
}
