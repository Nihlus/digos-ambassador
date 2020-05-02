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

using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DIGOS.Ambassador.Discord.Interactivity.Messages
{
    /// <summary>
    /// Represents the public interface of an interactive message.
    /// </summary>
    public interface IInteractiveMessage
    {
        /// <summary>
        /// Gets the message that the interactive message wraps.
        /// </summary>
        IUserMessage? Message { get; }

        /// <summary>
        /// Gets the user that caused the interactive message to be created.
        /// </summary>
        IUser SourceUser { get; }

        /// <summary>
        /// Sends the interactive message to the given channel.
        /// </summary>
        /// <param name="service">The interactivity service that manages this message.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SendAsync(InteractivityService service, IMessageChannel channel);

        /// <summary>
        /// Deletes the interactive message.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeleteAsync();

        /// <summary>
        /// Handles an added interaction, performing tasks as needed.
        /// </summary>
        /// <param name="reaction">The added interaction.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task HandleAddedInteractionAsync(SocketReaction reaction);

        /// <summary>
        /// Handles a removed interaction, performing tasks as needed.
        /// </summary>
        /// <param name="reaction">The removed interaction.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task HandleRemovedInteractionAsync(SocketReaction reaction);
    }
}
