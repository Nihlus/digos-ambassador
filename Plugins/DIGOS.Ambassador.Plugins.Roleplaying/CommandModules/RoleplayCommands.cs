//
//  RoleplayCommands.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Core.Preconditions;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using DIGOS.Ambassador.Plugins.Roleplaying.Extensions;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Permissions;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using DIGOS.Ambassador.Plugins.Roleplaying.Services.Exporters;
using Humanizer;
using JetBrains.Annotations;
using OneOf;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Pagination;
using Remora.Discord.Pagination.Extensions;
using Remora.Results;
using FeedbackMessage = Remora.Discord.Commands.Feedback.Messages.FeedbackMessage;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Roleplaying.CommandModules;

/// <summary>
/// Commands for interacting with and managing channel roleplays.
/// </summary>
[UsedImplicitly]
[Group("rp")]
[Description("Commands for interacting with and managing channel roleplays.")]
public partial class RoleplayCommands : CommandGroup
{
    private readonly IDiscordRestChannelAPI _channelAPI;
    private readonly RoleplayDiscordService _discordRoleplays;
    private readonly DedicatedChannelService _dedicatedChannels;
    private readonly FeedbackService _feedback;
    private readonly ICommandContext _context;
    private readonly PDFRoleplayExporter _pdfExporter;
    private readonly PlaintextRoleplayExporter _plaintextExporter;
    private readonly IDiscordRestUserAPI _userAPI;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleplayCommands"/> class.
    /// </summary>
    /// <param name="discordRoleplays">The roleplay service.</param>
    /// <param name="feedback">The user feedback service.</param>
    /// <param name="dedicatedChannels">The dedicated channel service.</param>
    /// <param name="context">The command context.</param>
    /// <param name="channelAPI">The channel API.</param>
    /// <param name="pdfExporter">The PDF roleplay exporter.</param>
    /// <param name="plaintextExporter">The plaintext roleplay exporter.</param>
    /// <param name="userAPI">The user API.</param>
    public RoleplayCommands
    (
        RoleplayDiscordService discordRoleplays,
        FeedbackService feedback,
        DedicatedChannelService dedicatedChannels,
        ICommandContext context,
        IDiscordRestChannelAPI channelAPI,
        PDFRoleplayExporter pdfExporter,
        PlaintextRoleplayExporter plaintextExporter,
        IDiscordRestUserAPI userAPI
    )
    {
        _discordRoleplays = discordRoleplays;
        _feedback = feedback;
        _dedicatedChannels = dedicatedChannels;
        _context = context;
        _channelAPI = channelAPI;
        _pdfExporter = pdfExporter;
        _plaintextExporter = plaintextExporter;
        _userAPI = userAPI;
    }

    /// <summary>
    /// Shows information about the given roleplay.
    /// </summary>
    /// <param name="roleplay">The roleplay.</param>
    [UsedImplicitly]
    [Command("show")]
    [Description("Shows information about the specified roleplay.")]
    [RequireContext(ChannelContext.Guild)]
    public async Task<IResult> ShowRoleplayAsync
    (
        [AutocompleteProvider("roleplay::any")]
        Roleplay? roleplay = null
    )
    {
        if (roleplay is null)
        {
            if (!_context.TryGetChannelID(out var channelID))
            {
                throw new InvalidOperationException();
            }

            var getCurrentRoleplayResult = await _discordRoleplays.GetActiveRoleplayAsync(channelID);
            if (!getCurrentRoleplayResult.IsSuccess)
            {
                return getCurrentRoleplayResult;
            }

            roleplay = getCurrentRoleplayResult.Entity;
        }

        var eb = CreateRoleplayInfoEmbed(roleplay);
        return await _feedback.SendContextualEmbedAsync(eb);
    }

