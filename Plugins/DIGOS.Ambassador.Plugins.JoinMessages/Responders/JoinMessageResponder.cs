//
//  JoinMessageResponder.cs
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

using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Abstractions.Results;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Discord.Rest.Results;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.JoinMessages.Responders
{
    /// <summary>
    /// Acts on user joins, sending them the server's join message.
    /// </summary>
    public class JoinMessageResponder : IResponder<IGuildMemberAdd>
    {
        private readonly ServerService _servers;
        private readonly FeedbackService _feedback;
        private readonly IDiscordRestUserAPI _userAPI;
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly IDiscordRestGuildAPI _guildAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinMessageResponder"/> class.
        /// </summary>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="servers">The server service.</param>
        /// <param name="userAPI">The user API.</param>
        /// <param name="channelAPI">The channel API.</param>
        /// <param name="guildAPI">The guild API.</param>
        public JoinMessageResponder
        (
            FeedbackService feedback,
            ServerService servers,
            IDiscordRestUserAPI userAPI,
            IDiscordRestChannelAPI channelAPI,
            IDiscordRestGuildAPI guildAPI
        )
        {
            _feedback = feedback;
            _servers = servers;
            _userAPI = userAPI;
            _channelAPI = channelAPI;
            _guildAPI = guildAPI;
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = default)
        {
            if (!gatewayEvent.User.IsDefined(out var user))
            {
                // We can't do anything about this
                return Result.FromSuccess();
            }

            var getServerResult = await _servers.GetOrRegisterServerAsync(gatewayEvent.GuildID, ct);
            if (!getServerResult.IsSuccess)
            {
                return Result.FromError(getServerResult);
            }

            var server = getServerResult.Entity;

            if (!server.SendJoinMessage)
            {
                return Result.FromSuccess();
            }

            var getJoinMessageResult = _servers.GetJoinMessage(server);
            if (!getJoinMessageResult.IsSuccess)
            {
                return Result.FromError(getJoinMessageResult);
            }

            var openDM = await _userAPI.CreateDMAsync(user.ID, ct);
            if (!openDM.IsSuccess)
            {
                return Result.FromError(openDM);
            }

            var userChannel = openDM.Entity;

            var embed = new Embed
            {
                Colour = _feedback.Theme.Secondary,
                Description = $"Welcome, <@{user.ID}>!\n" +
                              "\n" +
                              $"{getJoinMessageResult.Entity}"
            };

            var sendEmbed = await _channelAPI.CreateMessageAsync(userChannel.ID, embeds: new[] { embed }, ct: ct);
            if (sendEmbed.IsSuccess)
            {
                return Result.FromSuccess();
            }

            if (sendEmbed.Error is not DiscordRestResultError dre)
            {
                return Result.FromError(sendEmbed);
            }

            if (dre.DiscordError.Code is not DiscordError.CannotSendMessageToUser)
            {
                return Result.FromError(sendEmbed);
            }

            var getGuild = await _guildAPI.GetGuildAsync(gatewayEvent.GuildID, ct: ct);
            if (!getGuild.IsSuccess)
            {
                return Result.FromError(getGuild);
            }

            var guild = getGuild.Entity;
            if (!guild.SystemChannelID.HasValue)
            {
                // man, we tried
                return Result.FromSuccess();
            }

            var content = $"Welcome, <@{user.ID}>! You have DMs disabled, so I couldn't send you " +
                          "the first-join message. To see it, type \"!server join-message\".";

            var sendNotification = await _feedback.SendWarningAsync
            (
                guild.SystemChannelID.Value,
                content,
                user.ID,
                ct: ct
            );

            return sendNotification.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(sendNotification);
        }
    }
}
