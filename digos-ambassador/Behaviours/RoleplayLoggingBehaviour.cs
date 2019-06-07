//
//  RoleplayLoggingBehaviour.cs
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

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Services;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Behaviours
{
    /// <summary>
    /// Acts on user messages, logging them into an active roleplay if relevant.
    /// </summary>
    public class RoleplayLoggingBehaviour : BehaviourBase
    {
        [ProvidesContext]
        private readonly GlobalInfoContext _database;

        private readonly RoleplayService _roleplays;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayLoggingBehaviour"/> class.
        /// </summary>
        /// <param name="client">The discord client.</param>
        /// <param name="roleplays">The roleplay service.</param>
        /// <param name="database">The database.</param>
        public RoleplayLoggingBehaviour
        (
            DiscordSocketClient client,
            RoleplayService roleplays,
            GlobalInfoContext database
        )
            : base(client)
        {
            this._roleplays = roleplays;
            this._database = database;
        }

        /// <inheritdoc />
        protected override Task OnStartingAsync()
        {
            this.Client.MessageReceived += OnMessageReceived;
            this.Client.MessageUpdated += OnMessageUpdated;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task OnStoppingAsync()
        {
            this.Client.MessageReceived -= OnMessageReceived;
            this.Client.MessageUpdated -= OnMessageUpdated;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles incoming messages, passing them to the command context handler.
        /// </summary>
        /// <param name="arg">The message coming in from the socket client.</param>
        /// <returns>A task representing the message handling.</returns>
        private async Task OnMessageReceived(SocketMessage arg)
        {
            if (!(arg is SocketUserMessage message))
            {
                return;
            }

            if (arg.Author.IsBot || arg.Author.IsWebhook)
            {
                return;
            }

            int discard = 0;

            if (message.HasCharPrefix('!', ref discard))
            {
                return;
            }

            if (message.HasMentionPrefix(this.Client.CurrentUser, ref discard))
            {
                return;
            }

            await this._roleplays.ConsumeMessageAsync(this._database, new SocketCommandContext(this.Client, message));
        }

        /// <summary>
        /// Handles reparsing of edited messages.
        /// </summary>
        /// <param name="oldMessage">The old message.</param>
        /// <param name="updatedMessage">The new message.</param>
        /// <param name="messageChannel">The channel of the message.</param>
        private async Task OnMessageUpdated
        (
            Cacheable<IMessage, ulong> oldMessage,
            [CanBeNull] SocketMessage updatedMessage,
            ISocketMessageChannel messageChannel
        )
        {
            if (updatedMessage is null)
            {
                return;
            }

            // Ignore all changes except text changes
            bool isTextUpdate = updatedMessage.EditedTimestamp.HasValue && (updatedMessage.EditedTimestamp.Value > DateTimeOffset.Now - 1.Minutes());
            if (!isTextUpdate)
            {
                return;
            }

            await OnMessageReceived(updatedMessage);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();
            this._database.Dispose();
        }
    }
}