    private Embed CreateRoleplayInfoEmbed(Roleplay roleplay)
    {
        var fields = new List<IEmbedField>();
        var eb = new Embed
        {
            Colour = _feedback.Theme.Secondary,
            Title = roleplay.Name,
            Description = roleplay.GetSummaryOrDefault(),
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
        if (!_context.TryGetGuildID(out var guildID))
        {
            throw new InvalidOperationException();
        }

        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var roleplays = await _discordRoleplays.QueryRoleplaysAsync
        (
            q => q
                .Where(rp => rp.Server.DiscordID == guildID)
                .Where(rp => rp.IsPublic)
        );

        var pages = PaginatedEmbedFactory.SimpleFieldsFromCollection
        (
            roleplays,
            r => r.Name,
            r => r.GetSummaryOrDefault(),
            "There are no roleplays in the server that you can view."
        );

        return (Result)await _feedback.SendContextualPaginatedMessageAsync
        (
            userID,
            pages,
            ct: this.CancellationToken
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
        if (!_context.TryGetGuildID(out var guildID))
        {
            throw new InvalidOperationException();
        }

        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        if (discordUser is null)
        {
            var getUser = await _userAPI.GetUserAsync(userID, this.CancellationToken);
            if (!getUser.IsSuccess)
            {
                return (Result)getUser;
            }

            discordUser = getUser.Entity;
        }

        var roleplays = await _discordRoleplays.QueryRoleplaysAsync
        (
            q => q
                .Where(rp => rp.Server.DiscordID == guildID)
                .Where(rp => rp.Owner.DiscordID == discordUser.ID)
        );

        var pages = PaginatedEmbedFactory.SimpleFieldsFromCollection
        (
            roleplays,
            r => r.Name,
            r => r.GetSummaryOrDefault(),
            "You don't have any roleplays."
        );

        return (Result)await _feedback.SendContextualPaginatedMessageAsync
        (
            userID,
            pages,
            ct: this.CancellationToken
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
    public async Task<Result<FeedbackMessage>> CreateRoleplayAsync
    (
        string roleplayName,
        string roleplaySummary = "No summary set.",
        bool isNSFW = false,
        bool isPublic = true
    )
    {
        if (!_context.TryGetGuildID(out var guildID))
        {
            throw new InvalidOperationException();
        }

        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var result = await _discordRoleplays.CreateRoleplayAsync
        (
            guildID,
            userID,
            roleplayName,
            roleplaySummary,
            isNSFW,
            isPublic
        );

        return !result.IsSuccess
            ? Result<FeedbackMessage>.FromError(result)
            : new FeedbackMessage($"Roleplay \"{result.Entity.Name}\" created.", _feedback.Theme.Secondary);
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
        [RequireEntityOwner]
        [AutocompleteProvider("roleplay::owned")]
        Roleplay roleplay
    )
    {
        if (!_context.TryGetChannelID(out var channelID))
        {
            throw new InvalidOperationException();
        }

        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var deletionResult = await _discordRoleplays.DeleteRoleplayAsync(roleplay);
        if (!deletionResult.IsSuccess)
        {
            return deletionResult;
        }

        var canReplyInChannelAfterDeletion = channelID != roleplay.DedicatedChannelID;
        if (canReplyInChannelAfterDeletion)
        {
            return Result<FeedbackMessage>.FromSuccess
            (
                new FeedbackMessage($"Roleplay \"{roleplay.Name}\" deleted.", _feedback.Theme.Secondary)
            );
        }

        var eb = new Embed
        {
            Colour = _feedback.Theme.Secondary,
            Description = $"Roleplay \"{roleplay.Name}\" deleted."
        };

        return await _feedback.SendPrivateEmbedAsync(userID, eb);
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
    public async Task<Result<FeedbackMessage>> JoinRoleplayAsync
    (
        [AutocompleteProvider("roleplay::notjoined")]
        Roleplay roleplay
    )
    {
        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var addUserResult = await _discordRoleplays.AddUserToRoleplayAsync(roleplay, userID);
        if (!addUserResult.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(addUserResult);
        }

        return new FeedbackMessage
        (
            $"Joined <@{roleplay.Owner.ID}>'s roleplay \"{roleplay.Name}\"",
            _feedback.Theme.Secondary
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
    public async Task<Result<FeedbackMessage>> InvitePlayerAsync
    (
        IUser playerToInvite,
        [RequireEntityOwner]
        [AutocompleteProvider("roleplay::owned")]
        Roleplay roleplay
    )
    {
        var invitePlayerResult = await _discordRoleplays.InviteUserToRoleplayAsync(roleplay, playerToInvite.ID);
        if (!invitePlayerResult.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(invitePlayerResult);
        }

        var roleplayName = roleplay.Name.Contains(' ') ? roleplay.Name.Quote() : roleplay.Name;
        var message = new Embed
        {
            Colour = _feedback.Theme.Secondary,
            Description = $"You've been invited to join {roleplay.Name}. Use `!rp join {roleplayName}` to join."
        };

        var send = await _feedback.SendPrivateEmbedAsync(playerToInvite.ID, message);
        if (!send.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(send);
        }

        return new FeedbackMessage
        (
            $"Invited <@{playerToInvite.ID}> to {roleplay.Name}.",
            _feedback.Theme.Secondary
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
    public async Task<Result<FeedbackMessage>> LeaveRoleplayAsync
    (
        [AutocompleteProvider("roleplay::joined")]
        Roleplay roleplay
    )
    {
        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var removeUserResult = await _discordRoleplays.RemoveUserFromRoleplayAsync(roleplay, userID);
        if (!removeUserResult.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(removeUserResult);
        }

        return new FeedbackMessage
        (
            $"Left <@{roleplay.Owner.DiscordID}>'s roleplay \"{roleplay.Name}\"",
            _feedback.Theme.Secondary
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
    public async Task<Result<FeedbackMessage>> KickRoleplayParticipantAsync
    (
        IUser discordUser,
        [RequireEntityOwner]
        [AutocompleteProvider("roleplay::owned")]
        Roleplay roleplay
    )
    {
        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var kickUserResult = await _discordRoleplays.KickUserFromRoleplayAsync(roleplay, discordUser.ID);
        if (!kickUserResult.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(kickUserResult);
        }

        var message = new Embed
        {
            Colour = _feedback.Theme.Secondary,
            Description = $"You've been removed from the roleplay \"{roleplay.Name}\" by " +
                          $"<@{userID}>."
        };

        // It's fine if this one fails
        _ = await _feedback.SendPrivateEmbedAsync(discordUser.ID, message);

        return new FeedbackMessage
        (
            $"<@{discordUser.ID}> has been kicked from {roleplay.Name}.",
            _feedback.Theme.Secondary
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
    public async Task<Result<FeedbackMessage>> ShowOrCreateDedicatedRoleplayChannel
    (
        [RequireEntityOwner]
        [AutocompleteProvider("roleplay::owned")]
        Roleplay roleplay
    )
    {
        var getDedicatedChannelResult = DedicatedChannelService.GetDedicatedChannel(roleplay);
        if (getDedicatedChannelResult.IsSuccess)
        {
            var existingDedicatedChannel = getDedicatedChannelResult.Entity;
            var message = $"\"{roleplay.Name}\" has a dedicated channel at " +
                          $"<#{existingDedicatedChannel}>";

            return new FeedbackMessage(message, _feedback.Theme.Secondary);
        }

        var workingMessage = new Embed
        {
            Colour = _feedback.Theme.Secondary,
            Description = "Setting up dedicated channel..."
        };

        var send = await _feedback.SendContextualEmbedAsync(workingMessage);
        if (!send.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(send);
        }

        // The roleplay either doesn't have a channel, or the one it has has been deleted or is otherwise invalid.
        var result = await _dedicatedChannels.CreateDedicatedChannelAsync(roleplay);
        if (!result.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(result);
        }

        var dedicatedChannel = result.Entity;

        if (!roleplay.IsActive || roleplay.ActiveChannelID == dedicatedChannel.ID)
        {
            return new FeedbackMessage
            (
                $"All done! Your roleplay now has a dedicated channel at <#{dedicatedChannel}>.",
                _feedback.Theme.Secondary
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

        return new FeedbackMessage
        (
            $"All done! Your roleplay now has a dedicated channel at <#{dedicatedChannel}>.",
            _feedback.Theme.Secondary
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
    public async Task<Result<FeedbackMessage>> StartRoleplayAsync
    (
        [RequireEntityOwner]
        [AutocompleteProvider("roleplay::owned")]
        Roleplay roleplay
    )
    {
        if (!_context.TryGetChannelID(out var channelID))
        {
            throw new InvalidOperationException();
        }

        var startRoleplayResult = await _discordRoleplays.StartRoleplayAsync
        (
            channelID,
            roleplay
        );

        if (!startRoleplayResult.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(startRoleplayResult);
        }

        var activationMessage = $"The roleplay \"{roleplay.Name}\" is now active in " +
                                $"<#{roleplay.ActiveChannelID!.Value}>.";

        return new FeedbackMessage(activationMessage, _feedback.Theme.Secondary);
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
    public async Task<Result<FeedbackMessage>> StopRoleplayAsync
    (
        [RequireEntityOwner]
        [AutocompleteProvider("roleplay::owned")]
        Roleplay roleplay
    )
    {
        var stopRoleplayAsync = await _discordRoleplays.StopRoleplayAsync(roleplay);

        return !stopRoleplayAsync.IsSuccess
            ? Result<FeedbackMessage>.FromError(stopRoleplayAsync)
            : new FeedbackMessage($"The roleplay \"{roleplay.Name}\" has been stopped.", _feedback.Theme.Secondary);
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
    public async Task<Result<FeedbackMessage>> TransferRoleplayOwnershipAsync
    (
        IUser newOwner,
        [RequireEntityOwner]
        [AutocompleteProvider("roleplay::owned")]
        Roleplay roleplay
    )
    {
        var transferResult = await _discordRoleplays.TransferRoleplayOwnershipAsync(newOwner.ID, roleplay);

        return !transferResult.IsSuccess
            ? Result<FeedbackMessage>.FromError(transferResult)
            : new FeedbackMessage("Roleplay ownership transferred.", _feedback.Theme.Secondary);
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
        [RequireEntityOwner]
        [AutocompleteProvider("roleplay::owned")]
        Roleplay roleplay,
        ExportFormat format = ExportFormat.PDF
    )
    {
        if (!_context.TryGetChannelID(out var channelID))
        {
            throw new InvalidOperationException();
        }

        IRoleplayExporter exporter;
        switch (format)
        {
            case ExportFormat.PDF:
            {
                exporter = _pdfExporter;
                break;
            }
            case ExportFormat.Plaintext:
            {
                exporter = _plaintextExporter;
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

        var send = await _feedback.SendContextualNeutralAsync("Compiling the roleplay...");
        if (!send.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(send);
        }

        using var output = await exporter.ExportAsync(roleplay);
        var fileData = new FileData
        (
            $"{output.Title}.{output.Format.GetFileExtension()}",
            output.Data
        );

        return await _channelAPI.CreateMessageAsync
        (
            channelID,
            attachments: new List<OneOf<FileData, IPartialAttachment>> { fileData }
        );
    }

    /// <summary>
    /// Views the given roleplay, allowing you to read the channel.
    /// </summary>
    /// <param name="roleplay">The roleplay.</param>
    [UsedImplicitly]
    [Command("view")]
    [Description("Views the given roleplay, allowing you to read the channel.")]
    [RequireContext(ChannelContext.Guild)]
    public async Task<Result<FeedbackMessage>> ViewRoleplayAsync
    (
        [AutocompleteProvider("roleplay::any")] Roleplay roleplay
    )
    {
        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var getDedicatedChannelResult = DedicatedChannelService.GetDedicatedChannel(roleplay);
        if (!getDedicatedChannelResult.IsSuccess)
        {
            return new UserError
            (
                "The given roleplay doesn't have a dedicated channel. Try using \"!rp export\" instead."
            );
        }

        if (!roleplay.IsPublic && roleplay.ParticipatingUsers.All(p => p.User.DiscordID != userID))
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
            userID,
            true
        );

        if (!setVisibility.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(setVisibility);
        }

        return new FeedbackMessage
        (
            $"The roleplay \"{roleplay.Name}\" is now visible in <#{dedicatedChannel}>.",
            _feedback.Theme.Secondary
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
    public async Task<Result<FeedbackMessage>> HideRoleplayAsync
    (
        [AutocompleteProvider("roleplay::any")]
        Roleplay roleplay
    )
    {
        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var getDedicatedChannelResult = DedicatedChannelService.GetDedicatedChannel
        (
            roleplay
        );

        if (!getDedicatedChannelResult.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(getDedicatedChannelResult);
        }

        var dedicatedChannel = getDedicatedChannelResult.Entity;
        var setVisibility = await _dedicatedChannels.SetChannelVisibilityForUserAsync
        (
            dedicatedChannel,
            userID,
            false
        );

        return !setVisibility.IsSuccess
            ? Result<FeedbackMessage>.FromError(setVisibility)
            : new FeedbackMessage("Roleplay hidden.", _feedback.Theme.Secondary);
    }

    /// <summary>
    /// Hides all roleplays in the server for the user.
    /// </summary>
    [UsedImplicitly]
    [Command("hide-all")]
    [Description("Hides all roleplays in the server for the user.")]
    [RequireContext(ChannelContext.Guild)]
    public async Task<Result<FeedbackMessage>> HideAllRoleplaysAsync()
    {
        if (!_context.TryGetGuildID(out var guildID))
        {
            throw new InvalidOperationException();
        }

        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var roleplays = await _discordRoleplays.QueryRoleplaysAsync
        (
            q => q
                .Where(rp => rp.Server.DiscordID == guildID)
                .Where(rp => rp.DedicatedChannelID.HasValue)
        );

        foreach (var roleplay in roleplays)
        {
            var setVisibility = await _dedicatedChannels.SetChannelVisibilityForUserAsync
            (
                roleplay.DedicatedChannelID!.Value,
                userID,
                false
            );

            if (!setVisibility.IsSuccess)
            {
                return Result<FeedbackMessage>.FromError(setVisibility);
            }
        }

        return new FeedbackMessage("Roleplays hidden.", _feedback.Theme.Secondary);
    }

    /// <summary>
    /// Manually refreshes the given roleplay, resetting its last-updated time to now.
    /// </summary>
    /// <param name="roleplay">The roleplay.</param>
    [UsedImplicitly]
    [Command("refresh")]
    [Description("Manually refreshes the given roleplay, resetting its last-updated time to now.")]
    [RequireContext(ChannelContext.Guild)]
    public async Task<Result<FeedbackMessage>> RefreshRoleplayAsync
    (
        [AutocompleteProvider("roleplay::any")]
        Roleplay roleplay
    )
    {
        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var isOwner = roleplay.IsOwner(userID);
        var isParticipant = roleplay.HasJoined(userID);

        if (!(isOwner || isParticipant))
        {
            return new UserError("You don't own that roleplay, nor are you a participant.");
        }

        var refreshResult = await _discordRoleplays.RefreshRoleplayAsync(roleplay);

        return !refreshResult.IsSuccess
            ? Result<FeedbackMessage>.FromError(refreshResult)
            : new FeedbackMessage("Timeout refreshed.", _feedback.Theme.Secondary);
    }

    /// <summary>
    /// Resets the permission set of all dedicated channels.
    /// </summary>
    [UsedImplicitly]
    [Command("reset-permissions")]
    [Description("Resets the permission set of all dedicated channels.")]
    [RequireContext(ChannelContext.Guild)]
    [RequirePermission(typeof(EditRoleplayServerSettings), PermissionTarget.All)]
    public async Task<Result<FeedbackMessage>> ResetChannelPermissionsAsync()
    {
        if (!_context.TryGetGuildID(out var guildID))
        {
            throw new InvalidOperationException();
        }

        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var roleplays = await _discordRoleplays.QueryRoleplaysAsync
        (
            q => q
                .Where(rp => rp.Server.DiscordID == guildID)
                .Where(rp => rp.DedicatedChannelID.HasValue)
        );

        foreach (var roleplay in roleplays)
        {
            var reset = await _dedicatedChannels.ResetChannelPermissionsAsync(roleplay);
            if (!reset.IsSuccess)
            {
                await _feedback.SendContextualErrorAsync(reset.Error.Message, userID);
            }
        }

        return new FeedbackMessage("Permissions reset.", _feedback.Theme.Secondary);
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
    [ExcludeFromSlashCommands]
    public async Task<Result> MoveRoleplayIntoChannelAsync(string newName, params IUser[] participants)
    {
        if (!_context.TryGetGuildID(out var guildID))
        {
            throw new InvalidOperationException();
        }

        if (!_context.TryGetChannelID(out var channelID))
        {
            throw new InvalidOperationException();
        }

        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var createRoleplayAsync = await _discordRoleplays.CreateRoleplayAsync
        (
            guildID,
            userID,
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
            if (participant.ID == userID)
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
                $"I couldn't add <@{participant.ID}> to the roleplay ({addParticipantAsync.Error.Message}. " +
                "Please try to invite them manually.";

            var sendWarning = await _feedback.SendContextualWarningAsync
            (
                message,
                userID
            );

            if (!sendWarning.IsSuccess)
            {
                return Result.FromError(sendWarning);
            }
        }

        // Copy the last messages from the participants
        var before = _context switch
        {
            MessageContext messageContext => messageContext.Message.ID,
            InteractionContext interactionContext => interactionContext.Interaction.Message.Value.ID,
            _ => throw new ArgumentOutOfRangeException(nameof(_context))
        };

        var getMessageBatch = await _channelAPI.GetChannelMessagesAsync(channelID, before: before);
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

        var getDedicatedChannel = DedicatedChannelService.GetDedicatedChannel(roleplay);
        if (!getDedicatedChannel.IsSuccess)
        {
            return Result.FromError(getDedicatedChannel);
        }

        var dedicatedChannel = getDedicatedChannel.Entity;

        foreach (var participantMessage in participantMessages.OrderByDescending(m => m.Timestamp))
        {
            var messageLink = "https://discord.com/channels/" +
                              $"{guildID}/{channelID}/{participantMessage.ID}";

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
