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
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Discord.TypeReaders;
using DIGOS.Ambassador.Plugins.Core.Preconditions;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Permissions.Preconditions;
using DIGOS.Ambassador.Plugins.Roleplaying.Extensions;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Permissions;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using DIGOS.Ambassador.Plugins.Roleplaying.Services.Exporters;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Humanizer;
using JetBrains.Annotations;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Roleplaying.CommandModules
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
    public partial class RoleplayCommands : ModuleBase<SocketCommandContext>
    {
        private readonly RoleplayingDatabaseContext _database;
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
            RoleplayingDatabaseContext database,
            RoleplayService roleplays,
            UserFeedbackService feedback,
            InteractivityService interactivity,
            UserService users
        )
        {
            _database = database;
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
        [RequireContext(ContextType.Guild)]
        public async Task ShowRoleplayAsync()
        {
            var getCurrentRoleplayResult = await _roleplays.GetActiveRoleplayAsync
            (this.Context.Channel
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
        [RequireContext(ContextType.Guild)]
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
        /// Lists all available roleplays in the server.
        /// </summary>
        [UsedImplicitly]
        [Alias("list")]
        [Command("list")]
        [Summary("Lists all available roleplays in the server.")]
        [RequireContext(ContextType.Guild)]
        public async Task ListServerRoleplaysAsync()
        {
            // TODO: Filter so that ones where the user has joined but are private are also included
            var roleplays = _roleplays.GetRoleplays(this.Context.Guild)
                .Where(r => r.IsPublic);

            var appearance = PaginatedAppearanceOptions.Default;
            appearance.Title = "Available Roleplays";

            var paginatedEmbed = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                this.Context.User,
                roleplays,
                r => r.Name,
                r => r.Summary,
                "There are no roleplays in the server that you can view.",
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
        /// Lists the roleplays that the given user owns.
        /// </summary>
        /// <param name="discordUser">The user to show the roleplays of.</param>
        [UsedImplicitly]
        [Alias("list-owned")]
        [Command("list-owned")]
        [Summary("Lists the roleplays that the given user owns.")]
        [RequireContext(ContextType.Guild)]
        public async Task ListOwnedRoleplaysAsync([CanBeNull] IUser discordUser = null)
        {
            discordUser = discordUser ?? this.Context.Message.Author;

            var getUserResult = await _users.GetOrRegisterUserAsync(discordUser);
            if (!getUserResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
                return;
            }

            var user = getUserResult.Entity;

            var roleplays = _roleplays.GetUserRoleplays(user, this.Context.Guild);

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
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(CreateRoleplay), PermissionTarget.Self)]
        public async Task CreateRoleplayAsync
        (
            [NotNull] string roleplayName,
            [NotNull] string roleplaySummary = "No summary set.",
            bool isNSFW = false,
            bool isPublic = true
        )
        {
            var result = await _roleplays.CreateRoleplayAsync(this.Context, roleplayName, roleplaySummary, isNSFW, isPublic);
            if (!result.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, $"Roleplay \"{result.Entity.Name}\" created.");
        }

        /// <summary>
        /// Deletes the specified roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("delete")]
        [Summary("Deletes the specified roleplay.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(DeleteRoleplay), PermissionTarget.Self)]
        public async Task DeleteRoleplayAsync
        (
            [NotNull]
            [RequireEntityOwnerOrPermission(typeof(DeleteRoleplay), PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            _database.Roleplays.Remove(roleplay);

            var canSendMessageInCurrentChannelAfterDeletion = true;
            if (!(roleplay.DedicatedChannelID is null))
            {
                var dedicatedChannelID = roleplay.DedicatedChannelID;
                var deleteDedicatedChannelResult = await _roleplays.DeleteDedicatedRoleplayChannelAsync
                (
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

            await _database.SaveChangesAsync();
        }

        /// <summary>
        /// Joins the roleplay owned by the given person with the given name.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("join")]
        [Summary("Joins the roleplay owned by the given person with the given name.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(JoinRoleplay), PermissionTarget.Self)]
        public async Task JoinRoleplayAsync([NotNull] Roleplay roleplay)
        {
            var addUserResult = await _roleplays.AddUserToRoleplayAsync(this.Context, roleplay, this.Context.Message.Author);
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
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(EditRoleplay), PermissionTarget.Self)]
        public async Task InvitePlayerAsync
        (
            [NotNull]
            IUser playerToInvite,
            [NotNull]
            [RequireEntityOwnerOrPermission(typeof(EditRoleplay), PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            var invitePlayerResult = await _roleplays.InviteUserAsync(roleplay, playerToInvite);
            if (!invitePlayerResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, invitePlayerResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, $"Invited {playerToInvite.Mention} to {roleplay.Name}.");

            var userDMChannel = await playerToInvite.GetOrCreateDMChannelAsync();
            try
            {
                var roleplayName = roleplay.Name.Contains(" ") ? roleplay.Name.Quote() : roleplay.Name;

                await userDMChannel.SendMessageAsync
                (
                    $"You've been invited to join {roleplay.Name}. Use `!rp join {roleplayName}` to join."
                );
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
        [RequireContext(ContextType.Guild)]
        public async Task LeaveRoleplayAsync([NotNull] Roleplay roleplay)
        {
            var removeUserResult = await _roleplays.RemoveUserFromRoleplayAsync(this.Context, roleplay, this.Context.Message.Author);
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
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(KickRoleplayMember), PermissionTarget.Self)]
        public async Task KickRoleplayParticipantAsync
        (
            [NotNull]
            IUser discordUser,
            [NotNull]
            [RequireEntityOwnerOrPermission(typeof(KickRoleplayMember), PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            var kickUserResult = await _roleplays.KickUserFromRoleplayAsync(this.Context, roleplay, discordUser);
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
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(StartStopRoleplay), PermissionTarget.Self)]
        public async Task ShowOrCreateDedicatedRoleplayChannel
        (
            [NotNull]
            [RequireEntityOwnerOrPermission(typeof(StartStopRoleplay), PermissionTarget.Other)]
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
                this.Context,
                roleplay
            );

            if (!result.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                return;
            }

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
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(StartStopRoleplay), PermissionTarget.Self)]
        public async Task StartRoleplayAsync
        (
            [NotNull]
            [RequireEntityOwnerOrPermission(typeof(StartStopRoleplay), PermissionTarget.Other)]
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

            if (await _roleplays.HasActiveRoleplayAsync(channel))
            {
                await _feedback.SendWarningAsync(this.Context, "There's already a roleplay active in this channel.");

                var currentRoleplayResult = await _roleplays.GetActiveRoleplayAsync(channel);
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

            await _database.SaveChangesAsync();

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
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(StartStopRoleplay), PermissionTarget.Self)]
        public async Task StopRoleplayAsync
        (
            [NotNull]
            [RequireEntityOwnerOrPermission(typeof(StartStopRoleplay), PermissionTarget.Other)]
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

            await _database.SaveChangesAsync();

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
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(EditRoleplay), PermissionTarget.Self)]
        public async Task IncludePreviousMessagesAsync
        (
            [NotNull]
            [RequireEntityOwnerOrPermission(typeof(EditRoleplay), PermissionTarget.Other)]
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

                    var modifyResult = await _roleplays.AddToOrUpdateMessageInRoleplayAsync(roleplay, message);
                    if (modifyResult.IsSuccess)
                    {
                        ++addedOrUpdatedMessageCount;
                    }
                }
            }

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
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(TransferRoleplay), PermissionTarget.Self)]
        public async Task TransferRoleplayOwnershipAsync
        (
            [NotNull] IUser newOwner,
            [NotNull]
            [RequireEntityOwnerOrPermission(typeof(TransferRoleplay), PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            var getNewOwnerResult = await _users.GetOrRegisterUserAsync(newOwner);
            if (!getNewOwnerResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getNewOwnerResult.ErrorReason);
                return;
            }

            var newOwnerUser = getNewOwnerResult.Entity;

            var transferResult = await _roleplays.TransferRoleplayOwnershipAsync(newOwnerUser, roleplay, this.Context.Guild);
            if (!transferResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, transferResult.ErrorReason);
                return;
            }

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
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(ExportRoleplay), PermissionTarget.Self)]
        public async Task ExportRoleplayAsync
        (
            [NotNull]
            [RequireEntityOwnerOrPermission(typeof(ExportRoleplay), PermissionTarget.Other)]
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
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(ExportRoleplay), PermissionTarget.Self)]
        public async Task ReplayRoleplayAsync
        (
            [NotNull]
            [RequireEntityOwnerOrPermission(typeof(ExportRoleplay), PermissionTarget.Other)]
            Roleplay roleplay,
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
        [RequireContext(ContextType.Guild)]
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
        [RequireContext(ContextType.Guild)]
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
        [RequireContext(ContextType.Guild)]
        public async Task HideAllRoleplaysAsync()
        {
            var roleplays = _roleplays.GetRoleplays(this.Context.Guild);
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
    }
}
