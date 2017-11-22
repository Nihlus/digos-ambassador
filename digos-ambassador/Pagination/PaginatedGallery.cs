//
//  PaginatedGallery.cs
//
//  Author:
//        Jarl Gullberg <jarl.gullberg@gmail.com>
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

// Originally licensed under the ISC license; modified from https://github.com/foxbot/Discord.Addons.Interactive

using System.Collections.Generic;
using Discord;
using Discord.Addons.Interactive;
using Image = DIGOS.Ambassador.Database.Data.Image;

namespace DIGOS.Ambassador.Pagination
{
	/// <summary>
	/// Represents a paginated gallery of images.
	/// </summary>
	public class PaginatedGallery
	{
		/// <summary>
		/// Gets or sets the images in the gallery.
		/// </summary>
		public IEnumerable<Image> Images { get; set; }

		/// <summary>
		/// Gets or sets the colour of the gallery's embed.
		/// </summary>
		public Color Color { get; set; } = Color.Default;

		/// <summary>
		/// Gets or sets the title of the gallery.
		/// </summary>
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Gets the appearance options of the gallery.
		/// </summary>
		public PaginatedAppearanceOptions Options { get; } = PaginatedAppearanceOptions.Default;
	}
}
