//
//  BanCommands.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Plugins.Moderation.Permissions;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using Humanizer;
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

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Moderation.CommandModules
{
    /// <summary>
    /// Ban-related commands, such as viewing or editing info about a specific ban.
    /// </summary>
    [Group("ban")]
    [Description("Ban-related commands, such as viewing or editing info about a specific ban.")]
    public partial class BanCommands : CommandGroup
    {
        private readonly BanService _bans;
        private readonly ChannelLoggingService _logging;
        private readonly IDiscordRestGuildAPI _guildAPI;
        private readonly ICommandContext _context;
        private readonly IDiscordRestUserAPI _userAPI;
        private readonly InteractivityService _interactivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="BanCommands"/> class.
        /// </summary>
        /// <param name="bans">The ban service.</param>
        /// <param name="logging">The logging service.</param>
        /// <param name="context">The command context.</param>
        /// <param name="guildAPI">The guild API.</param>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="userAPI">The user API.</param>
        public BanCommands
        (
            BanService bans,
            ChannelLoggingService logging,
            ICommandContext context,
            IDiscordRestGuildAPI guildAPI,
            InteractivityService interactivity,
            IDiscordRestUserAPI userAPI
        )
        {
            _bans = bans;
            _logging = logging;
            _context = context;
            _guildAPI = guildAPI;
            _interactivity = interactivity;
            _userAPI = userAPI;
        }

        /// <summary>
        /// Bans the given user.
        /// </summary>
        /// <param name="user">The user to add the ban to.</param>
        /// <param name="reason">The reason for the ban.</param>
        /// <param name="expiresAfter">The duration of the ban, if any.</param>
        [Command("user")]
        [Description("Bans the given user.")]
        [RequirePermission(typeof(ManageBans), PermissionTarget.All)]
        [RequireContext(ChannelContext.Guild)]
        public async Task<IResult> AddBanAsync
        (
            IUser user,
            string reason,
            TimeSpan? expiresAfter = null
        )
        {
            DateTimeOffset? expiresOn = null;
            if (expiresAfter is not null)
            {
                expiresOn = DateTimeOffset.UtcNow.Add(expiresAfter.Value);
            }

            var createBan = await _bans.CreateBanAsync
            (
                _context.User.ID,
                user.ID,
                _context.GuildID.Value,
                reason,
                expiresOn: expiresOn
            );

            if (!createBan.IsSuccess)
            {
                return createBan;
            }

            var ban = createBan.Entity;

            var notifyResult = await _logging.NotifyUserBannedAsync(ban);
            if (!notifyResult.IsSuccess)
            {
                return notifyResult;
            }

            return await _guildAPI.CreateGuildBanAsync(_context.GuildID.Value, user.ID, reason: reason);
        }

        /// <summary>
        /// Lists the bans on the server.
        /// </summary>
        [Command("list")]
        [Description("Lists the bans on the server.")]
        [RequirePermission(typeof(ManageBans), PermissionTarget.Other)]
        [RequireContext(ChannelContext.Guild)]
        public async Task<IResult> ListBansAsync()
        {
            var bans = await _bans.GetBansAsync(_context.GuildID.Value);
            var createPages = await PaginatedEmbedFactory.PagesFromCollectionAsync
            (
                bans,
                async ban =>
                {
                    var getBanAuthor = await _userAPI.GetUserAsync(ban.Author.DiscordID);
                    if (!getBanAuthor.IsSuccess)
                    {
                        return Result<Embed>.FromError(getBanAuthor);
                    }

                    var banAuthor = getBanAuthor.Entity;

                    var getBannedUser = await _userAPI.GetUserAsync(ban.User.DiscordID);
                    if (!getBannedUser.IsSuccess)
                    {
                        return Result<Embed>.FromError(getBannedUser);
                    }

                    var bannedUser = getBanAuthor.Entity;

                    var getBanAuthorAvatar = CDN.GetUserAvatarUrl(banAuthor);

                    var embedFields = new List<IEmbedField>();
                    var eb = new Embed
                    {
                        Title = $"Ban #{ban.ID} for {bannedUser.Username}:{bannedUser.Discriminator}",
                        Colour = Color.Orange,
                        Author = new EmbedAuthor(banAuthor.Username)
                        {
                            Url = getBanAuthorAvatar.IsSuccess
                                ? getBanAuthorAvatar.Entity.ToString()
                                : default(Optional<string>)
                        },
                        Description = ban.Reason,
                        Fields = embedFields
                    };

                    embedFields.Add(new EmbedField("Created", ban.CreatedAt.Humanize()));

                    if (ban.CreatedAt != ban.UpdatedAt)
                    {
                        embedFields.Add(new EmbedField("Last Updated", ban.UpdatedAt.Humanize()));
                    }

                    if (ban.ExpiresOn.HasValue)
                    {
                        embedFields.Add(new EmbedField("Expires On", ban.ExpiresOn.Humanize()));
                    }

                    if (ban.MessageID is not null)
                    {
                        // TODO
                    }

                    return eb;
                }
            );

            if (createPages.Any(p => !p.IsSuccess))
            {
                return createPages.First(p => !p.IsSuccess);
            }

            var pages = createPages.Select(p => p.Entity).ToList();

            await _interactivity.SendContextualInteractiveMessageAsync
            (
                (channelID, messageID) => new PaginatedMessage
                (
                    channelID,
                    messageID,
                    _context.User.ID,
                    pages
                )
            );

            return Result.FromSuccess();
        }

        /// <summary>
        /// Deletes the given ban.
        /// </summary>
        /// <param name="banID">The ID of the ban to delete.</param>
        [Command("delete")]
        [Description("Deletes the given ban.")]
        [RequirePermission(typeof(ManageBans), PermissionTarget.All)]
        [RequireContext(ChannelContext.Guild)]
        public async Task<IResult> DeleteBanAsync(long banID)
        {
            var getBan = await _bans.GetBanAsync(_context.GuildID.Value, banID);
            if (!getBan.IsSuccess)
            {
                return getBan;
            }

            var ban = getBan.Entity;

            // This has to be done before the warning is actually deleted - otherwise, the lazy loader is removed and
            // navigation properties can't be evaluated
            var notifyResult = await _logging.NotifyUserUnbannedAsync(ban, _context.User.ID);
            if (!notifyResult.IsSuccess)
            {
                return notifyResult;
            }

            var deleteBan = await _bans.DeleteBanAsync(ban);
            if (!deleteBan.IsSuccess)
            {
                return deleteBan;
            }

            return await _guildAPI.RemoveGuildBanAsync(_context.GuildID.Value, ban.User.DiscordID);
        }
    }
}
