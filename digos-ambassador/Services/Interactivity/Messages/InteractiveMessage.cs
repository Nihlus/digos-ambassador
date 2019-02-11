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
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Services.Interactivity.Messages
{
    /// <summary>
    /// Acts as a base class for interactive messages.
    /// </summary>
    public abstract class InteractiveMessage : IInteractiveMessage
    {
        /// <inheritdoc/>
        public IUserMessage Message { get; private set; }

        /// <summary>
        /// Gets the context of the message the interactive message wraps.
        /// </summary>
        protected ICommandContext MessageContext { get; private set; }

        /// <summary>
        /// Gets the interactivity service that manages this interactive message.
        /// </summary>
        protected InteractivityService Interactivity { get; private set; }

        /// <inheritdoc/>
        public async Task SendAsync(InteractivityService service, IMessageChannel channel)
        {
            this.Interactivity = service;

            this.Message = await DisplayAsync(channel);
            this.MessageContext = new CommandContext(this.Interactivity.Client, this.Message);

            await UpdateAsync();
        }

        /// <inheritdoc />
        public Task DeleteAsync()
        {
            if (this.Message is null)
            {
                throw new InvalidOperationException("The message hasn't been sent yet.");
            }

            return this.Message.DeleteAsync();
        }

        /// <inheritdoc/>
        public virtual Task HandleAddedInteractionAsync(SocketReaction reaction)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual Task HandleRemovedInteractionAsync(SocketReaction reaction)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates the message contents.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        protected virtual Task UpdateAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Displays the message in the given channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>The displayed message.</returns>
        protected abstract Task<IUserMessage> DisplayAsync(IMessageChannel channel);
    }
}
