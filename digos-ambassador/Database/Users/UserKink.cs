//
//  UserKink.cs
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

using DIGOS.Ambassador.Database.Kinks;

namespace DIGOS.Ambassador.Database.Users
{
	/// <summary>
	/// Represents a user's kink, along with their preference for it.
	/// </summary>
	public class UserKink
	{
		/// <summary>
		/// Gets or sets the unique ID for this user's kink.
		/// </summary>
		public uint UserKinkID { get; set; }

		/// <summary>
		/// Gets or sets the kink.
		/// </summary>
		public Kink Kink { get; set; }

		/// <summary>
		/// Gets or sets the user's preference for the kink.
		/// </summary>
		public KinkPreference Preference { get; set; }
	}
}
