//
//  RoleplayDiscordService.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Services;

/// <summary>
/// Contains high-level business logic that can be used in Discord commands to consistently interact with roleplays.
/// The purpose of this class is to coordinate Discord-specific logic with platform-agnostic functionality.
/// </summary>
public class RoleplayDiscordService
{
    private readonly RoleplayService _roleplays;
    private readonly DedicatedChannelService _dedicatedChannels;
    private readonly IDiscordRestGuildAPI _guildAPI;
    private readonly IDiscordRestChannelAPI _channelAPI;

    private readonly UserService _users;
    private readonly ServerService _servers;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleplayDiscordService"/> class.
    /// </summary>
    /// <param name="roleplays">The roleplay service.</param>
    /// <param name="dedicatedChannels">The dedicated channel service.</param>
    /// <param name="users">The user service.</param>
    /// <param name="servers">The server service.</param>
    /// <param name="guildAPI">The guild API.</param>
    /// <param name="channelAPI">The channel API.</param>
    public RoleplayDiscordService
    (
        RoleplayService roleplays,
        DedicatedChannelService dedicatedChannels,
        UserService users,
        ServerService servers,
        IDiscordRestGuildAPI guildAPI,
        IDiscordRestChannelAPI channelAPI
    )
    {
        _roleplays = roleplays;
        _dedicatedChannels = dedicatedChannels;
        _users = users;
        _servers = servers;
        _guildAPI = guildAPI;
        _channelAPI = channelAPI;
    }

    /// <summary>
    /// Gets the roleplays that match the given query.
    /// </summary>
    /// <param name="query">Additional query statements.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    public Task<IReadOnlyList<Roleplay>> QueryRoleplaysAsync
    (
        Func<IQueryable<Roleplay>, IQueryable<Roleplay>>? query = default
    )
        => _roleplays.QueryDatabaseAsync(query);

    /// <summary>
    /// Gets the roleplays that match the given query.
    /// </summary>
    /// <typeparam name="TOut">The output type of the query.</typeparam>
    /// <param name="query">Additional query statements.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    public Task<TOut> QueryRoleplaysAsync<TOut>
    (
        Func<IQueryable<Roleplay>, Task<TOut>> query
    )
        => _roleplays.QueryDatabaseAsync(query);

    /// <summary>
    /// Creates a new roleplay with the given owner and parameters.
    /// </summary>
    /// <param name="guildID">The ID of the guild the user is on.</param>
    /// <param name="userID">The ID of the user.</param>
    /// <param name="name">The name of the roleplay.</param>
    /// <param name="summary">A short summary.</param>
    /// <param name="isNSFW">Whether the roleplay is NSFW.</param>
    /// <param name="isPublic">Whether the roleplay is public.</param>
    /// <returns>A creation result which may or may not have succeeded.</returns>
    public async Task<Result<Roleplay>> CreateRoleplayAsync
    (
        Snowflake guildID,
        Snowflake userID,
        string name,
        string summary,
        bool isNSFW,
        bool isPublic
    )
    {
        var getUser = await _users.GetOrRegisterUserAsync(userID);
        if (!getUser.IsSuccess)
        {
            return Result<Roleplay>.FromError(getUser);
        }

        var user = getUser.Entity;

        var getServer = await _servers.GetOrRegisterServerAsync(guildID);
        if (!getServer.IsSuccess)
        {
            return Result<Roleplay>.FromError(getServer);
        }

        var server = getServer.Entity;

        var createRoleplay = await _roleplays.CreateRoleplayAsync(user, server, name, summary, isNSFW, isPublic);
        if (!createRoleplay.IsSuccess)
        {
            return createRoleplay;
        }

        var roleplay = createRoleplay.Entity;

        var createChannel = await _dedicatedChannels.CreateDedicatedChannelAsync(roleplay);

        return !createChannel.IsSuccess
            ? Result<Roleplay>.FromError(createChannel)
            : roleplay;
    }

