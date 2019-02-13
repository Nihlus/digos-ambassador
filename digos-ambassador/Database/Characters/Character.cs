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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DIGOS.Ambassador.Database.Appearances;
using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Database.Users;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Transformations;

using Discord;
using JetBrains.Annotations;
using Image = DIGOS.Ambassador.Database.Data.Image;

namespace DIGOS.Ambassador.Database.Characters
{
    /// <summary>
    /// Represents a user's character.
    /// </summary>
    public class Character : IOwnedNamedEntity, IServerEntity
    {
        /// <inheritdoc />
        public long ID { get; set; }

        /// <inheritdoc />
        public long ServerID { get; set; }

        /// <inheritdoc />
        [Required]
        public User Owner { get; set; }

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
        public List<Image> Images { get; set; }

        /// <summary>
        /// Gets or sets the character's default appearance.
        /// </summary>
        public Appearance DefaultAppearance { get; set; }

        /// <summary>
        /// Gets or sets the character's transformed appearance.
        /// </summary>
        public Appearance CurrentAppearance { get; set; }

        /// <summary>
        /// Gets or sets the preferred pronoun family of the character.
        /// </summary>
        public string PronounProviderFamily { get; set; }

        /// <summary>
        /// Gets or sets a custom role that gets applied along with the character, similar to a nickname.
        /// </summary>
        [CanBeNull]
        public CharacterRole Role { get; set; }

        /// <summary>
        /// Determines whether or not the character has a given bodypart in their current appearance.
        /// </summary>
        /// <param name="bodypart">The bodypart to check for.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <returns>true if the character has the bodypart; otherwise, false.</returns>
        [Pure]
        public bool HasComponent(Bodypart bodypart, Chirality chirality)
        {
            if (bodypart.IsChiral() && chirality == Chirality.Center)
            {
                throw new ArgumentException("A chiral bodypart must have its chirality specified.", nameof(bodypart));
            }

            if (!bodypart.IsChiral() && chirality != Chirality.Center)
            {
                throw new ArgumentException("A nonchiral transformation cannot have chirality.", nameof(bodypart));
            }

            if (bodypart.IsComposite())
            {
                throw new ArgumentException("The bodypart must not be a composite part.");
            }

            return this.CurrentAppearance.Components.Any(c => c.Bodypart == bodypart && c.Chirality == chirality);
        }

        /// <summary>
        /// Gets the component on the character's current appearance that matches the given bodypart.
        /// </summary>
        /// <param name="bodypart">The bodypart to get.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <returns>The appearance component of the bodypart.</returns>
        [NotNull]
        public AppearanceComponent GetAppearanceComponent(Bodypart bodypart, Chirality chirality)
        {
            if (bodypart.IsChiral() && chirality == Chirality.Center)
            {
                throw new ArgumentException("A chiral bodypart must have its chirality specified.", nameof(bodypart));
            }

            if (!bodypart.IsChiral() && chirality != Chirality.Center)
            {
                throw new ArgumentException("A nonchiral transformation cannot have chirality.", nameof(bodypart));
            }

            if (bodypart.IsComposite())
            {
                throw new ArgumentException("The bodypart must not be a composite part.");
            }

            return this.CurrentAppearance.Components.First(c => c.Bodypart == bodypart && c.Chirality == chirality);
        }

        /// <summary>
        /// Tries to retrieve the component on the character's current appearance that matches the given bodypart.
        /// </summary>
        /// <param name="bodypart">The bodypart to get.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <param name="component">The component, or null.</param>
        /// <returns>True if a component could be retrieved, otherwise, false.</returns>
        [ContractAnnotation("=> true, component:notnull; => false, component:null")]
        public bool TryGetAppearanceComponent(Bodypart bodypart, Chirality chirality, [CanBeNull] out AppearanceComponent component)
        {
            component = null;

            if (!HasComponent(bodypart, chirality))
            {
                return false;
            }

            component = GetAppearanceComponent(bodypart, chirality);
            return true;
        }

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
