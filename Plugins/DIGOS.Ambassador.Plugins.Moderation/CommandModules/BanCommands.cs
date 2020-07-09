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
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Extensions.Results;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Plugins.Moderation.Permissions;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using DIGOS.Ambassador.Plugins.Permissions.Preconditions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Moderation.CommandModules
{
    /// <summary>
    /// Ban-related commands, such as viewing or editing info about a specific ban.
    /// </summary>
    [PublicAPI]
    [Group("ban")]
    [Alias("ban", "warn")]
    [Summary("Ban-related commands, such as viewing or editing info about a specific ban.")]
    public partial class BanCommands : ModuleBase
    {
        private readonly ModerationService _moderation;
        private readonly BanService _bans;
        private readonly UserFeedbackService _feedback;
        private readonly InteractivityService _interactivity;
        private readonly ChannelLoggingService _logging;

        /// <summary>
        /// Initializes a new instance of the <see cref="BanCommands"/> class.
        /// </summary>
        /// <param name="moderation">The moderation service.</param>
        /// <param name="bans">The ban service.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="logging">The logging service.</param>
        public BanCommands
        (
            ModerationService moderation,
            BanService bans,
            UserFeedbackService feedback,
            InteractivityService interactivity,
            ChannelLoggingService logging
        )
        {
            _moderation = moderation;
            _bans = bans;
            _feedback = feedback;
            _interactivity = interactivity;
            _logging = logging;
        }

        /// <summary>
        /// Lists the bans on the server.
        /// </summary>
        [Command("list")]
        [Summary("Lists the bans on the server.")]
        [RequirePermission(typeof(ManageBans), PermissionTarget.Other)]
        [RequireContext(ContextType.Guild)]
        public async Task<RuntimeResult> ListBansAsync()
        {
            var bans = await _bans.GetBansAsync(this.Context.Guild);

            var appearance = PaginatedAppearanceOptions.Default;
            appearance.Title = "Bans";
            appearance.Color = Color.Orange;

            var paginatedEmbed = await PaginatedEmbedFactory.PagesFromCollectionAsync
            (
                _feedback,
                _interactivity,
                this.Context.User,
                bans,
                async (eb, ban) =>
                {
                    IUser? bannedUser = null;
                    if (this.Context.Client is DiscordSocketClient socketClient)
                    {
                        bannedUser = await socketClient.Rest.GetUserAsync((ulong)ban.User.DiscordID);
                    }

                    if (bannedUser is null)
                    {
                        eb.WithTitle($"Ban #{ban.ID} for user with ID {ban.User.DiscordID}");
                    }
                    else
                    {
                        eb.WithTitle($"Ban #{ban.ID} for {bannedUser.Username}:{bannedUser.Discriminator}");
                    }

                    var author = await this.Context.Guild.GetUserAsync((ulong)ban.Author.DiscordID);
                    eb.WithAuthor(author);

                    eb.WithDescription(ban.Reason);

                    eb.AddField("Created", ban.CreatedAt);

                    if (ban.CreatedAt != ban.UpdatedAt)
                    {
                        eb.AddField("Last Updated", ban.UpdatedAt);
                    }

                    if (ban.IsTemporary)
                    {
                        eb.AddField("Expires On", ban.ExpiresOn);
                    }

                    if (!(ban.MessageID is null))
                    {
                        // TODO
                    }
                },
                appearance: appearance
            );

            await _interactivity.SendInteractiveMessageAndDeleteAsync
            (
                this.Context.Channel,
                paginatedEmbed,
                TimeSpan.FromMinutes(5)
            );

            return RuntimeCommandResult.FromSuccess();
        }

        /// <summary>
        /// Bans the given user.
        /// </summary>
        /// <param name="user">The user to add the ban to.</param>
        /// <param name="reason">The reason for the ban.</param>
        /// <param name="expiresAfter">The duration of the ban, if any.</param>
        [Command]
        [Summary("Bans the given user.")]
        [Priority(int.MinValue)]
        [RequirePermission(typeof(ManageBans), PermissionTarget.All)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [RequireContext(ContextType.Guild)]
        public async Task<RuntimeResult> AddBanAsync
        (
            IGuildUser user,
            string reason,
            TimeSpan? expiresAfter = null
        )
        {
            DateTime? expiresOn = null;
            if (!(expiresAfter is null))
            {
                expiresOn = DateTime.Now.Add(expiresAfter.Value);
            }

            var addBan = await _bans.CreateBanAsync(this.Context.User, user, reason, expiresOn: expiresOn);
            if (!addBan.IsSuccess)
            {
                return addBan.ToRuntimeResult();
            }

            var ban = addBan.Entity;

            var notifyResult = await _logging.NotifyUserBannedAsync(ban);
            if (!notifyResult.IsSuccess)
            {
                return notifyResult.ToRuntimeResult();
            }

            await this.Context.Guild.AddBanAsync((ulong)ban.User.DiscordID, reason: reason);
            return RuntimeCommandResult.FromSuccess($"User banned (ban ID {ban.ID}).");
        }

        /// <summary>
        /// Deletes the given ban.
        /// </summary>
        /// <param name="banID">The ID of the ban to delete.</param>
        [Command("delete")]
        [Summary("Deletes the given ban.")]
        [RequirePermission(typeof(ManageBans), PermissionTarget.All)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [RequireContext(ContextType.Guild)]
        public async Task<RuntimeResult> DeleteBanAsync(long banID)
        {
            var getBan = await _bans.GetBanAsync(this.Context.Guild, banID);
            if (!getBan.IsSuccess)
            {
                return getBan.ToRuntimeResult();
            }

            var ban = getBan.Entity;

            // This has to be done before the warning is actually deleted - otherwise, the lazy loader is removed and
            // navigation properties can't be evaluated
            var rescinder = await this.Context.Guild.GetUserAsync(this.Context.User.Id);
            var notifyResult = await _logging.NotifyUserUnbannedAsync(ban, rescinder);
            if (!notifyResult.IsSuccess)
            {
                return notifyResult.ToRuntimeResult();
            }

            var deleteBan = await _bans.DeleteBanAsync(ban);
            if (!deleteBan.IsSuccess)
            {
                return deleteBan.ToRuntimeResult();
            }

            await this.Context.Guild.RemoveBanAsync((ulong)ban.User.DiscordID);
            return RuntimeCommandResult.FromSuccess("Ban rescinded.");
        }
    }
}
