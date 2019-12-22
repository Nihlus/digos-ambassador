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
using System.ComponentModel.DataAnnotations.Schema;
using DIGOS.Ambassador.Core.Database.Entities;
using Discord;
using JetBrains.Annotations;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Roleplaying.Model
{
    /// <summary>
    /// Represents a saved user message.
    /// </summary>
    [PublicAPI]
    [Table("UserMessages", Schema = "RoleplayModule")]
    public class UserMessage : EFEntity
    {
        /// <summary>
        /// Gets the unique Discord message ID.
        /// </summary>
        public long DiscordMessageID { get; private set; }

        /// <summary>
        /// Gets the author of the message.
        /// </summary>
        public long AuthorDiscordID { get; private set; }

        /// <summary>
        /// Gets the timestamp of the message.
        /// </summary>
        public DateTimeOffset Timestamp { get; private set; }

        /// <summary>
        /// Gets the author's nickname at the time of sending.
        /// </summary>
        public string AuthorNickname { get; private set; } = null!;

        /// <summary>
        /// Gets the contents of the message.
        /// </summary>
        public string Contents { get; internal set; } = null!;

        /// <summary>
        /// Creates a new <see cref="UserMessage"/> from the specified Discord message.
        /// </summary>
        /// <param name="message">The message to create from.</param>
        /// <param name="authorNickname">The current display name of the author.</param>
        /// <returns>A new UserMessage.</returns>
        [NotNull]
        [Pure]
        public static UserMessage FromDiscordMessage
        (
            [NotNull] IMessage message,
            [NotNull] string authorNickname
        )
        {
            return new UserMessage
            {
                DiscordMessageID = (long)message.Id,
                AuthorDiscordID = (long)message.Author.Id,
                Timestamp = message.Timestamp,
                AuthorNickname = authorNickname,
                Contents = message.Content
            };
        }
    }
}
