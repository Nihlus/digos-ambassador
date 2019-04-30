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
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Roleplaying;
using DIGOS.Ambassador.Extensions;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Services
{
    /// <summary>
    /// Acts as an interface for accessing, enabling, and disabling ongoing roleplays.
    /// </summary>
    public class RoleplayService
    {
        private readonly CommandService Commands;

        private readonly OwnedEntityService OwnedEntities;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayService"/> class.
        /// </summary>
        /// <param name="commands">The application's command service.</param>
        /// <param name="entityService">The application's owned entity service.</param>
        public RoleplayService(CommandService commands, OwnedEntityService entityService)
        {
            this.Commands = commands;
            this.OwnedEntities = entityService;
        }

        /// <summary>
        /// Consumes a message, adding it to the active roleplay in its channel if the author is a participant.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="context">The message to consume.</param>
        /// <returns>A task that must be awaited.</returns>
        public async Task ConsumeMessageAsync([NotNull] GlobalInfoContext db, [NotNull] ICommandContext context)
        {
            var result = await GetActiveRoleplayAsync(db, context.Channel);
            if (!result.IsSuccess)
            {
                return;
            }

            var roleplay = result.Entity;

            await AddToOrUpdateMessageInRoleplayAsync(db, roleplay, context.Message);
        }

        /// <summary>
        /// Creates a roleplay with the given parameters.
        /// </summary>
        /// <param name="db">The database where the roleplays are stored.</param>
        /// <param name="context">The context of the command.</param>
        /// <param name="roleplayName">The name of the roleplay.</param>
        /// <param name="roleplaySummary">The summary of the roleplay.</param>
        /// <param name="isNSFW">Whether or not the roleplay is NSFW.</param>
        /// <param name="isPublic">Whether or not the roleplay is public.</param>
        /// <returns>A creation result which may or may not have been successful.</returns>
        public async Task<CreateEntityResult<Roleplay>> CreateRoleplayAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] SocketCommandContext context,
            [NotNull] string roleplayName,
            [NotNull] string roleplaySummary,
            bool isNSFW,
            bool isPublic)
        {
            var getOwnerResult = await db.GetOrRegisterUserAsync(context.User);
            if (!getOwnerResult.IsSuccess)
            {
                return CreateEntityResult<Roleplay>.FromError(getOwnerResult);
            }

            var owner = getOwnerResult.Entity;

            var roleplay = new Roleplay
            {
                Owner = owner,
                ServerID = (long)context.Guild.Id,
                IsActive = false,
                ActiveChannelID = null,
                ParticipatingUsers = new List<RoleplayParticipant>(),
                Messages = new List<UserMessage>()
            };

            roleplay.ParticipatingUsers.Add(new RoleplayParticipant(roleplay, owner, ParticipantStatus.Joined));

            var setNameResult = await SetRoleplayNameAsync(db, context, roleplay, roleplayName);
            if (!setNameResult.IsSuccess)
            {
                return CreateEntityResult<Roleplay>.FromError(setNameResult);
            }

            var setSummaryResult = await SetRoleplaySummaryAsync(db, roleplay, roleplaySummary);
            if (!setSummaryResult.IsSuccess)
            {
                return CreateEntityResult<Roleplay>.FromError(setSummaryResult);
            }

            var setIsNSFWResult = await SetRoleplayIsNSFWAsync(db, roleplay, isNSFW);
            if (!setIsNSFWResult.IsSuccess)
            {
                return CreateEntityResult<Roleplay>.FromError(setIsNSFWResult);
            }

            var setIsPublicResult = await SetRoleplayIsPublicAsync(db, roleplay, isPublic);
            if (!setIsPublicResult.IsSuccess)
            {
                return CreateEntityResult<Roleplay>.FromError(setIsPublicResult);
            }

            await db.Roleplays.AddAsync(roleplay);
            await db.SaveChangesAsync();

            var roleplayResult = await GetUserRoleplayByNameAsync(db, context, context.Message.Author, roleplayName);
            if (!roleplayResult.IsSuccess)
            {
                return CreateEntityResult<Roleplay>.FromError(roleplayResult);
            }

            return CreateEntityResult<Roleplay>.FromSuccess(roleplayResult.Entity);
        }

        /// <summary>
        /// Adds a new message to the given roleplay, or edits it if there is an existing one.
        /// </summary>
        /// <param name="db">The database where the roleplays are stored.</param>
        /// <param name="roleplay">The roleplay to modify.</param>
        /// <param name="message">The message to add or update.</param>
        /// <returns>A task wrapping the update action.</returns>
        public async Task<ModifyEntityResult> AddToOrUpdateMessageInRoleplayAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] Roleplay roleplay,
            [NotNull] IMessage message
        )
        {
            if (!roleplay.HasJoined(message.Author))
            {
                return ModifyEntityResult.FromError(CommandError.Unsuccessful, "The given message was not authored by a participant of the roleplay.");
            }

            string userNick = message.Author.Username;
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
                    return ModifyEntityResult.FromError(CommandError.Unsuccessful, "Nothing to do; message content match.");
                }

                existingMessage.Contents = message.Content;

                await db.SaveChangesAsync();
                return ModifyEntityResult.FromSuccess();
            }

            var roleplayMessage = UserMessage.FromDiscordMessage(message, userNick);
            roleplay.Messages.Add(roleplayMessage);

            await db.SaveChangesAsync();
            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// This method searches for the best matching roleplay given an owner and a name. If no owner is provided, then
        /// the global list is searched for a unique name. If neither are provided, the currently active roleplay is
        /// returned. If no match can be found, a failed result is returned.
        /// </summary>
        /// <param name="db">The database where the roleplays are stored.</param>
        /// <param name="context">The command context.</param>
        /// <param name="roleplayOwner">The owner of the roleplay, if any.</param>
        /// <param name="roleplayName">The name of the roleplay, if any.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public async Task<RetrieveEntityResult<Roleplay>> GetBestMatchingRoleplayAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] ICommandContext context,
            [CanBeNull] IUser roleplayOwner,
            [CanBeNull] string roleplayName
        )
        {
            if (roleplayOwner is null && roleplayName is null)
            {
                return await GetActiveRoleplayAsync(db, context.Channel);
            }

            if (roleplayOwner is null)
            {
                return await GetNamedRoleplayAsync(db, roleplayName, context.Guild);
            }

            if (roleplayName.IsNullOrWhitespace())
            {
                return await GetActiveRoleplayAsync(db, context.Channel);
            }

            return await GetUserRoleplayByNameAsync(db, context, roleplayOwner, roleplayName);
        }

        /// <summary>
        /// Gets a roleplay by its given name.
        /// </summary>
        /// <param name="db">The database context where the data is stored.</param>
        /// <param name="roleplayName">The name of the roleplay.</param>
        /// <param name="guild">The guild that the search is scoped to.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public async Task<RetrieveEntityResult<Roleplay>> GetNamedRoleplayAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] string roleplayName,
            [NotNull] IGuild guild
        )
        {
            if (await db.Roleplays.CountAsync(rp => string.Equals(rp.Name, roleplayName, StringComparison.OrdinalIgnoreCase)) > 1)
            {
                return RetrieveEntityResult<Roleplay>.FromError
                (
                    CommandError.MultipleMatches,
                    "There's more than one roleplay with that name. Please specify which user it belongs to."
                );
            }

            var roleplay = GetRoleplays(db, guild)
                .FirstOrDefault(rp => string.Equals(rp.Name, roleplayName, StringComparison.OrdinalIgnoreCase));

            if (roleplay is null)
            {
                return RetrieveEntityResult<Roleplay>.FromError(CommandError.ObjectNotFound, "No roleplay with that name found.");
            }

            return RetrieveEntityResult<Roleplay>.FromSuccess(roleplay);
        }

        /// <summary>
        /// Gets the current active roleplay in the given channel.
        /// </summary>
        /// <param name="db">The database where the roleplays are stored.</param>
        /// <param name="channel">The channel to get the roleplay from.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public async Task<RetrieveEntityResult<Roleplay>> GetActiveRoleplayAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] IMessageChannel channel
        )
        {
            var roleplay = await db.Roleplays.FirstOrDefaultAsync
            (
                rp => rp.IsActive && rp.ActiveChannelID == (long)channel.Id
            );

            if (roleplay is null)
            {
                return RetrieveEntityResult<Roleplay>.FromError
                (
                    CommandError.ObjectNotFound, "There is no roleplay that is currently active in this channel."
                );
            }

            return RetrieveEntityResult<Roleplay>.FromSuccess(roleplay);
        }

        /// <summary>
        /// Determines whether or not there is an active roleplay in the given channel.
        /// </summary>
        /// <param name="db">The database where the roleplays are stored.</param>
        /// <param name="channel">The channel to check.</param>
        /// <returns>true if there is an active roleplay; otherwise, false.</returns>
        [Pure]
        public async Task<bool> HasActiveRoleplayAsync([NotNull] GlobalInfoContext db, [NotNull] IChannel channel)
        {
            return await db.Roleplays.AnyAsync(rp => rp.IsActive && rp.ActiveChannelID == (long)channel.Id);
        }

        /// <summary>
        /// Determines whether or not the given roleplay name is unique for a given user.
        /// </summary>
        /// <param name="db">The database where the roleplays are stored.</param>
        /// <param name="discordUser">The user to check.</param>
        /// <param name="roleplayName">The roleplay name to check.</param>
        /// <param name="guild">The guild to scope the roleplays to.</param>
        /// <returns>true if the name is unique; otherwise, false.</returns>
        [Pure]
        public async Task<bool> IsRoleplayNameUniqueForUserAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] IUser discordUser,
            [NotNull] string roleplayName,
            [NotNull] IGuild guild
        )
        {
            var userRoleplays = GetUserRoleplays(db, discordUser, guild);
            return await this.OwnedEntities.IsEntityNameUniqueForUserAsync(userRoleplays, roleplayName);
        }

        /// <summary>
        /// Get the roleplays owned by the given user.
        /// </summary>
        /// <param name="db">The database where the roleplays are stored.</param>
        /// <param name="guild">The guild to scope the search to.</param>
        /// <returns>A queryable list of roleplays belonging to the user.</returns>
        [Pure]
        [NotNull]
        [ItemNotNull]
        public IQueryable<Roleplay> GetRoleplays([NotNull] GlobalInfoContext db, [NotNull] IGuild guild)
        {
            return db.Roleplays
                .Where
                (
                    rp =>
                        rp.ServerID == (long)guild.Id
                );
        }

        /// <summary>
        /// Get the roleplays owned by the given user.
        /// </summary>
        /// <param name="db">The database where the roleplays are stored.</param>
        /// <param name="discordUser">The user to get the roleplays of.</param>
        /// <param name="guild">The guild that the search is scoped to.</param>
        /// <returns>A queryable list of roleplays belonging to the user.</returns>
        [Pure]
        [NotNull]
        [ItemNotNull]
        public IQueryable<Roleplay> GetUserRoleplays([NotNull] GlobalInfoContext db, [NotNull] IUser discordUser, [NotNull] IGuild guild)
        {
            return GetRoleplays(db, guild)
                .Where
                (
                    rp =>
                        rp.Owner.DiscordID == (long)discordUser.Id
                );
        }

        /// <summary>
        /// Gets a roleplay belonging to a given user by a given name.
        /// </summary>
        /// <param name="db">The database where the roleplays are stored.</param>
        /// <param name="context">The context of the user.</param>
        /// <param name="roleplayOwner">The user to get the roleplay from.</param>
        /// <param name="roleplayName">The name of the roleplay.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public async Task<RetrieveEntityResult<Roleplay>> GetUserRoleplayByNameAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] ICommandContext context,
            [NotNull] IUser roleplayOwner,
            [NotNull] string roleplayName
        )
        {
            var roleplay = await GetRoleplays(db, context.Guild)
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

                return RetrieveEntityResult<Roleplay>.FromError(CommandError.ObjectNotFound, errorMessage);
            }

            return RetrieveEntityResult<Roleplay>.FromSuccess(roleplay);
        }

        /// <summary>
        /// Kicks the given user from the given roleplay.
        /// </summary>
        /// <param name="db">The database where the roleplays are stored.</param>
        /// <param name="context">The context of the user.</param>
        /// <param name="roleplay">The roleplay to remove the user from.</param>
        /// <param name="kickedUser">The user to remove from the roleplay.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<ExecuteResult> KickUserFromRoleplayAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] SocketCommandContext context,
            [NotNull] Roleplay roleplay,
            [NotNull] IUser kickedUser
        )
        {
            if (!roleplay.HasJoined(kickedUser) && !roleplay.IsInvited(kickedUser))
            {
                return ExecuteResult.FromError(CommandError.ObjectNotFound, "That user is neither invited to or a participant of the roleplay.");
            }

            if (!roleplay.HasJoined(kickedUser))
            {
                var removeUserResult = await RemoveUserFromRoleplayAsync(db, context, roleplay, kickedUser);
                if (!removeUserResult.IsSuccess)
                {
                    return removeUserResult;
                }
            }

            var participantEntry = roleplay.JoinedUsers.First(p => p.User.DiscordID == (long)kickedUser.Id);
            participantEntry.Status = ParticipantStatus.Kicked;

            await db.SaveChangesAsync();

            return ExecuteResult.FromSuccess();
        }

        /// <summary>
        /// Removes the given user from the given roleplay.
        /// </summary>
        /// <param name="db">The database where the roleplays are stored.</param>
        /// <param name="context">The context of the user.</param>
        /// <param name="roleplay">The roleplay to remove the user from.</param>
        /// <param name="removedUser">The user to remove from the roleplay.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<ExecuteResult> RemoveUserFromRoleplayAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] SocketCommandContext context,
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

                return ExecuteResult.FromError(CommandError.Unsuccessful, errorMessage);
            }

            if (roleplay.IsOwner(removedUser))
            {
                var errorMessage = isCurrentUser
                    ? "You can't leave a roleplay you own."
                    : "The owner of a roleplay can't be removed from it.";

                return ExecuteResult.FromError(CommandError.Unsuccessful, errorMessage);
            }

            var participantEntry = roleplay.JoinedUsers.First(p => p.User.DiscordID == (long)removedUser.Id);
            participantEntry.Status = ParticipantStatus.None;

            await db.SaveChangesAsync();

            return ExecuteResult.FromSuccess();
        }

        /// <summary>
        /// Adds the given user to the given roleplay.
        /// </summary>
        /// <param name="db">The database where the roleplays are stored.</param>
        /// <param name="context">The context of the user.</param>
        /// <param name="roleplay">The roleplay to add the user to.</param>
        /// <param name="newUser">The user to add to the roleplay.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<RoleplayParticipant>> AddUserToRoleplayAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] SocketCommandContext context,
            [NotNull] Roleplay roleplay,
            [NotNull] IUser newUser
        )
        {
            var isCurrentUser = context.Message.Author.Id == newUser.Id;
            if (roleplay.HasJoined(newUser))
            {
                var errorMessage = isCurrentUser
                    ? "You're already in that roleplay."
                    : "The user is aleady in that roleplay.";

                return CreateEntityResult<RoleplayParticipant>.FromError(CommandError.Unsuccessful, errorMessage);
            }

            if (roleplay.IsKicked(newUser))
            {
                var errorMessage = isCurrentUser
                    ? "You've been kicked from that roleplay, and can't rejoin unless invited."
                    : "The user has been kicked from that roleplay, and can't rejoin unless invited.";

                return CreateEntityResult<RoleplayParticipant>.FromError(CommandError.UnmetPrecondition, errorMessage);
            }

            // Check the invite list for nonpublic roleplays.
            if (!roleplay.IsPublic && !roleplay.IsInvited(newUser))
            {
                var errorMessage = isCurrentUser
                    ? "You haven't been invited to that roleplay."
                    : "The user hasn't been invited to that roleplay.";

                return CreateEntityResult<RoleplayParticipant>.FromError(CommandError.UnmetPrecondition, errorMessage);
            }

            var participantEntry = roleplay.ParticipatingUsers.FirstOrDefault(p => p.User.DiscordID == (long)newUser.Id);
            if (participantEntry is null)
            {
                var getUserResult = await db.GetOrRegisterUserAsync(newUser);
                if (!getUserResult.IsSuccess)
                {
                    return CreateEntityResult<RoleplayParticipant>.FromError(getUserResult);
                }

                var user = getUserResult.Entity;

                participantEntry = new RoleplayParticipant(roleplay, user, ParticipantStatus.Joined);
                roleplay.ParticipatingUsers.Add(participantEntry);
            }
            else
            {
                participantEntry.Status = ParticipantStatus.Joined;
            }

            await db.SaveChangesAsync();

            return CreateEntityResult<RoleplayParticipant>.FromSuccess(participantEntry);
        }

        /// <summary>
        /// Invites the user to the given roleplay.
        /// </summary>
        /// <param name="db">The database where the roleplays are stored.</param>
        /// <param name="roleplay">The roleplay to invite the user to.</param>
        /// <param name="invitedUser">The user to invite.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<ExecuteResult> InviteUserAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] Roleplay roleplay,
            [NotNull] IUser invitedUser
        )
        {
            if (roleplay.IsPublic && !roleplay.IsKicked(invitedUser))
            {
                return ExecuteResult.FromError(CommandError.UnmetPrecondition, "The roleplay is not set to private.");
            }

            if (roleplay.InvitedUsers.Any(p => p.User.DiscordID == (long)invitedUser.Id))
            {
                return ExecuteResult.FromError(CommandError.Unsuccessful, "The user has already been invited to that roleplay.");
            }

            // Remove the invited user from the kick list, if they're on it
            var participantEntry = roleplay.ParticipatingUsers.FirstOrDefault(p => p.User.DiscordID == (long)invitedUser.Id);
            if (participantEntry is null)
            {
                var getUserResult = await db.GetOrRegisterUserAsync(invitedUser);
                if (!getUserResult.IsSuccess)
                {
                    return ExecuteResult.FromError(getUserResult);
                }

                var user = getUserResult.Entity;

                participantEntry = new RoleplayParticipant(roleplay, user, ParticipantStatus.Invited);
                roleplay.ParticipatingUsers.Add(participantEntry);
            }
            else
            {
                participantEntry.Status = ParticipantStatus.Invited;
            }

            await db.SaveChangesAsync();

            return ExecuteResult.FromSuccess();
        }

        /// <summary>
        /// Transfers ownership of the named roleplay to the specified user.
        /// </summary>
        /// <param name="db">The database where the roleplays are stored.</param>
        /// <param name="newOwner">The new owner.</param>
        /// <param name="roleplay">The roleplay to transfer.</param>
        /// <param name="guild">The guild to scope the roleplays to.</param>
        /// <returns>An execution result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> TransferRoleplayOwnershipAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] IUser newOwner,
            [NotNull] Roleplay roleplay,
            [NotNull] IGuild guild
        )
        {
            var newOwnerRoleplays = GetUserRoleplays(db, newOwner, guild);
            return await this.OwnedEntities.TransferEntityOwnershipAsync
            (
                db,
                newOwner,
                newOwnerRoleplays,
                roleplay
            );
        }

        /// <summary>
        /// Sets the name of the given roleplay.
        /// </summary>
        /// <param name="db">The database containing the roleplays.</param>
        /// <param name="context">The context of the operation.</param>
        /// <param name="roleplay">The roleplay to set the name of.</param>
        /// <param name="newRoleplayName">The new name.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetRoleplayNameAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] SocketCommandContext context,
            [NotNull] Roleplay roleplay,
            [NotNull] string newRoleplayName
        )
        {
            var isCurrentUser = context.Message.Author.Id == (ulong)roleplay.Owner.DiscordID;
            if (string.IsNullOrWhiteSpace(newRoleplayName))
            {
                return ModifyEntityResult.FromError(CommandError.BadArgCount, "You need to provide a name.");
            }

            if (newRoleplayName.Contains("\""))
            {
                return ModifyEntityResult.FromError(CommandError.Unsuccessful, "The name may not contain double quotes.");
            }

            if (!await IsRoleplayNameUniqueForUserAsync(db, context.Message.Author, newRoleplayName, context.Guild))
            {
                var errorMessage = isCurrentUser
                    ? "You already have a roleplay with that name."
                    : "The user already has a roleplay with that name.";

                return ModifyEntityResult.FromError(CommandError.MultipleMatches, errorMessage);
            }

            var commandModule = this.Commands.Modules.First(m => m.Name == "roleplay");
            var validNameResult = this.OwnedEntities.IsEntityNameValid(commandModule, newRoleplayName);
            if (!validNameResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(validNameResult);
            }

            roleplay.Name = newRoleplayName;
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the summary of the given roleplay.
        /// </summary>
        /// <param name="db">The database containing the roleplays.</param>
        /// <param name="roleplay">The roleplay to set the summary of.</param>
        /// <param name="newRoleplaySummary">The new summary.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetRoleplaySummaryAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] Roleplay roleplay,
            [NotNull] string newRoleplaySummary
        )
        {
            if (string.IsNullOrWhiteSpace(newRoleplaySummary))
            {
                return ModifyEntityResult.FromError(CommandError.BadArgCount, "You need to provide a new summary.");
            }

            roleplay.Summary = newRoleplaySummary;
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets whether or not a roleplay is NSFW.
        /// </summary>
        /// <param name="db">The database containing the roleplays.</param>
        /// <param name="roleplay">The roleplay to set the value in.</param>
        /// <param name="isNSFW">The new value.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetRoleplayIsNSFWAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] Roleplay roleplay,
            bool isNSFW
        )
        {
            if (roleplay.Messages.Count > 0 && roleplay.IsNSFW && !isNSFW)
            {
                return ModifyEntityResult.FromError(CommandError.UnmetPrecondition, "You can't mark a NSFW roleplay with messages in it as non-NSFW.");
            }

            roleplay.IsNSFW = isNSFW;
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets whether or not a roleplay is public.
        /// </summary>
        /// <param name="db">The database containing the roleplays.</param>
        /// <param name="roleplay">The roleplay to set the value in.</param>
        /// <param name="isPublic">The new value.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetRoleplayIsPublicAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] Roleplay roleplay,
            bool isPublic
        )
        {
            roleplay.IsPublic = isPublic;
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Creates a dedicated channel for the roleplay.
        /// </summary>
        /// <param name="db">The database containing the roleplays.</param>
        /// <param name="context">The context in which the request was made.</param>
        /// <param name="roleplay">The roleplay to create the channel for.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<IGuildChannel>> CreateDedicatedRoleplayChannelAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] SocketCommandContext context,
            [NotNull] Roleplay roleplay
        )
        {
            var server = await db.GetOrRegisterServerAsync(context.Guild);
            if (!context.Guild.GetUser(context.Client.CurrentUser.Id).GuildPermissions.ManageChannels)
            {
                return CreateEntityResult<IGuildChannel>.FromError
                (
                    CommandError.UnmetPrecondition,
                    "I don't have permission to manage channels, so I can't create dedicated RP channels."
                );
            }

            var getExistingChannelResult = await GetDedicatedRoleplayChannelAsync(context, roleplay);
            if (getExistingChannelResult.IsSuccess)
            {
                return CreateEntityResult<IGuildChannel>.FromError
                (
                    CommandError.Unsuccessful,
                    "The roleplay already has a dedicated channel."
                );
            }

            var dedicatedChannel = await context.Guild.CreateTextChannelAsync
            (
                $"{roleplay.Name}-rp",
                properties =>
                {
                    properties.CategoryId = new Optional<ulong?>((ulong?)server.DedicatedRoleplayChannelsCategory);
                    properties.IsNsfw = roleplay.IsNSFW;
                    properties.Topic = $"Dedicated roleplay channel for {roleplay.Name}.";
                }
            );

            roleplay.DedicatedChannelID = (long)dedicatedChannel.Id;

            // Set up permission overrides
            foreach (var participant in roleplay.ParticipatingUsers)
            {
                var discordUser = context.Guild.GetUser((ulong)participant.User.DiscordID);
                var basicPermissions = OverwritePermissions.InheritAll;

                await dedicatedChannel.AddPermissionOverwriteAsync(discordUser, basicPermissions);
            }

            var botDiscordUser = context.Guild.GetUser(context.Client.CurrentUser.Id);
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

            return CreateEntityResult<IGuildChannel>.FromSuccess(dedicatedChannel);
        }

        /// <summary>
        /// Deletes the dedicated channel for the roleplay.
        /// </summary>
        /// <param name="db">The database containing the roleplays.</param>
        /// <param name="context">The context in which the request was made.</param>
        /// <param name="roleplay">The roleplay to delete the channel of.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> DeleteDedicatedRoleplayChannelAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] SocketCommandContext context,
            [NotNull] Roleplay roleplay
        )
        {
            if (!context.Guild.GetUser(context.Client.CurrentUser.Id).GuildPermissions.ManageChannels)
            {
                return ModifyEntityResult.FromError
                (
                    CommandError.UnmetPrecondition,
                    "I don't have permission to manage channels, so I can't delete dedicated RP channels."
                );
            }

            if (roleplay.DedicatedChannelID is null)
            {
                return ModifyEntityResult.FromError
                (
                    CommandError.ObjectNotFound, "The roleplay doesn't have a dedicated channel."
                );
            }

            var getDedicatedChannelResult = await GetDedicatedRoleplayChannelAsync(context, roleplay);
            if (getDedicatedChannelResult.IsSuccess)
            {
                await getDedicatedChannelResult.Entity.DeleteAsync();
            }

            roleplay.DedicatedChannelID = null;
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets the channel dedicated to the given roleplay.
        /// </summary>
        /// <param name="context">The context in which the request was made.</param>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<IGuildChannel>> GetDedicatedRoleplayChannelAsync
        (
            [NotNull] SocketCommandContext context,
            [NotNull] Roleplay roleplay
        )
        {
            if (!(roleplay.DedicatedChannelID is null))
            {
                var guildChannel = context.Guild.GetChannel((ulong)roleplay.DedicatedChannelID.Value);
                if (!(guildChannel is null))
                {
                    return RetrieveEntityResult<IGuildChannel>.FromSuccess(guildChannel);
                }

                return RetrieveEntityResult<IGuildChannel>.FromError
                (
                    CommandError.ObjectNotFound, "The roleplay had a channel set, but it appears to have been deleted."
                );
            }

            return RetrieveEntityResult<IGuildChannel>.FromError
            (
                CommandError.ObjectNotFound, "The roleplay doesn't have a dedicated channel."
            );
        }

        /// <summary>
        /// Sets the writability of the given dedicated channel for the given user.
        /// </summary>
        /// <param name="dedicatedChannel">The roleplay's dedicated channel.</param>
        /// <param name="participant">The participant to grant access to.</param>
        /// <param name="isVisible">Whether or not the channel should be writable.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
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
        public async Task<ModifyEntityResult> RevokeUserDedicatedChannelAccessAsync
        (
            [NotNull] SocketCommandContext context,
            [NotNull] IGuildChannel dedicatedChannel,
            [NotNull] IUser participant
        )
        {
            if (!context.Guild.GetUser(context.Client.CurrentUser.Id).GuildPermissions.ManageChannels)
            {
                return ModifyEntityResult.FromError
                (
                    CommandError.UnmetPrecondition,
                    "I don't have permission to manage channels, so I can't change permissions on dedicated RP channels."
                );
            }

            var user = context.Guild.GetUser(participant.Id);
            if (user is null)
            {
                return ModifyEntityResult.FromError(CommandError.ObjectNotFound, "User not found in guild.");
            }

            await dedicatedChannel.RemovePermissionOverwriteAsync(user);
            return ModifyEntityResult.FromSuccess();
        }
    }
}
