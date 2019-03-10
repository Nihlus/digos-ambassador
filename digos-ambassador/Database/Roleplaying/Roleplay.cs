//
//  Roleplay.cs
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
using System.Linq;

using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Database.Users;

using Discord;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Database.Roleplaying
{
    /// <summary>
    /// Represents a saved roleplay.
    /// </summary>
    public class Roleplay : IOwnedNamedEntity, IServerEntity
    {
        /// <inheritdoc />
        public long ID { get; set; }

        /// <inheritdoc />
        public long ServerID { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the roleplay is currently active in a channel.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the roleplay can be viewed or replayed by anyone.
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the roleplay is NSFW.
        /// </summary>
        public bool IsNSFW { get; set; }

        /// <summary>
        /// Gets or sets the ID of the channel that the roleplay is active in.
        /// </summary>
        public long ActiveChannelID { get; set; }

        /// <inheritdoc />
        [Required]
        public virtual User Owner { get; set; }

        /// <summary>
        /// Gets or sets the users that are participating in the roleplay in any way.
        /// </summary>
        [NotNull]
        public virtual List<RoleplayParticipant> ParticipatingUsers { get; set; } = new List<RoleplayParticipant>();

        /// <summary>
        /// Gets the users that have been kicked from the roleplay.
        /// </summary>
        [NotNull, NotMapped]
        public IEnumerable<RoleplayParticipant> JoinedUsers =>
            this.ParticipatingUsers.Where(p => p.Status == ParticipantStatus.Joined);

        /// <summary>
        /// Gets the users that have been kicked from the roleplay.
        /// </summary>
        [NotNull, NotMapped]
        public IEnumerable<RoleplayParticipant> KickedUsers =>
                this.ParticipatingUsers.Where(p => p.Status == ParticipantStatus.Kicked);

        /// <summary>
        /// Gets the users that have been invited to the roleplay.
        /// </summary>
        [NotNull, NotMapped]
        public IEnumerable<RoleplayParticipant> InvitedUsers =>
            this.ParticipatingUsers.Where(p => p.Status == ParticipantStatus.Invited);

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        [NotNull]
        public string EntityTypeDisplayName => nameof(Roleplay);

        /// <summary>
        /// Gets or sets the summary of the roleplay.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the saved messages in the roleplay.
        /// </summary>
        [NotNull]
        public virtual List<UserMessage> Messages { get; set; } = new List<UserMessage>();

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

        /// <summary>
        /// Determines whether or not the given user is a participant of the roleplay.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>true if the user is a participant; otherwise, false.</returns>
        [Pure]
        public bool HasJoined([NotNull] User user)
        {
            return HasJoined(user.DiscordID);
        }

        /// <summary>
        /// Determines whether or not the given user is a participant of the roleplay.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>true if the user is a participant; otherwise, false.</returns>
        [Pure]
        public bool HasJoined([NotNull] IUser user)
        {
            return HasJoined((long)user.Id);
        }

        /// <summary>
        /// Determines whether or not the given user ID is a participant of the roleplay.
        /// </summary>
        /// <param name="userID">The ID of the user.</param>
        /// <returns>true if the user is a participant; otherwise, false.</returns>
        [Pure]
        public bool HasJoined(long userID)
        {
            return this.JoinedUsers.Any(p => p.User.DiscordID == userID);
        }

        /// <summary>
        /// Determines whether or not the given user is invited to the roleplay.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>true if the user is invited; otherwise, false.</returns>
        [Pure]
        public bool IsInvited([NotNull] User user)
        {
            return IsInvited(user.DiscordID);
        }

        /// <summary>
        /// Determines whether or not the given user is invited to the roleplay.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>true if the user is invited; otherwise, false.</returns>
        [Pure]
        public bool IsInvited([NotNull] IUser user)
        {
            return IsInvited((long)user.Id);
        }

        /// <summary>
        /// Determines whether or not the given user ID is invited to the roleplay.
        /// </summary>
        /// <param name="userID">The ID of the user.</param>
        /// <returns>true if the user is invited; otherwise, false.</returns>
        [Pure]
        public bool IsInvited(long userID)
        {
            return this.InvitedUsers.Any(iu => iu.User.DiscordID == userID);
        }

        /// <summary>
        /// Determines whether or not the given user is kicked from the roleplay.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>true if the user is kicked; otherwise, false.</returns>
        [Pure]
        public bool IsKicked([NotNull] User user)
        {
            return IsKicked(user.DiscordID);
        }

        /// <summary>
        /// Determines whether or not the given user is kicked from the roleplay.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>true if the user is kicked; otherwise, false.</returns>
        [Pure]
        public bool IsKicked([NotNull] IUser user)
        {
            return IsKicked((long)user.Id);
        }

        /// <summary>
        /// Determines whether or not the given user ID is kicked from the roleplay.
        /// </summary>
        /// <param name="userID">The ID of the user.</param>
        /// <returns>true if the user is kicked; otherwise, false.</returns>
        [Pure]
        public bool IsKicked(long userID)
        {
            return this.KickedUsers.Any(ku => ku.User.DiscordID == userID);
        }
    }
}
