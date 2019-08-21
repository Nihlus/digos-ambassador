//
//  StatCommands.cs
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

using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

using JetBrains.Annotations;
using static Discord.Commands.ContextType;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Modules
{
    /// <summary>
    /// Various statistics-related commands.
    /// </summary>
    [UsedImplicitly]
    [Group("stats")]
    [Summary("Various statistics-related commands.")]
    public class StatCommands : ModuleBase<SocketCommandContext>
    {
        private readonly UserFeedbackService _feedback;
        private readonly InteractivityService _interactivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatCommands"/> class.
        /// </summary>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        public StatCommands(UserFeedbackService feedback, InteractivityService interactivity)
        {
            _feedback = feedback;
            _interactivity = interactivity;
        }

        /// <summary>
        /// Displays statistics about the current guild.
        /// </summary>
        [UsedImplicitly]
        [Command("guild")]
        [Alias("guild", "server")]
        [Summary("Displays statistics about the current guild.")]
        [RequireContext(Guild)]
        public async Task ShowServerStatsAsync()
        {
            var guild = this.Context.Guild;

            var eb = CreateGuildInfoEmbed(guild);

            await _feedback.SendEmbedAsync(this.Context.Channel, eb.Build());
        }

        /// <summary>
        /// Displays statistics about all guilds the bot has joined.
        /// </summary>
        [UsedImplicitly]
        [Command("guilds")]
        [Alias("guilds", "servers")]
        [Summary("Displays statistics about all guilds the bot has joined.")]
        [RequireContext(DM)]
        [RequireOwner]
        public async Task ShowServersStatsAsync()
        {
            var guilds = this.Context.Client.Guilds;
            var pages = guilds.Select(CreateGuildInfoEmbed);

            var paginatedMessage = new PaginatedEmbed(_feedback, this.Context.User).WithPages(pages);

            await _interactivity.SendPrivateInteractiveMessageAndDeleteAsync
            (
                this.Context,
                _feedback,
                paginatedMessage
            );
        }

        /// <summary>
        /// Creates an embed with information about a guild.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <returns>The embed.</returns>
        [NotNull]
        private EmbedBuilder CreateGuildInfoEmbed([NotNull] SocketGuild guild)
        {
            var eb = _feedback.CreateEmbedBase();

            if (!(guild.SplashUrl is null))
            {
                eb.WithThumbnailUrl(guild.SplashUrl);
            }
            else if (!(guild.IconUrl is null))
            {
                eb.WithThumbnailUrl(guild.IconUrl);
            }

            var authorBuilder = new EmbedAuthorBuilder().WithName(guild.Name);
            if (!(guild.IconUrl is null))
            {
                authorBuilder.WithIconUrl(guild.IconUrl);
            }

            eb.WithAuthor(authorBuilder);

            eb.AddField("Owner", guild.Owner.Mention);
            eb.AddField("Members", guild.MemberCount, true);

            eb.AddField("Created at", guild.CreatedAt);

            return eb;
        }
    }
}
