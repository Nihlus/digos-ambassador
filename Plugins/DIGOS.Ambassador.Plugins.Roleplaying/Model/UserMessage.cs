//
//  UserMessage.cs
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
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using JetBrains.Annotations;
using Remora.Discord.Core;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Roleplaying.Model
{
    /// <summary>
    /// Represents a saved user message.
    /// </summary>
    [Table("UserMessages", Schema = "RoleplayModule")]
    public class UserMessage : EFEntity
    {
        /// <summary>
        /// Gets the unique Discord message ID.
        /// </summary>
        public Snowflake DiscordMessageID { get; private set; }

        /// <summary>
        /// Gets the author of the message.
        /// </summary>
        public virtual User Author { get; private set; } = null!;

        /// <summary>
        /// Gets the timestamp of the message.
        /// </summary>
        public DateTimeOffset Timestamp { get; private set; }

        /// <summary>
        /// Gets the author's nickname at the time of sending.
        /// </summary>
        [Required]
        public string AuthorNickname { get; private set; } = null!;

        /// <summary>
        /// Gets the contents of the message.
        /// </summary>
        [Required]
        public string Contents { get; internal set; } = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMessage"/> class.
        /// </summary>
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized", Justification = "Required by EF Core.")]
        protected UserMessage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMessage"/> class.
        /// </summary>
        /// <param name="author">The author.</param>
        /// <param name="discordMessageID">The Discord ID of the message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        /// <param name="authorNickname">The nickname in use by the author at the time of sending.</param>
        /// <param name="contents">The contents of the message.</param>
        public UserMessage
        (
            User author,
            Snowflake discordMessageID,
            DateTimeOffset timestamp,
            string authorNickname,
            string contents
        )
        {
            this.Author = author;
            this.DiscordMessageID = discordMessageID;
            this.Timestamp = timestamp;
            this.AuthorNickname = authorNickname;
            this.Contents = contents;
        }
    }
}
