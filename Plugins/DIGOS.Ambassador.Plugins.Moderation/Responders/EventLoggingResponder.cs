//
//  EventLoggingResponder.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using DIGOS.Ambassador.Plugins.Moderation.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Caching;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Moderation.Responders;

/// <summary>
/// Logs various client events.
/// </summary>
[UsedImplicitly]
public class EventLoggingResponder :
    IResponder<IGuildMemberRemove>,
    IResponder<IGuildMemberUpdate>,
    IResponder<IMessageDelete>
{
    private readonly ChannelLoggingService _channelLogging;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventLoggingResponder"/> class.
    /// </summary>
    /// <param name="channelLogging">The channel logging service.</param>
    /// <param name="cache">The cache.</param>
    public EventLoggingResponder(ChannelLoggingService channelLogging, IMemoryCache cache)
    {
        _channelLogging = channelLogging;
        _cache = cache;
    }

    /// <inheritdoc />
    public Task<Result> RespondAsync(IGuildMemberRemove gatewayEvent, CancellationToken ct = default)
    {
        return _channelLogging.NotifyUserLeftAsync(gatewayEvent.GuildID, gatewayEvent.User.ID);
    }

    /// <inheritdoc />
    public async Task<Result> RespondAsync(IGuildMemberUpdate gatewayEvent, CancellationToken ct = default)
    {
        var oldMemberKey = KeyHelpers.CreateGuildMemberKey(gatewayEvent.GuildID, gatewayEvent.User.ID);
        if (!_cache.TryGetValue(oldMemberKey, out IGuildMember oldMember))
        {
            return Result.FromSuccess();
        }

        if (oldMember.Nickname != gatewayEvent.Nickname)
        {
            var notifyResult = await _channelLogging.NotifyUserNicknameChangedAsync
            (
                gatewayEvent.GuildID,
                gatewayEvent.User.ID,
                oldMember.Nickname,
                gatewayEvent.Nickname
            );

            if (!notifyResult.IsSuccess)
            {
                return notifyResult;
            }
        }

        if (!oldMember.User.IsDefined(out var oldUser))
        {
            return Result.FromSuccess();
        }

        if (oldUser.Discriminator != gatewayEvent.User.Discriminator)
        {
            return await _channelLogging.NotifyUserDiscriminatorChangedAsync
            (
                gatewayEvent.GuildID,
                gatewayEvent.User.ID,
                oldUser.Discriminator,
                gatewayEvent.User.Discriminator,
                ct
            );
        }

        return Result.FromSuccess();
    }

    /// <inheritdoc />
    public async Task<Result> RespondAsync(IMessageDelete gatewayEvent, CancellationToken ct = default)
    {
        var messageKey = KeyHelpers.CreateMessageCacheKey(gatewayEvent.ChannelID, gatewayEvent.ID);
        if (!_cache.TryGetValue(messageKey, out IMessage message))
        {
            return Result.FromSuccess();
        }

        if (!gatewayEvent.GuildID.IsDefined(out var guildID))
        {
            return Result.FromSuccess();
        }

        return await _channelLogging.NotifyMessageDeletedAsync(message, guildID);
    }
}