    /// <summary>
    /// Deletes a roleplay and its associated channel, if any.
    /// </summary>
    /// <param name="roleplay">The roleplay.</param>
    /// <returns>A deletion result which may or may not have succeeded.</returns>
    public async Task<Result> DeleteRoleplayAsync(Roleplay roleplay)
    {
        if (!roleplay.DedicatedChannelID.HasValue)
        {
            return await _roleplays.DeleteRoleplayAsync(roleplay);
        }

        var deleteChannel = await _dedicatedChannels.DeleteChannelAsync(roleplay);
        if (!deleteChannel.IsSuccess)
        {
            return deleteChannel;
        }

        return await _roleplays.DeleteRoleplayAsync(roleplay);
    }

    /// <summary>
    /// Invites the given user to the given roleplay.
    /// </summary>
    /// <param name="roleplay">The roleplay to invite the user to.</param>
    /// <param name="userID">The ID of the user.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> InviteUserToRoleplayAsync(Roleplay roleplay, Snowflake userID)
    {
        var getUser = await _users.GetOrRegisterUserAsync(userID);
        if (!getUser.IsSuccess)
        {
            return Result.FromError(getUser);
        }

        var user = getUser.Entity;

        return await _roleplays.InviteUserToRoleplayAsync(roleplay, user);
    }

    /// <summary>
    /// Adds the given user to the given roleplay.
    /// </summary>
    /// <param name="roleplay">The roleplay.</param>
    /// <param name="userID">The ID of the user.</param>
    /// <returns>A creation result which may or may not have succeeded.</returns>
    public async Task<Result<RoleplayParticipant>> AddUserToRoleplayAsync
    (
        Roleplay roleplay,
        Snowflake userID
    )
    {
        var getUser = await _users.GetOrRegisterUserAsync(userID);
        if (!getUser.IsSuccess)
        {
            return Result<RoleplayParticipant>.FromError(getUser);
        }

        var user = getUser.Entity;

        var addUserAsync = await _roleplays.AddUserToRoleplayAsync(roleplay, user);
        if (!addUserAsync.IsSuccess)
        {
            return addUserAsync;
        }

        if (!roleplay.DedicatedChannelID.HasValue)
        {
            return addUserAsync;
        }

        var updatePermissions = await _dedicatedChannels.UpdateParticipantPermissionsAsync(roleplay);

        return !updatePermissions.IsSuccess
            ? Result<RoleplayParticipant>.FromError(updatePermissions)
            : addUserAsync;
    }

    /// <summary>
    /// Removes the given user from the given roleplay, revoking their access to the dedicated channel (if one
    /// exists).
    /// </summary>
    /// <param name="roleplay">The roleplay.</param>
    /// <param name="userID">The ID of the user.</param>
    /// <returns>A deletion result which may or may not have succeeded.</returns>
    public async Task<Result> RemoveUserFromRoleplayAsync(Roleplay roleplay, Snowflake userID)
    {
        var getUser = await _users.GetOrRegisterUserAsync(userID);
        if (!getUser.IsSuccess)
        {
            return Result.FromError(getUser);
        }

        var user = getUser.Entity;

        var removeUserAsync = await _roleplays.RemoveUserFromRoleplayAsync(roleplay, user);
        if (!removeUserAsync.IsSuccess)
        {
            return removeUserAsync;
        }

        if (!roleplay.DedicatedChannelID.HasValue)
        {
            return removeUserAsync;
        }

        return await _dedicatedChannels.RevokeUserAccessAsync(roleplay, userID);
    }

