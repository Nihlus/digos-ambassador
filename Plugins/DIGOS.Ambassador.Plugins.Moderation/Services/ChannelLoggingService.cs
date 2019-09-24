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
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Moderation.Services
{
    /// <summary>
    /// Assists in logging various events to the configured log channels.
    /// </summary>
    public sealed class ChannelLoggingService
    {
        private readonly ModerationService _moderation;

        private readonly DiscordSocketClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelLoggingService"/> class.
        /// </summary>
        /// <param name="moderation">The moderation service.</param>
        /// <param name="client">The Discord client in use.</param>
        public ChannelLoggingService([NotNull] ModerationService moderation, [NotNull] DiscordSocketClient client)
        {
            _moderation = moderation;
            _client = client;
        }

        /// <summary>
        /// Posts a notification that a user was banned.
        /// </summary>
        /// <param name="ban">The ban.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyUserBanned([NotNull] UserBan ban)
        {
            var guild = _client.GetGuild((ulong)ban.Server.DiscordID);
            var getChannel = await GetModerationLogChannelAsync(guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;
        }

        /// <summary>
        /// Posts a notification that a user was unbanned.
        /// </summary>
        /// <param name="ban">The ban.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyUserUnbanned([NotNull] UserBan ban)
        {
            var guild = _client.GetGuild((ulong)ban.Server.DiscordID);
            var getChannel = await GetModerationLogChannelAsync(guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;
        }

        /// <summary>
        /// Posts a notification that a user was warned.
        /// </summary>
        /// <param name="warning">The warning.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyUserWarningAdded([NotNull] UserWarning warning)
        {
            var guild = _client.GetGuild((ulong)warning.Server.DiscordID);
            var getChannel = await GetModerationLogChannelAsync(guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;
        }

        /// <summary>
        /// Posts a notification that a warning was rescinded.
        /// </summary>
        /// <param name="warning">The warning.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyUserWarningRemoved([NotNull] UserWarning warning)
        {
            var guild = _client.GetGuild((ulong)warning.Server.DiscordID);
            var getChannel = await GetModerationLogChannelAsync(guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;
        }

        /// <summary>
        /// Posts a notification that a note was added to a user.
        /// </summary>
        /// <param name="note">The note.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyUserNoteAdded([NotNull] UserNote note)
        {
            var guild = _client.GetGuild((ulong)note.Server.DiscordID);
            var getChannel = await GetModerationLogChannelAsync(guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;
        }

        /// <summary>
        /// Posts a notification that a note was removed from a user.
        /// </summary>
        /// <param name="note">The note.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyUserNoteRemoved([NotNull] UserNote note)
        {
            var guild = _client.GetGuild((ulong)note.Server.DiscordID);
            var getChannel = await GetModerationLogChannelAsync(guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;
        }

        /// <summary>
        /// Posts a notification that a user left the server.
        /// </summary>
        /// <param name="user">The user that left.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyUserLeft([NotNull] IGuildUser user)
        {
            var getChannel = await GetModerationLogChannelAsync(user.Guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;
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
            [NotNull] IGuildUser user,
            [NotNull] string oldUsername,
            [NotNull] string newUsername
        )
        {
            var getChannel = await GetModerationLogChannelAsync(user.Guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;
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
            [NotNull] IGuildUser user,
            [NotNull] string oldDiscriminator,
            [NotNull] string newDiscriminator
        )
        {
            var getChannel = await GetModerationLogChannelAsync(user.Guild);
            if (!getChannel.IsSuccess)
            {
                return;
            }

            var channel = getChannel.Entity;
        }

        /// <summary>
        /// Retrieves the moderation log channel.
        /// </summary>
        /// <param name="guild">The guild to grab the channel from.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        private async Task<RetrieveEntityResult<ITextChannel>> GetModerationLogChannelAsync([NotNull] IGuild guild)
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
        private async Task<RetrieveEntityResult<ITextChannel>> GetMonitoringChannelAsync([NotNull] IGuild guild)
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
    }
}
