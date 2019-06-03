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

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Servers;

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
        private readonly GlobalInfoContext Database;

        private readonly UserFeedbackService Feedback;
        private readonly ServerService Servers;

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
            GlobalInfoContext database,
            UserFeedbackService feedback,
            ServerService servers
        )
            : base(client)
        {
            this.Database = database;
            this.Feedback = feedback;
            this.Servers = servers;
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
            var server = await this.Database.GetOrRegisterServerAsync(user.Guild);

            if (!server.SendJoinMessage)
            {
                return;
            }

            var getJoinMessageResult = this.Servers.GetJoinMessage(server);
            if (!getJoinMessageResult.IsSuccess)
            {
                return;
            }

            var userChannel = await user.GetOrCreateDMChannelAsync();
            try
            {
                var eb = this.Feedback.CreateEmbedBase();
                eb.WithDescription($"Welcome, {user.Mention}!");
                eb.WithDescription(getJoinMessageResult.Entity);

                await this.Feedback.SendEmbedAsync(userChannel, eb.Build());
            }
            catch (HttpException hex)
            {
                if (!hex.WasCausedByDMsNotAccepted())
                {
                    throw;
                }

                var content = $"Welcome, {user.Mention}! You have DMs disabled, so I couldn't send you the " +
                              "first-join message. To see it, type \"!server join-message\".";

                var welcomeMessage = this.Feedback.CreateFeedbackEmbed
                (
                    user,
                    Color.Orange,
                    content
                );

                try
                {
                    await this.Feedback.SendEmbedAsync(user.Guild.DefaultChannel, welcomeMessage);
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
            this.Database.Dispose();
        }
    }
}
