//
//  UserBan.cs
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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Moderation.Model.Bases;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Moderation.Model
{
    /// <summary>
    /// Represents a ban of a user.
    /// </summary>
    [PublicAPI]
    [Table("UserBans", Schema = "ModerationModule")]
    public class UserBan : AuthoredUserEntity
    {
        /// <summary>
        /// Gets the reason for the ban.
        /// </summary>
        [NotNull, Required]
        public string Reason { get; internal set; }

        /// <summary>
        /// Gets the message that caused the warning, if any.
        /// </summary>
        [CanBeNull]
        public long? MessageID { get; internal set; }

        /// <summary>
        /// Gets the time at which the ban was last updated.
        /// </summary>
        [Required]
        public DateTime UpdatedAt { get; internal set; }

        /// <summary>
        /// Gets the time at which the ban expires.
        /// </summary>
        [CanBeNull]
        public DateTime? ExpiresOn { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the ban is temporary.
        /// </summary>
        public bool IsTemporary => this.ExpiresOn.HasValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserBan"/> class.
        /// </summary>
        /// <remarks>
        /// Required by EF Core.
        /// </remarks>
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized", Justification = "Initialized by EF Core.")]
        protected UserBan()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserBan"/> class.
        /// </summary>
        /// <param name="server">The server that the ban was created on.</param>
        /// <param name="user">The user that the ban is attached to.</param>
        /// <param name="author">The user that created the ban.</param>
        /// <param name="reason">The reason for the ban.</param>
        /// <param name="messageID">The message that caused the warning, if any.</param>
        /// <param name="expiresOn">The time at which the ban expires.</param>
        public UserBan
        (
            [NotNull] Server server,
            [NotNull] User user,
            [NotNull] User author,
            [NotNull] string reason,
            [CanBeNull] long? messageID = null,
            [CanBeNull] DateTime? expiresOn = null
        )
            : base(server, user, author)
        {
            this.Reason = reason;
            this.MessageID = messageID;

            this.UpdatedAt = DateTime.UtcNow;
            this.ExpiresOn = expiresOn;
        }

        /// <summary>
        /// Notifies the entity that it has been updated, updating its timestamp.
        /// </summary>
        public void NotifyUpdate()
        {
            this.UpdatedAt = DateTime.UtcNow;
        }
    }
}
