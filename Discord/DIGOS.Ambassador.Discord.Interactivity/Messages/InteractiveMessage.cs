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

namespace DIGOS.Ambassador.Discord.Interactivity.Messages
{
    /// <summary>
    /// Acts as a base class for interactive messages.
    /// </summary>
    public abstract class InteractiveMessage : IInteractiveMessage
    {
        /// <inheritdoc/>
        public IUserMessage? Message { get; private set; }

        /// <inheritdoc />
        public IUser SourceUser { get; }

        /// <summary>
        /// Gets the channel the message is in.
        /// </summary>
        protected IMessageChannel? Channel { get; private set; }

        /// <summary>
        /// Gets the context of the message the interactive message wraps.
        /// </summary>
        protected ICommandContext MessageContext => new CommandContext(this.Interactivity.Client, this.Message);

        /// <summary>
        /// Gets the interactivity service that manages this interactive message.
        /// </summary>
        protected InteractivityService Interactivity { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the message is in the process of getting deleted.
        /// </summary>
        private bool IsDeleting { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractiveMessage"/> class.
        /// </summary>
        /// <param name="sourceUser">The source user.</param>
        /// <param name="interactivityService">The interactivity service.</param>
        protected InteractiveMessage(IUser sourceUser, InteractivityService interactivityService)
        {
            this.Interactivity = interactivityService;
            this.SourceUser = sourceUser;
        }

        /// <inheritdoc/>
        public async Task SendAsync(InteractivityService service, IMessageChannel channel)
        {
            this.Channel = channel;
            this.Message = await DisplayAsync(channel);

            await UpdateAsync();
        }

        /// <inheritdoc />
        public Task DeleteAsync()
        {
            if (this.Message is null)
            {
                throw new InvalidOperationException("The message hasn't been sent yet.");
            }

            this.IsDeleting = true;

            return this.Message.DeleteAsync();
        }

        /// <inheritdoc/>
        public async Task HandleAddedInteractionAsync(SocketReaction reaction)
        {
            if (this.IsDeleting)
            {
                return;
            }

            await OnInteractionAddedAsync(reaction);
        }

        /// <summary>
        /// Raised when a valid interaction is added.
        /// </summary>
        /// <param name="reaction">The reaction.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task OnInteractionAddedAsync(SocketReaction reaction)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task HandleRemovedInteractionAsync(SocketReaction reaction)
        {
            if (this.IsDeleting)
            {
                return;
            }

            await OnInteractionRemovedAsync(reaction);
        }

        /// <summary>
        /// Raised when a valid interaction is removed.
        /// </summary>
        /// <param name="reaction">The reaction.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task OnInteractionRemovedAsync(SocketReaction reaction)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates the message contents.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task UpdateAsync()
        {
            if (this.IsDeleting)
            {
                return;
            }

            await OnUpdateAsync();
        }

        /// <summary>
        /// Updates the message contents.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task OnUpdateAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Displays the message in the given channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>The displayed message.</returns>
        private async Task<IUserMessage?> DisplayAsync(IMessageChannel channel)
        {
            if (this.IsDeleting)
            {
                return null;
            }

            return await OnDisplayAsync(channel);
        }

        /// <summary>
        /// Displays the message in the given channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>The displayed message.</returns>
        protected abstract Task<IUserMessage> OnDisplayAsync(IMessageChannel channel);
    }
}
