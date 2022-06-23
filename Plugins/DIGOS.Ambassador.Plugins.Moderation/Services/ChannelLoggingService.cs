//
//  ChannelLoggingService.cs
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
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using DIGOS.Ambassador.Plugins.Quotes.Services;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Moderation.Services;

/// <summary>
/// Assists in logging various events to the configured log channels.
/// </summary>
public sealed class ChannelLoggingService
{
    private readonly ModerationService _moderation;
    private readonly QuoteService _quotes;
    private readonly FeedbackService _feedback;

    private readonly IDiscordRestGuildAPI _guildAPI;
    private readonly IDiscordRestAuditLogAPI _auditLogAPI;
    private readonly IDiscordRestUserAPI _userAPI;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelLoggingService"/> class.
    /// </summary>
    /// <param name="moderation">The moderation service.</param>
    /// <param name="feedback">The feedback service.</param>
    /// <param name="quotes">The quote service.</param>
    /// <param name="guildAPI">The guild API.</param>
    /// <param name="auditLogAPI">The audit log API.</param>
    /// <param name="userAPI">The user API.</param>
    public ChannelLoggingService
    (
        ModerationService moderation,
        FeedbackService feedback,
        QuoteService quotes,
        IDiscordRestGuildAPI guildAPI,
        IDiscordRestAuditLogAPI auditLogAPI,
        IDiscordRestUserAPI userAPI
    )
    {
        _moderation = moderation;
        _feedback = feedback;
        _quotes = quotes;
        _guildAPI = guildAPI;
        _auditLogAPI = auditLogAPI;
        _userAPI = userAPI;
    }

    /// <summary>
    /// Posts a notification that a user was banned.
    /// </summary>
    /// <param name="ban">The ban.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> NotifyUserBannedAsync(UserBan ban)
    {
        var getChannel = await GetModerationLogChannelAsync(ban.Server.DiscordID);
        if (!getChannel.IsSuccess)
        {
            return Result.FromError(getChannel);
        }

        var channel = getChannel.Entity;

        var eb = new Embed
        {
            Colour = _feedback.Theme.FaultOrDanger,
            Title = $"User Banned (#{ban.ID})",
            Description = $"<@{ban.User.DiscordID}> (ID {ban.User.DiscordID}) was banned by " +
                          $"<@{ban.Author.DiscordID}>.\n" +
                          $"Reason: {ban.Reason}"
        };

        var sendResult = await _feedback.SendEmbedAsync(channel, eb);
        return sendResult.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(sendResult);
    }

    /// <summary>
    /// Posts a notification that a user was unbanned.
    /// </summary>
    /// <param name="ban">The ban.</param>
    /// <param name="rescinderID">The person who rescinded the ban.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> NotifyUserUnbannedAsync(UserBan ban, Snowflake rescinderID)
    {
        var getChannel = await GetModerationLogChannelAsync(ban.Server.DiscordID);
        if (!getChannel.IsSuccess)
        {
            return Result.FromError(getChannel);
        }

        var getSelf = await _userAPI.GetCurrentUserAsync();
        if (!getSelf.IsSuccess)
        {
            return Result.FromError(getSelf);
        }

        var self = getSelf.Entity;
        var channel = getChannel.Entity;

        var whoDidIt = rescinderID == self.ID
            ? "(expired)"
            : $"by <@{rescinderID}>";

        var eb = new Embed
        {
            Colour = _feedback.Theme.Success,
            Title = $"User Unbanned (#{ban.ID})",
            Description = $"<@{ban.User.DiscordID}> (ID {ban.User.DiscordID}) was unbanned {whoDidIt}."
        };

        var sendResult = await _feedback.SendEmbedAsync(channel, eb);
        return sendResult.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(sendResult);
    }

    /// <summary>
    /// Posts a notification that a user was warned.
    /// </summary>
    /// <param name="warning">The warning.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> NotifyUserWarningAddedAsync(UserWarning warning)
    {
        var getChannel = await GetModerationLogChannelAsync(warning.Server.DiscordID);
        if (!getChannel.IsSuccess)
        {
            return Result.FromError(getChannel);
        }

        var channel = getChannel.Entity;

        var eb = new Embed
        {
            Colour = _feedback.Theme.Warning,
            Title = $"User Warned (#{warning.ID})",
            Description = $"<@{warning.User.DiscordID}> (ID {warning.User.DiscordID}) was warned by " +
                          $"<@{warning.Author.DiscordID}>.\n" +
                          $"Reason: {warning.Reason}"
        };

        var sendResult = await _feedback.SendEmbedAsync(channel, eb);
        return sendResult.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(sendResult);
    }