    /// <summary>
    /// Kicks the given user from the given roleplay, preventing them from joining again.
    /// </summary>
    /// <param name="roleplay">The roleplay.</param>
    /// <param name="userID">The ID of the user.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> KickUserFromRoleplayAsync(Roleplay roleplay, Snowflake userID)
    {
        var getUser = await _users.GetOrRegisterUserAsync(userID);
        if (!getUser.IsSuccess)
        {
            return Result.FromError(getUser);
        }

        var user = getUser.Entity;

        var removeUserAsync = await _roleplays.KickUserFromRoleplayAsync(roleplay, user);
        if (!removeUserAsync.IsSuccess)
        {
            return removeUserAsync;
        }

        if (!roleplay.DedicatedChannelID.HasValue)
        {
            return removeUserAsync;
        }

        return await _dedicatedChannels.RevokeUserAccessAsync(roleplay, userID);
    }

    /// <summary>
    /// Starts the given roleplay in the current channel, or the dedicated channel if one exists.
    /// </summary>
    /// <param name="currentChannelID">The current channel.</param>
    /// <param name="roleplay">The roleplay.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> StartRoleplayAsync(Snowflake currentChannelID, Roleplay roleplay)
    {
        var getDedicatedChannelResult = _dedicatedChannels.GetDedicatedChannel(roleplay);

        // Identify the channel to start the RP in. Preference is given to the roleplay's dedicated channel.
        var channelID = getDedicatedChannelResult.IsSuccess ? getDedicatedChannelResult.Entity : currentChannelID;
        var getChannel = await _channelAPI.GetChannelAsync(channelID);
        if (!getChannel.IsSuccess)
        {
            return Result.FromError(getChannel);
        }

        var channel = getChannel.Entity;

        if (roleplay.IsNSFW && !(channel.IsNsfw.HasValue && channel.IsNsfw.Value))
        {
            return new UserError
            (
                "This channel is not marked as NSFW, while your roleplay is... naughty!"
            );
        }

        var getHasActiveRoleplay = await HasActiveRoleplayAsync(channelID);
        if (!getHasActiveRoleplay.IsSuccess)
        {
            return Result.FromError(getHasActiveRoleplay);
        }

        if (getHasActiveRoleplay.Entity)
        {
            var currentRoleplayResult = await GetActiveRoleplayAsync(channelID);
            if (!currentRoleplayResult.IsSuccess)
            {
                return Result.FromError(currentRoleplayResult);
            }

            var currentRoleplay = currentRoleplayResult.Entity;
            var timeOfLastMessage = currentRoleplay.Messages.Last().Timestamp;
            var currentTime = DateTimeOffset.UtcNow;

            if (timeOfLastMessage < currentTime.AddHours(-4))
            {
                currentRoleplay.IsActive = false;
            }
            else
            {
                return new UserError("There's already a roleplay active in this channel.");
            }
        }

        var start = await _roleplays.StartRoleplayAsync(roleplay, channelID);
        if (!start.IsSuccess)
        {
            return start;
        }

        // If the channel in question is the roleplay's dedicated channel, enable it
        if (!roleplay.DedicatedChannelID.HasValue)
        {
            return Result.FromSuccess();
        }

        var enableChannel = await _dedicatedChannels.UpdateParticipantPermissionsAsync(roleplay);
        if (!enableChannel.IsSuccess)
        {
            return enableChannel;
        }

        var joinedUsers = roleplay.JoinedUsers.Select
        (
            u => $"<@{u.User.DiscordID}>"
        );

        var participantList = joinedUsers.Humanize();

        var send = await _channelAPI.CreateMessageAsync
        (
            roleplay.ActiveChannelID!.Value,
            $"Calling {participantList}!"
        );

        return !send.IsSuccess
            ? Result.FromError(send)
            : Result.FromSuccess();
    }

    /// <summary>
    /// Stops the given roleplay, disabling its dedicated channel if one exists.
    /// </summary>
    /// <param name="roleplay">The roleplay.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> StopRoleplayAsync(Roleplay roleplay)
    {
        var stop = await _roleplays.StopRoleplayAsync(roleplay);
        if (!stop.IsSuccess)
        {
            return stop;
        }

        // If the channel in question is the roleplay's dedicated channel, disable it
        if (!roleplay.DedicatedChannelID.HasValue)
        {
            return Result.FromSuccess();
        }

        return await _dedicatedChannels.UpdateParticipantPermissionsAsync(roleplay);
    }

