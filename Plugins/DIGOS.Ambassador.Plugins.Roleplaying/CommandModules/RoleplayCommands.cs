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
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Feedback.Errors;
using DIGOS.Ambassador.Discord.Feedback.Results;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Discord.TypeReaders;
using DIGOS.Ambassador.Plugins.Core.Preconditions;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using DIGOS.Ambassador.Plugins.Roleplaying.Extensions;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Permissions;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using DIGOS.Ambassador.Plugins.Roleplaying.Services.Exporters;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Results;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Roleplaying.CommandModules
{
    /// <summary>
    /// Commands for interacting with and managing channel roleplays.
    /// </summary>
    [UsedImplicitly]
    [Group("roleplay")]
    [Description("Commands for interacting with and managing channel roleplays.")]
    public partial class RoleplayCommands : CommandGroup
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
        [Command("show")]
        [Description("Shows information about the current roleplay.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result> ShowRoleplayAsync()
        {
            var getCurrentRoleplayResult = await _discordRoleplays.GetActiveRoleplayAsync(textChannel);
            if (!getCurrentRoleplayResult.IsSuccess)
            {
                return getCurrentRoleplayResult;
            }

            var roleplay = getCurrentRoleplayResult.Entity;
            var eb = await CreateRoleplayInfoEmbedAsync(roleplay);

            await _feedback.SendEmbedAsync(_context.Channel, eb);
            return Result.FromSuccess();
        }

        /// <summary>
        /// Shows information about the named roleplay owned by the specified user.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("show")]
        [Description("Shows information about the specified roleplay.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result> ShowRoleplayAsync(Roleplay roleplay)
        {
            var eb = await CreateRoleplayInfoEmbedAsync(roleplay);

            await _feedback.SendEmbedAsync(_context.Channel, eb);
            return Result.FromSuccess();
        }

        private async Task<Embed> CreateRoleplayInfoEmbedAsync(Roleplay roleplay)
        {
            var eb = _feedback.CreateEmbedBase();

            eb.WithAuthor(await _context.Client.GetUserAsync((ulong)roleplay.Owner.DiscordID));
            eb.WithTitle(roleplay.Name);
            eb.WithDescription(roleplay.Summary);

            eb.AddField("Currently", $"{(roleplay.IsActive ? "Active" : "Inactive")}", true);

            var dedicatedChannelName = roleplay.DedicatedChannelID is null
                ? "None"
                : MentionUtils.MentionChannel((ulong)roleplay.DedicatedChannelID.Value);

            eb.AddField("Dedicated Channel", dedicatedChannelName, true);

            eb.AddField("NSFW", roleplay.IsNSFW ? "Yes" : "No");
            eb.AddField("Public", roleplay.IsPublic ? "Yes" : "No", true);

            var joinedUsers =
                roleplay.JoinedUsers.Select(async p => await _context.Client.GetUserAsync((ulong)p.User.DiscordID));
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
        [Command("list")]
        [Description("Lists all available roleplays in the server.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result> ListServerRoleplaysAsync()
        {
            var getRoleplays = await _discordRoleplays.GetRoleplaysAsync(_context.Guild);
            if (!getRoleplays.IsSuccess)
            {
                return getRoleplays;
            }

            var roleplays = getRoleplays.Entity.Where(r => r.IsPublic).ToList();

            var appearance = PaginatedAppearanceOptions.Default;
            appearance.Title = "Available Roleplays";

            var paginatedEmbed = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                _interactivity,
                _context.User,
                roleplays,
                r => r.Name,
                r => r.Summary,
                "There are no roleplays in the server that you can view.",
                appearance
            );

            await _interactivity.SendInteractiveMessageAndDeleteAsync
            (
                _context.Channel,
                paginatedEmbed,
                TimeSpan.FromMinutes(5.0)
            );

            return Result.FromSuccess();
        }

        /// <summary>
        /// Lists the roleplays that the given user owns.
        /// </summary>
        /// <param name="discordUser">The user to show the roleplays of.</param>
        [UsedImplicitly]
        [Command("list-owned")]
        [Description("Lists the roleplays that the given user owns.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result> ListOwnedRoleplaysAsync(IGuildMember? discordUser = null)
        {
            if (discordUser is null)
            {
                var authorUser = _context.User;
                if (!(authorUser is IGuildMember guildUser))
                {
                    return new UserError("The owner isn't a guild user.");
                }

                discordUser = guildUser;
            }

            var getUserRoleplays = await _discordRoleplays.GetUserRoleplaysAsync(discordUser);
            if (!getUserRoleplays.IsSuccess)
            {
                return getUserRoleplays;
            }

            var roleplays = getUserRoleplays.Entity.ToList();

            var appearance = PaginatedAppearanceOptions.Default;
            appearance.Title = "Your roleplays";
            appearance.Author = discordUser;

            var paginatedEmbed = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                _interactivity,
                _context.User,
                roleplays,
                r => r.Name,
                r => r.Summary,
                "You don't have any roleplays.",
                appearance
            );

            await _interactivity.SendInteractiveMessageAndDeleteAsync
            (
                _context.Channel,
                paginatedEmbed,
                TimeSpan.FromMinutes(5.0)
            );

            return Result.FromSuccess();
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
        [Description("Creates a new roleplay with the specified name.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(CreateRoleplay), PermissionTarget.Self)]
        public async Task<Result> CreateRoleplayAsync
        (
            string roleplayName,
            string roleplaySummary = "No summary set.",
            bool isNSFW = false,
            bool isPublic = true
        )
        {
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
                return result;
            }

            return new ConfirmationMessage($"Roleplay \"{result.Entity.Name}\" created.");
        }

        /// <summary>
        /// Deletes the specified roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("delete")]
        [Description("Deletes the specified roleplay.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(DeleteRoleplay), PermissionTarget.Self)]
        public async Task<Result> DeleteRoleplayAsync
        (
            [RequireEntityOwner] Roleplay roleplay
        )
        {
            var deletionResult = await _discordRoleplays.DeleteRoleplayAsync(roleplay);
            if (!deletionResult.IsSuccess)
            {
                return deletionResult;
            }

            var canReplyInChannelAfterDeletion = (long)_context.Channel.Id != roleplay.DedicatedChannelID;
            if (canReplyInChannelAfterDeletion)
            {
                return new ConfirmationMessage($"Roleplay \"{roleplay.Name}\" deleted.");
            }

            var eb = _feedback.CreateEmbedBase();
            eb.WithDescription($"Roleplay \"{roleplay.Name}\" deleted.");

            await _feedback.SendPrivateEmbedAsync(_context, _context.User, eb.Build(), false);
            return Result.FromSuccess();
        }

        /// <summary>
        /// Joins the roleplay owned by the given person with the given name.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("join")]
        [Description("Joins the roleplay owned by the given person with the given name.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(JoinRoleplay), PermissionTarget.Self)]
        public async Task<Result> JoinRoleplayAsync(Roleplay roleplay)
        {
            var addUserResult = await _discordRoleplays.AddUserToRoleplayAsync(roleplay);
            if (!addUserResult.IsSuccess)
            {
                return addUserResult;
            }

            var roleplayOwnerUser = await _context.Guild.GetUserAsync((ulong)roleplay.Owner.DiscordID);

            return Result.FromSuccess
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
        [Description("Invites the specified user to the given roleplay.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(EditRoleplay), PermissionTarget.Self)]
        public async Task<Result> InvitePlayerAsync
        (
            IGuildMember playerToInvite,
            [RequireEntityOwner] Roleplay roleplay
        )
        {
            var invitePlayerResult = await _discordRoleplays.InviteUserToRoleplayAsync(roleplay);
            if (!invitePlayerResult.IsSuccess)
            {
                return invitePlayerResult;
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

            return Result.FromSuccess
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
        [Description("Leaves the roleplay owned by the given person with the given name.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result> LeaveRoleplayAsync(Roleplay roleplay)
        {
            var removeUserResult = await _discordRoleplays.RemoveUserFromRoleplayAsync(roleplay);
            if (!removeUserResult.IsSuccess)
            {
                return removeUserResult;
            }

            var roleplayOwnerUser = await _context.Guild.GetUserAsync((ulong)roleplay.Owner.DiscordID);
            return Result.FromSuccess
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
        [Description("Kicks the given user from the named roleplay.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(KickRoleplayMember), PermissionTarget.Self)]
        public async Task<Result> KickRoleplayParticipantAsync
        (
            IGuildMember discordUser,
            [RequireEntityOwner] Roleplay roleplay
        )
        {
            var kickUserResult = await _discordRoleplays.KickUserFromRoleplayAsync(roleplay);
            if (!kickUserResult.IsSuccess)
            {
                return kickUserResult;
            }

            var userDMChannel = await discordUser.GetOrCreateDMChannelAsync();
            try
            {
                await userDMChannel.SendMessageAsync
                (
                    $"You've been removed from the roleplay \"{roleplay.Name}\" by " +
                    $"{_context.Message.Author.Username}."
                );
            }
            catch (HttpException hex) when (hex.WasCausedByDMsNotAccepted())
            {
            }
            finally
            {
                await userDMChannel.CloseAsync();
            }

            return Result.FromSuccess
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
        [Description("Makes the roleplay with the given name current in the current channel.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(StartStopRoleplay), PermissionTarget.Self)]
        public async Task<Result> ShowOrCreateDedicatedRoleplayChannel
        (
            [RequireEntityOwner] Roleplay roleplay
        )
        {
            var getDedicatedChannelResult = await _dedicatedChannels.GetDedicatedChannelAsync
            (roleplay
            );

            if (getDedicatedChannelResult.IsSuccess)
            {
                var existingDedicatedChannel = getDedicatedChannelResult.Entity;
                var message = $"\"{roleplay.Name}\" has a dedicated channel at " +
                              $"{MentionUtils.MentionChannel(existingDedicatedChannel.Id)}";

                return Result.FromSuccess(message);
            }

            await _feedback.SendConfirmationAsync(_context, "Setting up dedicated channel...");

            // The roleplay either doesn't have a channel, or the one it has has been deleted or is otherwise invalid.
            var result = await _dedicatedChannels.CreateDedicatedChannelAsync
            (
                _context.Guild,
                roleplay
            );

            if (!result.IsSuccess)
            {
                return result;
            }

            var dedicatedChannel = result.Entity;

            if (!roleplay.IsActive || roleplay.ActiveChannelID == (long)dedicatedChannel.Id)
            {
                return Result.FromSuccess
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

            return Result.FromSuccess
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
        [Description("Starts the roleplay with the given name.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(StartStopRoleplay), PermissionTarget.Self)]
        public async Task<Result> StartRoleplayAsync
        (
            [RequireEntityOwner] Roleplay roleplay
        )
        {
            var startRoleplayResult = await _discordRoleplays.StartRoleplayAsync
            (
                (IChannel)_context.Channel,
                roleplay
            );

            if (!startRoleplayResult.IsSuccess)
            {
                return startRoleplayResult;
            }

            var joinedUsers = roleplay.JoinedUsers.Select
            (
                async p => await _context.Client.GetUserAsync((ulong)p.User.DiscordID)
            );

            var joinedMentions = joinedUsers.Select(async u => (await u).Mention);

            var channel = await _context.Guild.GetTextChannelAsync((ulong)roleplay.ActiveChannelID!);
            var participantList = (await Task.WhenAll(joinedMentions)).Humanize();

            await channel.SendMessageAsync($"Calling {participantList}!");

            var activationMessage = $"The roleplay \"{roleplay.Name}\" is now active in " +
                                    $"{MentionUtils.MentionChannel(channel.Id)}.";

            return Result.FromSuccess(activationMessage);
        }

        /// <summary>
        /// Stops the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("stop")]
        [Description("Stops the given roleplay.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(StartStopRoleplay), PermissionTarget.Self)]
        public async Task<Result> StopRoleplayAsync
        (
            [RequireEntityOwner] Roleplay roleplay
        )
        {
            var stopRoleplayAsync = await _discordRoleplays.StopRoleplayAsync(roleplay);
            if (!stopRoleplayAsync.IsSuccess)
            {
                return stopRoleplayAsync;
            }

            return new ConfirmationMessage($"The roleplay \"{roleplay.Name}\" has been stopped.");
        }

        /// <summary>
        /// Includes previous messages into the roleplay, starting at the given time.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="startMessage">The earliest message to start adding from.</param>
        /// <param name="finalMessage">The final message in the range.</param>
        [UsedImplicitly]
        [Command("include-previous")]
        [Description("Includes previous messages into the roleplay, starting at the given message.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(EditRoleplay), PermissionTarget.Self)]
        public async Task<Result> IncludePreviousMessagesAsync
        (
            [RequireEntityOwner] Roleplay roleplay,
            IMessage startMessage,
            IMessage? finalMessage = null
        )
        {
            finalMessage ??= _context.Message;

            if (startMessage.Channel != finalMessage.Channel)
            {
                return new UserError("The messages are not in the same channel.");
            }

            var addedOrUpdatedMessageCount = 0;

            var latestMessage = startMessage;
            while (latestMessage.Timestamp < finalMessage.Timestamp)
            {
                var messages = (await _context.Channel.GetMessagesAsync
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

            return Result.FromSuccess
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
        [Description("Transfers ownership of the named roleplay to the specified user.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(TransferRoleplay), PermissionTarget.Self)]
        public async Task<Result> TransferRoleplayOwnershipAsync
        (
            IGuildMember newOwner,
            [RequireEntityOwner] Roleplay roleplay
        )
        {
            var transferResult = await _discordRoleplays.TransferRoleplayOwnershipAsync
            (roleplay
            );

            if (!transferResult.IsSuccess)
            {
                return transferResult;
            }

            return new ConfirmationMessage("Roleplay ownership transferred.");
        }

        /// <summary>
        /// Exports the named roleplay owned by the given user, sending you a file with the contents.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="format">The export format.</param>
        [UsedImplicitly]
        [Command("export")]
        [Description(" Exports the named roleplay owned by the given user, sending you a file with the contents.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(ExportRoleplay), PermissionTarget.Self)]
        public async Task<Result> ExportRoleplayAsync
        (
            [RequireEntityOwner] Roleplay roleplay,
            ExportFormat format = ExportFormat.PDF
        )
        {
            IRoleplayExporter exporter;
            switch (format)
            {
                case ExportFormat.PDF:
                {
                    exporter = new PDFRoleplayExporter(_context.Guild);
                    break;
                }
                case ExportFormat.Plaintext:
                {
                    exporter = new PlaintextRoleplayExporter(_context.Guild);
                    break;
                }
                default:
                {
                    return new UserError("That export format hasn't been implemented yet.");
                }
            }

            await _feedback.SendConfirmationAsync(_context, "Compiling the roleplay...");
            using var output = await exporter.ExportAsync(roleplay);

            await _context.Channel.SendFileAsync(output.Data, $"{output.Title}.{output.Format.GetFileExtension()}");
            return Result.FromSuccess();
        }

        /// <summary>
        /// Replays the named roleplay owned by the given user to you.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="from">The time from which you want to replay.</param>
        /// <param name="to">The time until you want to replay.</param>
        [UsedImplicitly]
        [Command("replay")]
        [Description("Replays the named roleplay owned by the given user to you.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(ExportRoleplay), PermissionTarget.Self)]
        public async Task<Result> ReplayRoleplayAsync
        (
            [RequireEntityOwner] Roleplay roleplay,
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

            var userDMChannel = await _context.Message.Author.GetOrCreateDMChannelAsync();
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
                    _context.User,
                    Color.DarkPurple,
                    $"Roleplay began at {messages.First().Timestamp.ToUniversalTime()}"
                );

                await userDMChannel.SendMessageAsync(string.Empty, false, timestampEmbed);

                if (messages.Count <= 0)
                {
                    await userDMChannel.SendMessageAsync("No messages found in the specified timeframe.");
                    return Result.FromSuccess();
                }

                await _feedback.SendConfirmationAsync
                (
                    _context,
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
                    _context,
                    "I can't do that, since you don't accept DMs from non-friends on this server."
                );
            }
            finally
            {
                await userDMChannel.CloseAsync();
            }

            return Result.FromSuccess();
        }

        /// <summary>
        /// Views the given roleplay, allowing you to read the channel.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("view")]
        [Description("Views the given roleplay, allowing you to read the channel.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result> ViewRoleplayAsync(Roleplay roleplay)
        {
            var getDedicatedChannelResult = await _dedicatedChannels.GetDedicatedChannelAsync
            (roleplay
            );

            if (!getDedicatedChannelResult.IsSuccess)
            {
                return new UserError
                (
                    "The given roleplay doesn't have a dedicated channel. Try using \"!rp export\" instead."
                );
            }

            var user = _context.User;
            if (!roleplay.IsPublic && roleplay.ParticipatingUsers.All(p => p.User.DiscordID != (long)user.Id))
            {
                return new UserError
                (
                    "You don't have permission to view that roleplay."
                );
            }

            var dedicatedChannel = getDedicatedChannelResult.Entity;
            await _dedicatedChannels.SetChannelVisibilityForUserAsync(dedicatedChannel, user, true);

            var channelMention = MentionUtils.MentionChannel(dedicatedChannel.Id);
            return Result.FromSuccess
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
        [Description("Hides the given roleplay.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result> HideRoleplayAsync(Roleplay roleplay)
        {
            var getDedicatedChannelResult = await _dedicatedChannels.GetDedicatedChannelAsync
            (roleplay
            );

            if (!getDedicatedChannelResult.IsSuccess)
            {
                return new UserError
                (
                    "The given roleplay doesn't have a dedicated channel."
                );
            }

            var user = _context.User;
            var dedicatedChannel = getDedicatedChannelResult.Entity;
            await _dedicatedChannels.SetChannelVisibilityForUserAsync(dedicatedChannel, user, false);

            return new ConfirmationMessage("Roleplay hidden.");
        }

        /// <summary>
        /// Hides all roleplays in the server for the user.
        /// </summary>
        [UsedImplicitly]
        [Command("hide-all")]
        [Description("Hides all roleplays in the server for the user.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result> HideAllRoleplaysAsync()
        {
            var getRoleplays = await _discordRoleplays.GetRoleplaysAsync(_context.Guild);
            if (!getRoleplays.IsSuccess)
            {
                return getRoleplays;
            }

            var roleplays = getRoleplays.Entity.ToList();
            foreach (var roleplay in roleplays)
            {
                var getDedicatedChannelResult = await _dedicatedChannels.GetDedicatedChannelAsync
                (roleplay
                );

                if (!getDedicatedChannelResult.IsSuccess)
                {
                    continue;
                }

                var user = _context.User;
                var dedicatedChannel = getDedicatedChannelResult.Entity;
                await _dedicatedChannels.SetChannelVisibilityForUserAsync(dedicatedChannel, user, false);
            }

            return new ConfirmationMessage("Roleplays hidden.");
        }

        /// <summary>
        /// Manually refreshes the given roleplay, resetting its last-updated time to now.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("refresh")]
        [Description("Manually refreshes the given roleplay, resetting its last-updated time to now.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result> RefreshRoleplayAsync(Roleplay roleplay)
        {
            var isOwner = roleplay.IsOwner(_context.User);
            var isParticipant = roleplay.HasJoined(_context.User);

            if (!(isOwner || isParticipant))
            {
                return new UserError("You don't own that roleplay, nor are you a participant.");
            }

            var refreshResult = await _discordRoleplays.RefreshRoleplayAsync(roleplay);
            if (!refreshResult.IsSuccess)
            {
                return refreshResult;
            }

            return new ConfirmationMessage("Timeout refreshed.");
        }

        /// <summary>
        /// Resets the permission set of all dedicated channels.
        /// </summary>
        [UsedImplicitly]
        [Command("reset-permissions")]
        [Description("Resets the permission set of all dedicated channels.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(EditRoleplayServerSettings), PermissionTarget.All)]
        public async Task<Result> ResetChannelPermissionsAsync()
        {
            await _feedback.SendConfirmationAsync(_context, "Working...");

            var getRoleplays = await _discordRoleplays.GetRoleplaysAsync(_context.Guild);
            if (!getRoleplays.IsSuccess)
            {
                return getRoleplays;
            }

            var roleplays = getRoleplays.Entity.ToList();

            foreach (var roleplay in roleplays)
            {
                if (!roleplay.DedicatedChannelID.HasValue)
                {
                    continue;
                }

                var reset = await _dedicatedChannels.ResetChannelPermissionsAsync(_context.Guild, roleplay);
                if (!reset.IsSuccess)
                {
                    await _feedback.SendErrorAsync(_context, reset.ErrorReason);
                }
            }

            return new ConfirmationMessage("Permissions reset.");
        }

        /// <summary>
        /// Moves an ongoing roleplay outside of the bot's systems into a channel with the given name.
        /// </summary>
        /// <param name="newName">The name of the new bot-managed roleplay.</param>
        /// <param name="participants">The participants of the roleplay.</param>
        [UsedImplicitly]
        [Command("move-to")]
        [Description("Moves an ongoing roleplay outside of the bot's systems into a channel with the given name.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(CreateRoleplay), PermissionTarget.Self)]
        public async Task<Result> MoveRoleplayIntoChannelAsync(string newName, params IGuildMember[] participants)
        {
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
                return createRoleplayAsync;
            }

            var roleplay = createRoleplayAsync.Entity;

            foreach (var participant in participants)
            {
                if (participant == _context.User)
                {
                    // Already added
                    continue;
                }

                var addParticipantAsync = await _discordRoleplays.AddUserToRoleplayAsync(roleplay);
                if (addParticipantAsync.IsSuccess)
                {
                    continue;
                }

                var message =
                    $"I couldn't add {participant.Mention} to the roleplay ({addParticipantAsync.ErrorReason}. " +
                    $"Please try to invite them manually.";

                await _feedback.SendWarningAsync
                (
                    _context,
                    message
                );
            }

            var participantMessages = new List<IMessage>();

            // Copy the last messages from the participants
            foreach (var participant in participants)
            {
                // Find the last message in the current channel from the user
                var channel = _context.Channel;
                var messageBatch = await channel.GetMessagesAsync(_context.Message, Direction.Before)
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

            var getDedicatedChannel = await _dedicatedChannels.GetDedicatedChannelAsync(roleplay);
            if (!getDedicatedChannel.IsSuccess)
            {
                return getDedicatedChannel;
            }

            var dedicatedChannel = getDedicatedChannel.Entity;

            foreach (var participantMessage in participantMessages.OrderByDescending(m => m.Timestamp))
            {
                var messageLink = participantMessage.GetJumpUrl();
                await dedicatedChannel.SendMessageAsync(messageLink);
            }

            var startRoleplayAsync = await _discordRoleplays.StartRoleplayAsync
            (
                (IChannel)_context.Channel,
                roleplay
            );

            if (!startRoleplayAsync.IsSuccess)
            {
                return startRoleplayAsync;
            }

            var joinedUsers =
                roleplay.JoinedUsers.Select(async p => await _context.Client.GetUserAsync((ulong)p.User.DiscordID));
            var joinedMentions = joinedUsers.Select(async u => (await u).Mention);

            var participantList = (await Task.WhenAll(joinedMentions)).Humanize();
            await dedicatedChannel.SendMessageAsync($"Calling {participantList}!");

            return Result.FromSuccess
            (
                $"All done! Your roleplay is now available in {MentionUtils.MentionChannel(dedicatedChannel.Id)}."
            );
        }
    }
}
