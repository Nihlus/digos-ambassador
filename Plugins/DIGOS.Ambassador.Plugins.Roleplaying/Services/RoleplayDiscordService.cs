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
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Services
{
    /// <summary>
    /// Contains high-level business logic that can be used in Discord commands to consistently interact with roleplays.
    /// The purpose of this class is to coordinate Discord-specific logic with platform-agnostic functionality.
    /// </summary>
    public class RoleplayDiscordService
    {
        private readonly IDiscordClient _client;
        private readonly RoleplayService _roleplays;
        private readonly DedicatedChannelService _dedicatedChannels;

        private readonly UserService _users;
        private readonly ServerService _servers;

        private readonly CommandService _commands;
        private readonly OwnedEntityService _ownedEntities;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayDiscordService"/> class.
        /// </summary>
        /// <param name="client">The Discord client.</param>
        /// <param name="roleplays">The roleplay service.</param>
        /// <param name="dedicatedChannels">The dedicated channel service.</param>
        /// <param name="users">The user service.</param>
        /// <param name="servers">The server service.</param>
        /// <param name="commands">The command service.</param>
        /// <param name="ownedEntities">The owned entity service.</param>
        public RoleplayDiscordService
        (
            IDiscordClient client,
            RoleplayService roleplays,
            DedicatedChannelService dedicatedChannels,
            UserService users,
            ServerService servers,
            CommandService commands,
            OwnedEntityService ownedEntities
        )
        {
            _client = client;
            _roleplays = roleplays;
            _dedicatedChannels = dedicatedChannels;
            _users = users;
            _servers = servers;
            _commands = commands;
            _ownedEntities = ownedEntities;
        }

        /// <summary>
        /// Gets the roleplays on the given guild.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<IQueryable<Roleplay>>> GetRoleplaysAsync(IGuild guild)
        {
            var getServer = await _servers.GetOrRegisterServerAsync(guild);
            if (!getServer.IsSuccess)
            {
                return RetrieveEntityResult<IQueryable<Roleplay>>.FromError(getServer);
            }

            var server = getServer.Entity;

            return RetrieveEntityResult<IQueryable<Roleplay>>.FromSuccess(_roleplays.GetRoleplays(server));
        }

        /// <summary>
        /// Gets the roleplays belonging to the given guild user.
        /// </summary>
        /// <param name="guildUser">The guild user.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<IQueryable<Roleplay>>> GetUserRoleplaysAsync(IGuildUser guildUser)
        {
            var getGuildRoleplays = await GetRoleplaysAsync(guildUser.Guild);
            if (!getGuildRoleplays.IsSuccess)
            {
                return getGuildRoleplays;
            }

            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return RetrieveEntityResult<IQueryable<Roleplay>>.FromError(getUser);
            }

            var user = getUser.Entity;
            var guildRoleplays = getGuildRoleplays.Entity;

            return RetrieveEntityResult<IQueryable<Roleplay>>.FromSuccess(guildRoleplays.Where(r => r.Owner == user));
        }

        /// <summary>
        /// Creates a new roleplay with the given owner and parameters.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="name">The name of the roleplay.</param>
        /// <param name="summary">A short summary.</param>
        /// <param name="isNSFW">Whether the roleplay is NSFW.</param>
        /// <param name="isPublic">Whether the roleplay is public.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<Roleplay>> CreateRoleplayAsync
        (
            IGuildUser owner,
            string name,
            string summary,
            bool isNSFW,
            bool isPublic
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(owner);
            if (!getUser.IsSuccess)
            {
                return CreateEntityResult<Roleplay>.FromError(getUser);
            }

            var user = getUser.Entity;

            var getServer = await _servers.GetOrRegisterServerAsync(owner.Guild);
            if (!getServer.IsSuccess)
            {
                return CreateEntityResult<Roleplay>.FromError(getServer);
            }

            var server = getServer.Entity;

            var createRoleplay = await _roleplays.CreateRoleplayAsync(user, server, name, summary, isNSFW, isPublic);
            if (!createRoleplay.IsSuccess)
            {
                return createRoleplay;
            }

            var roleplay = createRoleplay.Entity;

            var createChannel = await _dedicatedChannels.CreateDedicatedChannelAsync(owner.Guild, roleplay);
            if (!createChannel.IsSuccess)
            {
                return CreateEntityResult<Roleplay>.FromError(createChannel);
            }

            return roleplay;
        }

        /// <summary>
        /// Deletes a roleplay and its associated channel, if any.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<DeleteEntityResult> DeleteRoleplayAsync(Roleplay roleplay)
        {
            var guild = await _client.GetGuildAsync((ulong)roleplay.Server.DiscordID);
            if (guild is null)
            {
                return DeleteEntityResult.FromError("Could not retrieve the associated guild.");
            }

            if (roleplay.DedicatedChannelID.HasValue)
            {
                var deleteChannel = await _dedicatedChannels.DeleteChannelAsync(guild, roleplay);
                if (!deleteChannel.IsSuccess)
                {
                    return deleteChannel;
                }
            }

            var deleteRoleplay = await _roleplays.DeleteRoleplayAsync(roleplay);
            if (!deleteRoleplay.IsSuccess)
            {
                return deleteRoleplay;
            }

            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// Invites the given user to the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to invite the user to.</param>
        /// <param name="discordUser">The user to invite.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> InviteUserToRoleplayAsync(Roleplay roleplay, IGuildUser discordUser)
        {
            var getUser = await _users.GetOrRegisterUserAsync(discordUser);
            if (!getUser.IsSuccess)
            {
                return ModifyEntityResult.FromError(getUser);
            }

            var user = getUser.Entity;

            return await _roleplays.InviteUserToRoleplayAsync(roleplay, user);
        }

        /// <summary>
        /// Adds the given user to the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="discordUser">The discord user.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<RoleplayParticipant>> AddUserToRoleplayAsync
        (
            Roleplay roleplay,
            IGuildUser discordUser
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(discordUser);
            if (!getUser.IsSuccess)
            {
                return CreateEntityResult<RoleplayParticipant>.FromError(getUser);
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

            var updatePermissions = await _dedicatedChannels.UpdateParticipantPermissionsAsync
            (
                discordUser.Guild,
                roleplay
            );

            if (!updatePermissions.IsSuccess)
            {
                return CreateEntityResult<RoleplayParticipant>.FromError(updatePermissions);
            }

            return addUserAsync;
        }

        /// <summary>
        /// Removes the given user from the given roleplay, revoking their access to the dedicated channel (if one
        /// exists).
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="discordUser">The user.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<DeleteEntityResult> RemoveUserFromRoleplayAsync(Roleplay roleplay, IGuildUser discordUser)
        {
            var getUser = await _users.GetOrRegisterUserAsync(discordUser);
            if (!getUser.IsSuccess)
            {
                return DeleteEntityResult.FromError(getUser);
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

            var revoke = await _dedicatedChannels.RevokeUserAccessAsync(roleplay, discordUser);
            if (!revoke.IsSuccess)
            {
                return DeleteEntityResult.FromError(revoke);
            }

            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// Kicks the given user from the given roleplay, preventing them from joining again.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="discordUser">The user.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> KickUserFromRoleplayAsync(Roleplay roleplay, IGuildUser discordUser)
        {
            var getUser = await _users.GetOrRegisterUserAsync(discordUser);
            if (!getUser.IsSuccess)
            {
                return ModifyEntityResult.FromError(getUser);
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

            var revoke = await _dedicatedChannels.RevokeUserAccessAsync(roleplay, discordUser);
            if (!revoke.IsSuccess)
            {
                return ModifyEntityResult.FromError(revoke);
            }

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Starts the given roleplay in the current channel, or the dedicated channel if one exists.
        /// </summary>
        /// <param name="currentChannel">The current channel.</param>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> StartRoleplayAsync(ITextChannel currentChannel, Roleplay roleplay)
        {
            var guild = currentChannel.Guild;

            var getDedicatedChannelResult = await _dedicatedChannels.GetDedicatedChannelAsync
            (
                guild,
                roleplay
            );

            // Identify the channel to start the RP in. Preference is given to the roleplay's dedicated channel.
            var channel = getDedicatedChannelResult.IsSuccess ? getDedicatedChannelResult.Entity : currentChannel;
            if (roleplay.IsNSFW && !channel.IsNsfw)
            {
                return ModifyEntityResult.FromError
                (
                    "This channel is not marked as NSFW, while your roleplay is... naughty!"
                );
            }

            if (await HasActiveRoleplayAsync(channel))
            {
                var currentRoleplayResult = await GetActiveRoleplayAsync(channel);
                if (!currentRoleplayResult.IsSuccess)
                {
                    return ModifyEntityResult.FromError(currentRoleplayResult);
                }

                var currentRoleplay = currentRoleplayResult.Entity;
                var timeOfLastMessage = currentRoleplay.Messages.Last().Timestamp;
                var currentTime = DateTimeOffset.Now;

                if (timeOfLastMessage < currentTime.AddHours(-4))
                {
                    currentRoleplay.IsActive = false;
                }
                else
                {
                    return ModifyEntityResult.FromError("There's already a roleplay active in this channel.");
                }
            }

            var start = await _roleplays.StartRoleplayAsync(roleplay, (long)channel.Id);
            if (!start.IsSuccess)
            {
                return start;
            }

            // If the channel in question is the roleplay's dedicated channel, enable it
            if (!roleplay.DedicatedChannelID.HasValue)
            {
                return ModifyEntityResult.FromSuccess();
            }

            var enableChannel = await _dedicatedChannels.UpdateParticipantPermissionsAsync(guild, roleplay);
            if (!enableChannel.IsSuccess)
            {
                return enableChannel;
            }

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Stops the given roleplay, disabling its dedicated channel if one exists.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> StopRoleplayAsync(Roleplay roleplay)
        {
            var stop = await _roleplays.StopRoleplayAsync(roleplay);
            if (!stop.IsSuccess)
            {
                return stop;
            }

            // If the channel in question is the roleplay's dedicated channel, disable it
            if (!roleplay.DedicatedChannelID.HasValue)
            {
                return ModifyEntityResult.FromSuccess();
            }

            var guild = await _client.GetGuildAsync((ulong)roleplay.Server.DiscordID);
            if (guild is null)
            {
                return ModifyEntityResult.FromError("Failed to get a valid guild for the roleplay.");
            }

            var enableChannel = await _dedicatedChannels.UpdateParticipantPermissionsAsync(guild, roleplay);
            if (!enableChannel.IsSuccess)
            {
                return enableChannel;
            }

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the name of the roleplay, updating the name of the dedicated channel (if one exists).
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="name">The new name.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetRoleplayNameAsync(Roleplay roleplay, string name)
        {
            var commandModule = _commands.Modules.First(m => m.Name == "roleplay");
            var validNameResult = _ownedEntities.IsEntityNameValid(commandModule.GetAllCommandNames(), name);
            if (!validNameResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(validNameResult);
            }

            var setName = await _roleplays.SetRoleplayNameAsync(roleplay, name);
            if (!setName.IsSuccess)
            {
                return setName;
            }

            if (!roleplay.DedicatedChannelID.HasValue)
            {
                return ModifyEntityResult.FromSuccess();
            }

            var setChannelName = await _dedicatedChannels.UpdateChannelNameAsync(roleplay);
            if (!setChannelName.IsSuccess)
            {
                return setChannelName;
            }

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the summary of the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to set the summary of.</param>
        /// <param name="summary">The new summary.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetRoleplaySummaryAsync(Roleplay roleplay, string summary)
        {
            var setSummary = await _roleplays.SetRoleplaySummaryAsync(roleplay, summary);
            if (!setSummary.IsSuccess)
            {
                return setSummary;
            }

            if (!roleplay.DedicatedChannelID.HasValue)
            {
                return ModifyEntityResult.FromSuccess();
            }

            var setChannelSummary = await _dedicatedChannels.UpdateChannelSummaryAsync(roleplay);
            if (!setChannelSummary.IsSuccess)
            {
                return setChannelSummary;
            }

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the NSFW status of the roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="isNSFW">Whether the roleplay is NSFW.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetRoleplayIsNSFWAsync(Roleplay roleplay, bool isNSFW)
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

            var setChannelNSFW = await _dedicatedChannels.UpdateChannelNSFWStatus(roleplay);
            if (!setChannelNSFW.IsSuccess)
            {
                return setChannelNSFW;
            }

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the public status of the roleplay, updating channel permissions if necessary.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="isPublic">Whether the roleplay is public.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetRoleplayIsPublicAsync(Roleplay roleplay, bool isPublic)
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

            var guild = await _client.GetGuildAsync((ulong)roleplay.Server.DiscordID);
            if (guild is null)
            {
                return ModifyEntityResult.FromError("Failed to get a valid guild");
            }

            var updatePermissions = await _dedicatedChannels.UpdateParticipantPermissionsAsync(guild, roleplay);
            if (!updatePermissions.IsSuccess)
            {
                return updatePermissions;
            }

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets the roleplay currently active in the given channel.
        /// </summary>
        /// <param name="textChannel">The channel.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<Roleplay>> GetActiveRoleplayAsync(ITextChannel textChannel)
        {
            var getServer = await _servers.GetOrRegisterServerAsync(textChannel.Guild);
            if (!getServer.IsSuccess)
            {
                return RetrieveEntityResult<Roleplay>.FromError(getServer);
            }

            var server = getServer.Entity;

            var roleplay = await _roleplays.GetRoleplays(server).AsQueryable().FirstOrDefaultAsync
            (
                rp => rp.IsActive && rp.ActiveChannelID == (long)textChannel.Id
            );

            if (roleplay is null)
            {
                return RetrieveEntityResult<Roleplay>.FromError
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
        /// <param name="currentChannel">The channel the command was executed in.</param>
        /// <param name="guild">The guild the command was executed in.</param>
        /// <param name="roleplayOwner">The owner of the roleplay, if any.</param>
        /// <param name="roleplayName">The name of the roleplay, if any.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<Roleplay>> GetBestMatchingRoleplayAsync
        (
            ITextChannel currentChannel,
            IGuild guild,
            IUser? roleplayOwner,
            string? roleplayName
        )
        {
            if (roleplayOwner is null && roleplayName is null)
            {
                return await GetActiveRoleplayAsync(currentChannel);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guild);
            if (!getServer.IsSuccess)
            {
                return RetrieveEntityResult<Roleplay>.FromError(getServer);
            }

            var server = getServer.Entity;

            if (roleplayOwner is null)
            {
                return await _roleplays.GetNamedRoleplayAsync(roleplayName!, server);
            }

            if (roleplayName.IsNullOrWhitespace())
            {
                return await GetActiveRoleplayAsync(currentChannel);
            }

            var getOwner = await _users.GetOrRegisterUserAsync(roleplayOwner);
            if (!getOwner.IsSuccess)
            {
                return RetrieveEntityResult<Roleplay>.FromError(getOwner);
            }

            var owner = getOwner.Entity;

            return await _roleplays.GetUserRoleplayByNameAsync(server, owner, roleplayName);
        }

        /// <summary>
        /// Determines whether or not there is an active roleplay in the given channel.
        /// </summary>
        /// <param name="textChannel">The channel to check.</param>
        /// <returns>true if there is an active roleplay; otherwise, false.</returns>
        public async Task<bool> HasActiveRoleplayAsync(ITextChannel textChannel)
        {
            var getServer = await _servers.GetOrRegisterServerAsync(textChannel.Guild);
            if (!getServer.IsSuccess)
            {
                // TODO: Better return type
                return false;
            }

            var server = getServer.Entity;

            return await _roleplays.GetRoleplays(server).AsQueryable().AnyAsync
            (
                rp => rp.IsActive && rp.ActiveChannelID == (long)textChannel.Id
            );
        }

        /// <summary>
        /// Consumes a message, adding it to the active roleplay in its channel if the author is a participant.
        /// </summary>
        /// <param name="message">The received message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<ModifyEntityResult> ConsumeMessageAsync(IUserMessage message)
        {
            if (!(message.Channel is ITextChannel textChannel))
            {
                return ModifyEntityResult.FromError("The message did not come from a text channel.");
            }

            var result = await GetActiveRoleplayAsync(textChannel);
            if (!result.IsSuccess)
            {
                return ModifyEntityResult.FromError(result);
            }

            var roleplay = result.Entity;

            if (!roleplay.HasJoined(message.Author))
            {
                return ModifyEntityResult.FromError("The given message was not authored by a participant of the roleplay.");
            }

            var userNick = message.Author.Username;
            if (message.Author is IGuildUser guildUser && !string.IsNullOrEmpty(guildUser.Nickname))
            {
                userNick = guildUser.Nickname;
            }

            var getAuthor = await _users.GetOrRegisterUserAsync(message.Author);
            if (!getAuthor.IsSuccess)
            {
                return ModifyEntityResult.FromError(getAuthor);
            }

            var author = getAuthor.Entity;
            var roleplayMessage = UserMessage.FromDiscordMessage(author, message, userNick);

            return await _roleplays.AddOrUpdateMessageInRoleplayAsync(roleplay, roleplayMessage);
        }

        /// <summary>
        /// Transfers ownership of the given roleplay to the given user.
        /// </summary>
        /// <param name="newDiscordOwner">The new owner.</param>
        /// <param name="roleplay">The roleplay to transfer.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> TransferRoleplayOwnershipAsync
        (
            IGuildUser newDiscordOwner,
            Roleplay roleplay
        )
        {
            var getNewOwner = await _users.GetOrRegisterUserAsync(newDiscordOwner);
            if (!getNewOwner.IsSuccess)
            {
                return ModifyEntityResult.FromError(getNewOwner);
            }

            var newOwner = getNewOwner.Entity;
            return await _roleplays.TransferRoleplayOwnershipAsync(newOwner, roleplay);
        }
    }
}
