//
//  UserKinkOverlapEqualityComparer.cs
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
using DIGOS.Ambassador.Database.Users;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Determines overlap equality for two user kinks.
	/// </summary>
	public class UserKinkOverlapEqualityComparer : IEqualityComparer<UserKink>
	{
		/// <inheritdoc />
		public bool Equals(UserKink x, UserKink y)
		{
			if (x is null ^ y is null)
			{
				return false;
			}

			if (x is null) // then y must also be null
			{
				return true;
			}

			return
				x.Kink.FListID == y.Kink.FListID &&
				x.Preference == y.Preference;
		}

		/// <inheritdoc />
		public int GetHashCode(UserKink obj)
		{
			unchecked
			{
				int hash = 17;

				hash *= 23 + obj.Kink.FListID.GetHashCode();
				hash *= 23 + obj.Preference.GetHashCode();

				return hash;
			}
		}
	}
}
