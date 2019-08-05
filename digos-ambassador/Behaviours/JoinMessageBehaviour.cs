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
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using Discord;
using Discord.Net;
using Discord.WebSocket;

using JetBrains.Annotations;

namespace DIGOS.Ambassador.Behaviours
{
    /// <summary>
    /// Acts on user joins, sending them the server's join message.
    /// </summary>
    public class JoinMessageBehaviour : BehaviourBase
    {
        [ProvidesContext]
        private readonly CoreDatabaseContext _database;

        private readonly UserFeedbackService _feedback;
        private readonly ServerService _servers;

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinMessageBehaviour"/> class.
        /// </summary>
        /// <param name="client">The discord client.</param>
        /// <param name="database">The database.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="servers">The server service.</param>
        public JoinMessageBehaviour
        (
            DiscordSocketClient client,
            CoreDatabaseContext database,
            UserFeedbackService feedback,
            ServerService servers
        )
            : base(client)
        {
            _database = database;
            _feedback = feedback;
            _servers = servers;
        }

        /// <inheritdoc />
        protected override Task OnStartingAsync()
        {
            this.Client.UserJoined += OnUserJoined;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task OnStoppingAsync()
        {
            this.Client.UserJoined -= OnUserJoined;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles new users joining.
        /// </summary>
        /// <param name="user">The user.</param>
        private async Task OnUserJoined([NotNull] SocketGuildUser user)
        {
            var server = await _servers.GetOrRegisterServerAsync(user.Guild);

            if (!server.SendJoinMessage)
            {
                return;
            }

            var getJoinMessageResult = _servers.GetJoinMessage(server);
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
                    await _feedback.SendEmbedAsync(user.Guild.DefaultChannel, welcomeMessage);
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

        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();
            _database.Dispose();
        }
    }
}
