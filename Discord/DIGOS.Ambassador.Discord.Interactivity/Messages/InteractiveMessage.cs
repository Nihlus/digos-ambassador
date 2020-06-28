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
using Remora.Results;

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
        public async Task<OperationResult> SendAsync(InteractivityService service, IMessageChannel channel)
        {
            this.Channel = channel;
            var displayResult = await DisplayAsync(channel);
            if (!displayResult.IsSuccess)
            {
                return OperationResult.FromError(displayResult);
            }

            this.Message = displayResult.Entity;

            return await UpdateAsync();
        }

        /// <inheritdoc />
        public async Task<OperationResult> DeleteAsync()
        {
            if (this.Message is null)
            {
                return OperationResult.FromError("The message hasn't been sent yet.");
            }

            this.IsDeleting = true;

            await this.Message.DeleteAsync();
            return OperationResult.FromSuccess();
        }

        /// <inheritdoc/>
        public async Task<OperationResult> HandleAddedInteractionAsync(SocketReaction reaction)
        {
            if (this.IsDeleting)
            {
                return OperationResult.FromError("The message is being deleted.");
            }

            return await OnInteractionAddedAsync(reaction);
        }

        /// <summary>
        /// Raised when a valid interaction is added.
        /// </summary>
        /// <param name="reaction">The reaction.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task<OperationResult> OnInteractionAddedAsync(SocketReaction reaction)
        {
            return Task.FromResult(OperationResult.FromSuccess());
        }

        /// <inheritdoc/>
        public async Task<OperationResult> HandleRemovedInteractionAsync(SocketReaction reaction)
        {
            if (this.IsDeleting)
            {
                return OperationResult.FromError("The message is being deleted.");
            }

            return await OnInteractionRemovedAsync(reaction);
        }

        /// <summary>
        /// Raised when a valid interaction is removed.
        /// </summary>
        /// <param name="reaction">The reaction.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task<OperationResult> OnInteractionRemovedAsync(SocketReaction reaction)
        {
            return Task.FromResult(OperationResult.FromSuccess());
        }

        /// <summary>
        /// Updates the message contents.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task<OperationResult> UpdateAsync()
        {
            if (this.IsDeleting)
            {
                return OperationResult.FromError("The message is being deleted.");
            }

            return await OnUpdateAsync();
        }

        /// <summary>
        /// Updates the message contents.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task<OperationResult> OnUpdateAsync()
        {
            return Task.FromResult(OperationResult.FromSuccess());
        }

        /// <summary>
        /// Displays the message in the given channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>The displayed message.</returns>
        private async Task<CreateEntityResult<IUserMessage>> DisplayAsync(IMessageChannel channel)
        {
            if (this.IsDeleting)
            {
                return CreateEntityResult<IUserMessage>.FromError("The message is being deleted.");
            }

            return await OnDisplayAsync(channel);
        }

        /// <summary>
        /// Displays the message in the given channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>The displayed message.</returns>
        protected abstract Task<CreateEntityResult<IUserMessage>> OnDisplayAsync(IMessageChannel channel);
    }
}
