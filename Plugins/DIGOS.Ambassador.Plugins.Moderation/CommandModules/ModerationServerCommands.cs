//
//  ModerationServerCommands.cs
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
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Moderation.CommandModules
{
    public partial class ModerationCommands
    {
        /// <summary>
        /// Server-related commands, such as viewing or editing info about a specific server.
        /// </summary>
        [PublicAPI]
        [Group("server")]
        [Summary("Server-related commands, such as viewing or editing info about a specific server.")]
        public partial class ModerationServerCommands : ModuleBase
        {
            private readonly ModerationService _moderation;

            private readonly UserFeedbackService _feedback;

            /// <summary>
            /// Initializes a new instance of the <see cref="ModerationServerCommands"/> class.
            /// </summary>
            /// <param name="moderation">The moderation service.</param>
            /// <param name="feedback">The feedback service.</param>
            public ModerationServerCommands
            (
                ModerationService moderation,
                UserFeedbackService feedback
            )
            {
                _moderation = moderation;
                _feedback = feedback;
            }

            /// <summary>
            /// Shows the server's moderation settings.
            /// </summary>
            [Command("settings")]
            [Summary("Shows the server's moderation settings.")]
            [RequireContext(ContextType.Guild)]
            public async Task ShowServerSettingsAsync()
            {
                var guild = this.Context.Guild;

                var getSettings = await _moderation.GetOrCreateServerSettingsAsync(guild);
                if (!getSettings.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getSettings.ErrorReason);
                    return;
                }

                var settings = getSettings.Entity;

                var eb = _feedback.CreateEmbedBase();
                eb.WithTitle(guild.Name);
                eb.WithThumbnailUrl(guild.IconUrl);

                var moderationLogChannelName = settings.ModerationLogChannel.HasValue
                    ? MentionUtils.MentionChannel((ulong)settings.ModerationLogChannel)
                    : "None";

                eb.AddField("Moderation Log Channel", moderationLogChannelName);

                var monitoringChannelName = settings.MonitoringChannel.HasValue
                    ? MentionUtils.MentionChannel((ulong)settings.MonitoringChannel)
                    : "None";

                eb.AddField("Event Monitor Channel", monitoringChannelName);

                eb.AddField("Warning Threshold", settings.WarningThreshold);

                await _feedback.SendEmbedAsync(this.Context.Channel, eb.Build());
            }
        }
    }
}
