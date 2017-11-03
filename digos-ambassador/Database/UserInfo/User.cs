//
//  User.cs
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
using System.Collections.Generic;

namespace DIGOS.Ambassador.Database.UserInfo
{
	/// <summary>
	/// Represents globally accessible information about a user.
	/// </summary>
	public class User
	{
		/// <summary>
		/// Gets or sets the class of the user within the DIGOS 'verse.
		/// </summary>
		public UserClass Class { get; set; }

		/// <summary>
		/// Gets or sets the biography of the user. This contains useful information that the users provide themselves.
		/// </summary>
		public string Bio { get; set; }

		/// <summary>
		/// Gets or sets the current timezone of the user.
		/// </summary>
		public TimeZoneInfo Timezone { get; set; }

		/// <summary>
		/// Gets or sets the characters that the user has.
		/// </summary>
		public List<Character> Characters { get; set; }

		/// <summary>
		/// Gets or sets the kinks or fetishes of a user, as well as their preferences for each.
		/// </summary>
		public Dictionary<Kink, KinkPreference> Kinks { get; set; }
	}
}
