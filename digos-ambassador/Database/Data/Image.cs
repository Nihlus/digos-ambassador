//
//  Image.cs
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

using DIGOS.Ambassador.Database.Interfaces;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Database.Data
{
    /// <summary>
    /// Represents an image.
    /// </summary>
    public class Image : IEFEntity
    {
        /// <inheritdoc />
        public long ID { get; set; }

        /// <summary>
        /// Gets or sets the name of the image.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the caption of the image.
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the image is NSFW.
        /// </summary>
        public bool IsNSFW { get; set; }

        /// <summary>
        /// Gets or sets the online URL of the image.
        /// </summary>
        [CanBeNull]
        public string Url { get; set; }
    }
}
