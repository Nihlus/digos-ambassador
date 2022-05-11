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

using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Moderation.CommandModules;

public partial class ModerationCommands
{
    /// <summary>
    /// Server-related commands, such as viewing or editing info about a specific server.
    /// </summary>
    [Group("server")]
    [Description("Server-related commands, such as viewing or editing info about a specific server.")]
    public class ModerationServerCommands : CommandGroup
    {
        private readonly ModerationService _moderation;
        private readonly FeedbackService _feedback;
        private readonly ICommandContext _context;
        private readonly IDiscordRestGuildAPI _guildAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModerationServerCommands"/> class.
        /// </summary>
        /// <param name="moderation">The moderation service.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="context">The command context.</param>
        /// <param name="guildAPI">The guild API.</param>
        public ModerationServerCommands
        (
            ModerationService moderation,
            FeedbackService feedback,
            ICommandContext context,
            IDiscordRestGuildAPI guildAPI
        )
        {
            _moderation = moderation;
            _feedback = feedback;
            _context = context;
            _guildAPI = guildAPI;
        }

        /// <summary>
        /// Shows the server's moderation settings.
        /// </summary>
        [Command("settings")]
        [Description("Shows the server's moderation settings.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<IResult> ShowServerSettingsAsync()
        {
            var getGuild = await _guildAPI.GetGuildAsync(_context.GuildID.Value);
            if (!getGuild.IsSuccess)
            {
                return getGuild;
            }

            var guild = getGuild.Entity;

            var getSettings = await _moderation.GetOrCreateServerSettingsAsync(guild.ID);
            if (!getSettings.IsSuccess)
            {
                return getSettings;
            }

            var settings = getSettings.Entity;

            var getGuildIcon = CDN.GetGuildIconUrl(guild);

            var embedFields = new List<EmbedField>();
            var eb = new Embed
            {
                Colour = _feedback.Theme.Secondary,
                Title = guild.Name,
                Thumbnail = getGuildIcon.IsSuccess
                    ? new EmbedThumbnail(getGuildIcon.Entity.ToString())
                    : default(Optional<IEmbedThumbnail>),
                Fields = embedFields
            };

            var moderationLogChannelName = settings.ModerationLogChannel.HasValue
                ? $"<#{settings.ModerationLogChannel}>"
                : "None";

            embedFields.Add(new EmbedField("Moderation Log Channel", moderationLogChannelName));

            var monitoringChannelName = settings.MonitoringChannel.HasValue
                ? $"<#{settings.MonitoringChannel}>"
                : "None";

            embedFields.Add(new EmbedField("Event Monitor Channel", monitoringChannelName));

            embedFields.Add(new EmbedField("Warning Threshold", settings.WarningThreshold.ToString()));

            return await _feedback.SendContextualEmbedAsync(eb);
        }
    }
}