    /// <summary>
    /// Sets the name of the roleplay, updating the name of the dedicated channel (if one exists).
    /// </summary>
    /// <param name="roleplay">The roleplay.</param>
    /// <param name="name">The new name.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetRoleplayNameAsync(Roleplay roleplay, string name)
    {
        var setName = await _roleplays.SetRoleplayNameAsync(roleplay, name);
        if (!setName.IsSuccess)
        {
            return setName;
        }

        if (!roleplay.DedicatedChannelID.HasValue)
        {
            return Result.FromSuccess();
        }

        return await _dedicatedChannels.UpdateChannelNameAsync(roleplay);
    }

    /// <summary>
    /// Sets the summary of the given roleplay.
    /// </summary>
    /// <param name="roleplay">The roleplay to set the summary of.</param>
    /// <param name="summary">The new summary.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetRoleplaySummaryAsync(Roleplay roleplay, string summary)
    {
        var setSummary = await _roleplays.SetRoleplaySummaryAsync(roleplay, summary);
        if (!setSummary.IsSuccess)
        {
            return setSummary;
        }

        if (!roleplay.DedicatedChannelID.HasValue)
        {
            return Result.FromSuccess();
        }

        return await _dedicatedChannels.UpdateChannelSummaryAsync(roleplay);
    }

    /// <summary>
    /// Sets the NSFW status of the roleplay.
    /// </summary>
    /// <param name="roleplay">The roleplay.</param>
    /// <param name="isNSFW">Whether the roleplay is NSFW.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetRoleplayIsNSFWAsync(Roleplay roleplay, bool isNSFW)
    {
        var setNSFW = await _roleplays.SetRoleplayIsNSFWAsync(roleplay, isNSFW);
        if (!setNSFW.IsSuccess)
        {
            return setNSFW;
        }

        if (!roleplay.DedicatedChannelID.HasValue)
        {
            return setNSFW;
        }

        return await _dedicatedChannels.UpdateChannelNSFWStatus(roleplay);
    }

    /// <summary>
    /// Sets the public status of the roleplay, updating channel permissions if necessary.
    /// </summary>
    /// <param name="roleplay">The roleplay.</param>
    /// <param name="isPublic">Whether the roleplay is public.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetRoleplayIsPublicAsync(Roleplay roleplay, bool isPublic)
    {
        var setPublic = await _roleplays.SetRoleplayIsPublicAsync(roleplay, isPublic);
        if (!setPublic.IsSuccess)
        {
            return setPublic;
        }

        if (!roleplay.DedicatedChannelID.HasValue)
        {
            return setPublic;
        }

        return await _dedicatedChannels.UpdateParticipantPermissionsAsync(roleplay);
    }

    /// <summary>
    /// Gets the roleplay currently active in the given channel.
    /// </summary>
    /// <param name="channelID">The channel.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    public async Task<Result<Roleplay>> GetActiveRoleplayAsync(Snowflake channelID)
    {
        var roleplays = await _roleplays.QueryDatabaseAsync
        (
            q => q
                .Where(rp => rp.IsActive)
                .Where(rp => rp.ActiveChannelID == channelID)
        );

        var roleplay = roleplays.SingleOrDefault();

        if (roleplay is null)
        {
            return new UserError
            (
                "There is no roleplay that is currently active in this channel."
            );
        }

        return roleplay;
    }

