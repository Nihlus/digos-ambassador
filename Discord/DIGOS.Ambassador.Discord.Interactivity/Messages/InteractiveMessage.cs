//
//  InteractiveMessage.cs
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
using System.Threading;
using Remora.Discord.Core;

namespace DIGOS.Ambassador.Discord.Interactivity.Messages
{
    /// <summary>
    /// Acts as a base class for interactive messages.
    /// </summary>
    public abstract class InteractiveMessage : IInteractiveMessage
    {
        /// <inheritdoc />
        public Snowflake ChannelID { get; }

        /// <inheritdoc />
        public Snowflake MessageID { get; }

        /// <inheritdoc />
        public SemaphoreSlim Semaphore { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractiveMessage"/> class.
        /// </summary>
        /// <param name="channelID">The ID of the channel the message is in.</param>
        /// <param name="messageID">The ID of the message.</param>
        protected InteractiveMessage(Snowflake channelID, Snowflake messageID)
        {
            this.ChannelID = channelID;
            this.MessageID = messageID;
            this.Semaphore = new SemaphoreSlim(1);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Semaphore.Dispose();
        }
    }
}
