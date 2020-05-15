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
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Services
{
    /// <summary>
    /// Acts as an interface for accessing, enabling, and disabling ongoing roleplays.
    /// </summary>
    public sealed class RoleplayService
    {
        private readonly RoleplayingDatabaseContext _database;
        private readonly OwnedEntityService _ownedEntities;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayService"/> class.
        /// </summary>
        /// <param name="entityService">The application's owned entity service.</param>
        /// <param name="database">The database.</param>
        public RoleplayService
        (
            OwnedEntityService entityService,
            RoleplayingDatabaseContext database
        )
        {
            _ownedEntities = entityService;
            _database = database;
        }

        /// <summary>
        /// Creates a roleplay with the given parameters.
        /// </summary>
        /// <param name="owner">The user that owns the roleplay.</param>
        /// <param name="server">The server that the roleplay is associated with.</param>
        /// <param name="roleplayName">The name of the roleplay.</param>
        /// <param name="roleplaySummary">The summary of the roleplay.</param>
        /// <param name="isNSFW">Whether or not the roleplay is NSFW.</param>
        /// <param name="isPublic">Whether or not the roleplay is public.</param>
        /// <returns>A creation result which may or may not have been successful.</returns>
        public async Task<CreateEntityResult<Roleplay>> CreateRoleplayAsync
        (
            User owner,
            Server server,
            string roleplayName,
            string roleplaySummary,
            bool isNSFW,
            bool isPublic
        )
        {
            // Use a dummy name, since we'll be setting it using the service.
            var roleplay = _database.CreateProxy<Roleplay>(server, owner, string.Empty, string.Empty);
            _database.Attach(roleplay);

            var ownerParticipant = _database.CreateProxy<RoleplayParticipant>(roleplay, owner);
            ownerParticipant.Status = ParticipantStatus.Joined;

            roleplay.ParticipatingUsers.Add(ownerParticipant);

            var setNameResult = await SetRoleplayNameAsync(roleplay, roleplayName);
            if (!setNameResult.IsSuccess)
            {
                return CreateEntityResult<Roleplay>.FromError(setNameResult);
            }

            var setSummaryResult = await SetRoleplaySummaryAsync(roleplay, roleplaySummary);
            if (!setSummaryResult.IsSuccess)
            {
                return CreateEntityResult<Roleplay>.FromError(setSummaryResult);
            }

            roleplay.IsNSFW = isNSFW;
            roleplay.IsPublic = isPublic;

            _database.Roleplays.Update(roleplay);

            await _database.SaveChangesAsync();

            return roleplay;
        }

        /// <summary>
        /// Deletes the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<DeleteEntityResult> DeleteRoleplayAsync(Roleplay roleplay)
        {
            _database.Roleplays.Remove(roleplay);

            await _database.SaveChangesAsync();
            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// Starts the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="channelID">The Discord ID of the channel.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> StartRoleplayAsync(Roleplay roleplay, long channelID)
        {
            if (roleplay.IsActive && roleplay.ActiveChannelID == channelID)
            {
                return ModifyEntityResult.FromError("The roleplay is already running.");
            }

            roleplay.ActiveChannelID = channelID;
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
        public async Task<ModifyEntityResult> AddOrUpdateMessageInRoleplayAsync(Roleplay roleplay, UserMessage message)
        {
            var existingMessage = roleplay.Messages.FirstOrDefault(m => m.DiscordMessageID == message.DiscordMessageID);
            if (!(existingMessage is null))
            {
                // Edit the existing message
                if (existingMessage.Contents.Equals(message.Contents))
                {
                    return ModifyEntityResult.FromError("Nothing to do; message content match.");
                }

                existingMessage.Contents = message.Contents;

                // Update roleplay timestamp
                roleplay.LastUpdated = DateTime.Now;

                await _database.SaveChangesAsync();
                return ModifyEntityResult.FromSuccess();
            }

            roleplay.Messages.Add(message);

            // Update roleplay timestamp
            roleplay.LastUpdated = DateTime.Now;

            await _database.SaveChangesAsync();
            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets a roleplay by its given name.
        /// </summary>
        /// <param name="roleplayName">The name of the roleplay.</param>
        /// <param name="server">The server that the search is scoped to.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<Roleplay>> GetNamedRoleplayAsync(string roleplayName, Server server)
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

            var roleplay = GetRoleplays(server)
                .FirstOrDefault(rp => string.Equals(rp.Name.ToLower(), roleplayName.ToLower()));

            if (roleplay is null)
            {
                return RetrieveEntityResult<Roleplay>.FromError("No roleplay with that name found.");
            }

            return RetrieveEntityResult<Roleplay>.FromSuccess(roleplay);
        }

        /// <summary>
        /// Determines whether or not the given roleplay name is unique for a given user.
        /// </summary>
        /// <param name="user">The user to check.</param>
        /// <param name="roleplayName">The roleplay name to check.</param>
        /// <param name="server">The server to scope the roleplays to.</param>
        /// <returns>true if the name is unique; otherwise, false.</returns>
        [Pure]
        public async Task<bool> IsRoleplayNameUniqueForUserAsync
        (
            User user,
            string roleplayName,
            Server server
        )
        {
            var userRoleplays = GetUserRoleplays(user, server);
            return await _ownedEntities.IsEntityNameUniqueForUserAsync(userRoleplays, roleplayName);
        }

        /// <summary>
        /// Gets the roleplays on the given server.
        /// </summary>
        /// <param name="server">The server to scope the search to.</param>
        /// <returns>A queryable list of roleplays on the given server.</returns>
        [Pure, ItemNotNull]
        public IQueryable<Roleplay> GetRoleplays(Server? server = null)
        {
            if (server is null)
            {
                return _database.Roleplays;
            }

            return _database.Roleplays.AsQueryable()
            .Where
            (
                rp =>
                    rp.Server == server
            );
        }

        /// <summary>
        /// Get the roleplays owned by the given user.
        /// </summary>
        /// <param name="user">The user to get the roleplays of.</param>
        /// <param name="server">The server that the search is scoped to.</param>
        /// <returns>A queryable list of roleplays belonging to the user.</returns>
        [Pure, ItemNotNull]
        public IQueryable<Roleplay> GetUserRoleplays(User user, Server server)
        {
            return GetRoleplays(server).Where
            (
                rp =>
                    rp.Owner == user
            );
        }

        /// <summary>
        /// Gets a roleplay belonging to a given user by a given name.
        /// </summary>
        /// <param name="server">The server of the user.</param>
        /// <param name="roleplayOwner">The user to get the roleplay from.</param>
        /// <param name="roleplayName">The name of the roleplay.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, ItemNotNull]
        public async Task<RetrieveEntityResult<Roleplay>> GetUserRoleplayByNameAsync
        (
            Server server,
            User roleplayOwner,
            string roleplayName
        )
        {
            var roleplay = await GetRoleplays(server)
            .FirstOrDefaultAsync
            (
                rp =>
                    rp.Name.ToLower().Equals(roleplayName.ToLower()) &&
                    rp.Owner == roleplayOwner
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
        public async Task<ModifyEntityResult> KickUserFromRoleplayAsync(Roleplay roleplay, User kickedUser)
        {
            if (!roleplay.HasJoined(kickedUser) && !roleplay.IsInvited(kickedUser))
            {
                return ModifyEntityResult.FromError
                (
                    "That user is neither invited to or a participant of the roleplay."
                );
            }

            if (!roleplay.HasJoined(kickedUser))
            {
                var removeUserResult = await RemoveUserFromRoleplayAsync(roleplay, kickedUser);
                if (!removeUserResult.IsSuccess)
                {
                    return ModifyEntityResult.FromError(removeUserResult);
                }
            }

            var participantEntry = roleplay.JoinedUsers.First(p => p.User == kickedUser);
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
        public async Task<DeleteEntityResult> RemoveUserFromRoleplayAsync(Roleplay roleplay, User removedUser)
        {
            if (!roleplay.HasJoined(removedUser))
            {
                return DeleteEntityResult.FromError("No matching user found in the roleplay.");
            }

            if (roleplay.IsOwner(removedUser))
            {
                return DeleteEntityResult.FromError("The owner of a roleplay can't be removed from it.");
            }

            var participantEntry = roleplay.JoinedUsers.First(p => p.User == removedUser);
            roleplay.ParticipatingUsers.Remove(participantEntry);

            await _database.SaveChangesAsync();

            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// Adds the given user to the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to add the user to.</param>
        /// <param name="newUser">The user to add to the roleplay.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<RoleplayParticipant>> AddUserToRoleplayAsync
        (
            Roleplay roleplay,
            User newUser
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
                return CreateEntityResult<RoleplayParticipant>.FromError
                (
                    "The user hasn't been invited to that roleplay."
                );
            }

            var participantEntry = roleplay.ParticipatingUsers.FirstOrDefault(p => p.User == newUser);
            if (participantEntry is null)
            {
                participantEntry = _database.CreateProxy<RoleplayParticipant>(roleplay, newUser);
                participantEntry.Status = ParticipantStatus.Joined;

                roleplay.ParticipatingUsers.Add(participantEntry);
            }
            else
            {
                participantEntry.Status = ParticipantStatus.Joined;
            }

            await _database.SaveChangesAsync();

            return participantEntry;
        }

        /// <summary>
        /// Invites the user to the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to invite the user to.</param>
        /// <param name="invitedUser">The user to invite.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> InviteUserToRoleplayAsync(Roleplay roleplay, User invitedUser)
        {
            if (roleplay.InvitedUsers.Any(p => p.User.DiscordID == invitedUser.DiscordID))
            {
                return ModifyEntityResult.FromError("The user has already been invited to that roleplay.");
            }

            // Remove the invited user from the kick list, if they're on it
            var participantEntry = roleplay.ParticipatingUsers.FirstOrDefault
            (
                p => p.User.DiscordID == invitedUser.DiscordID
            );

            if (participantEntry is null)
            {
                participantEntry = _database.CreateProxy<RoleplayParticipant>(roleplay, invitedUser);
                participantEntry.Status = ParticipantStatus.Invited;

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
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> TransferRoleplayOwnershipAsync(User newOwner, Roleplay roleplay)
        {
            var newOwnerRoleplays = GetUserRoleplays(newOwner, roleplay.Server);
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
        /// <param name="roleplay">The roleplay to set the name of.</param>
        /// <param name="newRoleplayName">The new name.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetRoleplayNameAsync(Roleplay roleplay, string newRoleplayName)
        {
            if (string.IsNullOrWhiteSpace(newRoleplayName))
            {
                return ModifyEntityResult.FromError("You need to provide a name.");
            }

            if (newRoleplayName.Contains("\""))
            {
                return ModifyEntityResult.FromError("The name may not contain double quotes.");
            }

            if (!await IsRoleplayNameUniqueForUserAsync(roleplay.Owner, newRoleplayName, roleplay.Server))
            {
                return ModifyEntityResult.FromError("You already have a roleplay with that name.");
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
        public async Task<ModifyEntityResult> SetRoleplaySummaryAsync(Roleplay roleplay, string newRoleplaySummary)
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
        public async Task<ModifyEntityResult> SetRoleplayIsNSFWAsync(Roleplay roleplay, bool isNSFW)
        {
            if (roleplay.IsNSFW == isNSFW)
            {
                return ModifyEntityResult.FromError($"The roleplay is already {(isNSFW ? "NSFW" : "SFW")}.");
            }

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
        public async Task<ModifyEntityResult> SetRoleplayIsPublicAsync(Roleplay roleplay, bool isPublic)
        {
            if (roleplay.IsPublic == isPublic)
            {
                return ModifyEntityResult.FromError($"The roleplay is already {(isPublic ? "public" : "private")}.");
            }

            roleplay.IsPublic = isPublic;
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
