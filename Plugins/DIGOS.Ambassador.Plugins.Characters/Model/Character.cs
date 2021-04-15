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
using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using Remora.Discord.Core;
using Image = DIGOS.Ambassador.Plugins.Characters.Model.Data.Image;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Characters.Model
{
    /// <summary>
    /// Represents a user's character.
    /// </summary>
    [Table("Characters", Schema = "CharacterModule")]
    public class Character : EFEntity, IOwnedNamedEntity, IServerEntity
    {
        /// <inheritdoc />
        public virtual Server Server { get; private set; } = null!;

        /// <inheritdoc />
        [Required]
        public virtual User Owner { get; set; } = null!;

        /// <inheritdoc />
        [Required]
        public string Name { get; internal set; } = null!;

        /// <summary>
        /// Gets a value indicating whether the character is the user's default character.
        /// </summary>
        public bool IsDefault { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the character is currently in use on the server.
        /// </summary>
        public bool IsCurrent { get; internal set; }

        /// <summary>
        /// Gets a URL pointing to the character's avatar.
        /// </summary>
        [Required]
        public string AvatarUrl { get; internal set; } = null!;

        /// <summary>
        /// Gets the nickname that a user should have when playing as the character.
        /// </summary>
        [Required]
        public string Nickname { get; internal set; } = null!;

        /// <summary>
        /// Gets the character summary.
        /// </summary>
        [Required]
        public string Summary { get; internal set; } = null!;

        /// <summary>
        /// Gets the full description of the character.
        /// </summary>
        [Required]
        public string Description { get; internal set; } = null!;

        /// <summary>
        /// Gets a value indicating whether the character is NSFW.
        /// </summary>
        public bool IsNSFW { get; internal set; }

        /// <summary>
        /// Gets the images associated with the character.
        /// </summary>
        public virtual List<Image> Images { get; internal set; } = new();

        /// <summary>
        /// Gets the preferred pronoun family of the character.
        /// </summary>
        [Required]
        public string PronounProviderFamily { get; internal set; } = null!;

        /// <summary>
        /// Gets a custom role that gets applied along with the character, similar to a nickname.
        /// </summary>
        public virtual CharacterRole? Role { get; internal set; }

        /// <inheritdoc />
        [NotMapped]
        public string EntityTypeDisplayName => nameof(Character);

        /// <summary>
        /// Gets a value indicating whether the character has a role.
        /// </summary>
        [NotMapped]
        public bool HasRole => this.Role is not null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Character"/> class.
        /// </summary>
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized", Justification = "Initialized by EF Core.")]
        protected Character()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Character"/> class.
        /// </summary>
        /// <param name="server">The server that the character resides on.</param>
        /// <param name="owner">The owner of the character.</param>
        /// <param name="name">The character's name.</param>
        /// <param name="avatarUrl">The avatar URL for the character.</param>
        /// <param name="nickname">The character's nickname. Defaults to the character's name.</param>
        /// <param name="summary">The character's summary.</param>
        /// <param name="description">The character's description.</param>
        /// <param name="pronounProviderFamily">The character's pronoun provider family.</param>
        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor", Justification = "Required by EF Core.")]
        public Character
        (
            User owner,
            Server server,
            string name,
            string avatarUrl,
            string nickname,
            string summary,
            string description,
            string pronounProviderFamily
        )
        {
            this.Server = server;
            this.Owner = owner;
            this.Name = name;
            this.AvatarUrl = avatarUrl;
            this.Summary = summary;
            this.Description = description;
            this.PronounProviderFamily = pronounProviderFamily;
            this.Nickname = nickname;
        }

        /// <inheritdoc />
        public bool IsOwner(User user)
        {
            return IsOwner(user.DiscordID);
        }

        /// <inheritdoc />
        public bool IsOwner(Snowflake userID)
        {
            return this.Owner.DiscordID == userID;
        }
    }
}