    /// <summary>
    /// Posts a notification that a warning was rescinded.
    /// </summary>
    /// <param name="warning">The warning.</param>
    /// <param name="rescinderID">The person who rescinded the warning.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> NotifyUserWarningRemovedAsync(UserWarning warning, Snowflake rescinderID)
    {
        var getChannel = await GetModerationLogChannelAsync(warning.Server.DiscordID);
        if (!getChannel.IsSuccess)
        {
            return Result.FromError(getChannel);
        }

        var getSelf = await _userAPI.GetCurrentUserAsync();
        if (!getSelf.IsSuccess)
        {
            return Result.FromError(getSelf);
        }

        var self = getSelf.Entity;
        var channel = getChannel.Entity;

        var whoDidIt = rescinderID == self.ID
            ? "(expired)"
            : $"by <@{rescinderID}>";

        var eb = new Embed
        {
            Colour = _feedback.Theme.Success,
            Title = $"Warning Removed (#{warning.ID})",
            Description = $"A warning was removed from <@{warning.User.DiscordID}> (ID {warning.User.DiscordID}) " +
                          $"{whoDidIt}."
        };

        var sendResult = await _feedback.SendEmbedAsync(channel, eb);
        return sendResult.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(sendResult);
    }

    /// <summary>
    /// Posts a notification that a note was added to a user.
    /// </summary>
    /// <param name="note">The note.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> NotifyUserNoteAddedAsync(UserNote note)
    {
        var getChannel = await GetModerationLogChannelAsync(note.Server.DiscordID);
        if (!getChannel.IsSuccess)
        {
            return Result.FromError(getChannel);
        }

        var channel = getChannel.Entity;

        var eb = new Embed
        {
            Title = $"Note Added (#{note.ID})",
            Colour = Color.Gold,
            Description =
                $"A note was added to <@{note.User.DiscordID}> (ID {note.User.DiscordID}) by " +
                $"<@{note.Author.DiscordID}>.\n" +
                $"Contents: {note.Content}"
        };

        var sendResult = await _feedback.SendEmbedAsync(channel, eb);
        return sendResult.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(sendResult);
    }

    /// <summary>
    /// Posts a notification that a note was removed from a user.
    /// </summary>
    /// <param name="note">The note.</param>
    /// <param name="removerID">The person that removed the note.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> NotifyUserNoteRemovedAsync(UserNote note, Snowflake removerID)
    {
        var getChannel = await GetModerationLogChannelAsync(note.Server.DiscordID);
        if (!getChannel.IsSuccess)
        {
            return Result.FromError(getChannel);
        }

        var channel = getChannel.Entity;

        var eb = new Embed
        {
            Colour = _feedback.Theme.Success,
            Title = $"Note Removed (#{note.ID})",
            Description = $"A note was removed from <@{note.User.DiscordID}> (ID {note.User.DiscordID}) by " +
                          $"<@{removerID}>."
        };

        var sendResult = await _feedback.SendEmbedAsync(channel, eb);
        return sendResult.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(sendResult);
    }

    /// <summary>
    /// Posts a notification that a user left the server.
    /// </summary>
    /// <param name="guildID">The ID of guild the user left.</param>
    /// <param name="userID">The ID of the user that left.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> NotifyUserLeftAsync(Snowflake guildID, Snowflake userID)
    {
        var getChannel = await GetMonitoringChannelAsync(guildID);
        if (!getChannel.IsSuccess)
        {
            return Result.FromError(getChannel);
        }

        var channel = getChannel.Entity;

        var eb = new Embed
        {
            Colour = _feedback.Theme.Secondary,
            Description = $"<@{userID}> left the server."
        };

        var sendResult = await _feedback.SendEmbedAsync(channel, eb);
        return sendResult.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(sendResult);
    }

