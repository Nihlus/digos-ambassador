//
//  IInteractiveMessage.cs
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

using Remora.Discord.Core;

namespace DIGOS.Ambassador.Discord.Interactivity.Messages
{
    /// <summary>
    /// Represents the public interface of an interactive message.
    /// </summary>
    public interface IInteractiveMessage : IInteractiveEntity
    {
        /// <inheritdoc/>
        string IInteractiveEntity.Nonce => this.MessageID.ToString();

        /// <summary>
        /// Gets the ID of the channel the message is in.
        /// </summary>
        Snowflake ChannelID { get; }

        /// <summary>
        /// Gets the ID of the message.
        /// </summary>
        Snowflake MessageID { get; }
    }
}
