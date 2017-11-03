//
//  Kink.cs
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

namespace DIGOS.Ambassador.Database.UserInfo
{
	/// <summary>
	/// Represents a sexual kink or fetish.
	/// </summary>
	public class Kink
	{
		/// <summary>
		/// Gets or sets the unique ID of the kink.
		/// </summary>
		public uint KinkID { get; set; }

		/// <summary>
		/// Gets or sets the category the kink belongs to.
		/// </summary>
		public KinkCategory Category { get; set; }

		/// <summary>
		/// Gets or sets the F-List ID of the kink.
		/// </summary>
		public uint FListID { get; set; }

		/// <summary>
		/// Gets or sets the name of the kink.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the full description of the kink.
		/// </summary>
		public string Description { get; set; }
	}
}
