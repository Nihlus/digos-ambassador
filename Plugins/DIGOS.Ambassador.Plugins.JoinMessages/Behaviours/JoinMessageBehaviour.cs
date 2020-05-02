//
//  JoinMessageBehaviour.cs
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
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Discord.Behaviours;

namespace DIGOS.Ambassador.Plugins.JoinMessages.Behaviours
{
    /// <summary>
    /// Acts on user joins, sending them the server's join message.
    /// </summary>
    public class JoinMessageBehaviour : ClientEventBehaviour<JoinMessageBehaviour>
    {
        private readonly UserFeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinMessageBehaviour"/> class.
        /// </summary>
        /// <param name="client">The discord client.</param>
        /// <param name="serviceScope">The service scope in use.</param>
        /// <param name="logger">The logging instance for this type.</param>
        /// <param name="feedback">The feedback service.</param>
        public JoinMessageBehaviour
        (
            DiscordSocketClient client,
            IServiceScope serviceScope,
            ILogger<JoinMessageBehaviour> logger,
            UserFeedbackService feedback
        )
            : base(client, serviceScope, logger)
        {
            _feedback = feedback;
        }

        /// <inheritdoc />
        protected override async Task UserJoined(SocketGuildUser user)
        {
            using var scope = this.Services.CreateScope();
            var serverService = scope.ServiceProvider.GetRequiredService<ServerService>();

            var getServerResult = await serverService.GetOrRegisterServerAsync(user.Guild);
            if (!getServerResult.IsSuccess)
            {
                return;
            }

            var server = getServerResult.Entity;

            if (!server.SendJoinMessage)
            {
                return;
            }

            var getJoinMessageResult = serverService.GetJoinMessage(server);
            if (!getJoinMessageResult.IsSuccess)
            {
                return;
            }

            var userChannel = await user.GetOrCreateDMChannelAsync();
            try
            {
                var eb = _feedback.CreateEmbedBase();
                eb.WithDescription($"Welcome, {user.Mention}!");
                eb.WithDescription(getJoinMessageResult.Entity);

                await _feedback.SendEmbedAsync(userChannel, eb.Build());
            }
            catch (HttpException hex)
            {
                if (!hex.WasCausedByDMsNotAccepted())
                {
                    throw;
                }

                var content = $"Welcome, {user.Mention}! You have DMs disabled, so I couldn't send you the " +
                              "first-join message. To see it, type \"!server join-message\".";

                var welcomeMessage = _feedback.CreateFeedbackEmbed
                (
                    user,
                    Color.Orange,
                    content
                );

                try
                {
                    await _feedback.SendEmbedAsync(user.Guild.SystemChannel, welcomeMessage);
                }
                catch (HttpException pex)
                {
                    if (!pex.WasCausedByMissingPermission())
                    {
                        throw;
                    }
                }
            }
        }
    }
}
