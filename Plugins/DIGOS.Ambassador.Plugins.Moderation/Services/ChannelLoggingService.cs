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

using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Feedback.Errors;
using DIGOS.Ambassador.Discord.Feedback.Services;
using DIGOS.Ambassador.Plugins.Core.Services;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using DIGOS.Ambassador.Plugins.Quotes.Services;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Moderation.Services
{
    /// <summary>
    /// Assists in logging various events to the configured log channels.
    /// </summary>
    public sealed class ChannelLoggingService
    {
        private readonly ModerationService _moderation;
        private readonly QuoteService _quotes;
        private readonly UserFeedbackService _feedback;

        private readonly IdentityInformationService _identityInformation;
        private readonly IDiscordRestGuildAPI _guildAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelLoggingService"/> class.
        /// </summary>
        /// <param name="moderation">The moderation service.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="quotes">The quote service.</param>
        /// <param name="identityInformation">The identity information service.</param>
        /// <param name="guildAPI">The guild API.</param>
        public ChannelLoggingService
        (
            ModerationService moderation,
            UserFeedbackService feedback,
            QuoteService quotes,
            IdentityInformationService identityInformation,
            IDiscordRestGuildAPI guildAPI
        )
        {
            _moderation = moderation;
            _feedback = feedback;
            _quotes = quotes;
            _identityInformation = identityInformation;
            _guildAPI = guildAPI;
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

            var eb = _feedback.CreateEmbedBase() with
            {
                Title = $"User Banned (#{ban.ID})",
                Colour = Color.Red,
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

            var channel = getChannel.Entity;

            var whoDidIt = rescinderID == _identityInformation.ID
                ? "(expired)"
                : $"by <@{rescinderID}>";

            var eb = _feedback.CreateEmbedBase() with
            {
                Title = $"User Unbanned (#{ban.ID})",
                Colour = Color.Green,
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

            var eb = _feedback.CreateEmbedBase() with
            {
                Title = $"User Warned (#{warning.ID})",
                Colour = Color.Orange,
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

            var channel = getChannel.Entity;

            var whoDidIt = rescinderID == _identityInformation.ID
                ? "(expired)"
                : $"by <@{rescinderID}>";

            var eb = _feedback.CreateEmbedBase() with
            {
                Title = $"Warning Removed (#{warning.ID})",
                Colour = Color.Green,
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

            var eb = _feedback.CreateEmbedBase() with
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

            var eb = _feedback.CreateEmbedBase() with
            {
                Title = $"Note Removed (#{note.ID})",
                Colour = Color.Green,
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

            var eb = _feedback.CreateEmbedBase() with
            {
                Colour = Color.Cyan,
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

            var eb = _feedback.CreateEmbedBase() with
            {
                Colour = Color.Cyan,
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

            var eb = _feedback.CreateEmbedBase() with
            {
                Colour = Color.Cyan,
                Description = $"<@{userID}> changed their discriminator from {oldDiscriminator:D4} to " +
                              $"{newDiscriminator:D4}."
            };

            var sendResult = await _feedback.SendEmbedAsync(channel, eb, ct);
            return sendResult.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(sendResult);
        }

        /// <summary>
        /// Posts a notification that a message was deleted.
        /// </summary>
        /// <param name="message">The deleted message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<Result> NotifyMessageDeletedAsync(IMessage message)
        {
            // We don't care about bot messages
            var isNonUserMessage = (message.Author.IsBot.HasValue && message.Author.IsBot.Value) ||
                                   (message.Author.IsSystem.HasValue && message.Author.IsSystem.Value);

            if (isNonUserMessage)
            {
                return Result.FromSuccess();
            }

            // We don't care about non-guild messages
            if (!message.GuildID.HasValue)
            {
                return Result.FromSuccess();
            }

            var getChannel = await GetMonitoringChannelAsync(message.GuildID.Value);
            if (!getChannel.IsSuccess)
            {
                return Result.FromError(getChannel);
            }

            var channel = getChannel.Entity;

            var eb = _feedback.CreateEmbedBase() with
            {
                Title = "Message Deleted",
                Colour = Color.Orange
            };

            var getGuildRoles = await _guildAPI.GetGuildRolesAsync(message.GuildID.Value);
            if (!getGuildRoles.IsSuccess)
            {
                return Result.FromError(getGuildRoles);
            }

            var guildRoles = getGuildRoles.Entity;
            var everyoneRole = guildRoles.First(r => r.ID == message.GuildID.Value);

            var getGuildMember = await _guildAPI.GetGuildMemberAsync(message.GuildID.Value, _identityInformation.ID);
            if (!getGuildMember.IsSuccess)
            {
                return Result.FromError(getGuildMember);
            }

            var botGuildMember = getGuildMember.Entity;
            var botRoles = guildRoles.Where(r => botGuildMember.Roles.Contains(r.ID)).ToList();

            var botPermissions = DiscordPermissionSet.ComputePermissions
            (
                _identityInformation.ID,
                everyoneRole,
                botRoles
            );

            var extra = string.Empty;
            if (botPermissions.HasPermission(DiscordPermission.ViewAuditLog))
            {
                var mostProbableDeleter = await FindMostProbableDeleterAsync(message);

                var isNonUserDeleter = (mostProbableDeleter.IsBot.HasValue && mostProbableDeleter.IsBot.Value) ||
                                       (mostProbableDeleter.IsSystem.HasValue && mostProbableDeleter.IsSystem.Value);

                // We don't care about bot deletions
                if (!isNonUserDeleter)
                {
                    extra = $" by <@{mostProbableDeleter.ID}>";
                }
            }

            eb = eb with
            {
                Description = $"A message was deleted from <#{channel}>{extra}."
            };

            var quote = _quotes.CreateMessageQuote(message, _identityInformation.ID);

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
        private Task<IUser> FindMostProbableDeleterAsync(IMessage message)
        {
            // TODO: Wait for a hotfix from Discord.NET
            /*
            bool AreDateTimeOffsetsCloseEnough(DateTimeOffset first, DateTimeOffset second)
            {
                return (first - second) < TimeSpan.FromSeconds(5);
            }

            var deletionTime = DateTimeOffset.UtcNow;

            var auditLogs = await channel.Guild.GetAuditLogsAsync();
            var deletionEntry = auditLogs.FirstOrDefault
            (
                e => e.Data is MessageDeleteAuditLogData deleteAuditLogData &&
                     deleteAuditLogData.AuthorId == message.Author.Id &&
                     deleteAuditLogData.ChannelId == message.Channel.Id
                     && AreDateTimeOffsetsCloseEnough(deletionTime, e.CreatedAt)
            );

            if (!(deletionEntry is null))
            {
                return deletionEntry.User;
            }
            */

            // No audit entries are generated when the user deletes a message themselves
            return Task.FromResult(message.Author);
        }
    }
}
