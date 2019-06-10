//
//  RoleplayCommands.cs
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
using System.Text;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Roleplaying;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Modules.Base;
using DIGOS.Ambassador.Pagination;
using DIGOS.Ambassador.Permissions;
using DIGOS.Ambassador.Permissions.Preconditions;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Exporters;
using DIGOS.Ambassador.Services.Interactivity;
using DIGOS.Ambassador.Services.Users;
using DIGOS.Ambassador.TypeReaders;

using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using static Discord.Commands.ContextType;
using PermissionTarget = DIGOS.Ambassador.Permissions.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Modules
{
    /// <summary>
    /// Commands for interacting with and managing channel roleplays.
    /// </summary>
    [UsedImplicitly]
    [Alias("roleplay", "rp")]
    [Group("roleplay")]
    [Summary("Commands for interacting with and managing channel roleplays.")]
    [Remarks
    (
        "Parameters which take a roleplay can be specified in two ways - by just the name, which will search your " +
        "roleplays, and by mention and name, which will search the given user's roleplays. For example,\n" +
        "\n" +
        "Your roleplay: ipsum\n" +
        "Another user's roleplay: @DIGOS Ambassador:ipsum\n" +
        "\n" +
        "You can also substitute any roleplay name for \"current\", and the active roleplay will be used instead."
    )]
    public class RoleplayCommands : DatabaseModuleBase
    {
        private readonly RoleplayService _roleplays;

        private readonly UserService _users;
        private readonly UserFeedbackService _feedback;
        private readonly InteractivityService _interactivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayCommands"/> class.
        /// </summary>
        /// <param name="database">A database context from the context pool.</param>
        /// <param name="roleplays">The roleplay service.</param>
        /// <param name="feedback">The user feedback service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="users">The user service.</param>
        public RoleplayCommands
        (
            AmbyDatabaseContext database,
            RoleplayService roleplays,
            UserFeedbackService feedback,
            InteractivityService interactivity,
            UserService users
        )
            : base(database)
        {
            _roleplays = roleplays;
            _feedback = feedback;
            _interactivity = interactivity;
            _users = users;
        }

        /// <summary>
        /// Shows information about the current.
        /// </summary>
        [UsedImplicitly]
        [Alias("show", "info")]
        [Command("show")]
        [Summary("Shows information about the current roleplay.")]
        [RequireContext(Guild)]
        public async Task ShowRoleplayAsync()
        {
            var getCurrentRoleplayResult = await _roleplays.GetActiveRoleplayAsync
            (
                this.Database, this.Context.Channel
            );

            if (!getCurrentRoleplayResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getCurrentRoleplayResult.ErrorReason);
                return;
            }

            var roleplay = getCurrentRoleplayResult.Entity;
            var eb = CreateRoleplayInfoEmbed(roleplay);
            await _feedback.SendEmbedAsync(this.Context.Channel, eb);
        }

        /// <summary>
        /// Shows information about the named roleplay owned by the specified user.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Alias("show", "info")]
        [Command("show")]
        [Summary("Shows information about the specified roleplay.")]
        [RequireContext(Guild)]
        public async Task ShowRoleplayAsync([NotNull] Roleplay roleplay)
        {
            var eb = CreateRoleplayInfoEmbed(roleplay);
            await _feedback.SendEmbedAsync(this.Context.Channel, eb);
        }

        [NotNull]
        private Embed CreateRoleplayInfoEmbed([NotNull] Roleplay roleplay)
        {
            var eb = _feedback.CreateEmbedBase();

            eb.WithAuthor(this.Context.Client.GetUser((ulong)roleplay.Owner.DiscordID));
            eb.WithTitle(roleplay.Name);
            eb.WithDescription(roleplay.Summary);

            eb.AddField("Currently", $"{(roleplay.IsActive ? "Active" : "Inactive")}", true);

            var dedicatedChannelName = roleplay.DedicatedChannelID is null
                ? "None"
                : MentionUtils.MentionChannel((ulong)roleplay.DedicatedChannelID.Value);

            eb.AddField("Dedicated Channel", dedicatedChannelName, true);

            eb.AddField("NSFW", roleplay.IsNSFW ? "Yes" : "No");
            eb.AddField("Public", roleplay.IsPublic ? "Yes" : "No", true);

            var joinedUsers = roleplay.JoinedUsers.Select(p => this.Context.Client.GetUser((ulong)p.User.DiscordID));
            var joinedMentions = joinedUsers.Select(u => u.Mention);

            var participantList = joinedMentions.Humanize();
            participantList = string.IsNullOrEmpty(participantList) ? "None" : participantList;

            eb.AddField("Participants", $"{participantList}");

            return eb.Build();
        }

        /// <summary>
        /// Lists the roleplays that the given user owns.
        /// </summary>
        /// <param name="discordUser">The user to show the roleplays of.</param>
        [UsedImplicitly]
        [Alias("list-owned", "list")]
        [Command("list-owned")]
        [Summary("Lists the roleplays that the given user owns.")]
        [RequireContext(Guild)]
        public async Task ListOwnedRoleplaysAsync([CanBeNull] IUser discordUser = null)
        {
            discordUser = discordUser ?? this.Context.Message.Author;

            var getUserResult = await _users.GetOrRegisterUserAsync(this.Database, discordUser);
            if (!getUserResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
                return;
            }

            var user = getUserResult.Entity;

            var roleplays = _roleplays.GetUserRoleplays(this.Database, user, this.Context.Guild);

            var appearance = PaginatedAppearanceOptions.Default;
            appearance.Title = "Your roleplays";
            appearance.Author = discordUser;

            var paginatedEmbed = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                this.Context.User,
                roleplays,
                r => r.Name,
                r => r.Summary ?? "No summary set.",
                "You don't have any roleplays.",
                appearance
            );

            await _interactivity.SendInteractiveMessageAndDeleteAsync
            (
                this.Context.Channel,
                paginatedEmbed,
                TimeSpan.FromMinutes(5.0)
            );
        }

        /// <summary>
        /// Creates a new roleplay with the specified name.
        /// </summary>
        /// <param name="roleplayName">The user-unique name of the roleplay.</param>
        /// <param name="roleplaySummary">A summary of the roleplay.</param>
        /// <param name="isNSFW">Whether or not the roleplay is NSFW.</param>
        /// <param name="isPublic">Whether or not the roleplay is public.</param>
        [UsedImplicitly]
        [Command("create")]
        [Summary("Creates a new roleplay with the specified name.")]
        [RequireContext(Guild)]
        [RequirePermission(Permission.CreateRoleplay)]
        public async Task CreateRoleplayAsync
        (
            [NotNull] string roleplayName,
            [NotNull] string roleplaySummary = "No summary set.",
            bool isNSFW = false,
            bool isPublic = true
        )
        {
            var result = await _roleplays.CreateRoleplayAsync(this.Database, this.Context, roleplayName, roleplaySummary, isNSFW, isPublic);
            if (!result.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                return;
            }

            await this.Database.SaveChangesAsync();
            await _feedback.SendConfirmationAsync(this.Context, $"Roleplay \"{result.Entity.Name}\" created.");
        }

        /// <summary>
        /// Deletes the specified roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("delete")]
        [Summary("Deletes the specified roleplay.")]
        [RequireContext(Guild)]
        [RequirePermission(Permission.DeleteRoleplay)]
        public async Task DeleteRoleplayAsync
        (
            [NotNull]
            [RequireEntityOwnerOrPermission(Permission.DeleteRoleplay, PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            this.Database.Roleplays.Remove(roleplay);

            var canSendMessageInCurrentChannelAfterDeletion = true;
            if (!(roleplay.DedicatedChannelID is null))
            {
                var dedicatedChannelID = roleplay.DedicatedChannelID;
                var deleteDedicatedChannelResult = await _roleplays.DeleteDedicatedRoleplayChannelAsync
                (
                    this.Database,
                    this.Context,
                    roleplay
                );

                if (!deleteDedicatedChannelResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, deleteDedicatedChannelResult.ErrorReason);
                    return;
                }

                if ((long)this.Context.Channel.Id == dedicatedChannelID)
                {
                    canSendMessageInCurrentChannelAfterDeletion = false;
                }
            }

            await this.Database.SaveChangesAsync();
            if (canSendMessageInCurrentChannelAfterDeletion)
            {
                await _feedback.SendConfirmationAsync(this.Context, $"Roleplay \"{roleplay.Name}\" deleted.");
            }
            else
            {
                var eb = _feedback.CreateEmbedBase();
                eb.WithDescription($"Roleplay \"{roleplay.Name}\" deleted.");

                await _feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, eb.Build(), false);
            }
        }

        /// <summary>
        /// Joins the roleplay owned by the given person with the given name.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("join")]
        [Summary("Joins the roleplay owned by the given person with the given name.")]
        [RequireContext(Guild)]
        [RequirePermission(Permission.JoinRoleplay)]
        public async Task JoinRoleplayAsync([NotNull] Roleplay roleplay)
        {
            var addUserResult = await _roleplays.AddUserToRoleplayAsync(this.Database, this.Context, roleplay, this.Context.Message.Author);
            if (!addUserResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, addUserResult.ErrorReason);
                return;
            }

            // Ensure the user has the correct permissions for the dedicated channel
            var getDedicatedChannelResult = await _roleplays.GetDedicatedRoleplayChannelAsync(this.Context.Guild, roleplay);
            if (getDedicatedChannelResult.IsSuccess)
            {
                var dedicatedChannel = getDedicatedChannelResult.Entity;

                if (roleplay.IsActive)
                {
                    await _roleplays.SetDedicatedChannelWritabilityForUserAsync
                    (
                        dedicatedChannel,
                        this.Context.User,
                        true
                    );

                    await _roleplays.SetDedicatedChannelVisibilityForUserAsync
                    (
                        dedicatedChannel,
                        this.Context.User,
                        true
                    );
                }
            }

            await this.Database.SaveChangesAsync();

            var roleplayOwnerUser = this.Context.Guild.GetUser((ulong)roleplay.Owner.DiscordID);
            await _feedback.SendConfirmationAsync(this.Context, $"Joined {roleplayOwnerUser.Mention}'s roleplay \"{roleplay.Name}\"");
        }

        /// <summary>
        /// Invites the specified user to the given roleplay.
        /// </summary>
        /// <param name="playerToInvite">The player to invite.</param>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("invite")]
        [Summary("Invites the specified user to the given roleplay.")]
        [RequireContext(Guild)]
        [RequirePermission(Permission.EditRoleplay)]
        public async Task InvitePlayerAsync
        (
            [NotNull]
            IUser playerToInvite,
            [NotNull]
            [RequireEntityOwnerOrPermission(Permission.EditRoleplay, PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            var invitePlayerResult = await _roleplays.InviteUserAsync(this.Database, roleplay, playerToInvite);
            if (!invitePlayerResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, invitePlayerResult.ErrorReason);
                return;
            }

            await this.Database.SaveChangesAsync();
            await _feedback.SendConfirmationAsync(this.Context, $"Invited {playerToInvite.Mention} to {roleplay.Name}.");

            var userDMChannel = await playerToInvite.GetOrCreateDMChannelAsync();
            try
            {
                await userDMChannel.SendMessageAsync(
                    $"You've been invited to join {roleplay.Name}. Use \"!rp join {roleplay.Name}\" to join.");
            }
            catch (HttpException hex) when (hex.WasCausedByDMsNotAccepted())
            {
            }
            finally
            {
                await userDMChannel.CloseAsync();
            }
        }

        /// <summary>
        /// Leaves the roleplay owned by the given person with the given name.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("leave")]
        [Summary("Leaves the roleplay owned by the given person with the given name.")]
        [RequireContext(Guild)]
        public async Task LeaveRoleplayAsync([NotNull] Roleplay roleplay)
        {
            var removeUserResult = await _roleplays.RemoveUserFromRoleplayAsync(this.Database, this.Context, roleplay, this.Context.Message.Author);
            if (!removeUserResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, removeUserResult.ErrorReason);
                return;
            }

            // Ensure the user has the correct permissions for the dedicated channel
            var getDedicatedChannelResult = await _roleplays.GetDedicatedRoleplayChannelAsync(this.Context.Guild, roleplay);
            if (getDedicatedChannelResult.IsSuccess)
            {
                var dedicatedChannel = getDedicatedChannelResult.Entity;

                var grantPermissionResult = await _roleplays.RevokeUserDedicatedChannelAccessAsync
                (
                    this.Context,
                    dedicatedChannel,
                    this.Context.User
                );

                if (!grantPermissionResult.IsSuccess)
                {
                    await _feedback.SendWarningAsync(this.Context, grantPermissionResult.ErrorReason);
                }
            }

            await this.Database.SaveChangesAsync();

            var roleplayOwnerUser = this.Context.Guild.GetUser((ulong)roleplay.Owner.DiscordID);
            await _feedback.SendConfirmationAsync(this.Context, $"Left {roleplayOwnerUser.Mention}'s roleplay \"{roleplay.Name}\"");
        }

        /// <summary>
        /// Kicks the given user from the named roleplay.
        /// </summary>
        /// <param name="discordUser">The user to kick.</param>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("kick")]
        [Summary("Kicks the given user from the named roleplay.")]
        [RequireContext(Guild)]
        [RequirePermission(Permission.KickRoleplayMember)]
        public async Task KickRoleplayParticipantAsync
        (
            [NotNull]
            IUser discordUser,
            [NotNull]
            [RequireEntityOwnerOrPermission(Permission.KickRoleplayMember, PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            var kickUserResult = await _roleplays.KickUserFromRoleplayAsync(this.Database, this.Context, roleplay, discordUser);
            if (!kickUserResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, kickUserResult.ErrorReason);
                return;
            }

            // Ensure the user has the correct permissions for the dedicated channel
            var getDedicatedChannelResult = await _roleplays.GetDedicatedRoleplayChannelAsync(this.Context.Guild, roleplay);
            if (getDedicatedChannelResult.IsSuccess)
            {
                var dedicatedChannel = getDedicatedChannelResult.Entity;

                var grantPermissionResult = await _roleplays.RevokeUserDedicatedChannelAccessAsync
                (
                    this.Context,
                    dedicatedChannel,
                    discordUser
                );

                if (!grantPermissionResult.IsSuccess)
                {
                    await _feedback.SendWarningAsync(this.Context, grantPermissionResult.ErrorReason);
                }
            }

            await this.Database.SaveChangesAsync();

            var userDMChannel = await discordUser.GetOrCreateDMChannelAsync();
            try
            {
                await userDMChannel.SendMessageAsync
                (
                    $"You've been removed from the roleplay \"{roleplay.Name}\" by {this.Context.Message.Author.Username}."
                );
            }
            catch (HttpException hex) when (hex.WasCausedByDMsNotAccepted())
            {
            }
            finally
            {
                await userDMChannel.CloseAsync();
            }

            await _feedback.SendConfirmationAsync(this.Context, $"{discordUser.Mention} has been kicked from {roleplay.Name}.");
        }

        /// <summary>
        /// Displays the existing or creates a dedicated channel for the roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("channel")]
        [Summary("Makes the roleplay with the given name current in the current channel.")]
        [RequireContext(Guild)]
        [RequirePermission(Permission.StartStopRoleplay)]
        public async Task ShowOrCreateDedicatedRoleplayChannel
        (
            [NotNull]
            [RequireEntityOwnerOrPermission(Permission.StartStopRoleplay, PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            var getDedicatedChannelResult = await _roleplays.GetDedicatedRoleplayChannelAsync
            (
                this.Context.Guild,
                roleplay
            );

            if (getDedicatedChannelResult.IsSuccess)
            {
                var existingDedicatedChannel = getDedicatedChannelResult.Entity;
                var message = $"\"{roleplay.Name}\" has a dedicated channel at " +
                              $"{MentionUtils.MentionChannel(existingDedicatedChannel.Id)}";

                await _feedback.SendConfirmationAsync(this.Context, message);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "Setting up dedicated channel...");

            // The roleplay either doesn't have a channel, or the one it has has been deleted or is otherwise invalid.
            var result = await _roleplays.CreateDedicatedRoleplayChannelAsync
            (
                this.Database,
                this.Context,
                roleplay
            );

            if (!result.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                return;
            }

            await this.Database.SaveChangesAsync();

            var dedicatedChannel = result.Entity;
            await _feedback.SendConfirmationAsync
            (
                this.Context,
                $"All done! Your roleplay now has a dedicated channel at {MentionUtils.MentionChannel(dedicatedChannel.Id)}."
            );

            if (roleplay.IsActive && roleplay.ActiveChannelID != (long)dedicatedChannel.Id)
            {
                await StopRoleplayAsync(roleplay);
                await StartRoleplayAsync(roleplay);
            }
        }

        /// <summary>
        /// Starts the roleplay with the given name.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("start")]
        [Summary("Starts the roleplay with the given name.")]
        [RequireContext(Guild)]
        [RequirePermission(Permission.StartStopRoleplay)]
        public async Task StartRoleplayAsync
        (
            [NotNull]
            [RequireEntityOwnerOrPermission(Permission.StartStopRoleplay, PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            var getDedicatedChannelResult = await _roleplays.GetDedicatedRoleplayChannelAsync
            (
                this.Context.Guild,
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
                channel = this.Context.Channel;
            }

            var isNsfwChannel = channel is ITextChannel textChannel && textChannel.IsNsfw;
            if (roleplay.IsNSFW && !isNsfwChannel)
            {
                await _feedback.SendErrorAsync
                (
                    this.Context,
                    "This channel is not marked as NSFW, while your roleplay is... naughty!"
                );

                return;
            }

            if (await _roleplays.HasActiveRoleplayAsync(this.Database, channel))
            {
                await _feedback.SendWarningAsync(this.Context, "There's already a roleplay active in this channel.");

                var currentRoleplayResult = await _roleplays.GetActiveRoleplayAsync(this.Database, channel);
                if (!currentRoleplayResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, currentRoleplayResult.ErrorReason);
                    return;
                }

                var currentRoleplay = currentRoleplayResult.Entity;
                var timeOfLastMessage = currentRoleplay.Messages.Last().Timestamp;
                var currentTime = DateTimeOffset.Now;
                if (timeOfLastMessage < currentTime.AddHours(-4))
                {
                    await _feedback.SendConfirmationAsync
                    (
                        this.Context,
                        "However, that roleplay has been inactive for over four hours."
                    );

                    currentRoleplay.IsActive = false;
                }
                else
                {
                    return;
                }
            }

            if (roleplay.ActiveChannelID != (long)channel.Id)
            {
                roleplay.ActiveChannelID = (long)channel.Id;
            }

            roleplay.IsActive = true;

            // Make the channel visible for all participants
            if (getDedicatedChannelResult.IsSuccess)
            {
                var dedicatedChannel = getDedicatedChannelResult.Entity;

                foreach (var participant in roleplay.ParticipatingUsers)
                {
                    var user = this.Context.Guild.GetUser((ulong)participant.User.DiscordID);
                    if (user is null)
                    {
                        continue;
                    }

                    await _roleplays.SetDedicatedChannelWritabilityForUserAsync
                    (
                        dedicatedChannel,
                        user,
                        true
                    );

                    await _roleplays.SetDedicatedChannelVisibilityForUserAsync
                    (
                        dedicatedChannel,
                        user,
                        true
                    );
                }

                if (roleplay.IsPublic)
                {
                    var everyoneRole = this.Context.Guild.EveryoneRole;
                    await _roleplays.SetDedicatedChannelVisibilityForRoleAsync
                    (
                        dedicatedChannel,
                        everyoneRole,
                        true
                    );
                }
            }

            roleplay.LastUpdated = DateTime.Now;

            await this.Database.SaveChangesAsync();

            var joinedUsers = roleplay.JoinedUsers.Select(p => this.Context.Client.GetUser((ulong)p.User.DiscordID));
            var joinedMentions = joinedUsers.Select(u => u.Mention);

            var participantList = joinedMentions.Humanize();
            await _feedback.SendConfirmationAsync
            (
                this.Context,
                $"The roleplay \"{roleplay.Name}\" is now active in {MentionUtils.MentionChannel(channel.Id)}."
            );

            await channel.SendMessageAsync($"Calling {participantList}!");
        }

        /// <summary>
        /// Stops the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("stop")]
        [Summary("Stops the given roleplay.")]
        [RequireContext(Guild)]
        [RequirePermission(Permission.StartStopRoleplay)]
        public async Task StopRoleplayAsync
        (
            [NotNull]
            [RequireEntityOwnerOrPermission(Permission.StartStopRoleplay, PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            roleplay.IsActive = false;
            roleplay.ActiveChannelID = null;

            var getDedicatedChannelResult = await _roleplays.GetDedicatedRoleplayChannelAsync
            (
                this.Context.Guild,
                roleplay
            );

            // Hide the channel for all participants
            if (getDedicatedChannelResult.IsSuccess)
            {
                var dedicatedChannel = getDedicatedChannelResult.Entity;

                foreach (var participant in roleplay.ParticipatingUsers)
                {
                    var user = this.Context.Guild.GetUser((ulong)participant.User.DiscordID);
                    if (user is null)
                    {
                        continue;
                    }

                    await _roleplays.SetDedicatedChannelWritabilityForUserAsync
                    (
                        dedicatedChannel,
                        user,
                        false
                    );

                    await _roleplays.SetDedicatedChannelVisibilityForUserAsync
                    (
                        dedicatedChannel,
                        user,
                        false
                    );
                }

                if (roleplay.IsPublic)
                {
                    var everyoneRole = this.Context.Guild.EveryoneRole;
                    await _roleplays.SetDedicatedChannelVisibilityForRoleAsync
                    (
                        dedicatedChannel,
                        everyoneRole,
                        false
                    );
                }
            }

            await this.Database.SaveChangesAsync();
            await _feedback.SendConfirmationAsync(this.Context, $"The roleplay \"{roleplay.Name}\" has been stopped.");
        }

        /// <summary>
        /// Includes previous messages into the roleplay, starting at the given time.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="startMessage">The earliest message to start adding from.</param>
        /// <param name="finalMessage">The final message in the range.</param>
        [UsedImplicitly]
        [Command("include-previous")]
        [Summary("Includes previous messages into the roleplay, starting at the given message.")]
        [RequireContext(Guild)]
        [RequirePermission(Permission.EditRoleplay)]
        public async Task IncludePreviousMessagesAsync
        (
            [NotNull]
            [RequireEntityOwnerOrPermission(Permission.EditRoleplay, PermissionTarget.Other)]
            Roleplay roleplay,
            [OverrideTypeReader(typeof(UncachedMessageTypeReader<IMessage>))]
            IMessage startMessage,
            [CanBeNull]
            [OverrideTypeReader(typeof(UncachedMessageTypeReader<IMessage>))]
            IMessage finalMessage = null
        )
        {
            finalMessage = finalMessage ?? this.Context.Message;

            if (startMessage.Channel != finalMessage.Channel)
            {
                await _feedback.SendErrorAsync(this.Context, "The messages are not in the same channel.");
                return;
            }

            int addedOrUpdatedMessageCount = 0;

            var latestMessage = startMessage;
            while (latestMessage.Timestamp < finalMessage.Timestamp)
            {
                var messages = (await this.Context.Channel.GetMessagesAsync(latestMessage, Direction.After).FlattenAsync()).OrderBy(m => m.Timestamp).ToList();
                latestMessage = messages.Last();

                foreach (var message in messages)
                {
                    // Jump out if we've passed the final message
                    if (message.Timestamp > finalMessage.Timestamp)
                    {
                        break;
                    }

                    var modifyResult = await _roleplays.AddToOrUpdateMessageInRoleplayAsync(this.Database, roleplay, message);
                    if (modifyResult.IsSuccess)
                    {
                        ++addedOrUpdatedMessageCount;
                    }
                }
            }

            await this.Database.SaveChangesAsync();
            await _feedback.SendConfirmationAsync(this.Context, $"{addedOrUpdatedMessageCount} messages added to \"{roleplay.Name}\".");
        }

        /// <summary>
        /// Transfers ownership of the named roleplay to the specified user.
        /// </summary>
        /// <param name="newOwner">The new owner.</param>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("transfer-ownership")]
        [Summary("Transfers ownership of the named roleplay to the specified user.")]
        [RequireContext(Guild)]
        [RequirePermission(Permission.TransferRoleplay)]
        public async Task TransferRoleplayOwnershipAsync
        (
            [NotNull] IUser newOwner,
            [NotNull]
            [RequireEntityOwnerOrPermission(Permission.TransferRoleplay, PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            var getNewOwnerResult = await _users.GetOrRegisterUserAsync(this.Database, newOwner);
            if (!getNewOwnerResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getNewOwnerResult.ErrorReason);
                return;
            }

            var newOwnerUser = getNewOwnerResult.Entity;

            var transferResult = await _roleplays.TransferRoleplayOwnershipAsync(this.Database, newOwnerUser, roleplay, this.Context.Guild);
            if (!transferResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, transferResult.ErrorReason);
                return;
            }

            await this.Database.SaveChangesAsync();
            await _feedback.SendConfirmationAsync(this.Context, "Roleplay ownership transferred.");
        }

        /// <summary>
        /// Exports the named roleplay owned by the given user, sending you a file with the contents.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="format">The export format.</param>
        [UsedImplicitly]
        [Command("export")]
        [Summary(" Exports the named roleplay owned by the given user, sending you a file with the contents.")]
        [RequireContext(Guild)]
        [RequirePermission(Permission.ReplayRoleplay)]
        public async Task ExportRoleplayAsync
        (
            [NotNull]
            Roleplay roleplay,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<ExportFormat>))]
            ExportFormat format = ExportFormat.PDF
        )
        {
            IRoleplayExporter exporter;
            switch (format)
            {
                case ExportFormat.PDF:
                {
                    exporter = new PDFRoleplayExporter(this.Context);
                    break;
                }
                case ExportFormat.Plaintext:
                {
                    exporter = new PlaintextRoleplayExporter(this.Context);
                    break;
                }
                default:
                {
                    await _feedback.SendErrorAsync(this.Context, "That export format hasn't been implemented yet.");
                    return;
                }
            }

            await _feedback.SendConfirmationAsync(this.Context, "Compiling the roleplay...");
            using (var output = await exporter.ExportAsync(roleplay))
            {
                await this.Context.Channel.SendFileAsync(output.Data, $"{output.Title}.{output.Format.GetFileExtension()}");
            }
        }

        /// <summary>
        /// Replays the named roleplay owned by the given user to you.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="from">The time from which you want to replay.</param>
        /// <param name="to">The time until you want to replay.</param>
        [UsedImplicitly]
        [Command("replay")]
        [Summary("Replays the named roleplay owned by the given user to you.")]
        [RequireContext(Guild)]
        [RequirePermission(Permission.ReplayRoleplay)]
        public async Task ReplayRoleplayAsync
        (
            [NotNull] Roleplay roleplay,
            DateTimeOffset from = default,
            DateTimeOffset to = default
        )
        {
            if (from == default)
            {
                from = DateTimeOffset.MinValue;
            }

            if (to == default)
            {
                to = DateTimeOffset.Now;
            }

            var userDMChannel = await this.Context.Message.Author.GetOrCreateDMChannelAsync();
            var eb = CreateRoleplayInfoEmbed(roleplay);

            try
            {
                await userDMChannel.SendMessageAsync(string.Empty, false, eb);

                var messages = roleplay.Messages.Where
                (
                    m =>
                        m.Timestamp > from && m.Timestamp < to
                )
                .OrderBy(msg => msg.Timestamp).ToList();

                var timestampEmbed = _feedback.CreateFeedbackEmbed
                (
                    this.Context.User,
                    Color.DarkPurple,
                    $"Roleplay began at {messages.First().Timestamp.ToUniversalTime()}"
                );

                await userDMChannel.SendMessageAsync(string.Empty, false, timestampEmbed);

                if (messages.Count <= 0)
                {
                    await userDMChannel.SendMessageAsync("No messages found in the specified timeframe.");
                    return;
                }

                await _feedback.SendConfirmationAsync
                (
                    this.Context,
                    $"Replaying \"{roleplay.Name}\". Please check your private messages."
                );

                const int messageCharacterLimit = 2000;
                var sb = new StringBuilder(messageCharacterLimit);

                foreach (var message in messages)
                {
                    var newContent = $"**{message.AuthorNickname}** {message.Contents}\n";

                    if (sb.Length + newContent.Length >= messageCharacterLimit)
                    {
                        await userDMChannel.SendMessageAsync(sb.ToString());
                        await Task.Delay(TimeSpan.FromSeconds(2));

                        sb.Clear();
                        sb.AppendLine();
                    }

                    sb.Append(newContent);

                    if (message.ID == messages.Last().ID)
                    {
                        await userDMChannel.SendMessageAsync(sb.ToString());
                    }
                }
            }
            catch (HttpException hex) when (hex.WasCausedByDMsNotAccepted())
            {
                await _feedback.SendWarningAsync
                (
                    this.Context,
                    "I can't do that, since you don't accept DMs from non-friends on this server."
                );
            }
            finally
            {
                await userDMChannel.CloseAsync();
            }
        }

        /// <summary>
        /// Views the given roleplay, allowing you to read the channel.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("view")]
        [Summary("Views the given roleplay, allowing you to read the channel.")]
        [RequireContext(Guild)]
        public async Task ViewRoleplayAsync([NotNull] Roleplay roleplay)
        {
            var getDedicatedChannelResult = await _roleplays.GetDedicatedRoleplayChannelAsync
            (
                this.Context.Guild,
                roleplay
            );

            if (!getDedicatedChannelResult.IsSuccess)
            {
                await _feedback.SendErrorAsync
                (
                    this.Context,
                    "The given roleplay doesn't have a dedicated channel. Try using \"!rp export\" instead."
                );

                return;
            }

            var user = this.Context.User;
            if (!roleplay.IsPublic && roleplay.ParticipatingUsers.All(p => p.User.DiscordID != (long)user.Id))
            {
                await _feedback.SendErrorAsync
                (
                    this.Context,
                    "You don't have permission to view that roleplay."
                );

                return;
            }

            var dedicatedChannel = getDedicatedChannelResult.Entity;
            await _roleplays.SetDedicatedChannelVisibilityForUserAsync(dedicatedChannel, user, true);

            var channelMention = MentionUtils.MentionChannel(dedicatedChannel.Id);
            await _feedback.SendConfirmationAsync
            (
                this.Context, $"The roleplay \"{roleplay.Name}\" is now visible in {channelMention}."
            );
        }

        /// <summary>
        /// Hides the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("hide")]
        [Summary("Hides the given roleplay.")]
        [RequireContext(Guild)]
        public async Task HideRoleplayAsync([NotNull] Roleplay roleplay)
        {
            var getDedicatedChannelResult = await _roleplays.GetDedicatedRoleplayChannelAsync
            (
                this.Context.Guild,
                roleplay
            );

            if (!getDedicatedChannelResult.IsSuccess)
            {
                await _feedback.SendErrorAsync
                (
                    this.Context,
                    "The given roleplay doesn't have a dedicated channel."
                );

                return;
            }

            var user = this.Context.User;
            var dedicatedChannel = getDedicatedChannelResult.Entity;
            await _roleplays.SetDedicatedChannelVisibilityForUserAsync(dedicatedChannel, user, false);

            await _feedback.SendConfirmationAsync
            (
                this.Context, "Roleplay hidden."
            );
        }

        /// <summary>
        /// Hides all roleplays in the server for the user.
        /// </summary>
        [UsedImplicitly]
        [Command("hide-all")]
        [Summary("Hides all roleplays in the server for the user.")]
        [RequireContext(Guild)]
        public async Task HideAllRoleplaysAsync()
        {
            var roleplays = _roleplays.GetRoleplays(this.Database, this.Context.Guild);
            foreach (var roleplay in roleplays)
            {
                var getDedicatedChannelResult = await _roleplays.GetDedicatedRoleplayChannelAsync
                (
                    this.Context.Guild,
                    roleplay
                );

                if (!getDedicatedChannelResult.IsSuccess)
                {
                    continue;
                }

                var user = this.Context.User;
                var dedicatedChannel = getDedicatedChannelResult.Entity;
                await _roleplays.SetDedicatedChannelVisibilityForUserAsync(dedicatedChannel, user, false);
            }

            await _feedback.SendConfirmationAsync
            (
                this.Context, "Roleplays hidden."
            );
        }

        /// <summary>
        /// Setter commands for roleplay properties.
        /// </summary>
        [UsedImplicitly]
        [Group("set")]
        public class SetCommands : DatabaseModuleBase
        {
            private readonly RoleplayService _roleplays;

            private readonly UserFeedbackService _feedback;

            /// <summary>
            /// Initializes a new instance of the <see cref="SetCommands"/> class.
            /// </summary>
            /// <param name="database">A database context from the context pool.</param>
            /// <param name="roleplays">The roleplay service.</param>
            /// <param name="feedback">The user feedback service.</param>
            public SetCommands(AmbyDatabaseContext database, RoleplayService roleplays, UserFeedbackService feedback)
                : base(database)
            {
                _roleplays = roleplays;
                _feedback = feedback;
            }

            /// <summary>
            /// Sets the name of the named roleplay.
            /// </summary>
            /// <param name="newRoleplayName">The roleplay's new name.</param>
            /// <param name="roleplay">The roleplay.</param>
            [UsedImplicitly]
            [Command("name")]
            [Summary("Sets the new name of the named roleplay.")]
            [RequireContext(Guild)]
            [RequirePermission(Permission.EditRoleplay)]
            public async Task SetRoleplayNameAsync
            (
                [NotNull]
                string newRoleplayName,
                [NotNull]
                [RequireEntityOwnerOrPermission(Permission.EditRoleplay, PermissionTarget.Other)]
                Roleplay roleplay
            )
            {
                var result = await _roleplays.SetRoleplayNameAsync(this.Database, this.Context, roleplay, newRoleplayName);
                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                var getDedicatedChannelResult = await _roleplays.GetDedicatedRoleplayChannelAsync
                (
                    this.Context.Guild,
                    roleplay
                );

                if (getDedicatedChannelResult.IsSuccess)
                {
                    var dedicatedChannel = getDedicatedChannelResult.Entity;

                    await dedicatedChannel.ModifyAsync(p => p.Name = $"{roleplay.Name}-rp");
                }

                await this.Database.SaveChangesAsync();
                await _feedback.SendConfirmationAsync(this.Context, "Roleplay name set.");
            }

            /// <summary>
            /// Sets the summary of the named roleplay.
            /// </summary>
            /// <param name="newRoleplaySummary">The roleplay's new summary.</param>
            /// <param name="roleplay">The roleplay.</param>
            [UsedImplicitly]
            [Command("summary")]
            [Summary("Sets the summary of the named roleplay.")]
            [RequireContext(Guild)]
            [RequirePermission(Permission.EditRoleplay)]
            public async Task SetRoleplaySummaryAsync
            (
                [NotNull]
                string newRoleplaySummary,
                [NotNull]
                [RequireEntityOwnerOrPermission(Permission.EditRoleplay, PermissionTarget.Other)]
                Roleplay roleplay
            )
            {
                var result = await _roleplays.SetRoleplaySummaryAsync(this.Database, roleplay, newRoleplaySummary);
                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await this.Database.SaveChangesAsync();
                await _feedback.SendConfirmationAsync(this.Context, "Roleplay summary set.");
            }

            /// <summary>
            /// Sets a value indicating whether or not the named roleplay is NSFW. This restricts which channels it
            /// can be made active in.
            /// </summary>
            /// <param name="isNSFW">true if the roleplay is NSFW; otherwise, false.</param>
            /// <param name="roleplay">The roleplay.</param>
            [UsedImplicitly]
            [Command("nsfw")]
            [Summary("Sets a value indicating whether or not the named roleplay is NSFW. This restricts which channels it can be made active in.")]
            [RequireContext(Guild)]
            [RequirePermission(Permission.EditRoleplay)]
            public async Task SetRoleplayIsNSFW
            (
                bool isNSFW,
                [NotNull]
                [RequireEntityOwnerOrPermission(Permission.EditRoleplay, PermissionTarget.Other)]
                Roleplay roleplay
            )
            {
                var result = await _roleplays.SetRoleplayIsNSFWAsync(this.Database, roleplay, isNSFW);
                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await this.Database.SaveChangesAsync();
                await _feedback.SendConfirmationAsync(this.Context, $"Roleplay set to {(isNSFW ? "NSFW" : "SFW")}");
            }

            /// <summary>
            /// Sets a value indicating whether or not the named roleplay is private. This restricts replays to participants.
            /// </summary>
            /// <param name="isPrivate">true if the roleplay is private; otherwise, false.</param>
            /// <param name="roleplay">The roleplay.</param>
            [UsedImplicitly]
            [Command("private")]
            [Summary("Sets a value indicating whether or not the named roleplay is private. This restricts replays to participants.")]
            [RequireContext(Guild)]
            [RequirePermission(Permission.EditRoleplay)]
            public Task SetRoleplayIsPrivate
            (
                bool isPrivate,
                [NotNull] [RequireEntityOwnerOrPermission(Permission.EditRoleplay, PermissionTarget.Other)]
                Roleplay roleplay
            )
            => SetRoleplayIsPublic(!isPrivate, roleplay);

            /// <summary>
            /// Sets a value indicating whether or not the named roleplay is publíc. This restricts replays to participants.
            /// </summary>
            /// <param name="isPublic">true if the roleplay is public; otherwise, false.</param>
            /// <param name="roleplay">The roleplay.</param>
            [UsedImplicitly]
            [Command("public")]
            [Summary("Sets a value indicating whether or not the named roleplay is public. This restricts replays to participants.")]
            [RequireContext(Guild)]
            [RequirePermission(Permission.EditRoleplay)]
            public async Task SetRoleplayIsPublic
            (
                bool isPublic,
                [NotNull]
                [RequireEntityOwnerOrPermission(Permission.EditRoleplay, PermissionTarget.Other)]
                Roleplay roleplay
            )
            {
                var result = await _roleplays.SetRoleplayIsPublicAsync(this.Database, roleplay, isPublic);
                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                var getDedicatedChannelResult = await _roleplays.GetDedicatedRoleplayChannelAsync
                (
                    this.Context.Guild,
                    roleplay
                );

                if (getDedicatedChannelResult.IsSuccess)
                {
                    var dedicatedChannel = getDedicatedChannelResult.Entity;
                    var everyoneRole = this.Context.Guild.EveryoneRole;

                    await _roleplays.SetDedicatedChannelVisibilityForRoleAsync
                    (
                        dedicatedChannel,
                        everyoneRole,
                        isPublic
                    );
                }

                await this.Database.SaveChangesAsync();
                await _feedback.SendConfirmationAsync(this.Context, $"Roleplay set to {(isPublic ? "public" : "private")}");
            }
        }

        /// <summary>
        /// Administrative commands for roleplays.
        /// </summary>
        [UsedImplicitly]
        [Group("admin")]
        public class AdminCommands : DatabaseModuleBase
        {
            private readonly UserFeedbackService _feedback;

            /// <summary>
            /// Initializes a new instance of the <see cref="AdminCommands"/> class.
            /// </summary>
            /// <param name="database">A database context from the context pool.</param>
            /// <param name="roleplays">The roleplay service.</param>
            /// <param name="feedback">The user feedback service.</param>
            public AdminCommands(AmbyDatabaseContext database, RoleplayService roleplays, UserFeedbackService feedback)
                : base(database)
            {
                _feedback = feedback;
            }

            /// <summary>
            /// Updates the timestamps of all roleplays in the bot.
            /// </summary>
            [UsedImplicitly]
            [Command("update-timestamps")]
            [Summary("Updates the timestamps of all roleplays in the bot.")]
            [RequireContext(DM)]
            [RequireOwner]
            public async Task UpdateTimestamps()
            {
                var roleplays = await this.Database.Roleplays.ToListAsync();
                foreach (var roleplay in roleplays)
                {
                    var lastMessage = roleplay.Messages.OrderBy(m => m.Timestamp).LastOrDefault();
                    if (lastMessage is null)
                    {
                        continue;
                    }

                    if (!(roleplay.LastUpdated is null))
                    {
                        var value = roleplay.LastUpdated.Value;
                        if (value != default)
                        {
                            continue;
                        }
                    }

                    roleplay.LastUpdated = lastMessage.Timestamp.DateTime;
                }

                await this.Database.SaveChangesAsync();
                await _feedback.SendConfirmationAsync(this.Context, "Timestamps updated.");
            }
        }
    }
}
