//
//  StatCommands.cs
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

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Pagination.Extensions;
using Remora.Rest.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Statistics.CommandGroups;

/// <summary>
/// Various statistics-related commands.
/// </summary>
[Group("stats")]
[Description("Various statistics-related commands.")]
public class StatCommands : CommandGroup
{
    private readonly FeedbackService _feedback;
    private readonly ICommandContext _context;
    private readonly IDiscordRestGuildAPI _guildAPI;
    private readonly IDiscordRestUserAPI _userAPI;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatCommands"/> class.
    /// </summary>
    /// <param name="feedback">The feedback service.</param>
    /// <param name="context">The command context.</param>
    /// <param name="guildAPI">The guild API.</param>
    /// <param name="userAPI">The user API.</param>
    public StatCommands
    (
        FeedbackService feedback,
        ICommandContext context,
        IDiscordRestGuildAPI guildAPI,
        IDiscordRestUserAPI userAPI
    )
    {
        _feedback = feedback;
        _context = context;
        _guildAPI = guildAPI;
        _userAPI = userAPI;
    }

    /// <summary>
    /// Displays statistics about the current guild.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [UsedImplicitly]
    [Command("guild")]
    [Description("Displays statistics about the current guild.")]
    [RequireContext(ChannelContext.Guild)]
    public async Task<IResult> ShowServerStatsAsync()
    {
        var getGuild = await _guildAPI.GetGuildAsync(_context.GuildID.Value, ct: this.CancellationToken);
        if (!getGuild.IsSuccess)
        {
            return getGuild;
        }

        var guild = getGuild.Entity;

        var eb = CreateGuildInfoEmbed(guild);
        return await _feedback.SendContextualEmbedAsync(eb, ct: this.CancellationToken);
    }

    /// <summary>
    /// Displays statistics about all guilds the bot has joined.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [UsedImplicitly]
    [Command("guilds")]
    [Description("Displays statistics about all guilds the bot has joined.")]
    [RequireContext(ChannelContext.DM)]
    [RequireOwner]
    public async Task<IResult> ShowServersStatsAsync()
    {
        var pages = new List<Embed>();
        await foreach (var getGuild in GetGuildsAsync(this.CancellationToken))
        {
            if (!getGuild.IsSuccess)
            {
                return getGuild;
            }

            pages.Add(CreateGuildInfoEmbed(getGuild.Entity));
        }

        return (Result)await _feedback.SendContextualPaginatedMessageAsync
        (
            _context.User.ID,
            pages,
            ct: this.CancellationToken
        );
    }

    private async IAsyncEnumerable<Result<IGuild>> GetGuildsAsync
    (
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        Optional<Snowflake> after = default;
        while (true)
        {
            if (ct.IsCancellationRequested)
            {
                yield break;
            }

            var getGuilds = await _userAPI.GetCurrentUserGuildsAsync(after: after, ct: ct);
            if (!getGuilds.IsSuccess)
            {
                yield break;
            }

            var retrievedGuilds = getGuilds.Entity;
            if (retrievedGuilds.Count == 0)
            {
                break;
            }

            foreach (var retrievedGuild in retrievedGuilds)
            {
                if (!retrievedGuild.ID.HasValue)
                {
                    continue;
                }

                yield return await _guildAPI.GetGuildAsync(retrievedGuild.ID.Value, ct: ct);
            }

            after = getGuilds.Entity[^1].ID;
        }
    }

    /// <summary>
    /// Creates an embed with information about a guild.
    /// </summary>
    /// <param name="guild">The guild.</param>
    /// <returns>The embed.</returns>
    private static Embed CreateGuildInfoEmbed(IGuild guild)
    {
        var eb = new Embed();

        var getGuildSplash = CDN.GetGuildSplashUrl(guild);
        if (getGuildSplash.IsSuccess)
        {
            eb = eb with
            {
                Thumbnail = new EmbedThumbnail(getGuildSplash.Entity.ToString())
            };
        }
        else
        {
            var getGuildIcon = CDN.GetGuildIconUrl(guild);
            if (getGuildIcon.IsSuccess)
            {
                eb = eb with
                {
                    Thumbnail = new EmbedThumbnail(getGuildIcon.Entity.ToString())
                };
            }
        }

        var getGuildAuthorIcon = CDN.GetGuildIconUrl(guild);
        var author = new EmbedAuthor(guild.Name)
        {
            IconUrl = getGuildAuthorIcon.IsSuccess
                ? getGuildAuthorIcon.Entity.ToString()
                : default(Optional<string>)
        };

        eb = eb with
        {
            Author = author
        };

        var fields = new List<EmbedField>
        {
            new("Owner", $"<@{guild.OwnerID}>")
        };

        if (guild.ApproximateMemberCount.HasValue)
        {
            fields.Add(new EmbedField("Members", $"~{guild.ApproximateMemberCount.Value}"));
        }

        fields.Add(new EmbedField("Created at", guild.ID.Timestamp.ToString()));

        return eb with { Fields = fields };
    }
}
