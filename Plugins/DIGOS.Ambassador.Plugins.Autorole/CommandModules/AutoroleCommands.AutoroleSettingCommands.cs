//
//  AutoroleCommands.AutoroleSettingCommands.cs
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

using System.ComponentModel;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Feedback.Errors;
using DIGOS.Ambassador.Discord.Feedback.Results;
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
using Remora.Results;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Autorole.CommandModules
{
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
            private readonly UserFeedbackService _feedback;
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
                UserFeedbackService feedback,
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
                var getSettings = await _autoroles.GetOrCreateServerSettingsAsync(_context.GuildID.Value);
                if (!getSettings.IsSuccess)
                {
                    return Result.FromError(getSettings);
                }

                var settings = getSettings.Entity;

                var notificationChannelValue = settings.AffirmationRequiredNotificationChannelID.HasValue
                    ? $"<#{settings.AffirmationRequiredNotificationChannelID.Value}"
                    : "None";

                var embed = _feedback.CreateEmbedBase() with
                {
                    Title = "Autorole Settings",
                    Fields = new[] { new EmbedField("Confirmation Notification Channel", notificationChannelValue) }
                };

                var send = await _feedback.SendContextualEmbedAsync(embed, this.CancellationToken);
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
            public async Task<Result<UserMessage>> ClearAffirmationNotificationChannel()
            {
                var clearResult = await _autoroles.ClearAffirmationNotificationChannelAsync
                (
                    _context.GuildID.Value
                );

                return !clearResult.IsSuccess
                    ? Result<UserMessage>.FromError(clearResult)
                    : new ConfirmationMessage("Channel cleared.");
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
            public async Task<Result<UserMessage>> SetAffirmationNotificationChannel(IChannel channel)
            {
                if (channel.Type is not ChannelType.GuildText)
                {
                    return new UserError("That's not a text channel.");
                }

                var setResult = await _autoroles.SetAffirmationNotificationChannelAsync
                (
                    _context.GuildID.Value,
                    channel.ID
                );

                return !setResult.IsSuccess
                    ? Result<UserMessage>.FromError(setResult)
                    : new ConfirmationMessage("Channel set.");
            }
        }
    }
}
