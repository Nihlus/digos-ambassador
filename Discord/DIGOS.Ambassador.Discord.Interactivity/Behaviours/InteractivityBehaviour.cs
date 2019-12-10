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

using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Behaviours;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DIGOS.Ambassador.Discord.Interactivity.Behaviours
{
    /// <summary>
    /// Represents a behaviour that continuously monitors and responds to interactions.
    /// </summary>
    public class InteractivityBehaviour : ClientEventBehaviour<InteractivityBehaviour>
    {
        private readonly InteractivityService _interactivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractivityBehaviour"/> class.
        /// </summary>
        /// <param name="client">The Discord client.</param>
        /// <param name="serviceScope">The service scope in use.</param>
        /// <param name="logger">The logging instance for this type.</param>
        /// <param name="interactivity">The interactivity service.</param>
        public InteractivityBehaviour
        (
            DiscordSocketClient client,
            [NotNull] IServiceScope serviceScope,
            [NotNull] ILogger<InteractivityBehaviour> logger,
            InteractivityService interactivity
        )
            : base(client, serviceScope, logger)
        {
            _interactivity = interactivity;
        }

        /// <inheritdoc />
        protected override async Task ReactionRemoved
        (
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction
        )
        {
            await _interactivity.OnReactionRemoved(message, reaction);
        }

        /// <inheritdoc />
        protected override async Task ReactionAdded
        (
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction
        )
        {
            await _interactivity.OnReactionAdded(message, reaction);
        }

        /// <inheritdoc />
        protected override async Task MessageDeleted
        (
            Cacheable<IMessage, ulong> message,
            ISocketMessageChannel channel
        )
        {
            await _interactivity.OnMessageDeleted(message);
        }

        /// <inheritdoc />
        protected override async Task MessageReceived(SocketMessage message)
        {
            await _interactivity.OnMessageReceived(message);
        }
    }
}
