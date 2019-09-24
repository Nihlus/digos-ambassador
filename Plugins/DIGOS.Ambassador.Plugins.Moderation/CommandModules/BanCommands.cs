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
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Plugins.Moderation.Permissions;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using DIGOS.Ambassador.Plugins.Permissions.Preconditions;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
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
        [NotNull]
        private readonly ModerationService _moderation;

        [NotNull]
        private readonly BanService _bans;

        [NotNull]
        private readonly UserFeedbackService _feedback;

        [NotNull]
        private readonly InteractivityService _interactivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="BanCommands"/> class.
        /// </summary>
        /// <param name="moderation">The moderation service.</param>
        /// <param name="bans">The ban service.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        public BanCommands
        (
            [NotNull] ModerationService moderation,
            [NotNull] BanService bans,
            [NotNull] UserFeedbackService feedback,
            [NotNull] InteractivityService interactivity
        )
        {
            _moderation = moderation;
            _bans = bans;
            _feedback = feedback;
            _interactivity = interactivity;
        }

        /// <summary>
        /// Lists the bans attached to the given user.
        /// </summary>
        /// <param name="user">The user.</param>
        [Command("list")]
        [Summary("Lists the bans attached to the given user.")]
        [RequirePermission(typeof(ManageBans), PermissionTarget.Other)]
        [RequireContext(ContextType.Guild)]
        public async Task ListBansAsync([NotNull] IGuildUser user)
        {
            var bans = _bans.GetBans(user);

            var appearance = PaginatedAppearanceOptions.Default;
            appearance.Title = "Bans";
            appearance.Color = Color.Orange;

            var paginatedEmbed = await PaginatedEmbedFactory.PagesFromCollectionAsync
            (
                _feedback,
                this.Context.User,
                bans,
                async (eb, ban) =>
                {
                    eb.WithTitle($"Ban #{ban.ID} for {user.Username}:{user.Discriminator}");

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
        public async Task AddBanAsync
        (
            [NotNull] IGuildUser user,
            [NotNull] string reason,
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
                await _feedback.SendErrorAsync(this.Context, addBan.ErrorReason);
                return;
            }

            var ban = addBan.Entity;

            await this.Context.Guild.AddBanAsync((ulong)ban.User.DiscordID, reason: reason);
            await _feedback.SendConfirmationAsync(this.Context, $"User banned (ban ID {ban.ID}).");
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
        public async Task DeleteBanAsync(long banID)
        {
            var getBan = await _bans.GetBanAsync(this.Context.Guild, banID);
            if (!getBan.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getBan.ErrorReason);
                return;
            }

            var ban = getBan.Entity;

            var deleteBan = await _bans.DeleteBanAsync(ban);
            if (!deleteBan.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, deleteBan.ErrorReason);
                return;
            }

            await this.Context.Guild.RemoveBanAsync((ulong)ban.User.DiscordID);
            await _feedback.SendConfirmationAsync(this.Context, "Ban rescinded.");
        }
    }
}
