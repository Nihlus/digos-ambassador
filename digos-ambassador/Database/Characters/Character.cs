//
//  Character.cs
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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Database.Users;
using Discord;
using JetBrains.Annotations;
using Image = DIGOS.Ambassador.Database.Characters.Data.Image;

namespace DIGOS.Ambassador.Database.Characters
{
    /// <summary>
    /// Represents a user's character.
    /// </summary>
    [Table("Characters", Schema = "CharacterModule")]
    public class Character : IOwnedNamedEntity, IServerEntity
    {
        /// <inheritdoc />
        public long ID { get; set; }

        /// <inheritdoc />
        public long ServerID { get; set; }

        /// <inheritdoc />
        [Required]
        public virtual User Owner { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        [NotNull]
        public string EntityTypeDisplayName => nameof(Character);

        /// <summary>
        /// Gets or sets a value indicating whether the character is currently in use on the server.
        /// </summary>
        public bool IsCurrent { get; set; }

        /// <summary>
        /// Gets or sets a URL pointing to the character's avatar.
        /// </summary>
        public string AvatarUrl { get; set; }

        /// <summary>
        /// Gets or sets the nickname that a user should have when playing as the character.
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// Gets or sets the character summary.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the full description of the character.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the character is NSFW.
        /// </summary>
        public bool IsNSFW { get; set; }

        /// <summary>
        /// Gets or sets the images associated with the character.
        /// </summary>
        [NotNull]
        public virtual List<Image> Images { get; set; } = new List<Image>();

        /// <summary>
        /// Gets or sets the preferred pronoun family of the character.
        /// </summary>
        public string PronounProviderFamily { get; set; }

        /// <summary>
        /// Gets or sets a custom role that gets applied along with the character, similar to a nickname.
        /// </summary>
        [CanBeNull]
        public virtual CharacterRole Role { get; set; }

        /// <inheritdoc />
        public bool IsOwner(User user)
        {
            return IsOwner(user.DiscordID);
        }

        /// <inheritdoc />
        public bool IsOwner(IUser user)
        {
            return IsOwner((long)user.Id);
        }

        /// <inheritdoc />
        public bool IsOwner(long userID)
        {
            return this.Owner.DiscordID == userID;
        }
    }
}