    /// <summary>
    /// This method searches for the best matching roleplay given an owner and a name. If no owner is provided, then
    /// the global list is searched for a unique name. If neither are provided, the currently active roleplay is
    /// returned. If no match can be found, a failed result is returned.
    /// </summary>
    /// <param name="channelID">The channel the command was executed in.</param>
    /// <param name="guildID">The guild the command was executed in.</param>
    /// <param name="roleplayOwnerID">The owner of the roleplay, if any.</param>
    /// <param name="roleplayName">The name of the roleplay, if any.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    public async Task<Result<Roleplay>> GetBestMatchingRoleplayAsync
    (
        Snowflake channelID,
        Snowflake guildID,
        Snowflake? roleplayOwnerID,
        string? roleplayName
    )
    {
        switch (roleplayOwnerID)
        {
            case null when roleplayName is null:
            {
                return await GetActiveRoleplayAsync(channelID);
            }
            case null:
            {
                return await _roleplays.GetNamedRoleplayAsync(roleplayName, guildID);
            }
        }

        if (roleplayName.IsNullOrWhitespace())
        {
            return await GetActiveRoleplayAsync(channelID);
        }

        var getUserRoleplay = await _roleplays.GetUserRoleplayByNameAsync
        (
            guildID,
            roleplayOwnerID.Value,
            roleplayName
        );

        if (!getUserRoleplay.IsSuccess)
        {
            // Search again, but this time globally
            return await GetBestMatchingRoleplayAsync(channelID, guildID, null, roleplayName);
        }

        return getUserRoleplay;
    }

    /// <summary>
    /// Determines whether or not there is an active roleplay in the given channel.
    /// </summary>
    /// <param name="channelID">The channel to check.</param>
    /// <returns>true if there is an active roleplay; otherwise, false.</returns>
    public async Task<Result<bool>> HasActiveRoleplayAsync(Snowflake channelID)
    {
        return await QueryRoleplaysAsync
        (
            q => q
                .Where(rp => rp.IsActive)
                .Where(rp => rp.ActiveChannelID == channelID)
                .AnyAsync()
        );
    }

    /// <summary>
    /// Consumes a message, adding it to the active roleplay in its channel if the author is a participant.
    /// </summary>
    /// <param name="message">The received message.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> ConsumeMessageAsync(IMessage message)
    {
        var checkForActive = await HasActiveRoleplayAsync(message.ChannelID);
        if (!checkForActive.IsSuccess)
        {
            return Result.FromError(checkForActive);
        }

        if (!checkForActive.Entity)
        {
            // There's no roleplay in that channel, so it's fine
            return Result.FromSuccess();
        }

        var result = await GetActiveRoleplayAsync(message.ChannelID);
        if (!result.IsSuccess)
        {
            return Result.FromError(result);
        }

        var roleplay = result.Entity;

        if (!roleplay.HasJoined(message.Author))
        {
            return new UserError("The given message was not authored by a participant of the roleplay.");
        }

        var userNick = message.Author.Username;
        if (message.GuildID.HasValue)
        {
            var getMember = await _guildAPI.GetGuildMemberAsync(message.GuildID.Value, message.Author.ID);
            if (getMember.IsSuccess)
            {
                var member = getMember.Entity;
                if (member.Nickname.HasValue && member.Nickname.Value is not null)
                {
                    userNick = member.Nickname.Value;
                }
            }
        }

        var getAuthor = await _users.GetOrRegisterUserAsync(message.Author.ID);
        if (!getAuthor.IsSuccess)
        {
            return Result.FromError(getAuthor);
        }

        var author = getAuthor.Entity;

        return await _roleplays.AddOrUpdateMessageInRoleplayAsync
        (
            roleplay,
            author,
            message.ID,
            message.Timestamp,
            userNick,
            message.Content
        );
    }

