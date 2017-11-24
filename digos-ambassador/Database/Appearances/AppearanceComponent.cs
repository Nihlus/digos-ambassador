//
//  AppearanceComponent.cs
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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Services;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Database.Appearances
{
	/// <summary>
	/// Represents a distinct part of a character's appearance.
	/// </summary>
	public class AppearanceComponent : IEFEntity
	{
		/// <inheritdoc />
		public uint ID { get; set; }

		/// <summary>
		/// Gets the bodypart that the component is.
		/// </summary>
		[NotMapped]
		public Bodypart Bodypart => this.Transformation.Part;

		/// <summary>
		/// Gets or sets the component's current transformation.
		/// </summary>
		public Transformation Transformation { get; set; }

		/// <summary>
		/// Gets or sets the base colour of the component.
		/// </summary>
		public string BaseColour { get; set; }

		/// <summary>
		/// Gets or sets the pattern of the component's secondary colour (if any)
		/// </summary>
		public string Pattern { get; set; }

		/// <summary>
		/// Gets or sets the component's pattern colour.
		/// </summary>
		public string PatternColour { get; set; }

		/// <summary>
		/// Gets or sets the size of the component. This is, by default, a unitless value and is only contextually relevant.
		/// </summary>
		public int Size { get; set; }

		[Pure]
		public static AppearanceComponent CreateFrom(Transformation transformation)
		{
			throw new System.NotImplementedException();
		}
	}
}
