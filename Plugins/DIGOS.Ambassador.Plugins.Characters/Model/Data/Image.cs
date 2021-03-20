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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DIGOS.Ambassador.Core.Database.Entities;

namespace DIGOS.Ambassador.Plugins.Characters.Model.Data
{
    /// <summary>
    /// Represents an image.
    /// </summary>
    [Table("Images", Schema = "CharacterModule")]
    public class Image : EFEntity
    {
        /// <summary>
        /// Gets the name of the image.
        /// </summary>
        [Required]
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the caption of the image.
        /// </summary>
        [Required]
        public string Caption { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether or not the image is NSFW.
        /// </summary>
        public bool IsNSFW { get; internal set; }

        /// <summary>
        /// Gets the online URL of the image.
        /// </summary>
        [Required]
        public string Url { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="name">The name of the image.</param>
        /// <param name="url">The URL to the image.</param>
        /// <param name="caption">The caption of the image.</param>
        public Image(string name, string url, string caption = "No caption set.")
        {
            this.Name = name;
            this.Url = url;
            this.Caption = caption;
        }
    }
}