    /// <summary>
    /// Consumes a message, adding it to the active roleplay in its channel if the author is a participant.
    /// </summary>
    /// <param name="message">The received message.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> ConsumeMessageAsync(IPartialMessage message)
    {
        if (!message.ID.HasValue)
        {
            return new InvalidOperationError("Unable to process messages without IDs.");
        }

        if (!message.Timestamp.HasValue)
        {
            return new InvalidOperationError("Unable to process messages without timestamps.");
        }

        if (!message.Content.HasValue)
        {
            return new InvalidOperationError("Unable to process messages without content.");
        }

        if (!message.ChannelID.HasValue)
        {
            return new InvalidOperationError("Unable to process messages without channel IDs.");
        }

        if (!message.Author.HasValue)
        {
            return new InvalidOperationError("Unable to process messages without authors.");
        }

        var result = await GetActiveRoleplayAsync(message.ChannelID.Value);
        if (!result.IsSuccess)
        {
            return Result.FromError(result);
        }

        var roleplay = result.Entity;

        if (!roleplay.HasJoined(message.Author.Value))
        {
            return new UserError("The given message was not authored by a participant of the roleplay.");
        }

        var userNick = message.Author.Value.Username;
        if (message.GuildID.HasValue)
        {
            var getMember = await _guildAPI.GetGuildMemberAsync(message.GuildID.Value, message.Author.Value.ID);
            if (getMember.IsSuccess)
            {
                var member = getMember.Entity;
                if (member.Nickname.HasValue && member.Nickname.Value is not null)
                {
                    userNick = member.Nickname.Value;
                }
            }
        }

        var getAuthor = await _users.GetOrRegisterUserAsync(message.Author.Value.ID);
        if (!getAuthor.IsSuccess)
        {
            return Result.FromError(getAuthor);
        }

        var author = getAuthor.Entity;

        return await _roleplays.AddOrUpdateMessageInRoleplayAsync
        (
            roleplay,
            author,
            message.ID.Value,
            message.Timestamp.Value,
            userNick,
            message.Content.Value
        );
    }

    /// <summary>
    /// Ensures all messages from the roleplay's dedicated channel are logged, in case the bot was down when one or
    /// more messages were sent.
    /// </summary>
    /// <param name="roleplay">The roleplay.</param>
    /// <returns>An execution result which may or may not have succeeded.</returns>
    public async Task<Result<ulong>> EnsureAllMessagesAreLoggedAsync(Roleplay roleplay)
    {
        var targetChannel = roleplay.DedicatedChannelID ?? (roleplay.ActiveChannelID ?? null);
        if (targetChannel is null)
        {
            return new UserError("The roleplay doesn't have a dedicated channel, nor is it active in one.");
        }

        var lastMessage = roleplay.Messages.LastOrDefault()?.DiscordMessageID ?? default;

        ulong updatedMessages = 0;
        while (true)
        {
            var getMessages = await _channelAPI.GetChannelMessagesAsync
            (
                roleplay.DedicatedChannelID!.Value,
                after: lastMessage
            );

            if (!getMessages.IsSuccess)
            {
                return Result<ulong>.FromError(getMessages);
            }

            var messages = getMessages.Entity;
            if (messages.Count == 0)
            {
                break;
            }

            lastMessage = messages[^1].ID;

            foreach (var message in messages)
            {
                if (!roleplay.HasJoined(message.Author))
                {
                    continue;
                }

                var updateResult = await ConsumeMessageAsync(message);
                if (!updateResult.IsSuccess)
                {
                    return Result<ulong>.FromError(updateResult);
                }

                ++updatedMessages;
            }
        }

        return updatedMessages;
    }

    /// <summary>
    /// Transfers ownership of the given roleplay to the given user.
    /// </summary>
    /// <param name="userID">The ID of the user.</param>
    /// <param name="roleplay">The roleplay to transfer.</param>
    /// <returns>An execution result which may or may not have succeeded.</returns>
    public async Task<Result> TransferRoleplayOwnershipAsync(Snowflake userID, Roleplay roleplay)
        => await _roleplays.TransferRoleplayOwnershipAsync(userID, roleplay);

    /// <summary>
    /// Refreshes the timestamp on the given roleplay.
    /// </summary>
    /// <param name="roleplay">The roleplay.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public Task<Result> RefreshRoleplayAsync(Roleplay roleplay)
        => _roleplays.RefreshRoleplayAsync(roleplay);
}
