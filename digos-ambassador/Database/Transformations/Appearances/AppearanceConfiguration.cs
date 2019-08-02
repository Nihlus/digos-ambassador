//
//  AppearanceConfiguration.cs
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
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Interfaces;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Database.Transformations.Appearances
{
    /// <summary>
    /// Represents the appearance configuration for a character.
    /// </summary>
    [Table("AppearanceConfigurations", Schema = "TransformationModule")]
    public class AppearanceConfiguration : IEFEntity
    {
        /// <inheritdoc />
        public long ID { get; set; }

        /// <summary>
        /// Gets or sets the character that the appearance configuration applies to.
        /// </summary>
        [Required, NotNull]
        public virtual Character Character { get; set; }

        /// <summary>
        /// Gets or sets the saved default appearance of the character.
        /// </summary>
        [Required, NotNull]
        public virtual Appearance DefaultAppearance { get; set; }

        /// <summary>
        /// Gets or sets the current appearance of the character.
        /// </summary>
        [Required, NotNull]
        public virtual Appearance CurrentAppearance { get; set; }
    }
}
