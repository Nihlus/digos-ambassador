//
//  WarningCommands.cs
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
    /// Warning-related commands, such as viewing or editing info about a specific warning.
    /// </summary>
    [PublicAPI]
    [Group("warning")]
    [Alias("warning", "warn")]
    [Summary("Warning-related commands, such as viewing or editing info about a specific warning.")]
    public partial class WarningCommands : ModuleBase
    {
        [NotNull]
        private readonly ModerationService _moderation;

        [NotNull]
        private readonly WarningService _warnings;

        [NotNull]
        private readonly UserFeedbackService _feedback;

        [NotNull]
        private readonly InteractivityService _interactivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="WarningCommands"/> class.
        /// </summary>
        /// <param name="moderation">The moderation service.</param>
        /// <param name="warnings">The warning service.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        public WarningCommands
        (
            [NotNull] ModerationService moderation,
            [NotNull] WarningService warnings,
            [NotNull] UserFeedbackService feedback,
            [NotNull] InteractivityService interactivity
        )
        {
            _moderation = moderation;
            _warnings = warnings;
            _feedback = feedback;
            _interactivity = interactivity;
        }

        /// <summary>
        /// Lists the warnings attached to the given user.
        /// </summary>
        /// <param name="user">The user.</param>
        [Command("list")]
        [Summary("Lists the warnings attached to the given user.")]
        [RequirePermission(typeof(ManageWarnings), PermissionTarget.Other)]
        [RequireContext(ContextType.Guild)]
        public async Task ListWarningsAsync([NotNull] IGuildUser user)
        {
            var warnings = _warnings.GetWarnings(user);

            var appearance = PaginatedAppearanceOptions.Default;
            appearance.Title = "Warnings";
            appearance.Color = Color.Orange;

            var paginatedEmbed = await PaginatedEmbedFactory.PagesFromCollectionAsync
            (
                _feedback,
                this.Context.User,
                warnings,
                async (eb, warning) =>
                {
                    eb.WithTitle($"Warning #{warning.ID} for {user.Username}:{user.Discriminator}");

                    var author = await this.Context.Guild.GetUserAsync((ulong)warning.Author.DiscordID);
                    eb.WithAuthor(author);

                    eb.WithDescription(warning.Reason);

                    eb.AddField("Created", warning.CreatedAt);

                    if (warning.CreatedAt != warning.UpdatedAt)
                    {
                        eb.AddField("Last Updated", warning.UpdatedAt);
                    }

                    if (warning.IsTemporary)
                    {
                        eb.AddField("Expires On", warning.ExpiresOn);
                    }

                    if (!(warning.MessageID is null))
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
        /// Adds a warning to the given user.
        /// </summary>
        /// <param name="user">The user to add the warning to.</param>
        /// <param name="content">The contents of the warning.</param>
        /// <param name="expiresAfter">The duration of the warning, if any.</param>
        [Command]
        [Summary("Adds a warning to the given user.")]
        [Priority(int.MinValue)]
        [RequirePermission(typeof(ManageWarnings), PermissionTarget.All)]
        [RequireContext(ContextType.Guild)]
        public async Task AddWarningAsync
        (
            [NotNull] IGuildUser user,
            [NotNull] string content,
            TimeSpan? expiresAfter = null
        )
        {
            DateTime? expiresOn = null;
            if (!(expiresAfter is null))
            {
                expiresOn = DateTime.Now.Add(expiresAfter.Value);
            }

            var addWarning = await _warnings.CreateWarningAsync(this.Context.User, user, content, expiresOn: expiresOn);
            if (!addWarning.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, addWarning.ErrorReason);
                return;
            }

            var warning = addWarning.Entity;
            await _feedback.SendConfirmationAsync(this.Context, $"Warning added (ID {warning.ID}).");

            var getSettings = await _moderation.GetOrCreateServerSettingsAsync(this.Context.Guild);
            if (!getSettings.IsSuccess)
            {
                return;
            }

            var settings = getSettings.Entity;

            var warningCount = await _warnings.GetWarnings(user).CountAsync();
            if (warningCount >= settings.WarningThreshold)
            {
                await _feedback.SendWarningAsync
                (
                    this.Context, $"The warned user now has {warningCount} warnings. Consider further action."
                );
            }
        }

        /// <summary>
        /// Deletes the given warning.
        /// </summary>
        /// <param name="warningID">The ID of the warning to delete.</param>
        [Command("delete")]
        [Summary("Deletes the given warning.")]
        [RequirePermission(typeof(ManageWarnings), PermissionTarget.All)]
        [RequireContext(ContextType.Guild)]
        public async Task DeleteWarningAsync(long warningID)
        {
            var getWarning = await _warnings.GetWarningAsync(this.Context.Guild, warningID);
            if (!getWarning.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getWarning.ErrorReason);
                return;
            }

            var warning = getWarning.Entity;

            var deleteWarning = await _warnings.DeleteWarningAsync(warning);
            if (!deleteWarning.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, deleteWarning.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "Warning deleted.");
        }
    }
}
