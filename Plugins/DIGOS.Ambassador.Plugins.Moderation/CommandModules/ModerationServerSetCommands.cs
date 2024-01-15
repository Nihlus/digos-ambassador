//
//  ModerationServerSetCommands.cs
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
using DIGOS.Ambassador.Plugins.Moderation.Permissions;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Moderation.CommandModules;

public partial class ModerationCommands
{
    /// <summary>
    /// Server setter commands.
    /// </summary>
    [Group("server-set")]
    public class ModerationServerSetCommands : CommandGroup
    {
        private readonly ModerationService _moderation;
        private readonly ICommandContext _context;
        private readonly FeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModerationServerSetCommands"/> class.
        /// </summary>
        /// <param name="moderation">The moderation service.</param>
        /// <param name="context">The command context.</param>
        /// <param name="feedback">The feedback service.</param>
        public ModerationServerSetCommands
        (
            ModerationService moderation,
            ICommandContext context,
            FeedbackService feedback
        )
        {
            _moderation = moderation;
            _context = context;
            _feedback = feedback;
        }

        /// <summary>
        /// Sets the moderation log channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        [Command("moderation-log-channel")]
        [Description("Sets the moderation log channel.")]
        [RequirePermission(typeof(EditModerationServerSettings), PermissionTarget.Self)]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<FeedbackMessage>> SetModerationLogChannelAsync(IChannel channel)
        {
            if (!_context.TryGetGuildID(out var guildID))
            {
                throw new InvalidOperationException();
            }

            var setChannel = await _moderation.SetModerationLogChannelAsync(guildID, channel.ID);
            return setChannel.IsSuccess
                ? new FeedbackMessage("Channel set.", _feedback.Theme.Secondary)
                : Result<FeedbackMessage>.FromError(setChannel);
        }

        /// <summary>
        /// Sets the event monitoring channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        [Command("event-monitoring-channel")]
        [Description("Sets the event monitoring channel.")]
        [RequirePermission(typeof(EditModerationServerSettings), PermissionTarget.Self)]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<FeedbackMessage>> SetMonitoringChannelAsync(IChannel channel)
        {
            if (!_context.TryGetGuildID(out var guildID))
            {
                throw new InvalidOperationException();
            }

            var setChannel = await _moderation.SetMonitoringChannelAsync(guildID, channel.ID);
            return setChannel.IsSuccess
                ? new FeedbackMessage("Channel set.", _feedback.Theme.Secondary)
                : Result<FeedbackMessage>.FromError(setChannel);
        }

        /// <summary>
        /// Sets the warning threshold.
        /// </summary>
        /// <param name="threshold">The threshold.</param>
        [Command("warning-threshold")]
        [Description("Sets the warning threshold.")]
        [RequirePermission(typeof(EditModerationServerSettings), PermissionTarget.Self)]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<FeedbackMessage>> SetWarningThresholdAsync(int threshold)
        {
            if (!_context.TryGetGuildID(out var guildID))
            {
                throw new InvalidOperationException();
            }

            var setChannel = await _moderation.SetWarningThresholdAsync(guildID, threshold);
            return setChannel.IsSuccess
                ? new FeedbackMessage("Threshold set.", _feedback.Theme.Secondary)
                : Result<FeedbackMessage>.FromError(setChannel);
        }
    }
}
