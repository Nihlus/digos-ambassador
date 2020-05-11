//
//  MessageCountInSourceCondition.cs
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

using Discord;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Autorole.Model.Conditions
{
    /// <summary>
    /// Represents a condition where a role would be assigned after a user posts a certain number of messages in a
    /// given source location.
    /// </summary>
    [PublicAPI]
    public class MessageCountInSourceCondition : AutoroleCondition
    {
        /// <summary>
        /// Gets the Discord ID of the message source.
        /// </summary>
        public long SourceID { get; private set; }

        /// <summary>
        /// Gets the type of the message source.
        /// </summary>
        public MessageSourceType SourceType { get; private set; }

        /// <summary>
        /// Gets the required number of messages.
        /// </summary>
        public long RequiredCount { get; private set; }

        private MessageCountInSourceCondition(long sourceID, MessageSourceType sourceType, long requiredCount)
        {
            this.SourceID = sourceID;
            this.SourceType = sourceType;
            this.RequiredCount = requiredCount;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCountInSourceCondition"/> class.
        /// </summary>
        /// <param name="guild">The source guild.</param>
        /// <param name="requiredCount">The required number of messages.</param>
        public MessageCountInSourceCondition(IGuild guild, long requiredCount)
            : this((long)guild.Id, MessageSourceType.Guild, requiredCount)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCountInSourceCondition"/> class.
        /// </summary>
        /// <param name="textChannel">The source guild.</param>
        /// <param name="requiredCount">The required number of messages.</param>
        public MessageCountInSourceCondition(ITextChannel textChannel, long requiredCount)
            : this((long)textChannel.Id, MessageSourceType.Guild, requiredCount)
        {
        }
    }
}
