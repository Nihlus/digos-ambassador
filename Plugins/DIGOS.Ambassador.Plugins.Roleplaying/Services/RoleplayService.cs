﻿//
//  RoleplayService.cs
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
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Services
{
    /// <summary>
    /// Acts as an interface for accessing, enabling, and disabling ongoing roleplays.
    /// </summary>
    [PublicAPI]
    public sealed class RoleplayService
    {
        [NotNull] private readonly RoleplayingDatabaseContext _database;
        [NotNull] private readonly ServerService _servers;
        [NotNull] private readonly UserService _users;
        [NotNull] private readonly CommandService _commands;
        [NotNull] private readonly OwnedEntityService _ownedEntities;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayService"/> class.
        /// </summary>
        /// <param name="commands">The application's command service.</param>
        /// <param name="entityService">The application's owned entity service.</param>
        /// <param name="users">The user service.</param>
        /// <param name="servers">The server service.</param>
        /// <param name="database">The database.</param>
        public RoleplayService
        (
            [NotNull] CommandService commands,
            [NotNull] OwnedEntityService entityService,
            [NotNull] UserService users,
            [NotNull] ServerService servers,
            [NotNull] RoleplayingDatabaseContext database
        )
        {
            _commands = commands;
            _ownedEntities = entityService;
            _users = users;
            _servers = servers;
            _database = database;
        }

        /// <summary>
        /// Consumes a message, adding it to the active roleplay in its channel if the author is a participant.
        /// </summary>
        /// <param name="context">The message to consume.</param>
        /// <returns>A task that must be awaited.</returns>
        [NotNull]
        public async Task ConsumeMessageAsync([NotNull] ICommandContext context)
        {
            var result = await GetActiveRoleplayAsync(context.Channel);
            if (!result.IsSuccess)
            {
                return;
            }

            var roleplay = result.Entity;

            await AddToOrUpdateMessageInRoleplayAsync(roleplay, context.Message);
        }

        /// <summary>
        /// Creates a roleplay with the given parameters.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <param name="roleplayName">The name of the roleplay.</param>
        /// <param name="roleplaySummary">The summary of the roleplay.</param>
        /// <param name="isNSFW">Whether or not the roleplay is NSFW.</param>
        /// <param name="isPublic">Whether or not the roleplay is public.</param>
        /// <returns>A creation result which may or may not have been successful.</returns>
        [NotNull, ItemNotNull]
        public async Task<CreateEntityResult<Roleplay>> CreateRoleplayAsync
        (
            [NotNull] ICommandContext context,
            [NotNull] string roleplayName,
            [NotNull] string roleplaySummary,
            bool isNSFW,
            bool isPublic
        )
        {
            var getOwnerResult = await _users.GetOrRegisterUserAsync(context.User);
            if (!getOwnerResult.IsSuccess)
            {
                return CreateEntityResult<Roleplay>.FromError(getOwnerResult);
            }

            // Ensure the user is attached, so we don't create any conflicts.
            var owner = getOwnerResult.Entity;
            _database.Attach(owner);

            // Use a dummy name, since we'll be setting it useng the service.
            var roleplay = new Roleplay((long)context.Guild.Id, owner, string.Empty);

            var ownerParticipant = new RoleplayParticipant(roleplay, owner)
            {
                Status = ParticipantStatus.Joined
            };

            roleplay.ParticipatingUsers.Add(ownerParticipant);

            var setNameResult = await SetRoleplayNameAsync(context, roleplay, roleplayName);
            if (!setNameResult.IsSuccess)
            {
                return CreateEntityResult<Roleplay>.FromError(setNameResult);
            }

            var setSummaryResult = await SetRoleplaySummaryAsync(roleplay, roleplaySummary);
            if (!setSummaryResult.IsSuccess)
            {
                return CreateEntityResult<Roleplay>.FromError(setSummaryResult);
            }

            var setIsNSFWResult = await SetRoleplayIsNSFWAsync(roleplay, isNSFW);
            if (!setIsNSFWResult.IsSuccess)
            {
                return CreateEntityResult<Roleplay>.FromError(setIsNSFWResult);
            }

            var setIsPublicResult = await SetRoleplayIsPublicAsync(roleplay, isPublic);
            if (!setIsPublicResult.IsSuccess)
            {
                return CreateEntityResult<Roleplay>.FromError(setIsPublicResult);
            }

            _database.Roleplays.Update(roleplay);

            await _database.SaveChangesAsync();

            var roleplayResult = await GetUserRoleplayByNameAsync(context, context.Message.Author, roleplayName);
            if (!roleplayResult.IsSuccess)
            {
                return CreateEntityResult<Roleplay>.FromError(roleplayResult);
            }

            return CreateEntityResult<Roleplay>.FromSuccess(roleplayResult.Entity);
        }

        /// <summary>
        /// Deletes the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<DeleteEntityResult> DeleteRoleplayAsync(Roleplay roleplay)
        {
            _database.Roleplays.Remove(roleplay);

            await _database.SaveChangesAsync();
            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// Starts the given roleplay.
        /// </summary>
        /// <param name="context">The context in which to start the roleplay.</param>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> StartRoleplayAsync(ICommandContext context, Roleplay roleplay)
        {
            var getDedicatedChannelResult = await GetDedicatedRoleplayChannelAsync
            (
                context.Guild,
                roleplay
            );

            // Identify the channel to start the RP in. Preference is given to the roleplay's dedicated channel.
            ISocketMessageChannel channel;
            if (getDedicatedChannelResult.IsSuccess)
            {
                channel = (ISocketMessageChannel)getDedicatedChannelResult.Entity;
            }
            else
            {
                channel = (ISocketMessageChannel)context.Channel;
            }

            var isNsfwChannel = channel is ITextChannel textChannel && textChannel.IsNsfw;
            if (roleplay.IsNSFW && !isNsfwChannel)
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

            if (roleplay.ActiveChannelID != (long)channel.Id)
            {
                roleplay.ActiveChannelID = (long)channel.Id;
            }

            roleplay.IsActive = true;
            roleplay.LastUpdated = DateTime.Now;

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Adds a new message to the given roleplay, or edits it if there is an existing one.
        /// </summary>
        /// <param name="roleplay">The roleplay to modify.</param>
        /// <param name="message">The message to add or update.</param>
        /// <returns>A task wrapping the update action.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> AddToOrUpdateMessageInRoleplayAsync
        (
            [NotNull] Roleplay roleplay,
            [NotNull] IMessage message
        )
        {
            if (!roleplay.HasJoined(message.Author))
            {
                return ModifyEntityResult.FromError("The given message was not authored by a participant of the roleplay.");
            }

            var userNick = message.Author.Username;
            if (message.Author is SocketGuildUser guildUser && !string.IsNullOrEmpty(guildUser.Nickname))
            {
                userNick = guildUser.Nickname;
            }

            if (roleplay.Messages.Any(m => m.DiscordMessageID == (long)message.Id))
            {
                // Edit the existing message
                var existingMessage = roleplay.Messages.Find(m => m.DiscordMessageID == (long)message.Id);

                if (existingMessage.Contents.Equals(message.Content))
                {
                    return ModifyEntityResult.FromError("Nothing to do; message content match.");
                }

                existingMessage.Contents = message.Content;

                // Update roleplay timestamp
                roleplay.LastUpdated = DateTime.Now;

                await _database.SaveChangesAsync();
                return ModifyEntityResult.FromSuccess();
            }

            var roleplayMessage = UserMessage.FromDiscordMessage(message, userNick);
            roleplay.Messages.Add(roleplayMessage);

            // Update roleplay timestamp
            roleplay.LastUpdated = DateTime.Now;

            await _database.SaveChangesAsync();
            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// This method searches for the best matching roleplay given an owner and a name. If no owner is provided, then
        /// the global list is searched for a unique name. If neither are provided, the currently active roleplay is
        /// returned. If no match can be found, a failed result is returned.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="roleplayOwner">The owner of the roleplay, if any.</param>
        /// <param name="roleplayName">The name of the roleplay, if any.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<Roleplay>> GetBestMatchingRoleplayAsync
        (
            [NotNull] ICommandContext context,
            IUser? roleplayOwner,
            string? roleplayName
        )
        {
            if (roleplayOwner is null && roleplayName is null)
            {
                return await GetActiveRoleplayAsync(context.Channel);
            }

            if (roleplayOwner is null)
            {
                return await GetNamedRoleplayAsync(roleplayName, context.Guild);
            }

            if (roleplayName.IsNullOrWhitespace())
            {
                return await GetActiveRoleplayAsync(context.Channel);
            }

            return await GetUserRoleplayByNameAsync(context, roleplayOwner, roleplayName);
        }

        /// <summary>
        /// Gets a roleplay by its given name.
        /// </summary>
        /// <param name="roleplayName">The name of the roleplay.</param>
        /// <param name="guild">The guild that the search is scoped to.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<Roleplay>> GetNamedRoleplayAsync
        (
            [NotNull] string roleplayName,
            [NotNull] IGuild guild
        )
        {
            if (await _database.Roleplays.CountAsync(rp => string.Equals(rp.Name, roleplayName, StringComparison.OrdinalIgnoreCase)) > 1)
            {
                return RetrieveEntityResult<Roleplay>.FromError
                (
                    "There's more than one roleplay with that name. Please specify which user it belongs to."
                );
            }

            var roleplay = GetRoleplays(guild)
                .FirstOrDefault(rp => string.Equals(rp.Name, roleplayName, StringComparison.OrdinalIgnoreCase));

            if (roleplay is null)
            {
                return RetrieveEntityResult<Roleplay>.FromError("No roleplay with that name found.");
            }

            return RetrieveEntityResult<Roleplay>.FromSuccess(roleplay);
        }

        /// <summary>
        /// Gets the current active roleplay in the given channel.
        /// </summary>
        /// <param name="channel">The channel to get the roleplay from.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<Roleplay>> GetActiveRoleplayAsync
        (
            [NotNull] IMessageChannel channel
        )
        {
            var roleplay = await _database.Roleplays.FirstOrDefaultAsync
            (
                rp => rp.IsActive && rp.ActiveChannelID == (long)channel.Id
            );

            if (roleplay is null)
            {
                return RetrieveEntityResult<Roleplay>.FromError
                (
                    "There is no roleplay that is currently active in this channel."
                );
            }

            return RetrieveEntityResult<Roleplay>.FromSuccess(roleplay);
        }

        /// <summary>
        /// Determines whether or not there is an active roleplay in the given channel.
        /// </summary>
        /// <param name="channel">The channel to check.</param>
        /// <returns>true if there is an active roleplay; otherwise, false.</returns>
        [Pure, NotNull]
        public async Task<bool> HasActiveRoleplayAsync([NotNull] IChannel channel)
        {
            return await _database.Roleplays.AnyAsync(rp => rp.IsActive && rp.ActiveChannelID == (long)channel.Id);
        }

        /// <summary>
        /// Determines whether or not the given roleplay name is unique for a given user.
        /// </summary>
        /// <param name="user">The user to check.</param>
        /// <param name="roleplayName">The roleplay name to check.</param>
        /// <param name="guild">The guild to scope the roleplays to.</param>
        /// <returns>true if the name is unique; otherwise, false.</returns>
        [Pure, NotNull]
        public async Task<bool> IsRoleplayNameUniqueForUserAsync
        (
            [NotNull] User user,
            [NotNull] string roleplayName,
            [NotNull] IGuild guild
        )
        {
            var userRoleplays = GetUserRoleplays(user, guild);
            return await _ownedEntities.IsEntityNameUniqueForUserAsync(userRoleplays, roleplayName);
        }

        /// <summary>
        /// Get the roleplays owned by the given user.
        /// </summary>
        /// <param name="guild">The guild to scope the search to.</param>
        /// <returns>A queryable list of roleplays belonging to the user.</returns>
        [Pure, NotNull, ItemNotNull]
        public IQueryable<Roleplay> GetRoleplays(IGuild? guild = null)
        {
            if (guild is null)
            {
                return _database.Roleplays;
            }

            return _database.Roleplays
                .Where
                (
                    rp =>
                        rp.ServerID == (long)guild.Id
                );
        }

        /// <summary>
        /// Get the roleplays owned by the given user.
        /// </summary>
        /// <param name="user">The user to get the roleplays of.</param>
        /// <param name="guild">The guild that the search is scoped to.</param>
        /// <returns>A queryable list of roleplays belonging to the user.</returns>
        [Pure, NotNull, ItemNotNull]
        public IQueryable<Roleplay> GetUserRoleplays([NotNull] User user, [NotNull] IGuild guild)
        {
            return GetRoleplays(guild).Where
            (
                rp =>
                    rp.Owner == user
            );
        }

        /// <summary>
        /// Gets a roleplay belonging to a given user by a given name.
        /// </summary>
        /// <param name="context">The context of the user.</param>
        /// <param name="roleplayOwner">The user to get the roleplay from.</param>
        /// <param name="roleplayName">The name of the roleplay.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<Roleplay>> GetUserRoleplayByNameAsync
        (
            [NotNull] ICommandContext context,
            [NotNull] IUser roleplayOwner,
            [NotNull] string roleplayName
        )
        {
            var roleplay = await GetRoleplays(context.Guild)
            .FirstOrDefaultAsync
            (
                rp =>
                    rp.Name.Equals(roleplayName, StringComparison.OrdinalIgnoreCase) &&
                    rp.Owner.DiscordID == (long)roleplayOwner.Id
            );

            if (roleplay is null)
            {
                var isCurrentUser = context.Message.Author.Id == roleplayOwner.Id;
                var errorMessage = isCurrentUser
                    ? "You don't own a roleplay with that name."
                    : "The user doesn't own a roleplay with that name.";

                return RetrieveEntityResult<Roleplay>.FromError(errorMessage);
            }

            return RetrieveEntityResult<Roleplay>.FromSuccess(roleplay);
        }

        /// <summary>
        /// Kicks the given user from the given roleplay.
        /// </summary>
        /// <param name="context">The context of the user.</param>
        /// <param name="roleplay">The roleplay to remove the user from.</param>
        /// <param name="kickedUser">The user to remove from the roleplay.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> KickUserFromRoleplayAsync
        (
            [NotNull] ICommandContext context,
            [NotNull] Roleplay roleplay,
            [NotNull] IUser kickedUser
        )
        {
            if (!roleplay.HasJoined(kickedUser) && !roleplay.IsInvited(kickedUser))
            {
                return ModifyEntityResult.FromError("That user is neither invited to or a participant of the roleplay.");
            }

            if (!roleplay.HasJoined(kickedUser))
            {
                var removeUserResult = await RemoveUserFromRoleplayAsync(context, roleplay, kickedUser);
                if (!removeUserResult.IsSuccess)
                {
                    return removeUserResult;
                }
            }

            var participantEntry = roleplay.JoinedUsers.First(p => p.User.DiscordID == (long)kickedUser.Id);
            participantEntry.Status = ParticipantStatus.Kicked;

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Removes the given user from the given roleplay.
        /// </summary>
        /// <param name="context">The context of the user.</param>
        /// <param name="roleplay">The roleplay to remove the user from.</param>
        /// <param name="removedUser">The user to remove from the roleplay.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> RemoveUserFromRoleplayAsync
        (
            [NotNull] ICommandContext context,
            [NotNull] Roleplay roleplay,
            [NotNull] IUser removedUser
        )
        {
            var isCurrentUser = context.Message.Author.Id == removedUser.Id;
            if (!roleplay.HasJoined(removedUser))
            {
                var errorMessage = isCurrentUser
                    ? "You're not in that roleplay."
                    : "No matching user found in the roleplay.";

                return ModifyEntityResult.FromError(errorMessage);
            }

            if (roleplay.IsOwner(removedUser))
            {
                var errorMessage = isCurrentUser
                    ? "You can't leave a roleplay you own."
                    : "The owner of a roleplay can't be removed from it.";

                return ModifyEntityResult.FromError(errorMessage);
            }

            var participantEntry = roleplay.JoinedUsers.First(p => p.User.DiscordID == (long)removedUser.Id);
            participantEntry.Status = ParticipantStatus.None;

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Adds the given user to the given roleplay.
        /// </summary>
        /// <param name="context">The context of the user.</param>
        /// <param name="roleplay">The roleplay to add the user to.</param>
        /// <param name="newUser">The user to add to the roleplay.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<CreateEntityResult<RoleplayParticipant>> AddUserToRoleplayAsync
        (
            [NotNull] ICommandContext context,
            [NotNull] Roleplay roleplay,
            [NotNull] IUser newUser
        )
        {
            var isCurrentUser = context.Message.Author.Id == newUser.Id;
            if (roleplay.HasJoined(newUser))
            {
                var errorMessage = isCurrentUser
                    ? "You're already in that roleplay."
                    : "The user is already in that roleplay.";

                return CreateEntityResult<RoleplayParticipant>.FromError(errorMessage);
            }

            if (roleplay.IsKicked(newUser))
            {
                var errorMessage = isCurrentUser
                    ? "You've been kicked from that roleplay, and can't rejoin unless invited."
                    : "The user has been kicked from that roleplay, and can't rejoin unless invited.";

                return CreateEntityResult<RoleplayParticipant>.FromError(errorMessage);
            }

            // Check the invite list for nonpublic roleplays.
            if (!roleplay.IsPublic && !roleplay.IsInvited(newUser))
            {
                var errorMessage = isCurrentUser
                    ? "You haven't been invited to that roleplay."
                    : "The user hasn't been invited to that roleplay.";

                return CreateEntityResult<RoleplayParticipant>.FromError(errorMessage);
            }

            var participantEntry = roleplay.ParticipatingUsers.FirstOrDefault(p => p.User.DiscordID == (long)newUser.Id);
            if (participantEntry is null)
            {
                var getUserResult = await _users.GetOrRegisterUserAsync(newUser);
                if (!getUserResult.IsSuccess)
                {
                    return CreateEntityResult<RoleplayParticipant>.FromError(getUserResult);
                }

                var user = getUserResult.Entity;

                // Ensure the user is attached, so we don't create any conflicts.
                _database.Attach(user);
                participantEntry = new RoleplayParticipant(roleplay, user)
                {
                    Status = ParticipantStatus.Joined
                };

                roleplay.ParticipatingUsers.Add(participantEntry);
            }
            else
            {
                participantEntry.Status = ParticipantStatus.Joined;
            }

            await _database.SaveChangesAsync();

            return CreateEntityResult<RoleplayParticipant>.FromSuccess(participantEntry);
        }

        /// <summary>
        /// Invites the user to the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to invite the user to.</param>
        /// <param name="invitedUser">The user to invite.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> InviteUserAsync
        (
            [NotNull] Roleplay roleplay,
            [NotNull] IUser invitedUser
        )
        {
            if (roleplay.IsPublic && !roleplay.IsKicked(invitedUser))
            {
                return ModifyEntityResult.FromError("The roleplay is not set to private.");
            }

            if (roleplay.InvitedUsers.Any(p => p.User.DiscordID == (long)invitedUser.Id))
            {
                return ModifyEntityResult.FromError("The user has already been invited to that roleplay.");
            }

            // Remove the invited user from the kick list, if they're on it
            var participantEntry = roleplay.ParticipatingUsers.FirstOrDefault(p => p.User.DiscordID == (long)invitedUser.Id);
            if (participantEntry is null)
            {
                var getUserResult = await _users.GetOrRegisterUserAsync(invitedUser);
                if (!getUserResult.IsSuccess)
                {
                    return ModifyEntityResult.FromError(getUserResult);
                }

                var user = getUserResult.Entity;

                // Ensure the user is attached, so we don't create any conflicts.
                _database.Attach(user);
                participantEntry = new RoleplayParticipant(roleplay, user)
                {
                    Status = ParticipantStatus.Invited
                };

                roleplay.ParticipatingUsers.Add(participantEntry);
            }
            else
            {
                participantEntry.Status = ParticipantStatus.Invited;
            }

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Transfers ownership of the named roleplay to the specified user.
        /// </summary>
        /// <param name="newOwner">The new owner.</param>
        /// <param name="roleplay">The roleplay to transfer.</param>
        /// <param name="guild">The guild to scope the roleplays to.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> TransferRoleplayOwnershipAsync
        (
            [NotNull] User newOwner,
            [NotNull] Roleplay roleplay,
            [NotNull] IGuild guild
        )
        {
            var newOwnerRoleplays = GetUserRoleplays(newOwner, guild);
            return await _ownedEntities.TransferEntityOwnershipAsync
            (
                _database,
                newOwner,
                newOwnerRoleplays,
                roleplay
            );
        }

        /// <summary>
        /// Sets the name of the given roleplay.
        /// </summary>
        /// <param name="context">The context of the operation.</param>
        /// <param name="roleplay">The roleplay to set the name of.</param>
        /// <param name="newRoleplayName">The new name.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> SetRoleplayNameAsync
        (
            [NotNull] ICommandContext context,
            [NotNull] Roleplay roleplay,
            [NotNull] string newRoleplayName
        )
        {
            if (string.IsNullOrWhiteSpace(newRoleplayName))
            {
                return ModifyEntityResult.FromError("You need to provide a name.");
            }

            if (newRoleplayName.Contains("\""))
            {
                return ModifyEntityResult.FromError("The name may not contain double quotes.");
            }

            var isCurrentUser = context.Message.Author.Id == (ulong)roleplay.Owner.DiscordID;
            if (!await IsRoleplayNameUniqueForUserAsync(roleplay.Owner, newRoleplayName, context.Guild))
            {
                var errorMessage = isCurrentUser
                    ? "You already have a roleplay with that name."
                    : "The user already has a roleplay with that name.";

                return ModifyEntityResult.FromError(errorMessage);
            }

            var commandModule = _commands.Modules.First(m => m.Name == "roleplay");
            var validNameResult = _ownedEntities.IsEntityNameValid(commandModule.GetAllCommandNames(), newRoleplayName);
            if (!validNameResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(validNameResult);
            }

            roleplay.Name = newRoleplayName;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the summary of the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to set the summary of.</param>
        /// <param name="newRoleplaySummary">The new summary.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> SetRoleplaySummaryAsync
        (
            [NotNull] Roleplay roleplay,
            [NotNull] string newRoleplaySummary
        )
        {
            if (string.IsNullOrWhiteSpace(newRoleplaySummary))
            {
                return ModifyEntityResult.FromError("You need to provide a new summary.");
            }

            roleplay.Summary = newRoleplaySummary;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets whether or not a roleplay is NSFW.
        /// </summary>
        /// <param name="roleplay">The roleplay to set the value in.</param>
        /// <param name="isNSFW">The new value.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> SetRoleplayIsNSFWAsync
        (
            [NotNull] Roleplay roleplay,
            bool isNSFW
        )
        {
            if (roleplay.Messages.Count > 0 && roleplay.IsNSFW && !isNSFW)
            {
                return ModifyEntityResult.FromError("You can't mark a NSFW roleplay with messages in it as non-NSFW.");
            }

            roleplay.IsNSFW = isNSFW;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets whether or not a roleplay is public.
        /// </summary>
        /// <param name="roleplay">The roleplay to set the value in.</param>
        /// <param name="isPublic">The new value.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> SetRoleplayIsPublicAsync
        (
            [NotNull] Roleplay roleplay,
            bool isPublic
        )
        {
            roleplay.IsPublic = isPublic;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Creates a dedicated channel for the roleplay.
        /// </summary>
        /// <param name="context">The context in which the request was made.</param>
        /// <param name="roleplay">The roleplay to create the channel for.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<CreateEntityResult<IGuildChannel>> CreateDedicatedRoleplayChannelAsync
        (
            [NotNull] ICommandContext context,
            [NotNull] Roleplay roleplay
        )
        {
            var getServerResult = await _servers.GetOrRegisterServerAsync(context.Guild);
            if (!getServerResult.IsSuccess)
            {
                return CreateEntityResult<IGuildChannel>.FromError(getServerResult);
            }

            var server = getServerResult.Entity;

            if (!(await context.Guild.GetUserAsync(context.Client.CurrentUser.Id)).GuildPermissions.ManageChannels)
            {
                return CreateEntityResult<IGuildChannel>.FromError
                (
                    "I don't have permission to manage channels, so I can't create dedicated RP channels."
                );
            }

            var getExistingChannelResult = await GetDedicatedRoleplayChannelAsync(context.Guild, roleplay);
            if (getExistingChannelResult.IsSuccess)
            {
                return CreateEntityResult<IGuildChannel>.FromError
                (
                    "The roleplay already has a dedicated channel."
                );
            }

            var getSettingsResult = await GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettingsResult.IsSuccess)
            {
                return CreateEntityResult<IGuildChannel>.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;

            if (!(settings.DedicatedRoleplayChannelsCategory is null))
            {
                var categoryChannelCount = (await context.Guild.GetTextChannelsAsync())
                    .Count(c => c.CategoryId == (ulong)settings.DedicatedRoleplayChannelsCategory);

                if (categoryChannelCount >= 50)
                {
                    return CreateEntityResult<IGuildChannel>.FromError
                    (
                        "The server's roleplaying category has reached its maximum number of channels. Try " +
                        "contacting the server's owners and either removing some old roleplays or setting up " +
                        "a new category."
                    );
                }
            }

            var dedicatedChannel = await context.Guild.CreateTextChannelAsync
            (
                $"{roleplay.Name}-rp",
                properties =>
                {
                    properties.CategoryId = new Optional<ulong?>((ulong?)settings.DedicatedRoleplayChannelsCategory);
                    properties.IsNsfw = roleplay.IsNSFW;
                    properties.Topic = $"Dedicated roleplay channel for {roleplay.Name}.";
                }
            );

            roleplay.DedicatedChannelID = (long)dedicatedChannel.Id;

            // Set up permission overrides
            foreach (var participant in roleplay.ParticipatingUsers)
            {
                var discordUser = await context.Guild.GetUserAsync((ulong)participant.User.DiscordID);
                var basicPermissions = OverwritePermissions.InheritAll;

                await dedicatedChannel.AddPermissionOverwriteAsync(discordUser, basicPermissions);
            }

            var botDiscordUser = await context.Guild.GetUserAsync(context.Client.CurrentUser.Id);
            await SetDedicatedChannelWritabilityForUserAsync(dedicatedChannel, botDiscordUser, true);
            await SetDedicatedChannelVisibilityForUserAsync(dedicatedChannel, botDiscordUser, true);

            // Configure visibility for everyone
            // viewChannel starts off as deny, since starting or stopping the RP will set the correct permissions.
            var everyoneRole = context.Guild.EveryoneRole;
            var everyonePermissions = OverwritePermissions.InheritAll.Modify
            (
                readMessageHistory: roleplay.IsPublic ? PermValue.Allow : PermValue.Deny,
                viewChannel: PermValue.Deny,
                sendMessages: PermValue.Deny,
                addReactions: PermValue.Deny,
                sendTTSMessages: PermValue.Deny
            );

            await dedicatedChannel.AddPermissionOverwriteAsync(everyoneRole, everyonePermissions);

            await _database.SaveChangesAsync();
            return CreateEntityResult<IGuildChannel>.FromSuccess(dedicatedChannel);
        }

        /// <summary>
        /// Deletes the dedicated channel for the roleplay.
        /// </summary>
        /// <param name="guild">The context in which the request was made.</param>
        /// <param name="roleplay">The roleplay to delete the channel of.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> DeleteDedicatedRoleplayChannelAsync
        (
            [NotNull] IGuild guild,
            [NotNull] Roleplay roleplay)
        {
            if (roleplay.DedicatedChannelID is null)
            {
                return ModifyEntityResult.FromError
                (
                    "The roleplay doesn't have a dedicated channel."
                );
            }

            var getDedicatedChannelResult = await GetDedicatedRoleplayChannelAsync(guild, roleplay);
            if (getDedicatedChannelResult.IsSuccess)
            {
                await getDedicatedChannelResult.Entity.DeleteAsync();
            }

            roleplay.DedicatedChannelID = null;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets the channel dedicated to the given roleplay.
        /// </summary>
        /// <param name="guild">The guild that contains the channel.</param>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<IGuildChannel>> GetDedicatedRoleplayChannelAsync
        (
            [NotNull] IGuild guild,
            [NotNull] Roleplay roleplay
        )
        {
            if (!(roleplay.DedicatedChannelID is null))
            {
                var guildChannel = await guild.GetChannelAsync((ulong)roleplay.DedicatedChannelID.Value);
                if (!(guildChannel is null))
                {
                    return RetrieveEntityResult<IGuildChannel>.FromSuccess(guildChannel);
                }

                return RetrieveEntityResult<IGuildChannel>.FromError
                (
                    "Attempted to delete a channel, but it appears to have been deleted."
                );
            }

            return RetrieveEntityResult<IGuildChannel>.FromError
            (
                "The roleplay doesn't have a dedicated channel."
            );
        }

        /// <summary>
        /// Sets the writability of the given dedicated channel for the given user.
        /// </summary>
        /// <param name="dedicatedChannel">The roleplay's dedicated channel.</param>
        /// <param name="participant">The participant to grant access to.</param>
        /// <param name="isVisible">Whether or not the channel should be writable.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> SetDedicatedChannelWritabilityForUserAsync
        (
            [NotNull] IGuildChannel dedicatedChannel,
            [NotNull] IUser participant,
            bool isVisible
        )
        {
            var permissions = OverwritePermissions.InheritAll;
            var existingOverwrite = dedicatedChannel.GetPermissionOverwrite(participant);
            if (!(existingOverwrite is null))
            {
                permissions = existingOverwrite.Value;
            }

            permissions = permissions.Modify
            (
                sendMessages: isVisible ? PermValue.Allow : PermValue.Deny,
                addReactions: isVisible ? PermValue.Allow : PermValue.Deny,
                embedLinks: isVisible ? PermValue.Allow : PermValue.Deny,
                attachFiles: isVisible ? PermValue.Allow : PermValue.Deny,
                useExternalEmojis: isVisible ? PermValue.Allow : PermValue.Deny
            );

            await dedicatedChannel.AddPermissionOverwriteAsync(participant, permissions);

            // Ugly hack - there seems to be some kind of race condition on Discord's end.
            await Task.Delay(20);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the visibility of the given dedicated channel for the given user.
        /// </summary>
        /// <param name="dedicatedChannel">The roleplay's dedicated channel.</param>
        /// <param name="participant">The participant to grant access to.</param>
        /// <param name="isVisible">Whether or not the channel should be visible.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> SetDedicatedChannelVisibilityForUserAsync
        (
            [NotNull] IGuildChannel dedicatedChannel,
            [NotNull] IUser participant,
            bool isVisible
        )
        {
            var permissions = OverwritePermissions.InheritAll;
            var existingOverwrite = dedicatedChannel.GetPermissionOverwrite(participant);
            if (!(existingOverwrite is null))
            {
                permissions = existingOverwrite.Value;
            }

            permissions = permissions.Modify
            (
                readMessageHistory: isVisible ? PermValue.Allow : PermValue.Deny,
                viewChannel: isVisible ? PermValue.Allow : PermValue.Deny
            );

            await dedicatedChannel.AddPermissionOverwriteAsync(participant, permissions);

            // Ugly hack - there seems to be some kind of race condition on Discord's end.
            await Task.Delay(20);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the visibility of the given dedicated channel for the given user.
        /// </summary>
        /// <param name="dedicatedChannel">The roleplay's dedicated channel.</param>
        /// <param name="role">The role to grant access to.</param>
        /// <param name="isVisible">Whether or not the channel should be visible.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> SetDedicatedChannelVisibilityForRoleAsync
        (
            [NotNull] IGuildChannel dedicatedChannel,
            [NotNull] IRole role,
            bool isVisible
        )
        {
            var permissions = OverwritePermissions.InheritAll;
            var existingOverwrite = dedicatedChannel.GetPermissionOverwrite(role);
            if (!(existingOverwrite is null))
            {
                permissions = existingOverwrite.Value;
            }

            permissions = permissions.Modify
            (
                readMessageHistory: isVisible ? PermValue.Allow : PermValue.Deny,
                viewChannel: isVisible ? PermValue.Allow : PermValue.Deny
            );

            await dedicatedChannel.AddPermissionOverwriteAsync(role, permissions);

            // Ugly hack - there seems to be some kind of race condition on Discord's end.
            await Task.Delay(20);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Revokes the given roleplay participant access to the given roleplay channel.
        /// </summary>
        /// <param name="context">The context in which the request was made.</param>
        /// <param name="dedicatedChannel">The roleplay's dedicated channel.</param>
        /// <param name="participant">The participant to grant access to.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> RevokeUserDedicatedChannelAccessAsync
        (
            [NotNull] ICommandContext context,
            [NotNull] IGuildChannel dedicatedChannel,
            [NotNull] IUser participant
        )
        {
            if (!(await context.Guild.GetUserAsync(context.Client.CurrentUser.Id)).GuildPermissions.ManageChannels)
            {
                return ModifyEntityResult.FromError
                (
                    "I don't have permission to manage channels, so I can't change permissions on dedicated RP channels."
                );
            }

            var user = await context.Guild.GetUserAsync(participant.Id);
            if (user is null)
            {
                return ModifyEntityResult.FromError("User not found in guild.");
            }

            await dedicatedChannel.RemovePermissionOverwriteAsync(user);
            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets or creates a set of server-specific roleplaying settings for the given server.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<ServerRoleplaySettings>> GetOrCreateServerRoleplaySettingsAsync
        (
            [NotNull] Server server
        )
        {
            var existingSettings = await _database.ServerSettings.FirstOrDefaultAsync(s => s.Server == server);

            if (!(existingSettings is null))
            {
                return RetrieveEntityResult<ServerRoleplaySettings>.FromSuccess(existingSettings);
            }

            existingSettings = new ServerRoleplaySettings(server);

            _database.ServerSettings.Update(existingSettings);
            await _database.SaveChangesAsync();

            // Requery the database
            return await GetOrCreateServerRoleplaySettingsAsync(server);
        }

        /// <summary>
        /// Sets the channel category to use for dedicated roleplay channels.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="category">The category to use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> SetDedicatedRoleplayChannelCategoryAsync
        (
            [NotNull] Server server,
            ICategoryChannel? category
        )
        {
            var getSettingsResult = await GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettingsResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;

            settings.DedicatedRoleplayChannelsCategory = (long?)category?.Id;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the channel to use for archived roleplays.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="channel">The channel to use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> SetArchiveChannelAsync
        (
            [NotNull] Server server,
            ITextChannel? channel
        )
        {
            var getSettingsResult = await GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettingsResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;

            settings.ArchiveChannel = (long?)channel?.Id;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Refreshes the timestamp on the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<ModifyEntityResult> RefreshRoleplayAsync(Roleplay roleplay)
        {
            roleplay.LastUpdated = DateTime.Now;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }
    }
}