    /// <summary>
    /// Posts a notification that a user changed their nickname.
    /// </summary>
    /// <param name="guildID">The ID of the guild the user is on.</param>
    /// <param name="userID">The ID of the user.</param>
    /// <param name="oldNickname">The old nickname.</param>
    /// <param name="newNickname">The new nickname.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> NotifyUserNicknameChangedAsync
    (
        Snowflake guildID,
        Snowflake userID,
        Optional<string?> oldNickname,
        Optional<string?> newNickname
    )
    {
        var getChannel = await GetMonitoringChannelAsync(guildID);
        if (!getChannel.IsSuccess)
        {
            return Result.FromError(getChannel);
        }

        var channel = getChannel.Entity;

        var eb = new Embed
        {
            Colour = _feedback.Theme.Secondary,
            Description = $"<@{userID}> changed their nickname from {oldNickname} to {newNickname}."
        };

        var sendResult = await _feedback.SendEmbedAsync(channel, eb);
        return sendResult.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(sendResult);
    }

    /// <summary>
    /// Posts a notification that a user changed their discriminator.
    /// </summary>
    /// <param name="guildID">The ID of the guild the user is on.</param>
    /// <param name="userID">The ID of the user.</param>
    /// <param name="oldDiscriminator">The old discriminator.</param>
    /// <param name="newDiscriminator">The new discriminator.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> NotifyUserDiscriminatorChangedAsync
    (
        Snowflake guildID,
        Snowflake userID,
        ushort oldDiscriminator,
        ushort newDiscriminator,
        CancellationToken ct = default
    )
    {
        var getChannel = await GetMonitoringChannelAsync(guildID);
        if (!getChannel.IsSuccess)
        {
            return Result.FromError(getChannel);
        }

        var channel = getChannel.Entity;

        var eb = new Embed
        {
            Colour = _feedback.Theme.Secondary,
            Description = $"<@{userID}> changed their discriminator from {oldDiscriminator:D4} to " +
                          $"{newDiscriminator:D4}."
        };

        var sendResult = await _feedback.SendEmbedAsync(channel, eb, ct: ct);
        return sendResult.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(sendResult);
    }

    /// <summary>
    /// Posts a notification that a message was deleted.
    /// </summary>
    /// <param name="message">The deleted message.</param>
    /// <param name="guildID">The ID of the guild in which the message was.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> NotifyMessageDeletedAsync(IMessage message, Snowflake guildID)
    {
        // We don't care about bot messages
        var isNonFeedbackMessage = (message.Author.IsBot.IsDefined(out var isBot) && isBot) ||
                                   (message.Author.IsSystem.IsDefined(out var isSystem) && isSystem);

        if (isNonFeedbackMessage)
        {
            return Result.FromSuccess();
        }

        var getChannel = await GetMonitoringChannelAsync(guildID);
        if (!getChannel.IsSuccess)
        {
            return Result.FromError(getChannel);
        }

        var getSelf = await _userAPI.GetCurrentUserAsync();
        if (!getSelf.IsSuccess)
        {
            return Result.FromError(getSelf);
        }

        var self = getSelf.Entity;
        var channel = getChannel.Entity;

        var eb = new Embed
        {
            Colour = _feedback.Theme.Warning,
            Title = "Message Deleted"
        };

        var getGuildRoles = await _guildAPI.GetGuildRolesAsync(guildID);
        if (!getGuildRoles.IsSuccess)
        {
            return Result.FromError(getGuildRoles);
        }

        var guildRoles = getGuildRoles.Entity;
        var everyoneRole = guildRoles.First(r => r.ID == guildID);

        var getGuildMember = await _guildAPI.GetGuildMemberAsync(guildID, self.ID);
        if (!getGuildMember.IsSuccess)
        {
            return Result.FromError(getGuildMember);
        }

        var botGuildMember = getGuildMember.Entity;
        var botRoles = guildRoles.Where(r => botGuildMember.Roles.Contains(r.ID)).ToList();

        var botPermissions = DiscordPermissionSet.ComputePermissions
        (
            self.ID,
            everyoneRole,
            botRoles
        );

        var extra = string.Empty;
        if (botPermissions.HasPermission(DiscordPermission.ViewAuditLog))
        {
            var getMostProbableDeleter = await FindMostProbableDeleterAsync(message, guildID);
            if (!getMostProbableDeleter.IsSuccess)
            {
                return Result.FromError(getMostProbableDeleter);
            }

            var userID = getMostProbableDeleter.Entity;
            var getUser = await _userAPI.GetUserAsync(userID);
            if (!getUser.IsSuccess)
            {
                return Result.FromError(getUser);
            }

            var mostProbableDeleter = getUser.Entity;

            var isNonUserDeleter = (mostProbableDeleter.IsBot.IsDefined(out var isDeleterBot) && isDeleterBot) ||
                                   (mostProbableDeleter.IsSystem.IsDefined(out var isDeleterSystem) && isDeleterSystem);

            // We don't care about bot deletions
            if (!isNonUserDeleter)
            {
                extra = $" (probably) by <@{mostProbableDeleter.ID}>";
            }
        }

        eb = eb with
        {
            Description = $"A message was deleted from <#{message.ChannelID}>{extra}."
        };

        var quote = _quotes.CreateMessageQuote(message, self.ID);

        var sendResult = await _feedback.SendEmbedAsync(channel, eb);
        if (!sendResult.IsSuccess)
        {
            return Result.FromError(sendResult);
        }

        sendResult = await _feedback.SendEmbedAsync(channel, quote);
        return sendResult.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(sendResult);
    }

