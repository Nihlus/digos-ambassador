//
//  ModerationServerSetCommands.cs
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
using DIGOS.Ambassador.Discord.Extensions.Results;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Moderation.Permissions;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using DIGOS.Ambassador.Plugins.Permissions.Preconditions;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Moderation.CommandModules
{
    public partial class ModerationCommands
    {
        public partial class ModerationServerCommands
        {
            /// <summary>
            /// Server setter commands.
            /// </summary>
            [PublicAPI]
            [Group("set")]
            public class ModerationServerSetCommands : ModuleBase
            {
                private readonly ModerationService _moderation;

                private readonly UserFeedbackService _feedback;

                /// <summary>
                /// Initializes a new instance of the <see cref="ModerationServerSetCommands"/> class.
                /// </summary>
                /// <param name="moderation">The moderation service.</param>
                /// <param name="feedback">The feedback service.</param>
                public ModerationServerSetCommands
                (
                    ModerationService moderation,
                    UserFeedbackService feedback
                )
                {
                    _moderation = moderation;
                    _feedback = feedback;
                }

                /// <summary>
                /// Sets the moderation log channel.
                /// </summary>
                /// <param name="channel">The channel.</param>
                [Command("moderation-log-channel")]
                [Summary("Sets the moderation log channel.")]
                [RequirePermission(typeof(EditModerationServerSettings), PermissionTarget.Self)]
                [RequireContext(ContextType.Guild)]
                public async Task<RuntimeResult> SetModerationLogChannelAsync(ITextChannel channel)
                {
                    var setChannel = await _moderation.SetModerationLogChannelAsync(this.Context.Guild, channel);
                    if (!setChannel.IsSuccess)
                    {
                        return setChannel.ToRuntimeResult();
                    }

                    return RuntimeCommandResult.FromSuccess("Channel set.");
                }

                /// <summary>
                /// Sets the event monitoring channel.
                /// </summary>
                /// <param name="channel">The channel.</param>
                [Command("event-monitoring-channel")]
                [Summary("Sets the event monitoring channel.")]
                [RequirePermission(typeof(EditModerationServerSettings), PermissionTarget.Self)]
                [RequireContext(ContextType.Guild)]
                public async Task<RuntimeResult> SetMonitoringChannelAsync(ITextChannel channel)
                {
                    var setChannel = await _moderation.SetMonitoringChannelAsync(this.Context.Guild, channel);
                    if (!setChannel.IsSuccess)
                    {
                        return setChannel.ToRuntimeResult();
                    }

                    return RuntimeCommandResult.FromSuccess("Channel set.");
                }

                /// <summary>
                /// Sets the warning threshold.
                /// </summary>
                /// <param name="threshold">The threshold.</param>
                [Command("warning-threshold")]
                [Summary("Sets the warning threshold.")]
                [RequirePermission(typeof(EditModerationServerSettings), PermissionTarget.Self)]
                [RequireContext(ContextType.Guild)]
                public async Task<RuntimeResult> SetWarningThresholdAsync(int threshold)
                {
                    var setChannel = await _moderation.SetWarningThresholdAsync(this.Context.Guild, threshold);
                    if (!setChannel.IsSuccess)
                    {
                        return setChannel.ToRuntimeResult();
                    }

                    return RuntimeCommandResult.FromSuccess("Threshold set.");
                }
            }
        }
    }
}
