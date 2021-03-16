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
using System.Diagnostics;
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
using DIGOS.Ambassador.Discord.Pagination.Extensions;
using DIGOS.Ambassador.Discord.TypeReaders;
using DIGOS.Ambassador.Plugins.Core.Preconditions;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using DIGOS.Ambassador.Plugins.Roleplaying.Extensions;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Permissions;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using DIGOS.Ambassador.Plugins.Roleplaying.Services.Exporters;
using Humanizer;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;
using UserMessage = DIGOS.Ambassador.Discord.Feedback.Results.UserMessage;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Roleplaying.CommandModules
{
    /// <summary>
    /// Commands for interacting with and managing channel roleplays.
    /// </summary>
    [UsedImplicitly]
    [Group("rp")]
    [Description("Commands for interacting with and managing channel roleplays.")]
    public partial class RoleplayCommands : CommandGroup
    {
        private readonly IServiceProvider _services;
        private readonly IDiscordRestChannelAPI _channelAPI;

        private readonly RoleplayDiscordService _discordRoleplays;
        private readonly DedicatedChannelService _dedicatedChannels;

        private readonly UserFeedbackService _feedback;
        private readonly InteractivityService _interactivity;

        private readonly ICommandContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayCommands"/> class.
        /// </summary>
        /// <param name="discordRoleplays">The roleplay service.</param>
        /// <param name="feedback">The user feedback service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="dedicatedChannels">The dedicated channel service.</param>
        /// <param name="context">The command context.</param>
        /// <param name="channelAPI">The channel API.</param>
        /// <param name="services">The available services.</param>
        public RoleplayCommands
        (
            RoleplayDiscordService discordRoleplays,
            UserFeedbackService feedback,
            InteractivityService interactivity,
            DedicatedChannelService dedicatedChannels,
            ICommandContext context,
            IDiscordRestChannelAPI channelAPI,
            IServiceProvider services
        )
        {
            _discordRoleplays = discordRoleplays;
            _feedback = feedback;
            _interactivity = interactivity;
            _dedicatedChannels = dedicatedChannels;
            _context = context;
            _channelAPI = channelAPI;
            _services = services;
        }

        /// <summary>
        /// Shows information about the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("show")]
        [Description("Shows information about the specified roleplay.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<IResult> ShowRoleplayAsync(Roleplay? roleplay = null)
        {
            if (roleplay is null)
            {
                var getCurrentRoleplayResult = await _discordRoleplays.GetActiveRoleplayAsync(_context.ChannelID);
                if (!getCurrentRoleplayResult.IsSuccess)
                {
                    return getCurrentRoleplayResult;
                }

                roleplay = getCurrentRoleplayResult.Entity;
            }

            var eb = CreateRoleplayInfoEmbed(roleplay);
            return await _feedback.SendEmbedAsync(_context.ChannelID, eb);
        }

        private Embed CreateRoleplayInfoEmbed(Roleplay roleplay)
        {
            var fields = new List<IEmbedField>();
            var eb = _feedback.CreateEmbedBase() with
            {
                Title = roleplay.Name,
                Description = roleplay.Summary,
                Fields = fields
            };

            fields.Add(new EmbedField("Currently", $"{(roleplay.IsActive ? "Active" : "Inactive")}", true));

            var dedicatedChannelName = roleplay.DedicatedChannelID is null
                ? "None"
                : $"<#{roleplay.DedicatedChannelID.Value}";

            fields.Add(new EmbedField("Dedicated Channel", dedicatedChannelName, true));

            fields.Add(new EmbedField("NSFW", roleplay.IsNSFW ? "Yes" : "No"));
            fields.Add(new EmbedField("Public", roleplay.IsPublic ? "Yes" : "No", true));

            var joinedUsers = roleplay.JoinedUsers.Select(u => $"<@{u.User.DiscordID}>");

            var participantList = joinedUsers.Humanize();
            participantList = string.IsNullOrEmpty(participantList) ? "None" : participantList;

            fields.Add(new EmbedField("Participants", $"{participantList}"));

            return eb;
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
            var roleplays = await _discordRoleplays.QueryRoleplaysAsync
            (
                q => q
                    .Where(rp => rp.Server.DiscordID == _context.GuildID.Value)
                    .Where(rp => rp.IsPublic)
            );

            var pages = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                roleplays,
                r => r.Name,
                r => r.Summary,
                "There are no roleplays in the server that you can view."
            );

            return await _interactivity.SendInteractiveMessageAsync
            (
                _context.ChannelID,
                _context.User.ID,
                pages
            );
        }

        /// <summary>
        /// Lists the roleplays that the given user owns.
        /// </summary>
        /// <param name="discordUser">The user to show the roleplays of.</param>
        [UsedImplicitly]
        [Command("list-owned")]
        [Description("Lists the roleplays that the given user owns.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result> ListOwnedRoleplaysAsync(IUser? discordUser = null)
        {
            discordUser ??= _context.User;

            var roleplays = await _discordRoleplays.QueryRoleplaysAsync
            (
                q => q
                    .Where(rp => rp.Server.DiscordID == _context.GuildID.Value)
                    .Where(rp => rp.Owner.DiscordID == discordUser.ID)
            );

            var pages = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                roleplays,
                r => r.Name,
                r => r.Summary,
                "You don't have any roleplays."
            );

            return await _interactivity.SendInteractiveMessageAsync
            (
                _context.ChannelID,
                _context.User.ID,
                pages
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
        [Description("Creates a new roleplay with the specified name.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(CreateRoleplay), PermissionTarget.Self)]
        public async Task<Result<UserMessage>> CreateRoleplayAsync
        (
            string roleplayName,
            string roleplaySummary = "No summary set.",
            bool isNSFW = false,
            bool isPublic = true
        )
        {
            var result = await _discordRoleplays.CreateRoleplayAsync
            (
                _context.GuildID.Value,
                _context.User.ID,
                roleplayName,
                roleplaySummary,
                isNSFW,
                isPublic
            );

            if (!result.IsSuccess)
            {
                return Result<UserMessage>.FromError(result);
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
        public async Task<IResult> DeleteRoleplayAsync
        (
            [RequireEntityOwner] Roleplay roleplay
        )
        {
            var deletionResult = await _discordRoleplays.DeleteRoleplayAsync(roleplay);
            if (!deletionResult.IsSuccess)
            {
                return deletionResult;
            }

            var canReplyInChannelAfterDeletion = _context.ChannelID != roleplay.DedicatedChannelID;
            if (canReplyInChannelAfterDeletion)
            {
                return Result<UserMessage>.FromSuccess(new ConfirmationMessage($"Roleplay \"{roleplay.Name}\" deleted."));
            }

            var eb = _feedback.CreateEmbedBase() with
            {
                Description = $"Roleplay \"{roleplay.Name}\" deleted."
            };

            return await _feedback.SendPrivateEmbedAsync(_context.User.ID, eb);
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
        public async Task<Result<UserMessage>> JoinRoleplayAsync(Roleplay roleplay)
        {
            var addUserResult = await _discordRoleplays.AddUserToRoleplayAsync(roleplay, _context.User.ID);
            if (!addUserResult.IsSuccess)
            {
                return Result<UserMessage>.FromError(addUserResult);
            }

            return new ConfirmationMessage
            (
                $"Joined <@{roleplay.Owner.ID}>'s roleplay \"{roleplay.Name}\""
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
        public async Task<Result<UserMessage>> InvitePlayerAsync
        (
            IUser playerToInvite,
            [RequireEntityOwner] Roleplay roleplay
        )
        {
            var invitePlayerResult = await _discordRoleplays.InviteUserToRoleplayAsync(roleplay, playerToInvite.ID);
            if (!invitePlayerResult.IsSuccess)
            {
                return Result<UserMessage>.FromError(invitePlayerResult);
            }

            var roleplayName = roleplay.Name.Contains(" ") ? roleplay.Name.Quote() : roleplay.Name;
            var message = _feedback.CreateEmbedBase() with
            {
                Description = $"You've been invited to join {roleplay.Name}. Use `!rp join {roleplayName}` to join."
            };

            var send = await _feedback.SendPrivateEmbedAsync(playerToInvite.ID, message);
            if (!send.IsSuccess)
            {
                return Result<UserMessage>.FromError(send);
            }

            return new ConfirmationMessage
            (
                $"Invited <@{playerToInvite.ID}> to {roleplay.Name}."
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
        public async Task<Result<UserMessage>> LeaveRoleplayAsync(Roleplay roleplay)
        {
            var removeUserResult = await _discordRoleplays.RemoveUserFromRoleplayAsync(roleplay, _context.User.ID);
            if (!removeUserResult.IsSuccess)
            {
                return Result<UserMessage>.FromError(removeUserResult);
            }

            return new ConfirmationMessage
            (
                $"Left <@{roleplay.Owner.DiscordID}>'s roleplay \"{roleplay.Name}\""
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
        public async Task<Result<UserMessage>> KickRoleplayParticipantAsync
        (
            IUser discordUser,
            [RequireEntityOwner] Roleplay roleplay
        )
        {
            var kickUserResult = await _discordRoleplays.KickUserFromRoleplayAsync(roleplay, discordUser.ID);
            if (!kickUserResult.IsSuccess)
            {
                return Result<UserMessage>.FromError(kickUserResult);
            }

            var message = _feedback.CreateEmbedBase() with
            {
                Description = $"You've been removed from the roleplay \"{roleplay.Name}\" by " +
                              $"<@{_context.User.ID}>."
            };

            // It's fine if this one fails
            _ = await _feedback.SendPrivateEmbedAsync(discordUser.ID, message);

            return new ConfirmationMessage
            (
                $"<@{discordUser.ID}> has been kicked from {roleplay.Name}."
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
        public async Task<Result<UserMessage>> ShowOrCreateDedicatedRoleplayChannel
        (
            [RequireEntityOwner] Roleplay roleplay
        )
        {
            var getDedicatedChannelResult = _dedicatedChannels.GetDedicatedChannel(roleplay);
            if (getDedicatedChannelResult.IsSuccess)
            {
                var existingDedicatedChannel = getDedicatedChannelResult.Entity;
                var message = $"\"{roleplay.Name}\" has a dedicated channel at " +
                              $"<#{existingDedicatedChannel}>";

                return new ConfirmationMessage(message);
            }

            var workingMessage = _feedback.CreateEmbedBase() with
            {
                Description = "Setting up dedicated channel..."
            };

            var send = await _feedback.SendEmbedAsync(_context.ChannelID, workingMessage);
            if (!send.IsSuccess)
            {
                return Result<UserMessage>.FromError(send);
            }

            // The roleplay either doesn't have a channel, or the one it has has been deleted or is otherwise invalid.
            var result = await _dedicatedChannels.CreateDedicatedChannelAsync(roleplay);
            if (!result.IsSuccess)
            {
                return Result<UserMessage>.FromError(result);
            }

            var dedicatedChannel = result.Entity;

            if (!roleplay.IsActive || roleplay.ActiveChannelID == dedicatedChannel.ID)
            {
                return new ConfirmationMessage
                (
                    $"All done! Your roleplay now has a dedicated channel at " +
                    $"<#{dedicatedChannel}>."
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

            return new ConfirmationMessage
            (
                $"All done! Your roleplay now has a dedicated channel at " +
                $"<#{dedicatedChannel}>."
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
        public async Task<Result<UserMessage>> StartRoleplayAsync
        (
            [RequireEntityOwner] Roleplay roleplay
        )
        {
            var startRoleplayResult = await _discordRoleplays.StartRoleplayAsync
            (
                _context.ChannelID,
                roleplay
            );

            if (!startRoleplayResult.IsSuccess)
            {
                return Result<UserMessage>.FromError(startRoleplayResult);
            }

            var joinedUsers = roleplay.JoinedUsers.Select
            (
                u => $"<@{u.ID}>"
            );

            var participantList = joinedUsers.Humanize();

            var send = await _channelAPI.CreateMessageAsync
            (
                roleplay.ActiveChannelID!.Value,
                $"Calling {participantList}!"
            );

            if (!send.IsSuccess)
            {
                return Result<UserMessage>.FromError(send);
            }

            var activationMessage = $"The roleplay \"{roleplay.Name}\" is now active in " +
                                    $"<#{roleplay.ActiveChannelID.Value}>.";

            return new ConfirmationMessage(activationMessage);
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
        public async Task<Result<UserMessage>> StopRoleplayAsync
        (
            [RequireEntityOwner] Roleplay roleplay
        )
        {
            var stopRoleplayAsync = await _discordRoleplays.StopRoleplayAsync(roleplay);
            if (!stopRoleplayAsync.IsSuccess)
            {
                return Result<UserMessage>.FromError(stopRoleplayAsync);
            }

            return new ConfirmationMessage($"The roleplay \"{roleplay.Name}\" has been stopped.");
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
        public async Task<Result<UserMessage>> TransferRoleplayOwnershipAsync
        (
            IUser newOwner,
            [RequireEntityOwner] Roleplay roleplay
        )
        {
            var transferResult = await _discordRoleplays.TransferRoleplayOwnershipAsync(newOwner.ID, roleplay);
            if (!transferResult.IsSuccess)
            {
                return Result<UserMessage>.FromError(transferResult);
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
        public async Task<IResult> ExportRoleplayAsync
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
                    exporter = new PDFRoleplayExporter();
                    break;
                }
                case ExportFormat.Plaintext:
                {
                    exporter = new PlaintextRoleplayExporter();
                    break;
                }
                default:
                {
                    return Result.FromError
                    (
                        new UserError("That export format hasn't been implemented yet.")
                    );
                }
            }

            var message = _feedback.CreateFeedbackEmbed
            (
                _context.User.ID,
                Color.MediumPurple,
                "Compiling the roleplay..."
            );

            var send = await _feedback.SendEmbedAsync(_context.ChannelID, message);
            if (!send.IsSuccess)
            {
                return Result<UserMessage>.FromError(send);
            }

            using var output = await exporter.ExportAsync(_services, roleplay);
            var fileData = new FileData
            (
                $"{output.Title}.{output.Format.GetFileExtension()}",
                output.Data
            );

            return await _channelAPI.CreateMessageAsync(_context.ChannelID, file: fileData);
        }

        /// <summary>
        /// Views the given roleplay, allowing you to read the channel.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        [UsedImplicitly]
        [Command("view")]
        [Description("Views the given roleplay, allowing you to read the channel.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<UserMessage>> ViewRoleplayAsync(Roleplay roleplay)
        {
            var getDedicatedChannelResult = _dedicatedChannels.GetDedicatedChannel(roleplay);
            if (!getDedicatedChannelResult.IsSuccess)
            {
                return new UserError
                (
                    "The given roleplay doesn't have a dedicated channel. Try using \"!rp export\" instead."
                );
            }

            if (!roleplay.IsPublic && roleplay.ParticipatingUsers.All(p => p.User.DiscordID != _context.User.ID))
            {
                return new UserError
                (
                    "You don't have permission to view that roleplay."
                );
            }

            var dedicatedChannel = getDedicatedChannelResult.Entity;
            var setVisibility = await _dedicatedChannels.SetChannelVisibilityForUserAsync
            (
                dedicatedChannel,
                _context.User.ID,
                true
            );

            if (!setVisibility.IsSuccess)
            {
                return Result<UserMessage>.FromError(setVisibility);
            }

            return new ConfirmationMessage
            (
                $"The roleplay \"{roleplay.Name}\" is now visible in <#{dedicatedChannel}>."
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
        public async Task<Result<UserMessage>> HideRoleplayAsync(Roleplay roleplay)
        {
            var getDedicatedChannelResult = _dedicatedChannels.GetDedicatedChannel
            (
                roleplay
            );

            if (!getDedicatedChannelResult.IsSuccess)
            {
                return Result<UserMessage>.FromError(getDedicatedChannelResult);
            }

            var dedicatedChannel = getDedicatedChannelResult.Entity;
            var setVisibility = await _dedicatedChannels.SetChannelVisibilityForUserAsync
            (
                dedicatedChannel,
                _context.User.ID,
                false
            );

            if (!setVisibility.IsSuccess)
            {
                return Result<UserMessage>.FromError(setVisibility);
            }

            return new ConfirmationMessage("Roleplay hidden.");
        }

        /// <summary>
        /// Hides all roleplays in the server for the user.
        /// </summary>
        [UsedImplicitly]
        [Command("hide-all")]
        [Description("Hides all roleplays in the server for the user.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<UserMessage>> HideAllRoleplaysAsync()
        {
            var roleplays = await _discordRoleplays.QueryRoleplaysAsync
            (
                q => q
                    .Where(rp => rp.Server.DiscordID == _context.GuildID.Value)
                    .Where(rp => rp.DedicatedChannelID.HasValue)
            );

            foreach (var roleplay in roleplays)
            {
                var setVisibility = await _dedicatedChannels.SetChannelVisibilityForUserAsync
                (
                    roleplay.DedicatedChannelID!.Value,
                    _context.User.ID,
                    false
                );

                if (!setVisibility.IsSuccess)
                {
                    return Result<UserMessage>.FromError(setVisibility);
                }
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
        public async Task<Result<UserMessage>> RefreshRoleplayAsync(Roleplay roleplay)
        {
            var isOwner = roleplay.IsOwner(_context.User.ID);
            var isParticipant = roleplay.HasJoined(_context.User);

            if (!(isOwner || isParticipant))
            {
                return new UserError("You don't own that roleplay, nor are you a participant.");
            }

            var refreshResult = await _discordRoleplays.RefreshRoleplayAsync(roleplay);
            if (!refreshResult.IsSuccess)
            {
                return Result<UserMessage>.FromError(refreshResult);
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
        public async Task<Result<UserMessage>> ResetChannelPermissionsAsync()
        {
            var roleplays = await _discordRoleplays.QueryRoleplaysAsync
            (
                q => q
                    .Where(rp => rp.Server.DiscordID == _context.GuildID.Value)
                    .Where(rp => rp.DedicatedChannelID.HasValue)
            );

            foreach (var roleplay in roleplays)
            {
                var reset = await _dedicatedChannels.ResetChannelPermissionsAsync(roleplay);
                if (!reset.IsSuccess)
                {
                    await _feedback.SendErrorAsync(_context.ChannelID, _context.User.ID, reset.Unwrap().Message);
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
        public async Task<Result> MoveRoleplayIntoChannelAsync(string newName, params IUser[] participants)
        {
            var createRoleplayAsync = await _discordRoleplays.CreateRoleplayAsync
            (
                _context.GuildID.Value,
                _context.User.ID,
                newName,
                "No summary set.",
                false,
                true
            );

            if (!createRoleplayAsync.IsSuccess)
            {
                return Result.FromError(createRoleplayAsync);
            }

            var roleplay = createRoleplayAsync.Entity;

            foreach (var participant in participants)
            {
                if (participant.ID == _context.User.ID)
                {
                    // Already added
                    continue;
                }

                var addParticipantAsync = await _discordRoleplays.AddUserToRoleplayAsync(roleplay, participant.ID);
                if (addParticipantAsync.IsSuccess)
                {
                    continue;
                }

                var message =
                    $"I couldn't add <@{participant.ID}> to the roleplay ({addParticipantAsync.Unwrap().Message}. " +
                    $"Please try to invite them manually.";

                var sendWarning = await _feedback.SendWarningAsync
                (
                    _context.ChannelID,
                    _context.User.ID,
                    message
                );

                if (sendWarning.Any(r => !r.IsSuccess))
                {
                    return Result.FromError(sendWarning.First(r => !r.IsSuccess));
                }
            }

            // Copy the last messages from the participants
            var before = _context switch
            {
                MessageContext messageContext => messageContext.MessageID,
                InteractionContext interactionContext => interactionContext.ID,
                _ => throw new ArgumentOutOfRangeException()
            };

            var getMessageBatch = await _channelAPI.GetChannelMessagesAsync(_context.ChannelID, before: before);
            if (!getMessageBatch.IsSuccess)
            {
                return Result.FromError(getMessageBatch);
            }

            var messageBatch = getMessageBatch.Entity;

            var participantMessages = participants
                .Select(participant => messageBatch.FirstOrDefault(m => m.Author.ID == participant.ID))
                .Where(message => message is not null)
                .Select(m => m!)
                .ToList();

            var getDedicatedChannel = _dedicatedChannels.GetDedicatedChannel(roleplay);
            if (!getDedicatedChannel.IsSuccess)
            {
                return Result.FromError(getDedicatedChannel);
            }

            var dedicatedChannel = getDedicatedChannel.Entity;

            foreach (var participantMessage in participantMessages.OrderByDescending(m => m.Timestamp))
            {
                var messageLink = $"https://discord.com/channels/" +
                                  $"{_context.GuildID.Value}/{_context.ChannelID}/{participantMessage.ID}";

                var send = await _channelAPI.CreateMessageAsync(dedicatedChannel, messageLink);
                if (!send.IsSuccess)
                {
                    return Result.FromError(send);
                }
            }

            var start = await StartRoleplayAsync(roleplay);
            return start.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(start);
        }
    }
}
