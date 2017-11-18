//
//  Appearance.cs
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

namespace DIGOS.Ambassador.Database.Appearances
{
	/// <summary>
	/// Represents the physical appearance of a character.
	/// </summary>
	public class Appearance
	{
		/// <summary>
		/// Gets or sets the unique ID of this appearance.
		/// </summary>
		public uint AppearanceID { get; set; }

		/// <summary>
		/// Gets or sets the current appearance of a character's surface (that is, skin, scales, fur, etc).
		/// </summary>
		public AppearanceComponent<SurfaceType> Surface { get; set; }

		/// <summary>
		/// Gets or sets the current appearance of a character's hair.
		/// </summary>
		public AppearanceComponent<string> Hair { get; set; }

		/// <summary>
		/// Gets or sets the current appearance of a character's head.
		/// </summary>
		public AppearanceComponent<string> Head { get; set; }

		/// <summary>
		/// Gets or sets the current appearance of a character's eyes.
		/// </summary>
		public AppearanceComponent<string> Eyes { get; set; }

		/// <summary>
		/// Gets or sets the current appearance of a character's body.
		/// </summary>
		public AppearanceComponent<string> Body { get; set; }

		/// <summary>
		/// Gets or sets the current appearance of a character's genitalia.
		/// </summary>
		public AppearanceComponent<GenitaliaType> Genitalia { get; set; }

		/// <summary>
		/// Gets or sets the current appearance of a character's tail.
		/// </summary>
		public AppearanceComponent<string> Tail { get; set; }

		/// <summary>
		/// Gets or sets the current appearance of a character's arms.
		/// </summary>
		public AppearanceComponent<string> Arms { get; set; }

		/// <summary>
		/// Gets or sets the current appearance of a character's legs.
		/// </summary>
		public AppearanceComponent<string> Legs { get; set; }

		/// <summary>
		/// Gets or sets a character's height (in meters).
		/// </summary>
		public float Height { get; set; }

		/// <summary>
		/// Gets or sets a character's weight (in kilograms).
		/// </summary>
		public float Weight { get; set; }

		/// <summary>
		/// Gets or sets how feminine or masculine a character appears to be, on a -1 to 1 scale.
		/// </summary>
		public float GenderScale { get; set; }

		/// <summary>
		/// Gets or sets how muscular a character appears to be, on a 0 to 1 scale.
		/// </summary>
		public float Muscularity { get; set; }
	}
}
