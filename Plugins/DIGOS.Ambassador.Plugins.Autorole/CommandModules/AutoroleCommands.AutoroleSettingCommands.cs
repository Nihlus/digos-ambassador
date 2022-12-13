//
//  AutoroleCommands.AutoroleSettingCommands.cs
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
using System.ComponentModel;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Plugins.Autorole.Permissions;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Autorole.CommandModules;

public partial class AutoroleCommands
{
    /// <summary>
    /// Contains commands for server-wide autorole settings.
    /// </summary>
    [UsedImplicitly]
    [Group("settings")]
    [Description("Commands for server-wide autorole settings.")]
    public class AutoroleSettingCommands : CommandGroup
    {
        private readonly AutoroleService _autoroles;
        private readonly FeedbackService _feedback;
        private readonly ICommandContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoroleSettingCommands"/> class.
        /// </summary>
        /// <param name="autoroles">The autorole service.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="context">The command context.</param>
        public AutoroleSettingCommands
        (
            AutoroleService autoroles,
            FeedbackService feedback,
            ICommandContext context
        )
        {
            _autoroles = autoroles;
            _feedback = feedback;
            _context = context;
        }

        /// <summary>
        /// Shows the server-wide settings.
        /// </summary>
        [UsedImplicitly]
        [Command("show")]
        [Description("Shows the server-wide autorole settings.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(ShowAutoroleServerSettings), PermissionTarget.Self)]
        public async Task<Result> ShowSettingsAsync()
        {
            if (!_context.TryGetGuildID(out var guildID))
            {
                throw new InvalidOperationException();
            }

            var getSettings = await _autoroles.GetOrCreateServerSettingsAsync(guildID.Value);
            if (!getSettings.IsSuccess)
            {
                return Result.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            var notificationChannelValue = settings.AffirmationRequiredNotificationChannelID.HasValue
                ? $"<#{settings.AffirmationRequiredNotificationChannelID.Value}"
                : "None";

            var embed = new Embed
            {
                Colour = _feedback.Theme.Secondary,
                Title = "Autorole Settings",
                Fields = new[] { new EmbedField("Confirmation Notification Channel", notificationChannelValue) }
            };

            var send = await _feedback.SendContextualEmbedAsync(embed, ct: this.CancellationToken);
            return send.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(send);
        }

        /// <summary>
        /// Clears the confirmation notification channel.
        /// </summary>
        [UsedImplicitly]
        [Command("clear-notification-channel")]
        [Description("clears the confirmation notification channel.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(EditAutoroleServerSettings), PermissionTarget.Self)]
        public async Task<Result<FeedbackMessage>> ClearAffirmationNotificationChannel()
        {
            if (!_context.TryGetGuildID(out var guildID))
            {
                throw new InvalidOperationException();
            }

            var clearResult = await _autoroles.ClearAffirmationNotificationChannelAsync(guildID.Value);

            return !clearResult.IsSuccess
                ? Result<FeedbackMessage>.FromError(clearResult)
                : new FeedbackMessage("Channel cleared.", _feedback.Theme.Secondary);
        }

        /// <summary>
        /// Sets the confirmation notification channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        [UsedImplicitly]
        [Command("set-notification-channel")]
        [Description("Sets the confirmation notification channel.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(EditAutoroleServerSettings), PermissionTarget.Self)]
        public async Task<Result<FeedbackMessage>> SetAffirmationNotificationChannel(IChannel channel)
        {
            if (!_context.TryGetGuildID(out var guildID))
            {
                throw new InvalidOperationException();
            }

            if (channel.Type is not ChannelType.GuildText)
            {
                return new UserError("That's not a text channel.");
            }

            var setResult = await _autoroles.SetAffirmationNotificationChannelAsync(guildID.Value, channel.ID);

            return !setResult.IsSuccess
                ? Result<FeedbackMessage>.FromError(setResult)
                : new FeedbackMessage("Channel set.", _feedback.Theme.Secondary);
        }
    }
}
