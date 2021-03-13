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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Discord.Feedback.Errors;
using DIGOS.Ambassador.Plugins.Core.Extensions;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.Core;
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
        public RoleplayService(OwnedEntityService entityService, RoleplayingDatabaseContext database)
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
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A creation result which may or may not have been successful.</returns>
        public async Task<Result<Roleplay>> CreateRoleplayAsync
        (
            User owner,
            Server server,
            string roleplayName,
            string roleplaySummary,
            bool isNSFW,
            bool isPublic,
            CancellationToken ct = default
        )
        {
            owner = _database.NormalizeReference(owner);
            server = _database.NormalizeReference(server);

            // Use a dummy name, since we'll be setting it using the service.
            var roleplay = _database.CreateProxy<Roleplay>(server, owner, string.Empty, string.Empty);
            _database.Roleplays.Update(roleplay);

            var ownerParticipant = _database.CreateProxy<RoleplayParticipant>(roleplay, owner);
            _database.Update(ownerParticipant);

            ownerParticipant.Status = ParticipantStatus.Joined;
            roleplay.ParticipatingUsers.Add(ownerParticipant);

            var setNameResult = await SetRoleplayNameAsync(roleplay, roleplayName, ct);
            if (!setNameResult.IsSuccess)
            {
                return Result<Roleplay>.FromError(setNameResult);
            }

            var setSummaryResult = await SetRoleplaySummaryAsync(roleplay, roleplaySummary, ct);
            if (!setSummaryResult.IsSuccess)
            {
                return Result<Roleplay>.FromError(setSummaryResult);
            }

            roleplay.IsNSFW = isNSFW;
            roleplay.IsPublic = isPublic;

            await _database.SaveChangesAsync(ct);

            return roleplay;
        }

        /// <summary>
        /// Deletes the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<Result> DeleteRoleplayAsync
        (
            Roleplay roleplay,
            CancellationToken ct = default
        )
        {
            _database.Roleplays.Remove(roleplay);
            await _database.SaveChangesAsync(ct);

            return Result.FromSuccess();
        }

        /// <summary>
        /// Starts the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="channelID">The Discord ID of the channel.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> StartRoleplayAsync
        (
            Roleplay roleplay,
            Snowflake channelID,
            CancellationToken ct = default
        )
        {
            if (roleplay.IsActive && roleplay.ActiveChannelID == channelID)
            {
                return new UserError("The roleplay is already running.");
            }

            roleplay.ActiveChannelID = channelID;
            roleplay.IsActive = true;
            roleplay.LastUpdated = DateTime.Now;

            await _database.SaveChangesAsync(ct);

            return Result.FromSuccess();
        }

        /// <summary>
        /// Adds a new message to the given roleplay, or edits it if there is an existing one.
        /// </summary>
        /// <param name="roleplay">The roleplay to modify.</param>
        /// <param name="author">The author of the message.</param>
        /// <param name="messageID">The ID of the message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        /// <param name="authorNickname">The nickname of the author at the time of sending.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A task wrapping the update action.</returns>
        public async Task<Result> AddOrUpdateMessageInRoleplayAsync
        (
            Roleplay roleplay,
            User author,
            Snowflake messageID,
            DateTimeOffset timestamp,
            string authorNickname,
            string contents,
            CancellationToken ct = default
        )
        {
            author = _database.NormalizeReference(author);

            var existingMessage = roleplay.Messages.FirstOrDefault(m => m.DiscordMessageID == messageID);
            if (!(existingMessage is null))
            {
                // Edit the existing message
                if (existingMessage.Contents.Equals(contents))
                {
                    return new UserError("Nothing to do; message content match.");
                }

                existingMessage.Contents = contents;

                // Update roleplay timestamp
                roleplay.LastUpdated = DateTime.Now;

                await _database.SaveChangesAsync(ct);
                return Result.FromSuccess();
            }

            var newMessage = _database.CreateProxy<UserMessage>(author, messageID, timestamp, authorNickname, contents);
            _database.Update(newMessage);

            roleplay.Messages.Add(newMessage);

            // Update roleplay timestamp
            roleplay.LastUpdated = DateTime.Now;

            await _database.SaveChangesAsync(ct);
            return Result.FromSuccess();
        }

        /// <summary>
        /// Gets a roleplay by its given name.
        /// </summary>
        /// <param name="roleplayName">The name of the roleplay.</param>
        /// <param name="server">The server that the search is scoped to.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<Roleplay>> GetNamedRoleplayAsync
        (
            string roleplayName,
            Server server,
            CancellationToken ct = default
        )
        {
            server = _database.NormalizeReference(server);

            var roleplays = await _database.Roleplays.ServerScopedServersideQueryAsync
            (
                server,
                q => q.Where(rp => string.Equals(rp.Name.ToLower(), roleplayName.ToLower())),
                ct
            );

            var enumeratedRoleplays = roleplays.ToList();

            if (enumeratedRoleplays.Count > 1)
            {
                return new UserError
                (
                    "There's more than one roleplay with that name. Please specify which user it belongs to."
                );
            }

            var roleplay = enumeratedRoleplays.SingleOrDefault();

            if (!(roleplay is null))
            {
                return roleplay;
            }

            return new UserError("No roleplay with that name found.");
        }

        /// <summary>
        /// Determines whether or not the given roleplay name is unique for a given user.
        /// </summary>
        /// <param name="user">The user to check.</param>
        /// <param name="roleplayName">The roleplay name to check.</param>
        /// <param name="server">The server to scope the roleplays to.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>true if the name is unique; otherwise, false.</returns>
        [Pure]
        public async Task<bool> IsRoleplayNameUniqueForUserAsync
        (
            User user,
            string roleplayName,
            Server server,
            CancellationToken ct = default
        )
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            var userRoleplays = await GetUserRoleplaysAsync(user, server, ct);
            return _ownedEntities.IsEntityNameUniqueForUser(userRoleplays.ToList(), roleplayName);
        }

        /// <summary>
        /// Gets the roleplays on the given server.
        /// </summary>
        /// <param name="server">The server to scope the search to.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A queryable list of roleplays on the given server.</returns>
        [Pure]
        public async Task<IReadOnlyList<Roleplay>> GetRoleplaysAsync
        (
            Server server,
            CancellationToken ct = default
        )
        {
            server = _database.NormalizeReference(server);

            return await _database.Roleplays.ServerScopedServersideQueryAsync(server, q => q, ct);
        }

        /// <summary>
        /// Get the roleplays owned by the given user.
        /// </summary>
        /// <param name="user">The user to get the roleplays of.</param>
        /// <param name="server">The server that the search is scoped to.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A queryable list of roleplays belonging to the user.</returns>
        [Pure]
        public Task<IReadOnlyList<Roleplay>> GetUserRoleplaysAsync
        (
            User user,
            Server server,
            CancellationToken ct = default
        )
        {
            user = _database.NormalizeReference(user);
            server = _database.NormalizeReference(server);

            return _database.Roleplays.UserScopedServersideQueryAsync(user, server, q => q, ct);
        }

        /// <summary>
        /// Gets a roleplay belonging to a given user by a given name.
        /// </summary>
        /// <param name="server">The server of the user.</param>
        /// <param name="roleplayOwner">The user to get the roleplay from.</param>
        /// <param name="roleplayName">The name of the roleplay.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public async Task<Result<Roleplay>> GetUserRoleplayByNameAsync
        (
            Server server,
            User roleplayOwner,
            string roleplayName,
            CancellationToken ct = default
        )
        {
            roleplayOwner = _database.NormalizeReference(roleplayOwner);
            server = _database.NormalizeReference(server);

            var userRoleplays = await _database.Roleplays.UserScopedServersideQueryAsync
            (
                roleplayOwner,
                server,
                q => q.Where(rp => rp.Name.ToLower().Equals(roleplayName.ToLower())),
                ct
            );

            var roleplay = userRoleplays.SingleOrDefault();

            if (!(roleplay is null))
            {
                return roleplay;
            }

            return new UserError("You don't own a roleplay with that name.");
        }

        /// <summary>
        /// Kicks the given user from the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to remove the user from.</param>
        /// <param name="kickedUser">The user to remove from the roleplay.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<Result> KickUserFromRoleplayAsync
        (
            Roleplay roleplay,
            User kickedUser,
            CancellationToken ct = default
        )
        {
            kickedUser = _database.NormalizeReference(kickedUser);

            if (!roleplay.HasJoined(kickedUser) && !roleplay.IsInvited(kickedUser))
            {
                return new UserError
                (
                    "That user is neither invited to or a participant of the roleplay."
                );
            }

            if (!roleplay.HasJoined(kickedUser))
            {
                var removeUserResult = await RemoveUserFromRoleplayAsync(roleplay, kickedUser, ct);
                if (!removeUserResult.IsSuccess)
                {
                    return removeUserResult;
                }
            }

            var participantEntry = roleplay.JoinedUsers.First(p => p.User == kickedUser);
            participantEntry.Status = ParticipantStatus.Kicked;

            await _database.SaveChangesAsync(ct);
            return Result.FromSuccess();
        }

        /// <summary>
        /// Removes the given user from the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to remove the user from.</param>
        /// <param name="removedUser">The user to remove from the roleplay.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<Result> RemoveUserFromRoleplayAsync
        (
            Roleplay roleplay,
            User removedUser,
            CancellationToken ct = default
        )
        {
            removedUser = _database.NormalizeReference(removedUser);

            if (!roleplay.HasJoined(removedUser))
            {
                return new UserError("No matching user found in the roleplay.");
            }

            if (roleplay.IsOwner(removedUser))
            {
                return new UserError("The owner of a roleplay can't be removed from it.");
            }

            var participantEntry = roleplay.JoinedUsers.First(p => p.User == removedUser);
            roleplay.ParticipatingUsers.Remove(participantEntry);

            await _database.SaveChangesAsync(ct);
            return Result.FromSuccess();
        }

        /// <summary>
        /// Adds the given user to the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to add the user to.</param>
        /// <param name="newUser">The user to add to the roleplay.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<Result<RoleplayParticipant>> AddUserToRoleplayAsync
        (
            Roleplay roleplay,
            User newUser,
            CancellationToken ct = default
        )
        {
            newUser = _database.NormalizeReference(newUser);

            if (roleplay.HasJoined(newUser))
            {
                return new UserError("The user is already in that roleplay.");
            }

            if (roleplay.IsKicked(newUser))
            {
                return new UserError
                (
                    "The user has been kicked from that roleplay, and can't rejoin unless invited."
                );
            }

            // Check the invite list for nonpublic roleplays.
            if (!roleplay.IsPublic && !roleplay.IsInvited(newUser))
            {
                return new UserError
                (
                    "The user hasn't been invited to that roleplay."
                );
            }

            var participantEntry = roleplay.ParticipatingUsers.FirstOrDefault(p => p.User == newUser);
            if (participantEntry is null)
            {
                participantEntry = _database.CreateProxy<RoleplayParticipant>(roleplay, newUser);
                _database.Update(participantEntry);

                participantEntry.Status = ParticipantStatus.Joined;
                roleplay.ParticipatingUsers.Add(participantEntry);
            }
            else
            {
                participantEntry.Status = ParticipantStatus.Joined;
            }

            await _database.SaveChangesAsync(ct);
            return participantEntry;
        }

        /// <summary>
        /// Invites the user to the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to invite the user to.</param>
        /// <param name="invitedUser">The user to invite.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<Result> InviteUserToRoleplayAsync
        (
            Roleplay roleplay,
            User invitedUser,
            CancellationToken ct = default
        )
        {
            invitedUser = _database.NormalizeReference(invitedUser);

            if (roleplay.InvitedUsers.Any(p => p.User.DiscordID == invitedUser.DiscordID))
            {
                return new UserError("The user has already been invited to that roleplay.");
            }

            // Remove the invited user from the kick list, if they're on it
            var participantEntry = roleplay.ParticipatingUsers.FirstOrDefault
            (
                p => p.User.DiscordID == invitedUser.DiscordID
            );

            if (participantEntry is null)
            {
                participantEntry = _database.CreateProxy<RoleplayParticipant>(roleplay, invitedUser);
                _database.Update(participantEntry);

                participantEntry.Status = ParticipantStatus.Invited;
                roleplay.ParticipatingUsers.Add(participantEntry);
            }
            else
            {
                participantEntry.Status = ParticipantStatus.Invited;
            }

            await _database.SaveChangesAsync(ct);
            return Result.FromSuccess();
        }

        /// <summary>
        /// Transfers ownership of the named roleplay to the specified user.
        /// </summary>
        /// <param name="newOwner">The new owner.</param>
        /// <param name="roleplay">The roleplay to transfer.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<Result> TransferRoleplayOwnershipAsync
        (
            User newOwner,
            Roleplay roleplay,
            CancellationToken ct = default
        )
        {
            newOwner = _database.NormalizeReference(newOwner);

            var newOwnerRoleplays = await GetUserRoleplaysAsync(newOwner, roleplay.Server, ct);
            var transferResult = _ownedEntities.TransferEntityOwnership
            (
                newOwner,
                newOwnerRoleplays.ToList(),
                roleplay
            );

            if (transferResult.IsSuccess)
            {
                await _database.SaveChangesAsync(ct);
            }

            return transferResult;
        }

        /// <summary>
        /// Sets the name of the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to set the name of.</param>
        /// <param name="newRoleplayName">The new name.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetRoleplayNameAsync
        (
            Roleplay roleplay,
            string newRoleplayName,
            CancellationToken ct = default
        )
        {
            if (string.IsNullOrWhiteSpace(newRoleplayName))
            {
                return new UserError("You need to provide a name.");
            }

            if (newRoleplayName.Contains("\""))
            {
                return new UserError("The name may not contain double quotes.");
            }

            if (!await IsRoleplayNameUniqueForUserAsync(roleplay.Owner, newRoleplayName, roleplay.Server, ct))
            {
                return new UserError("You already have a roleplay with that name.");
            }

            roleplay.Name = newRoleplayName;

            await _database.SaveChangesAsync(ct);
            return Result.FromSuccess();
        }

        /// <summary>
        /// Sets the summary of the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to set the summary of.</param>
        /// <param name="newRoleplaySummary">The new summary.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetRoleplaySummaryAsync
        (
            Roleplay roleplay,
            string newRoleplaySummary,
            CancellationToken ct = default
        )
        {
            if (string.IsNullOrWhiteSpace(newRoleplaySummary))
            {
                return new UserError("You need to provide a new summary.");
            }

            roleplay.Summary = newRoleplaySummary;

            await _database.SaveChangesAsync(ct);
            return Result.FromSuccess();
        }

        /// <summary>
        /// Sets whether or not a roleplay is NSFW.
        /// </summary>
        /// <param name="roleplay">The roleplay to set the value in.</param>
        /// <param name="isNSFW">The new value.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetRoleplayIsNSFWAsync
        (
            Roleplay roleplay,
            bool isNSFW,
            CancellationToken ct = default
        )
        {
            if (roleplay.IsNSFW == isNSFW)
            {
                return new UserError($"The roleplay is already {(isNSFW ? "NSFW" : "SFW")}.");
            }

            if (roleplay.Messages.Count > 0 && roleplay.IsNSFW && !isNSFW)
            {
                return new UserError("You can't mark a NSFW roleplay with messages in it as non-NSFW.");
            }

            roleplay.IsNSFW = isNSFW;

            await _database.SaveChangesAsync(ct);
            return Result.FromSuccess();
        }

        /// <summary>
        /// Sets whether or not a roleplay is public.
        /// </summary>
        /// <param name="roleplay">The roleplay to set the value in.</param>
        /// <param name="isPublic">The new value.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetRoleplayIsPublicAsync
        (
            Roleplay roleplay,
            bool isPublic,
            CancellationToken ct = default
        )
        {
            if (roleplay.IsPublic == isPublic)
            {
                return new UserError($"The roleplay is already {(isPublic ? "public" : "private")}.");
            }

            roleplay.IsPublic = isPublic;

            await _database.SaveChangesAsync(ct);
            return Result.FromSuccess();
        }

        /// <summary>
        /// Refreshes the timestamp on the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<Result> RefreshRoleplayAsync
        (
            Roleplay roleplay,
            CancellationToken ct = default
        )
        {
            roleplay.LastUpdated = DateTime.Now;

            await _database.SaveChangesAsync(ct);
            return Result.FromSuccess();
        }

        /// <summary>
        /// Stops the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to stop.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<Result> StopRoleplayAsync
        (
            Roleplay roleplay,
            CancellationToken ct = default
        )
        {
            if (!roleplay.IsActive)
            {
                return new UserError("The roleplay is not active.");
            }

            roleplay.IsActive = false;
            roleplay.ActiveChannelID = null;

            await _database.SaveChangesAsync(ct);
            return Result.FromSuccess();
        }
    }
}
