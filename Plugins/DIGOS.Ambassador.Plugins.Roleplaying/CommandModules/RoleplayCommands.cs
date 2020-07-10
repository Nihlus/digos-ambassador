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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Extensions.Results;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Discord.TypeReaders;
using DIGOS.Ambassador.Plugins.Core.Preconditions;
using DIGOS.Ambassador.Plugins.Permissions.Preconditions;
using DIGOS.Ambassador.Plugins.Roleplaying.Extensions;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Permissions;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using DIGOS.Ambassador.Plugins.Roleplaying.Services.Exporters;
using Discord;
using Discord.Commands;
using Discord.Net;
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
    public partial class RoleplayCommands : ModuleBase
    {
        private readonly RoleplayDiscordService _discordRoleplays;
        private readonly DedicatedChannelService _dedicatedChannels;

        private readonly UserFeedbackService _feedback;
        private readonly InteractivityService _interactivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayCommands"/> class.
        /// </summary>
        /// <param name="discordRoleplays">The roleplay service.</param>
        /// <param name="feedback">The user feedback service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="dedicatedChannels">The dedicated channel service.</param>
        public RoleplayCommands
        (
            RoleplayDiscordService discordRoleplays,
            UserFeedbackService feedback,
            InteractivityService interactivity,
            DedicatedChannelService dedicatedChannels
        )
        {
            _discordRoleplays = discordRoleplays;
            _feedback = feedback;
            _interactivity = interactivity;
            _dedicatedChannels = dedicatedChannels;
        }

        /// <summary>
        /// Shows information about the current.
        /// </summary>
        [UsedImplicitly]
        [Alias("show", "info")]
        [Command("show")]
        [Summary("Shows information about the current roleplay.")]
        [RequireContext(ContextType.Guild)]
        public async Task<RuntimeResult> ShowRoleplayAsync()
        {
            if (!(this.Context.Channel is ITextChannel textChannel))
            {
                return RuntimeCommandResult.FromError("This channel isn't a text channel.");
            }

            var getCurrentRoleplayResult = await _discordRoleplays.GetActiveRoleplayAsync(textChannel);
            if (!getCurrentRoleplayResult.IsSuccess)
            {
                return getCurrentRoleplayResult.ToRuntimeResult();
            }

            var roleplay = getCurrentRoleplayResult.Entity;
            var eb = await CreateRoleplayInfoEmbedAsync(roleplay);

            await _feedback.SendEmbedAsync(this.Context.Channel, eb);
            return RuntimeCommandResult.FromSuccess();
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
        public async Task<RuntimeResult> ShowRoleplayAsync(Roleplay roleplay)
        {
            var eb = await CreateRoleplayInfoEmbedAsync(roleplay);

            await _feedback.SendEmbedAsync(this.Context.Channel, eb);
            return RuntimeCommandResult.FromSuccess();
        }

        private async Task<Embed> CreateRoleplayInfoEmbedAsync(Roleplay roleplay)
        {
            var eb = _feedback.CreateEmbedBase();

            eb.WithAuthor(await this.Context.Client.GetUserAsync((ulong)roleplay.Owner.DiscordID));
            eb.WithTitle(roleplay.Name);
            eb.WithDescription(roleplay.Summary);

            eb.AddField("Currently", $"{(roleplay.IsActive ? "Active" : "Inactive")}", true);

            var dedicatedChannelName = roleplay.DedicatedChannelID is null
                ? "None"
                : MentionUtils.MentionChannel((ulong)roleplay.DedicatedChannelID.Value);

            eb.AddField("Dedicated Channel", dedicatedChannelName, true);

            eb.AddField("NSFW", roleplay.IsNSFW ? "Yes" : "No");
            eb.AddField("Public", roleplay.IsPublic ? "Yes" : "No", true);

            var joinedUsers = roleplay.JoinedUsers.Select(async p => await this.Context.Client.GetUserAsync((ulong)p.User.DiscordID));
            var joinedMentions = joinedUsers.Select(async u => (await u).Mention);

            var participantList = (await Task.WhenAll(joinedMentions)).Humanize();
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
        public async Task<RuntimeResult> ListServerRoleplaysAsync()
        {
            var getRoleplays = await _discordRoleplays.GetRoleplaysAsync(this.Context.Guild);
            if (!getRoleplays.IsSuccess)
            {
                return getRoleplays.ToRuntimeResult();
            }

            var roleplays = getRoleplays.Entity.Where(r => r.IsPublic).ToList();

            var appearance = PaginatedAppearanceOptions.Default;
            appearance.Title = "Available Roleplays";

            var paginatedEmbed = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                _interactivity,
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

            return RuntimeCommandResult.FromSuccess();
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
        public async Task<RuntimeResult> ListOwnedRoleplaysAsync(IGuildUser? discordUser = null)
        {
            if (discordUser is null)
            {
                var authorUser = this.Context.User;
                if (!(authorUser is IGuildUser guildUser))
                {
                    return RuntimeCommandResult.FromError("The owner isn't a guild user.");
                }

                discordUser = guildUser;
            }

            var getUserRoleplays = await _discordRoleplays.GetUserRoleplaysAsync(discordUser);
            if (!getUserRoleplays.IsSuccess)
            {
                return getUserRoleplays.ToRuntimeResult();
            }

            var roleplays = getUserRoleplays.Entity.ToList();

            var appearance = PaginatedAppearanceOptions.Default;
            appearance.Title = "Your roleplays";
            appearance.Author = discordUser;

            var paginatedEmbed = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                _interactivity,
                this.Context.User,
                roleplays,
                r => r.Name,
                r => r.Summary,
                "You don't have any roleplays.",
                appearance
            );

            await _interactivity.SendInteractiveMessageAndDeleteAsync
            (
                this.Context.Channel,
                paginatedEmbed,
                TimeSpan.FromMinutes(5.0)
            );

            return RuntimeCommandResult.FromSuccess();
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
        public async Task<RuntimeResult> CreateRoleplayAsync
        (
            string roleplayName,
            string roleplaySummary = "No summary set.",
            bool isNSFW = false,
            bool isPublic = true
        )
        {
            if (!(this.Context.User is IGuildUser guildUser))
            {
                return RuntimeCommandResult.FromError("The current user isn't a guild user.");
            }

            var result = await _discordRoleplays.CreateRoleplayAsync
            (
                guildUser,
                roleplayName,
                roleplaySummary,
                isNSFW,
                isPublic
            );

            if (!result.IsSuccess)
            {
                return result.ToRuntimeResult();
            }

            return RuntimeCommandResult.FromSuccess($"Roleplay \"{result.Entity.Name}\" created.");
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
        public async Task<RuntimeResult> DeleteRoleplayAsync
        (
            [RequireEntityOwnerOrPermission(typeof(DeleteRoleplay), PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            var deletionResult = await _discordRoleplays.DeleteRoleplayAsync(roleplay);
            if (!deletionResult.IsSuccess)
            {
                return deletionResult.ToRuntimeResult();
            }

            var canReplyInChannelAfterDeletion = (long)this.Context.Channel.Id != roleplay.DedicatedChannelID;
            if (canReplyInChannelAfterDeletion)
            {
                return RuntimeCommandResult.FromSuccess($"Roleplay \"{roleplay.Name}\" deleted.");
            }

            var eb = _feedback.CreateEmbedBase();
            eb.WithDescription($"Roleplay \"{roleplay.Name}\" deleted.");

            await _feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, eb.Build(), false);
            return RuntimeCommandResult.FromSuccess();
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
        public async Task<RuntimeResult> JoinRoleplayAsync(Roleplay roleplay)
        {
            if (!(this.Context.User is IGuildUser guildUser))
            {
                return RuntimeCommandResult.FromError("The current user isn't a guild user.");
            }

            var addUserResult = await _discordRoleplays.AddUserToRoleplayAsync(roleplay, guildUser);
            if (!addUserResult.IsSuccess)
            {
                return addUserResult.ToRuntimeResult();
            }

            var roleplayOwnerUser = await this.Context.Guild.GetUserAsync((ulong)roleplay.Owner.DiscordID);

            return RuntimeCommandResult.FromSuccess
            (
                $"Joined {roleplayOwnerUser.Mention}'s roleplay \"{roleplay.Name}\""
            );
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
        public async Task<RuntimeResult> InvitePlayerAsync
        (
            IGuildUser playerToInvite,
            [RequireEntityOwnerOrPermission(typeof(EditRoleplay), PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            var invitePlayerResult = await _discordRoleplays.InviteUserToRoleplayAsync(roleplay, playerToInvite);
            if (!invitePlayerResult.IsSuccess)
            {
                return invitePlayerResult.ToRuntimeResult();
            }

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

            return RuntimeCommandResult.FromSuccess
            (
                $"Invited {playerToInvite.Mention} to {roleplay.Name}."
            );
        }

        /// <summary>
        /// Leaves the roleplay owned by the given person with the given name.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("leave")]
        [Summary("Leaves the roleplay owned by the given person with the given name.")]
        [RequireContext(ContextType.Guild)]
        public async Task<RuntimeResult> LeaveRoleplayAsync(Roleplay roleplay)
        {
            if (!(this.Context.User is IGuildUser guildUser))
            {
                return RuntimeCommandResult.FromError("The current user isn't a guild user.");
            }

            var removeUserResult = await _discordRoleplays.RemoveUserFromRoleplayAsync(roleplay, guildUser);
            if (!removeUserResult.IsSuccess)
            {
                return removeUserResult.ToRuntimeResult();
            }

            var roleplayOwnerUser = await this.Context.Guild.GetUserAsync((ulong)roleplay.Owner.DiscordID);
            return RuntimeCommandResult.FromSuccess
            (
                $"Left {roleplayOwnerUser.Mention}'s roleplay \"{roleplay.Name}\""
            );
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
        public async Task<RuntimeResult> KickRoleplayParticipantAsync
        (
            IGuildUser discordUser,
            [RequireEntityOwnerOrPermission(typeof(KickRoleplayMember), PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            var kickUserResult = await _discordRoleplays.KickUserFromRoleplayAsync(roleplay, discordUser);
            if (!kickUserResult.IsSuccess)
            {
                return kickUserResult.ToRuntimeResult();
            }

            var userDMChannel = await discordUser.GetOrCreateDMChannelAsync();
            try
            {
                await userDMChannel.SendMessageAsync
                (
                    $"You've been removed from the roleplay \"{roleplay.Name}\" by " +
                    $"{this.Context.Message.Author.Username}."
                );
            }
            catch (HttpException hex) when (hex.WasCausedByDMsNotAccepted())
            {
            }
            finally
            {
                await userDMChannel.CloseAsync();
            }

            return RuntimeCommandResult.FromSuccess
            (
                $"{discordUser.Mention} has been kicked from {roleplay.Name}."
            );
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
        public async Task<RuntimeResult> ShowOrCreateDedicatedRoleplayChannel
        (
            [RequireEntityOwnerOrPermission(typeof(StartStopRoleplay), PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            var getDedicatedChannelResult = await _dedicatedChannels.GetDedicatedChannelAsync
            (
                this.Context.Guild,
                roleplay
            );

            if (getDedicatedChannelResult.IsSuccess)
            {
                var existingDedicatedChannel = getDedicatedChannelResult.Entity;
                var message = $"\"{roleplay.Name}\" has a dedicated channel at " +
                              $"{MentionUtils.MentionChannel(existingDedicatedChannel.Id)}";

                return RuntimeCommandResult.FromSuccess(message);
            }

            await _feedback.SendConfirmationAsync(this.Context, "Setting up dedicated channel...");

            // The roleplay either doesn't have a channel, or the one it has has been deleted or is otherwise invalid.
            var result = await _dedicatedChannels.CreateDedicatedChannelAsync
            (
                this.Context.Guild,
                roleplay
            );

            if (!result.IsSuccess)
            {
                return result.ToRuntimeResult();
            }

            var dedicatedChannel = result.Entity;

            if (!roleplay.IsActive || roleplay.ActiveChannelID == (long)dedicatedChannel.Id)
            {
                return RuntimeCommandResult.FromSuccess
                (
                    $"All done! Your roleplay now has a dedicated channel at " +
                    $"{MentionUtils.MentionChannel(dedicatedChannel.Id)}."
                );
            }

            var stopResult = await StopRoleplayAsync(roleplay);
            if (!stopResult.IsSuccess)
            {
                return stopResult;
            }

            var startResult = await StartRoleplayAsync(roleplay);
            if (!startResult.IsSuccess)
            {
                return startResult;
            }

            return RuntimeCommandResult.FromSuccess
            (
                $"All done! Your roleplay now has a dedicated channel at " +
                $"{MentionUtils.MentionChannel(dedicatedChannel.Id)}."
            );
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
        public async Task<RuntimeResult> StartRoleplayAsync
        (
            [RequireEntityOwnerOrPermission(typeof(StartStopRoleplay), PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            var startRoleplayResult = await _discordRoleplays.StartRoleplayAsync
            (
                (ITextChannel)this.Context.Channel,
                roleplay
            );

            if (!startRoleplayResult.IsSuccess)
            {
                return startRoleplayResult.ToRuntimeResult();
            }

            var joinedUsers = roleplay.JoinedUsers.Select
            (
                async p => await this.Context.Client.GetUserAsync((ulong)p.User.DiscordID)
            );

            var joinedMentions = joinedUsers.Select(async u => (await u).Mention);

            var channel = await this.Context.Guild.GetTextChannelAsync((ulong)roleplay.ActiveChannelID!);
            var participantList = (await Task.WhenAll(joinedMentions)).Humanize();

            await channel.SendMessageAsync($"Calling {participantList}!");

            var activationMessage = $"The roleplay \"{roleplay.Name}\" is now active in " +
                                    $"{MentionUtils.MentionChannel(channel.Id)}.";

            return RuntimeCommandResult.FromSuccess(activationMessage);
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
        public async Task<RuntimeResult> StopRoleplayAsync
        (
            [RequireEntityOwnerOrPermission(typeof(StartStopRoleplay), PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            var stopRoleplayAsync = await _discordRoleplays.StopRoleplayAsync(roleplay);
            if (!stopRoleplayAsync.IsSuccess)
            {
                return stopRoleplayAsync.ToRuntimeResult();
            }

            return RuntimeCommandResult.FromSuccess($"The roleplay \"{roleplay.Name}\" has been stopped.");
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
        public async Task<RuntimeResult> IncludePreviousMessagesAsync
        (
            [RequireEntityOwnerOrPermission(typeof(EditRoleplay), PermissionTarget.Other)]
            Roleplay roleplay,
            [OverrideTypeReader(typeof(UncachedMessageTypeReader<IMessage>))]
            IMessage startMessage,
            [OverrideTypeReader(typeof(UncachedMessageTypeReader<IMessage>))]
            IMessage? finalMessage = null
        )
        {
            finalMessage ??= this.Context.Message;

            if (startMessage.Channel != finalMessage.Channel)
            {
                return RuntimeCommandResult.FromError("The messages are not in the same channel.");
            }

            var addedOrUpdatedMessageCount = 0;

            var latestMessage = startMessage;
            while (latestMessage.Timestamp < finalMessage.Timestamp)
            {
                var messages = (await this.Context.Channel.GetMessagesAsync
                (
                    latestMessage, Direction.After
                ).FlattenAsync()).OrderBy(m => m.Timestamp).ToList();

                latestMessage = messages.Last();

                foreach (var message in messages)
                {
                    // Jump out if we've passed the final message
                    if (message.Timestamp > finalMessage.Timestamp)
                    {
                        break;
                    }

                    if (!(message is IUserMessage userMessage))
                    {
                        continue;
                    }

                    var modifyResult = await _discordRoleplays.ConsumeMessageAsync(userMessage);
                    if (modifyResult.IsSuccess)
                    {
                        ++addedOrUpdatedMessageCount;
                    }
                }
            }

            return RuntimeCommandResult.FromSuccess
            (
                $"{addedOrUpdatedMessageCount} messages added to \"{roleplay.Name}\"."
            );
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
        public async Task<RuntimeResult> TransferRoleplayOwnershipAsync
        (
            IGuildUser newOwner,
            [RequireEntityOwnerOrPermission(typeof(TransferRoleplay), PermissionTarget.Other)]
            Roleplay roleplay
        )
        {
            var transferResult = await _discordRoleplays.TransferRoleplayOwnershipAsync
            (
                newOwner,
                roleplay
            );

            if (!transferResult.IsSuccess)
            {
                return transferResult.ToRuntimeResult();
            }

            return RuntimeCommandResult.FromSuccess("Roleplay ownership transferred.");
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
        public async Task<RuntimeResult> ExportRoleplayAsync
        (
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
                    exporter = new PDFRoleplayExporter(this.Context.Guild);
                    break;
                }
                case ExportFormat.Plaintext:
                {
                    exporter = new PlaintextRoleplayExporter(this.Context.Guild);
                    break;
                }
                default:
                {
                    return RuntimeCommandResult.FromError("That export format hasn't been implemented yet.");
                }
            }

            await _feedback.SendConfirmationAsync(this.Context, "Compiling the roleplay...");
            using var output = await exporter.ExportAsync(roleplay);

            await this.Context.Channel.SendFileAsync(output.Data, $"{output.Title}.{output.Format.GetFileExtension()}");
            return RuntimeCommandResult.FromSuccess();
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
        public async Task<RuntimeResult> ReplayRoleplayAsync
        (
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
            var eb = await CreateRoleplayInfoEmbedAsync(roleplay);

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
                    return RuntimeCommandResult.FromSuccess();
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

            return RuntimeCommandResult.FromSuccess();
        }

        /// <summary>
        /// Views the given roleplay, allowing you to read the channel.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("view")]
        [Summary("Views the given roleplay, allowing you to read the channel.")]
        [RequireContext(ContextType.Guild)]
        public async Task<RuntimeResult> ViewRoleplayAsync(Roleplay roleplay)
        {
            var getDedicatedChannelResult = await _dedicatedChannels.GetDedicatedChannelAsync
            (
                this.Context.Guild,
                roleplay
            );

            if (!getDedicatedChannelResult.IsSuccess)
            {
                return RuntimeCommandResult.FromError
                (
                    "The given roleplay doesn't have a dedicated channel. Try using \"!rp export\" instead."
                );
            }

            var user = this.Context.User;
            if (!roleplay.IsPublic && roleplay.ParticipatingUsers.All(p => p.User.DiscordID != (long)user.Id))
            {
                return RuntimeCommandResult.FromError
                (
                    "You don't have permission to view that roleplay."
                );
            }

            var dedicatedChannel = getDedicatedChannelResult.Entity;
            await _dedicatedChannels.SetChannelVisibilityForUserAsync(dedicatedChannel, user, true);

            var channelMention = MentionUtils.MentionChannel(dedicatedChannel.Id);
            return RuntimeCommandResult.FromSuccess
            (
                $"The roleplay \"{roleplay.Name}\" is now visible in {channelMention}."
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
        public async Task<RuntimeResult> HideRoleplayAsync(Roleplay roleplay)
        {
            var getDedicatedChannelResult = await _dedicatedChannels.GetDedicatedChannelAsync
            (
                this.Context.Guild,
                roleplay
            );

            if (!getDedicatedChannelResult.IsSuccess)
            {
                return RuntimeCommandResult.FromError
                (
                    "The given roleplay doesn't have a dedicated channel."
                );
            }

            var user = this.Context.User;
            var dedicatedChannel = getDedicatedChannelResult.Entity;
            await _dedicatedChannels.SetChannelVisibilityForUserAsync(dedicatedChannel, user, false);

            return RuntimeCommandResult.FromSuccess("Roleplay hidden.");
        }

        /// <summary>
        /// Hides all roleplays in the server for the user.
        /// </summary>
        [UsedImplicitly]
        [Command("hide-all")]
        [Summary("Hides all roleplays in the server for the user.")]
        [RequireContext(ContextType.Guild)]
        public async Task<RuntimeResult> HideAllRoleplaysAsync()
        {
            var getRoleplays = await _discordRoleplays.GetRoleplaysAsync(this.Context.Guild);
            if (!getRoleplays.IsSuccess)
            {
                return getRoleplays.ToRuntimeResult();
            }

            var roleplays = getRoleplays.Entity.ToList();
            foreach (var roleplay in roleplays)
            {
                var getDedicatedChannelResult = await _dedicatedChannels.GetDedicatedChannelAsync
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
                await _dedicatedChannels.SetChannelVisibilityForUserAsync(dedicatedChannel, user, false);
            }

            return RuntimeCommandResult.FromSuccess("Roleplays hidden.");
        }

        /// <summary>
        /// Manually refreshes the given roleplay, resetting its last-updated time to now.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("refresh")]
        [Summary("Manually refreshes the given roleplay, resetting its last-updated time to now.")]
        [RequireContext(ContextType.Guild)]
        public async Task<RuntimeResult> RefreshRoleplayAsync(Roleplay roleplay)
        {
            var isOwner = roleplay.IsOwner(this.Context.User);
            var isParticipant = roleplay.HasJoined(this.Context.User);

            if (!(isOwner || isParticipant))
            {
                return RuntimeCommandResult.FromError("You don't own that roleplay, nor are you a participant.");
            }

            var refreshResult = await _discordRoleplays.RefreshRoleplayAsync(roleplay);
            if (!refreshResult.IsSuccess)
            {
                return refreshResult.ToRuntimeResult();
            }

            return RuntimeCommandResult.FromSuccess("Timeout refreshed.");
        }

        /// <summary>
        /// Resets the permission set of all dedicated channels.
        /// </summary>
        [UsedImplicitly]
        [Command("reset-permissions")]
        [Summary("Resets the permission set of all dedicated channels.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(EditRoleplayServerSettings), PermissionTarget.All)]
        public async Task<RuntimeResult> ResetChannelPermissionsAsync()
        {
            await _feedback.SendConfirmationAsync(this.Context, "Working...");

            var getRoleplays = await _discordRoleplays.GetRoleplaysAsync(this.Context.Guild);
            if (!getRoleplays.IsSuccess)
            {
                return getRoleplays.ToRuntimeResult();
            }

            var roleplays = getRoleplays.Entity.ToList();

            foreach (var roleplay in roleplays)
            {
                if (!roleplay.DedicatedChannelID.HasValue)
                {
                    continue;
                }

                var reset = await _dedicatedChannels.ResetChannelPermissionsAsync(this.Context.Guild, roleplay);
                if (!reset.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, reset.ErrorReason);
                }
            }

            return RuntimeCommandResult.FromSuccess("Permissions reset.");
        }

        /// <summary>
        /// Moves an ongoing roleplay outside of the bot's systems into a channel with the given name.
        /// </summary>
        /// <param name="newName">The name of the new bot-managed roleplay.</param>
        /// <param name="participants">The participants of the roleplay.</param>
        [UsedImplicitly]
        [Command("move-to")]
        [Alias("move-to", "copy-to", "move")]
        [Summary("Moves an ongoing roleplay outside of the bot's systems into a channel with the given name.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(CreateRoleplay), PermissionTarget.Self)]
        public async Task<RuntimeResult> MoveRoleplayIntoChannelAsync(string newName, params IGuildUser[] participants)
        {
            if (!(this.Context.User is IGuildUser guildUser))
            {
                return RuntimeCommandResult.FromError("The current user isn't a guild user.");
            }

            var createRoleplayAsync = await _discordRoleplays.CreateRoleplayAsync
            (
                guildUser,
                newName,
                "No summary set.",
                false,
                true
            );

            if (!createRoleplayAsync.IsSuccess)
            {
                return createRoleplayAsync.ToRuntimeResult();
            }

            var roleplay = createRoleplayAsync.Entity;

            foreach (var participant in participants)
            {
                if (participant == this.Context.User)
                {
                    // Already added
                    continue;
                }

                var addParticipantAsync = await _discordRoleplays.AddUserToRoleplayAsync(roleplay, participant);
                if (addParticipantAsync.IsSuccess)
                {
                    continue;
                }

                var message =
                    $"I couldn't add {participant.Mention} to the roleplay ({addParticipantAsync.ErrorReason}. " +
                    $"Please try to invite them manually.";

                await _feedback.SendWarningAsync
                (
                    this.Context,
                    message
                );
            }

            var participantMessages = new List<IMessage>();

            // Copy the last messages from the participants
            foreach (var participant in participants)
            {
                // Find the last message in the current channel from the user
                var channel = this.Context.Channel;
                var messageBatch = await channel.GetMessagesAsync(this.Context.Message, Direction.Before)
                    .FlattenAsync();

                foreach (var message in messageBatch)
                {
                    if (message.Author != participant)
                    {
                        continue;
                    }

                    participantMessages.Add(message);
                    break;
                }
            }

            var getDedicatedChannel = await _dedicatedChannels.GetDedicatedChannelAsync(this.Context.Guild, roleplay);
            if (!getDedicatedChannel.IsSuccess)
            {
                return getDedicatedChannel.ToRuntimeResult();
            }

            var dedicatedChannel = getDedicatedChannel.Entity;

            foreach (var participantMessage in participantMessages.OrderByDescending(m => m.Timestamp))
            {
                var messageLink = participantMessage.GetJumpUrl();
                await dedicatedChannel.SendMessageAsync(messageLink);
            }

            var startRoleplayAsync = await _discordRoleplays.StartRoleplayAsync
            (
                (ITextChannel)this.Context.Channel,
                roleplay
            );

            if (!startRoleplayAsync.IsSuccess)
            {
                return startRoleplayAsync.ToRuntimeResult();
            }

            var joinedUsers = roleplay.JoinedUsers.Select(async p => await this.Context.Client.GetUserAsync((ulong)p.User.DiscordID));
            var joinedMentions = joinedUsers.Select(async u => (await u).Mention);

            var participantList = (await Task.WhenAll(joinedMentions)).Humanize();
            await dedicatedChannel.SendMessageAsync($"Calling {participantList}!");

            return RuntimeCommandResult.FromSuccess
            (
                $"All done! Your roleplay is now available in {MentionUtils.MentionChannel(dedicatedChannel.Id)}."
            );
        }
    }
}
