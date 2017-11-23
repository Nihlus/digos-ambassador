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

using System.Collections.Generic;
using DIGOS.Ambassador.Database.Interfaces;

namespace DIGOS.Ambassador.Database.Appearances
{
	/// <summary>
	/// Represents the physical appearance of a character.
	/// </summary>
	public class Appearance : IEFEntity
	{
		/// <inheritdoc />
		public uint ID { get; set; }

		/// <summary>
		/// Gets or sets the parts that compose this appearance.
		/// </summary>
		public List<AppearanceComponent> Components { get; set; }

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
