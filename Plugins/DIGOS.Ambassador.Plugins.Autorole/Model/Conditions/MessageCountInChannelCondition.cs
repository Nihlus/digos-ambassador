//
//  MessageCountInChannelCondition.cs
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

using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.Bases;
using Discord;

namespace DIGOS.Ambassador.Plugins.Autorole.Model.Conditions
{
    /// <summary>
    /// Represents a condition that requires a certain number of messages in a specific channel.
    /// </summary>
    public class MessageCountInChannelCondition : MessageCountInSourceCondition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCountInChannelCondition"/> class.
        /// </summary>
        /// <param name="sourceID">The source ID.</param>
        /// <param name="requiredCount">The required message count.</param>
        private MessageCountInChannelCondition(long sourceID, long requiredCount)
            : base(sourceID, requiredCount)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCountInChannelCondition"/> class.
        /// </summary>
        /// <param name="textChannel">The source guild.</param>
        /// <param name="requiredCount">The required number of messages.</param>
        public MessageCountInChannelCondition(ITextChannel textChannel, long requiredCount)
            : this((long)textChannel.Id, requiredCount)
        {
        }
    }
}