    /// <summary>
    /// Retrieves the moderation log channel.
    /// </summary>
    /// <param name="guildID">The guild to grab the channel from.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    private async Task<Result<Snowflake>> GetModerationLogChannelAsync(Snowflake guildID)
    {
        var getSettings = await _moderation.GetOrCreateServerSettingsAsync(guildID);
        if (!getSettings.IsSuccess)
        {
            return Result<Snowflake>.FromError(getSettings);
        }

        var settings = getSettings.Entity;

        if (settings.ModerationLogChannel is null)
        {
            return new UserError("No configured channel.");
        }

        return settings.ModerationLogChannel.Value;
    }

    /// <summary>
    /// Retrieves the event monitoring channel.
    /// </summary>
    /// <param name="guildID">The guild to grab the channel from.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    private async Task<Result<Snowflake>> GetMonitoringChannelAsync(Snowflake guildID)
    {
        var getSettings = await _moderation.GetOrCreateServerSettingsAsync(guildID);
        if (!getSettings.IsSuccess)
        {
            return Result<Snowflake>.FromError(getSettings);
        }

        var settings = getSettings.Entity;

        if (settings.MonitoringChannel is null)
        {
            return new UserError("No configured channel.");
        }

        return settings.MonitoringChannel.Value;
    }

    // ReSharper disable once UnusedParameter.Local
    private async Task<Result<Snowflake>> FindMostProbableDeleterAsync(IMessage message, Snowflake guildID)
    {
        var now = Snowflake.CreateTimestampSnowflake(epoch: Constants.DiscordEpoch);
        var before = now;
        var after = message.ID;

        while (true)
        {
            var getAuditLogEntries = await _auditLogAPI.GetAuditLogAsync
            (
                guildID,
                actionType: AuditLogEvent.MessageDelete,
                before: before
            );

            if (!getAuditLogEntries.IsSuccess)
            {
                return Result<Snowflake>.FromError(getAuditLogEntries);
            }

            var entries = getAuditLogEntries.Entity;
            if (entries.AuditLogEntries.Count == 0)
            {
                break;
            }

            var match = entries.AuditLogEntries.OrderByDescending(i => i.ID.Timestamp).FirstOrDefault
            (
                e =>
                {
                    if (!e.Options.IsDefined(out var options))
                    {
                        return false;
                    }

                    if (!options.ChannelID.IsDefined(out var channelID))
                    {
                        return false;
                    }

                    if (channelID != message.ChannelID)
                    {
                        return false;
                    }

                    // Discard entries that are unreasonably old
                    if (now.Timestamp - e.ID.Timestamp > TimeSpan.FromMinutes(1))
                    {
                        return false;
                    }

                    return e.TargetID == message.Author.ID.ToString();
                }
            );

            if (match?.UserID != null)
            {
                return match.UserID.Value;
            }

            before = entries.AuditLogEntries.OrderBy(i => i.ID.Timestamp).First().ID;
            if (before.Timestamp < after.Timestamp)
            {
                // No more entries that are relevant
                break;
            }
        }

        // No audit entries are generated when the user deletes a message themselves
        return message.Author.ID;
    }
}
