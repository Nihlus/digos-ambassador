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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Feedback.Results;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Plugins.Moderation.Permissions;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using Humanizer;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Moderation.CommandModules
{
    /// <summary>
    /// Warning-related commands, such as viewing or editing info about a specific warning.
    /// </summary>
    [Group("warn")]
    [Description("Warning-related commands, such as viewing or editing info about a specific warning.")]
    public partial class WarningCommands : CommandGroup
    {
        private readonly ModerationService _moderation;
        private readonly WarningService _warnings;
        private readonly UserFeedbackService _feedback;
        private readonly InteractivityService _interactivity;
        private readonly ChannelLoggingService _logging;
        private readonly IDiscordRestUserAPI _userAPI;
        private readonly ICommandContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="WarningCommands"/> class.
        /// </summary>
        /// <param name="moderation">The moderation service.</param>
        /// <param name="warnings">The warning service.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="logging">The logging service.</param>
        /// <param name="userAPI">The user API.</param>
        /// <param name="context">The command context.</param>
        public WarningCommands
        (
            ModerationService moderation,
            WarningService warnings,
            UserFeedbackService feedback,
            InteractivityService interactivity,
            ChannelLoggingService logging,
            IDiscordRestUserAPI userAPI,
            ICommandContext context
        )
        {
            _moderation = moderation;
            _warnings = warnings;
            _feedback = feedback;
            _interactivity = interactivity;
            _logging = logging;
            _userAPI = userAPI;
            _context = context;
        }

        /// <summary>
        /// Lists the warnings attached to the given user.
        /// </summary>
        /// <param name="user">The user.</param>
        [Command("list")]
        [Description("Lists the warnings attached to the given user.")]
        [RequirePermission(typeof(ManageWarnings), PermissionTarget.Other)]
        [RequireContext(ChannelContext.Guild)]
        public async Task<IResult> ListWarningsAsync(IUser user)
        {
            var warnings = await _warnings.GetWarningsAsync(_context.GuildID.Value, user.ID);

            var createPages = await PaginatedEmbedFactory.PagesFromCollectionAsync
            (
                warnings,
                async warning =>
                {
                    var getAuthor = await _userAPI.GetUserAsync(warning.Author.DiscordID);
                    if (!getAuthor.IsSuccess)
                    {
                        return Result<Embed>.FromError(getAuthor);
                    }

                    var author = getAuthor.Entity;

                    var getAuthorAvatar = CDN.GetUserAvatarUrl(author);

                    var embedFields = new List<EmbedField>();
                    var eb = new Embed
                    {
                        Title = $"Warning #{warning.ID} for {user.Username}:{user.Discriminator}",
                        Colour = Color.Orange,
                        Author = new EmbedAuthor
                        {
                            Name = author.Username,
                            IconUrl = getAuthorAvatar.IsSuccess ? getAuthorAvatar.Entity.ToString() : default
                        },
                        Description = warning.Reason,
                        Fields = embedFields
                    };

                    embedFields.Add(new EmbedField("Created", warning.CreatedAt.Humanize()));

                    if (warning.CreatedAt != warning.UpdatedAt)
                    {
                        embedFields.Add(new EmbedField("Last Updated", warning.UpdatedAt.Humanize()));
                    }

                    if (warning.ExpiresOn.HasValue)
                    {
                        embedFields.Add(new EmbedField("Expires On", warning.ExpiresOn.Humanize()));
                    }

                    return eb;
                }
            );

            if (createPages.Any(p => !p.IsSuccess))
            {
                return createPages.First(p => !p.IsSuccess);
            }

            var pages = createPages.Select(p => p.Entity!).ToList();

            await _interactivity.SendInteractiveMessageAsync
            (
                _context.ChannelID,
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
        /// Deletes the given warning.
        /// </summary>
        /// <param name="warningID">The ID of the warning to delete.</param>
        [Command("delete")]
        [Description("Deletes the given warning.")]
        [RequirePermission(typeof(ManageWarnings), PermissionTarget.All)]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<UserMessage>> DeleteWarningAsync(long warningID)
        {
            var getWarning = await _warnings.GetWarningAsync(_context.GuildID.Value, warningID);
            if (!getWarning.IsSuccess)
            {
                return Result<UserMessage>.FromError(getWarning);
            }

            var warning = getWarning.Entity;

            // This has to be done before the warning is actually deleted - otherwise, the lazy loader is removed and
            // navigation properties can't be evaluated
            var notifyResult = await _logging.NotifyUserWarningRemovedAsync(warning, _context.User.ID);
            if (!notifyResult.IsSuccess)
            {
                return Result<UserMessage>.FromError(notifyResult);
            }

            var deleteWarning = await _warnings.DeleteWarningAsync(warning);
            if (!deleteWarning.IsSuccess)
            {
                return Result<UserMessage>.FromError(deleteWarning);
            }

            return new ConfirmationMessage("Warning deleted.");
        }

        /// <summary>
        /// Adds a warning to the given user.
        /// </summary>
        /// <param name="user">The user to add the warning to.</param>
        /// <param name="reason">The reason for the warning.</param>
        /// <param name="expiresAfter">The duration of the warning, if any.</param>
        [Command("user")]
        [Description("Adds a warning to the given user.")]
        [RequirePermission(typeof(ManageWarnings), PermissionTarget.All)]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<UserMessage>> AddWarningAsync
        (
            IUser user,
            string reason,
            TimeSpan? expiresAfter = null
        )
        {
            DateTime? expiresOn = null;
            if (expiresAfter is not null)
            {
                expiresOn = DateTime.Now.Add(expiresAfter.Value);
            }

            var addWarning = await _warnings.CreateWarningAsync
            (
                _context.User.ID,
                user.ID,
                _context.GuildID.Value,
                reason,
                expiresOn: expiresOn
            );

            if (!addWarning.IsSuccess)
            {
                return Result<UserMessage>.FromError(addWarning);
            }

            var warning = addWarning.Entity;
            var getSettings = await _moderation.GetOrCreateServerSettingsAsync(_context.GuildID.Value);
            if (!getSettings.IsSuccess)
            {
                return Result<UserMessage>.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            var notifyResult = await _logging.NotifyUserWarningAddedAsync(warning);
            if (!notifyResult.IsSuccess)
            {
                return Result<UserMessage>.FromError(notifyResult);
            }

            var warnings = await _warnings.GetWarningsAsync(user.ID);
            if (warnings.Count < settings.WarningThreshold)
            {
                return new ConfirmationMessage($"Warning added (ID {warning.ID}): {warning.Reason}.");
            }

            var sendAlert = await _feedback.SendContextualWarningAsync
            (
                _context.User.ID,
                $"The warned user now has {warnings.Count} warnings. Consider further action."
            );

            return !sendAlert.IsSuccess
                ? Result<UserMessage>.FromError(sendAlert)
                : new ConfirmationMessage($"Warning added (ID {warning.ID}): {warning.Reason}.");
        }
    }
}
