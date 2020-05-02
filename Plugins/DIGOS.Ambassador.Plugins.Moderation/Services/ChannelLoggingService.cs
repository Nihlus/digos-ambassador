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

using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using DIGOS.Ambassador.Plugins.Quotes.Services;
using Discord;
using Discord.WebSocket;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Moderation.Services
{
    /// <summary>
    /// Assists in logging various events to the configured log channels.
    /// </summary>
    public sealed class ChannelLoggingService
    {
        private readonly ModerationService _moderation;
        private readonly DiscordSocketClient _client;

        private readonly QuoteService _quotes;
        private readonly UserFeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelLoggingService"/> class.
        /// </summary>
        /// <param name="moderation">The moderation service.</param>
        /// <param name="client">The Discord client in use.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="quotes">The quote service.</param>
        public ChannelLoggingService
        (
            ModerationService moderation,
            DiscordSocketClient client,
            UserFeedbackService feedback,
            QuoteService quotes
        )
        {
            _moderation = moderation;
            _client = client;
            _feedback = feedback;
            _quotes = quotes;
        }

        /// <summary>
        /// Posts a notification that a user was banned.
        /// </summary>
        /// <param name="ban">The ban.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyUserBanned(UserBan ban)
        {
            var guild = _client.GetGuild((ulong)ban.Server.DiscordID);
            var getChannel = await GetModerationLogChannelAsync(guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;

            var author = guild.GetUser((ulong)ban.Author.DiscordID);

            var eb = _feedback.CreateEmbedBase();
            eb.WithTitle($"User Banned (#{ban.ID})");
            eb.WithColor(Color.Red);

            var bannedUser = guild.GetUser((ulong)ban.User.DiscordID);
            eb.WithDescription
            (
                $"{bannedUser.Mention} (ID {bannedUser.Id}) was banned by {author.Mention}.\n" +
                $"Reason: {ban.Reason}"
            );

            await _feedback.SendEmbedAsync(channel, eb.Build());
        }

        /// <summary>
        /// Posts a notification that a user was unbanned.
        /// </summary>
        /// <param name="ban">The ban.</param>
        /// <param name="rescinder">The person who rescinded the ban.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyUserUnbanned(UserBan ban, IGuildUser rescinder)
        {
            var guild = _client.GetGuild((ulong)ban.Server.DiscordID);
            var getChannel = await GetModerationLogChannelAsync(guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;

            var eb = _feedback.CreateEmbedBase();
            eb.WithTitle($"User Unbanned (#{ban.ID})");
            eb.WithColor(Color.Green);

            var whoDidIt = rescinder.IsMe(_client)
                ? "(expired)"
                : $"by {rescinder.Mention}";

            var bannedUser = guild.GetUser((ulong)ban.User.DiscordID);
            eb.WithDescription($"{bannedUser.Mention} (ID {bannedUser.Id}) was unbanned {whoDidIt}.");

            await _feedback.SendEmbedAsync(channel, eb.Build());
        }

        /// <summary>
        /// Posts a notification that a user was warned.
        /// </summary>
        /// <param name="warning">The warning.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyUserWarningAdded(UserWarning warning)
        {
            var guild = _client.GetGuild((ulong)warning.Server.DiscordID);
            var getChannel = await GetModerationLogChannelAsync(guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;

            var author = guild.GetUser((ulong)warning.Author.DiscordID);

            var eb = _feedback.CreateEmbedBase();
            eb.WithTitle($"User Warned (#{warning.ID})");
            eb.WithColor(Color.Orange);

            var warnedUser = guild.GetUser((ulong)warning.User.DiscordID);
            eb.WithDescription
            (
                $"{warnedUser.Mention} (ID {warnedUser.Id}) was warned by {author.Mention}.\n" +
                $"Reason: {warning.Reason}"
            );

            await _feedback.SendEmbedAsync(channel, eb.Build());
        }

        /// <summary>
        /// Posts a notification that a warning was rescinded.
        /// </summary>
        /// <param name="warning">The warning.</param>
        /// <param name="rescinder">The person who rescinded the warning.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyUserWarningRemoved(UserWarning warning, IGuildUser rescinder)
        {
            var guild = _client.GetGuild((ulong)warning.Server.DiscordID);
            var getChannel = await GetModerationLogChannelAsync(guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;

            var eb = _feedback.CreateEmbedBase();
            eb.WithTitle($"Warning Removed (#{warning.ID})");
            eb.WithColor(Color.Green);

            var whoDidIt = rescinder.IsMe(_client)
                ? "(expired)"
                : $"by {rescinder.Mention}";

            eb.WithDescription
            (
                $"A warning was removed from {MentionUtils.MentionUser((ulong)warning.User.DiscordID)} " +
                $"(ID {warning.User.DiscordID}) {whoDidIt}."
            );

            await _feedback.SendEmbedAsync(channel, eb.Build());
        }

        /// <summary>
        /// Posts a notification that a note was added to a user.
        /// </summary>
        /// <param name="note">The note.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyUserNoteAdded(UserNote note)
        {
            var guild = _client.GetGuild((ulong)note.Server.DiscordID);
            var getChannel = await GetModerationLogChannelAsync(guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;

            var author = guild.GetUser((ulong)note.Author.DiscordID);

            var eb = _feedback.CreateEmbedBase();
            eb.WithTitle($"Note Added (#{note.ID})");
            eb.WithColor(Color.Gold);

            var notedUser = guild.GetUser((ulong)note.User.DiscordID);
            eb.WithDescription
            (
                $"A note was added to {notedUser.Mention} (ID {notedUser.Id}) by {author.Mention}.\n" +
                $"Contents: {note.Content}"
            );

            await _feedback.SendEmbedAsync(channel, eb.Build());
        }

        /// <summary>
        /// Posts a notification that a note was removed from a user.
        /// </summary>
        /// <param name="note">The note.</param>
        /// <param name="remover">The person that removed the note.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyUserNoteRemoved(UserNote note, IGuildUser remover)
        {
            var guild = _client.GetGuild((ulong)note.Server.DiscordID);
            var getChannel = await GetModerationLogChannelAsync(guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;

            var eb = _feedback.CreateEmbedBase();
            eb.WithTitle($"Note Removed (#{note.ID})");
            eb.WithColor(Color.Green);

            var notedUser = guild.GetUser((ulong)note.User.DiscordID);
            eb.WithDescription
            (
                $"A note was removed from {notedUser.Mention} (ID {notedUser.Id}) by {remover.Mention}."
            );

            await _feedback.SendEmbedAsync(channel, eb.Build());
        }

        /// <summary>
        /// Posts a notification that a user left the server.
        /// </summary>
        /// <param name="user">The user that left.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyUserLeft(IGuildUser user)
        {
            var getChannel = await GetMonitoringChannelAsync(user.Guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;

            var eb = _feedback.CreateEmbedBase();
            eb.WithColor(Color.Blue);

            eb.WithDescription($"{user.Mention} left the server.");

            await _feedback.SendEmbedAsync(channel, eb.Build());
        }

        /// <summary>
        /// Posts a notification that a user changed their username.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="oldUsername">The old username.</param>
        /// <param name="newUsername">The new username.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyUserUsernameChanged
        (
            IGuildUser user,
            string oldUsername,
            string newUsername
        )
        {
            var getChannel = await GetMonitoringChannelAsync(user.Guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;

            var eb = _feedback.CreateEmbedBase();
            eb.WithColor(Color.Blue);

            eb.WithDescription($"{user.Mention} changed their username from {oldUsername} to {newUsername}.");

            await _feedback.SendEmbedAsync(channel, eb.Build());
        }

        /// <summary>
        /// Posts a notification that a user changed their discriminator.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="oldDiscriminator">The old discriminator.</param>
        /// <param name="newDiscriminator">The new discriminator.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyUserDiscriminatorChanged
        (
            IGuildUser user,
            string oldDiscriminator,
            string newDiscriminator
        )
        {
            var getChannel = await GetMonitoringChannelAsync(user.Guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;

            var eb = _feedback.CreateEmbedBase();
            eb.WithColor(Color.Blue);

            eb.WithDescription($"{user.Mention} changed their discriminator from {oldDiscriminator} to {newDiscriminator}.");

            await _feedback.SendEmbedAsync(channel, eb.Build());
        }

        /// <summary>
        /// Posts a notification that a message was deleted.
        /// </summary>
        /// <param name="message">The deleted message.</param>
        /// <param name="messageChannel">The channel the message was deleted from.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyMessageDeleted(IMessage message, ISocketMessageChannel messageChannel)
        {
            if (!(messageChannel is ITextChannel textChannel))
            {
                return;
            }

            // We don't care about bot messages
            if (message.Author.IsBot | message.Author.IsWebhook)
            {
                return;
            }

            var getChannel = await GetMonitoringChannelAsync(textChannel.Guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;

            var eb = _feedback.CreateEmbedBase();
            eb.WithTitle("Message Deleted");
            eb.WithColor(Color.Orange);

            var extra = string.Empty;
            if ((await textChannel.Guild.GetUserAsync(_client.CurrentUser.Id)).GuildPermissions.ViewAuditLog)
            {
                var mostProbableDeleter = await FindMostProbableDeleterAsync(message, textChannel);

                // We don't care about bot deletions
                if (!(mostProbableDeleter.IsBot || mostProbableDeleter.IsWebhook))
                {
                    extra = $"by {mostProbableDeleter.Mention}.";
                }
            }

            eb.WithDescription
            (
                $"A message was deleted from {MentionUtils.MentionChannel(textChannel.Id)} {extra}"
            );

            var quote = _quotes.CreateMessageQuote(message, _client.CurrentUser);

            await _feedback.SendEmbedAsync(channel, eb.Build());
            await _feedback.SendEmbedAsync(channel, quote.Build());
        }

        /// <summary>
        /// Retrieves the moderation log channel.
        /// </summary>
        /// <param name="guild">The guild to grab the channel from.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        private async Task<RetrieveEntityResult<ITextChannel>> GetModerationLogChannelAsync(IGuild guild)
        {
            var getSettings = await _moderation.GetOrCreateServerSettingsAsync(guild);
            if (!getSettings.IsSuccess)
            {
                return RetrieveEntityResult<ITextChannel>.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            if (settings.ModerationLogChannel is null)
            {
                return RetrieveEntityResult<ITextChannel>.FromError("No configured channel.");
            }

            var channel = await guild.GetChannelAsync((ulong)settings.ModerationLogChannel);
            if (channel is null)
            {
                return RetrieveEntityResult<ITextChannel>.FromError("Channel not found. Deleted?");
            }

            if (!(channel is ITextChannel textChannel))
            {
                return RetrieveEntityResult<ITextChannel>.FromError("The configured channel isn't a text channel.");
            }

            return RetrieveEntityResult<ITextChannel>.FromSuccess(textChannel);
        }

        /// <summary>
        /// Retrieves the event monitoring channel.
        /// </summary>
        /// <param name="guild">The guild to grab the channel from.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        private async Task<RetrieveEntityResult<ITextChannel>> GetMonitoringChannelAsync(IGuild guild)
        {
            var getSettings = await _moderation.GetOrCreateServerSettingsAsync(guild);
            if (!getSettings.IsSuccess)
            {
                return RetrieveEntityResult<ITextChannel>.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            if (settings.MonitoringChannel is null)
            {
                return RetrieveEntityResult<ITextChannel>.FromError("No configured channel.");
            }

            var channel = await guild.GetChannelAsync((ulong)settings.MonitoringChannel);
            if (channel is null)
            {
                return RetrieveEntityResult<ITextChannel>.FromError("Channel not found. Deleted?");
            }

            if (!(channel is ITextChannel textChannel))
            {
                return RetrieveEntityResult<ITextChannel>.FromError("The configured channel isn't a text channel.");
            }

            return RetrieveEntityResult<ITextChannel>.FromSuccess(textChannel);
        }

        // ReSharper disable once UnusedParameter.Local
        private Task<IUser> FindMostProbableDeleterAsync(IMessage message, ITextChannel channel)
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
