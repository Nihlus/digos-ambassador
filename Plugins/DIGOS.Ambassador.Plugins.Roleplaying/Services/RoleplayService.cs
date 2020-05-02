//
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
        private readonly IDiscordClient _client;
        private readonly RoleplayingDatabaseContext _database;
        private readonly ServerService _servers;
        private readonly UserService _users;
        private readonly CommandService _commands;
        private readonly OwnedEntityService _ownedEntities;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayService"/> class.
        /// </summary>
        /// <param name="client">The application's Discord client.</param>
        /// <param name="commands">The application's command service.</param>
        /// <param name="entityService">The application's owned entity service.</param>
        /// <param name="users">The user service.</param>
        /// <param name="servers">The server service.</param>
        /// <param name="database">The database.</param>
        public RoleplayService
        (
            IDiscordClient client,
            CommandService commands,
            OwnedEntityService entityService,
            UserService users,
            ServerService servers,
            RoleplayingDatabaseContext database
        )
        {
            _client = client;
            _commands = commands;
            _ownedEntities = entityService;
            _users = users;
            _servers = servers;
            _database = database;
        }

        /// <summary>
        /// Consumes a message, adding it to the active roleplay in its channel if the author is a participant.
        /// </summary>
        /// <param name="message">The received message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ConsumeMessageAsync(IMessage message)
        {
            var result = await GetActiveRoleplayAsync(message.Channel);
            if (!result.IsSuccess)
            {
                return;
            }

            var roleplay = result.Entity;

            await AddToOrUpdateMessageInRoleplayAsync(roleplay, message);
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
        [ItemNotNull]
        public async Task<CreateEntityResult<Roleplay>> CreateRoleplayAsync
        (
            ICommandContext context,
            string roleplayName,
            string roleplaySummary,
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

            // Use a dummy name, since we'll be setting it using the service.
            var roleplay = new Roleplay((long)context.Guild.Id, owner, string.Empty);

            var ownerParticipant = new RoleplayParticipant(roleplay, owner)
            {
                Status = ParticipantStatus.Joined
            };

            roleplay.ParticipatingUsers.Add(ownerParticipant);

            var setNameResult = await SetRoleplayNameAsync(context.Guild, roleplay, roleplayName);
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

            var roleplayResult = await GetUserRoleplayByNameAsync(context.Guild, context.User, roleplayName);
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
        [ItemNotNull]
        public async Task<DeleteEntityResult> DeleteRoleplayAsync(Roleplay roleplay)
        {
            _database.Roleplays.Remove(roleplay);

            await _database.SaveChangesAsync();
            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// Starts the given roleplay.
        /// </summary>
        /// <param name="currentChannel">The channel the command was executed in.</param>
        /// <param name="guild">The guild the command was executed in.</param>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> StartRoleplayAsync
        (
            ITextChannel currentChannel,
            IGuild guild,
            Roleplay roleplay
        )
        {
            var getDedicatedChannelResult = await GetDedicatedRoleplayChannelAsync
            (
                guild,
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
                channel = (ISocketMessageChannel)currentChannel;
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
        [ItemNotNull]
        public async Task<ModifyEntityResult> AddToOrUpdateMessageInRoleplayAsync
        (
            Roleplay roleplay,
            IMessage message
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
                var existingMessage = roleplay.Messages.First(m => m.DiscordMessageID == (long)message.Id);

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
        /// <param name="currentChannel">The channel the command was executed in.</param>
        /// <param name="guild">The guild the command was executed in.</param>
        /// <param name="roleplayOwner">The owner of the roleplay, if any.</param>
        /// <param name="roleplayName">The name of the roleplay, if any.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, ItemNotNull]
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

            if (roleplayOwner is null)
            {
                return await GetNamedRoleplayAsync(roleplayName!, guild);
            }

            if (roleplayName.IsNullOrWhitespace())
            {
                return await GetActiveRoleplayAsync(currentChannel);
            }

            return await GetUserRoleplayByNameAsync(guild, roleplayOwner, roleplayName);
        }

        /// <summary>
        /// Gets a roleplay by its given name.
        /// </summary>
        /// <param name="roleplayName">The name of the roleplay.</param>
        /// <param name="guild">The guild that the search is scoped to.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, ItemNotNull]
        public async Task<RetrieveEntityResult<Roleplay>> GetNamedRoleplayAsync
        (
            string roleplayName,
            IGuild guild
        )
        {
            if
            (
                await _database.Roleplays.AsQueryable()
                    .CountAsync(rp => string.Equals(rp.Name.ToLower(), roleplayName.ToLower())) > 1
            )
            {
                return RetrieveEntityResult<Roleplay>.FromError
                (
                    "There's more than one roleplay with that name. Please specify which user it belongs to."
                );
            }

            var roleplay = GetRoleplays(guild)
                .FirstOrDefault(rp => string.Equals(rp.Name.ToLower(), roleplayName.ToLower()));

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
        [Pure, ItemNotNull]
        public async Task<RetrieveEntityResult<Roleplay>> GetActiveRoleplayAsync
        (
            IMessageChannel channel
        )
        {
            var roleplay = await _database.Roleplays.AsQueryable().FirstOrDefaultAsync
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
        [Pure]
        public async Task<bool> HasActiveRoleplayAsync(IChannel channel)
        {
            return await _database.Roleplays.AsQueryable().AnyAsync
            (
                rp => rp.IsActive && rp.ActiveChannelID == (long)channel.Id
            );
        }

        /// <summary>
        /// Determines whether or not the given roleplay name is unique for a given user.
        /// </summary>
        /// <param name="user">The user to check.</param>
        /// <param name="roleplayName">The roleplay name to check.</param>
        /// <param name="guild">The guild to scope the roleplays to.</param>
        /// <returns>true if the name is unique; otherwise, false.</returns>
        [Pure]
        public async Task<bool> IsRoleplayNameUniqueForUserAsync
        (
            User user,
            string roleplayName,
            IGuild guild
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
        [Pure, ItemNotNull]
        public IQueryable<Roleplay> GetRoleplays(IGuild? guild = null)
        {
            if (guild is null)
            {
                return _database.Roleplays;
            }

            return _database.Roleplays.AsQueryable()
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
        [Pure, ItemNotNull]
        public IQueryable<Roleplay> GetUserRoleplays(User user, IGuild guild)
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
        /// <param name="guild">The guild of the user.</param>
        /// <param name="roleplayOwner">The user to get the roleplay from.</param>
        /// <param name="roleplayName">The name of the roleplay.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, ItemNotNull]
        public async Task<RetrieveEntityResult<Roleplay>> GetUserRoleplayByNameAsync
        (
            IGuild guild,
            IUser roleplayOwner,
            string roleplayName
        )
        {
            var roleplay = await GetRoleplays(guild)
            .FirstOrDefaultAsync
            (
                rp =>
                    rp.Name.ToLower().Equals(roleplayName.ToLower()) &&
                    rp.Owner.DiscordID == (long)roleplayOwner.Id
            );

            if (roleplay is null)
            {
                return RetrieveEntityResult<Roleplay>.FromError("You don't own a roleplay with that name.");
            }

            return RetrieveEntityResult<Roleplay>.FromSuccess(roleplay);
        }

        /// <summary>
        /// Kicks the given user from the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to remove the user from.</param>
        /// <param name="kickedUser">The user to remove from the roleplay.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> KickUserFromRoleplayAsync
        (
            Roleplay roleplay,
            IUser kickedUser
        )
        {
            if (!roleplay.HasJoined(kickedUser) && !roleplay.IsInvited(kickedUser))
            {
                return ModifyEntityResult.FromError("That user is neither invited to or a participant of the roleplay.");
            }

            if (!roleplay.HasJoined(kickedUser))
            {
                var removeUserResult = await RemoveUserFromRoleplayAsync(roleplay, kickedUser);
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
        /// <param name="roleplay">The roleplay to remove the user from.</param>
        /// <param name="removedUser">The user to remove from the roleplay.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> RemoveUserFromRoleplayAsync
        (
            Roleplay roleplay,
            IUser removedUser
        )
        {
            if (!roleplay.HasJoined(removedUser))
            {
                return ModifyEntityResult.FromError("No matching user found in the roleplay.");
            }

            if (roleplay.IsOwner(removedUser))
            {
                return ModifyEntityResult.FromError("The owner of a roleplay can't be removed from it.");
            }

            var participantEntry = roleplay.JoinedUsers.First(p => p.User.DiscordID == (long)removedUser.Id);
            participantEntry.Status = ParticipantStatus.None;

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Adds the given user to the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to add the user to.</param>
        /// <param name="newUser">The user to add to the roleplay.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<CreateEntityResult<RoleplayParticipant>> AddUserToRoleplayAsync
        (
            Roleplay roleplay,
            IUser newUser
        )
        {
            if (roleplay.HasJoined(newUser))
            {
                return CreateEntityResult<RoleplayParticipant>.FromError("The user is already in that roleplay.");
            }

            if (roleplay.IsKicked(newUser))
            {
                return CreateEntityResult<RoleplayParticipant>.FromError
                (
                    "The user has been kicked from that roleplay, and can't rejoin unless invited."
                );
            }

            // Check the invite list for nonpublic roleplays.
            if (!roleplay.IsPublic && !roleplay.IsInvited(newUser))
            {
                return CreateEntityResult<RoleplayParticipant>.FromError("The user hasn't been invited to that roleplay.");
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
        [ItemNotNull]
        public async Task<ModifyEntityResult> InviteUserAsync
        (
            Roleplay roleplay,
            IUser invitedUser
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
        [ItemNotNull]
        public async Task<ModifyEntityResult> TransferRoleplayOwnershipAsync
        (
            User newOwner,
            Roleplay roleplay,
            IGuild guild
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
        /// <param name="guild">The guild the roleplay is on.</param>
        /// <param name="roleplay">The roleplay to set the name of.</param>
        /// <param name="newRoleplayName">The new name.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetRoleplayNameAsync
        (
            IGuild guild,
            Roleplay roleplay,
            string newRoleplayName
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

            if (!await IsRoleplayNameUniqueForUserAsync(roleplay.Owner, newRoleplayName, guild))
            {
                return ModifyEntityResult.FromError("You already have a roleplay with that name.");
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
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetRoleplaySummaryAsync
        (
            Roleplay roleplay,
            string newRoleplaySummary
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
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetRoleplayIsNSFWAsync
        (
            Roleplay roleplay,
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
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetRoleplayIsPublicAsync
        (
            Roleplay roleplay,
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
        /// <param name="guild">The guild in which the request was made.</param>
        /// <param name="roleplay">The roleplay to create the channel for.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<CreateEntityResult<ITextChannel>> CreateDedicatedRoleplayChannelAsync
        (
            IGuild guild,
            Roleplay roleplay
        )
        {
            var getServerResult = await _servers.GetOrRegisterServerAsync(guild);
            if (!getServerResult.IsSuccess)
            {
                return CreateEntityResult<ITextChannel>.FromError(getServerResult);
            }

            var server = getServerResult.Entity;

            if (!(await guild.GetUserAsync(_client.CurrentUser.Id)).GuildPermissions.ManageChannels)
            {
                return CreateEntityResult<ITextChannel>.FromError
                (
                    "I don't have permission to manage channels, so I can't create dedicated RP channels."
                );
            }

            var getExistingChannelResult = await GetDedicatedRoleplayChannelAsync(guild, roleplay);
            if (getExistingChannelResult.IsSuccess)
            {
                return CreateEntityResult<ITextChannel>.FromError
                (
                    "The roleplay already has a dedicated channel."
                );
            }

            var getSettingsResult = await GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettingsResult.IsSuccess)
            {
                return CreateEntityResult<ITextChannel>.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;

            if (!(settings.DedicatedRoleplayChannelsCategory is null))
            {
                var categoryChannelCount = (await guild.GetTextChannelsAsync())
                    .Count(c => c.CategoryId == (ulong)settings.DedicatedRoleplayChannelsCategory);

                if (categoryChannelCount >= 50)
                {
                    return CreateEntityResult<ITextChannel>.FromError
                    (
                        "The server's roleplaying category has reached its maximum number of channels. Try " +
                        "contacting the server's owners and either removing some old roleplays or setting up " +
                        "a new category."
                    );
                }
            }

            Optional<ulong?> categoryId;
            if (settings.DedicatedRoleplayChannelsCategory is null)
            {
                categoryId = null;
            }
            else
            {
                categoryId = (ulong?)settings.DedicatedRoleplayChannelsCategory;
            }

            var dedicatedChannel = await guild.CreateTextChannelAsync
            (
                $"{roleplay.Name}-rp",
                properties =>
                {
                    properties.CategoryId = categoryId;
                    properties.IsNsfw = roleplay.IsNSFW;
                    properties.Topic = $"Dedicated roleplay channel for {roleplay.Name}.";
                }
            );

            roleplay.DedicatedChannelID = (long)dedicatedChannel.Id;

            // Set up permission overrides
            foreach (var participant in roleplay.ParticipatingUsers)
            {
                var discordUser = await guild.GetUserAsync((ulong)participant.User.DiscordID);
                var basicPermissions = OverwritePermissions.InheritAll;

                await dedicatedChannel.AddPermissionOverwriteAsync(discordUser, basicPermissions);
            }

            var botDiscordUser = await guild.GetUserAsync(_client.CurrentUser.Id);
            await SetDedicatedChannelWritabilityForUserAsync(dedicatedChannel, botDiscordUser, true);
            await SetDedicatedChannelVisibilityForUserAsync(dedicatedChannel, botDiscordUser, true);

            // Configure visibility for everyone
            // viewChannel starts off as deny, since starting or stopping the RP will set the correct permissions.
            var everyoneRole = guild.EveryoneRole;
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
            return CreateEntityResult<ITextChannel>.FromSuccess(dedicatedChannel);
        }

        /// <summary>
        /// Deletes the dedicated channel for the roleplay.
        /// </summary>
        /// <param name="guild">The context in which the request was made.</param>
        /// <param name="roleplay">The roleplay to delete the channel of.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> DeleteDedicatedRoleplayChannelAsync
        (
            IGuild guild,
            Roleplay roleplay)
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
        [ItemNotNull]
        public async Task<RetrieveEntityResult<IGuildChannel>> GetDedicatedRoleplayChannelAsync
        (
            IGuild guild,
            Roleplay roleplay
        )
        {
            if (roleplay.DedicatedChannelID is null)
            {
                return RetrieveEntityResult<IGuildChannel>.FromError
                (
                    "The roleplay doesn't have a dedicated channel."
                );
            }

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

        /// <summary>
        /// Sets the writability of the given dedicated channel for the given user.
        /// </summary>
        /// <param name="dedicatedChannel">The roleplay's dedicated channel.</param>
        /// <param name="participant">The participant to grant access to.</param>
        /// <param name="isVisible">Whether or not the channel should be writable.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetDedicatedChannelWritabilityForUserAsync
        (
            IGuildChannel dedicatedChannel,
            IUser participant,
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
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetDedicatedChannelVisibilityForUserAsync
        (
            IGuildChannel dedicatedChannel,
            IUser participant,
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
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetDedicatedChannelVisibilityForRoleAsync
        (
            IGuildChannel dedicatedChannel,
            IRole role,
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
        /// <param name="guild">The guild in which the request was made.</param>
        /// <param name="dedicatedChannel">The roleplay's dedicated channel.</param>
        /// <param name="participant">The participant to grant access to.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> RevokeUserDedicatedChannelAccessAsync
        (
            IGuild guild,
            IGuildChannel dedicatedChannel,
            IUser participant
        )
        {
            if (!(await guild.GetUserAsync(_client.CurrentUser.Id)).GuildPermissions.ManageChannels)
            {
                return ModifyEntityResult.FromError
                (
                    "I don't have permission to manage channels, so I can't change permissions on dedicated RP channels."
                );
            }

            var user = await guild.GetUserAsync(participant.Id);
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
        [ItemNotNull]
        public async Task<RetrieveEntityResult<ServerRoleplaySettings>> GetOrCreateServerRoleplaySettingsAsync
        (
            Server server
        )
        {
            var existingSettings = await _database.ServerSettings.AsQueryable().FirstOrDefaultAsync
            (
                s => s.Server == server
            );

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
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetDedicatedRoleplayChannelCategoryAsync
        (
            Server server,
            ICategoryChannel? category
        )
        {
            var getSettingsResult = await GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettingsResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;

            long? categoryId;
            if (category?.Id is null)
            {
                categoryId = null;
            }
            else
            {
                categoryId = (long)category.Id;
            }

            settings.DedicatedRoleplayChannelsCategory = categoryId;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the channel to use for archived roleplays.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="channel">The channel to use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [ItemNotNull]
        public async Task<ModifyEntityResult> SetArchiveChannelAsync
        (
            Server server,
            ITextChannel? channel
        )
        {
            var getSettingsResult = await GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettingsResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;

            long? channelId;
            if (channel?.Id is null)
            {
                channelId = null;
            }
            else
            {
                channelId = (long)channel.Id;
            }

            settings.ArchiveChannel = channelId;
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

        /// <summary>
        /// Stops the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to stop.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<ModifyEntityResult> StopRoleplayAsync(Roleplay roleplay)
        {
            if (!roleplay.IsActive)
            {
                return ModifyEntityResult.FromError("The roleplay is not active.");
            }

            roleplay.IsActive = false;
            roleplay.ActiveChannelID = null;

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }
    }
}
