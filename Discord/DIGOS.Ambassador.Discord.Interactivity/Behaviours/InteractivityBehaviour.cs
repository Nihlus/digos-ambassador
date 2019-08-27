//
//  InteractivityBehaviour.cs
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
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Behaviours;
using Discord;
using Discord.WebSocket;

namespace DIGOS.Ambassador.Discord.Interactivity.Behaviours
{
    /// <summary>
    /// Represents a behaviour that continuously monitors and responds to interactions.
    /// </summary>
    public class InteractivityBehaviour : ContinuousBehaviour
    {
        private readonly InteractivityService _interactivity;

        /// <summary>
        /// Gets the events that are currently running.
        /// </summary>
        private ConcurrentQueue<Task> RunningEventInteractions { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractivityBehaviour"/> class.
        /// </summary>
        /// <param name="client">The Discord client.</param>
        /// <param name="interactivity">The interactivity service.</param>
        public InteractivityBehaviour(DiscordSocketClient client, InteractivityService interactivity)
            : base(client)
        {
            _interactivity = interactivity;

            this.RunningEventInteractions = new ConcurrentQueue<Task>();
        }

        private Task OnReactionRemoved
        (
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction
        )
        {
            this.RunningEventInteractions.Enqueue(_interactivity.OnReactionRemoved(message, reaction));
            return Task.CompletedTask;
        }

        private Task OnReactionAdded
        (
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction
        )
        {
            this.RunningEventInteractions.Enqueue(_interactivity.OnReactionAdded(message, reaction));
            return Task.CompletedTask;
        }

        private Task OnMessageDeleted
        (
            Cacheable<IMessage, ulong> message,
            ISocketMessageChannel channel
        )
        {
            this.RunningEventInteractions.Enqueue(_interactivity.OnMessageDeleted(message));
            return Task.CompletedTask;
        }

        private Task OnMessageReceived(SocketMessage message)
        {
            this.RunningEventInteractions.Enqueue(_interactivity.OnMessageReceived(message));
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task OnStartingAsync()
        {
            this.Client.MessageReceived += OnMessageReceived;
            this.Client.MessageDeleted += OnMessageDeleted;
            this.Client.ReactionAdded += OnReactionAdded;
            this.Client.ReactionRemoved += OnReactionRemoved;

            return base.OnStartingAsync();
        }

        /// <inheritdoc />
        protected override Task OnStoppingAsync()
        {
            this.Client.MessageReceived -= OnMessageReceived;
            this.Client.MessageDeleted -= OnMessageDeleted;
            this.Client.ReactionAdded -= OnReactionAdded;
            this.Client.ReactionRemoved -= OnReactionRemoved;

            return base.OnStoppingAsync();
        }

        /// <inheritdoc />
        protected override async Task OnTickAsync(CancellationToken ct)
        {
            if (this.RunningEventInteractions.TryDequeue(out var interaction))
            {
                if (interaction.IsCompleted)
                {
                    try
                    {
                        await interaction;
                    }
                    catch (Exception e)
                    {
                        // Nom nom nom
                        this.Log.Error("Error in interaction.", e);
                    }
                }
                else
                {
                    this.RunningEventInteractions.Enqueue(interaction);
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), ct);
        }
    }
}
