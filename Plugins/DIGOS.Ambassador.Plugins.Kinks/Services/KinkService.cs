//
//  KinkService.cs
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Core.Database.Interfaces;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Kinks.Extensions;
using DIGOS.Ambassador.Plugins.Kinks.Model;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MoreLinq.Extensions;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Kinks.Services;

/// <summary>
/// Service class for user kinks.
/// </summary>
[PublicAPI]
public sealed class KinkService : IQueryService<UserKink>
{
    private readonly KinksDatabaseContext _database;
    private readonly UserService _users;
    private readonly FeedbackService _feedback;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinkService"/> class.
    /// </summary>
    /// <param name="feedback">The feedback service.</param>
    /// <param name="users">The user service.</param>
    /// <param name="database">The database.</param>
    public KinkService
    (
        FeedbackService feedback,
        UserService users,
        KinksDatabaseContext database
    )
    {
        _feedback = feedback;
        _users = users;
        _database = database;
    }

    /// <summary>
    /// Gets a kink by its name.
    /// </summary>
    /// <param name="name">The name of the kink.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    [Pure]
    public async Task<Result<Kink>> GetKinkByNameAsync(string name, CancellationToken ct = default)
    {
        name = name.Trim();

        return await _database.Kinks.SelectFromBestLevenshteinMatchAsync
        (
            x => x,
            k => k.Name,
            name,
            ct: ct
        );
    }

    /// <summary>
    /// Builds an informational embed for the given kink.
    /// </summary>
    /// <param name="kink">The kink.</param>
    /// <returns>An embed.</returns>
    [Pure]
    public Embed BuildKinkInfoEmbed(Kink kink)
    {
        return new Embed
        {
            Colour = _feedback.Theme.Secondary,
            Title = kink.Name.Transform(To.TitleCase),
            Description = kink.Description
        };
    }

    /// <summary>
    /// Gets a user's kink preference by the kink name.
    /// </summary>
    /// <param name="discordUser">The user.</param>
    /// <param name="name">The kink name.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    [Pure]
    public async Task<Result<UserKink>> GetUserKinkByNameAsync
    (
        Snowflake discordUser,
        string name,
        CancellationToken ct = default
    )
    {
        name = name.Trim();

        var userKinks = await QueryDatabaseAsync
        (
            q => q
                .Where(k => k.User.DiscordID == discordUser),
            ct
        );

        return userKinks.SelectFromBestLevenshteinMatch(x => x, k => k.Kink.Name, name);
    }

    /// <summary>
    /// Builds an embed displaying the user's current preference for a given kink.
    /// </summary>
    /// <param name="userKink">The kink.</param>
    /// <returns>An embed.</returns>
    [Pure]
    public Embed BuildUserKinkInfoEmbedBase(UserKink userKink)
    {
        return new Embed
        {
            Colour = _feedback.Theme.Secondary,
            Fields = new[]
            {
                new EmbedField(userKink.Kink.Name.Transform(To.TitleCase), userKink.Kink.Description),
                new EmbedField("Current preference", userKink.Preference.Humanize())
            }
        };
    }

    /// <summary>
    /// Builds a paginated embed displaying the given overlapping kinks.
    /// </summary>
    /// <param name="firstUser">The first user.</param>
    /// <param name="secondUser">The second user.</param>
    /// <param name="kinks">The kinks.</param>
    /// <returns>A paginated embed.</returns>
    [Pure]
    public List<Embed> BuildKinkOverlapEmbeds
    (
        Snowflake firstUser,
        Snowflake secondUser,
        IEnumerable<UserKink> kinks
    )
    {
        return
            (
                from batch in kinks.Batch(3)
                from kink in batch
                select BuildUserKinkInfoEmbedBase(kink) with
                {
                    Title = $"Matching kinks between <@{firstUser}> and <@{secondUser}>"
                }
            )
            .ToList();
    }

    /// <summary>
    /// Builds a paginated embed displaying the given kinks.
    /// </summary>
    /// <param name="kinks">The kinks.</param>
    /// <returns>A paginated embed.</returns>
    [Pure]
    public List<Embed> BuildPaginatedUserKinkEmbeds(IEnumerable<UserKink> kinks)
    {
        return
            (
                from batch in kinks.Batch(3)
                from kink in batch
                select BuildUserKinkInfoEmbedBase(kink)
            )
            .ToList();
    }

    /// <summary>
    /// Sets the user's preference for the given kink.
    /// </summary>
    /// <param name="userKink">The user's kink.</param>
    /// <param name="preference">The new preference.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetKinkPreferenceAsync
    (
        UserKink userKink,
        KinkPreference preference,
        CancellationToken ct = default
    )
    {
        userKink.Preference = preference;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <summary>
    /// Gets a user's kink preferences by the F-List kink ID.
    /// </summary>
    /// <param name="discordUser">The discord user.</param>
    /// <param name="onlineKinkID">The F-List kink ID.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>The user's kink preference.</returns>
    [Pure]
    public async Task<Result<UserKink>> GetUserKinkByFListIDAsync
    (
        Snowflake discordUser,
        long onlineKinkID,
        CancellationToken ct = default
    )
    {
        var userKink = await QueryDatabaseAsync
        (
            q => q
                .Where(k => k.User.DiscordID == discordUser)
                .Where(k => k.Kink.FListID == onlineKinkID)
                .SingleOrDefaultAsync(ct)
        );

        if (userKink is not null)
        {
            return Result<UserKink>.FromSuccess(userKink);
        }

        var getKink = await GetKinkByFListIDAsync(onlineKinkID, ct);
        if (!getKink.IsSuccess)
        {
            return Result<UserKink>.FromError(getKink);
        }

        var kink = getKink.Entity;
        var addKinkResult = await AddUserKinkAsync(discordUser, kink, ct);

        return addKinkResult.IsSuccess
            ? Result<UserKink>.FromSuccess(addKinkResult.Entity)
            : Result<UserKink>.FromError(addKinkResult);
    }

    /// <summary>
    /// Adds a kink to a user's preference list.
    /// </summary>
    /// <param name="discordUser">The user.</param>
    /// <param name="kink">The kink.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A creation result which may or may not have succeeded.</returns>
    public async Task<Result<UserKink>> AddUserKinkAsync
    (
        Snowflake discordUser,
        Kink kink,
        CancellationToken ct = default
    )
    {
        var hasKink = await QueryDatabaseAsync
        (
            q => q
                .Where(k => k.User.DiscordID == discordUser)
                .Where(k => k.Kink == kink)
                .AnyAsync(ct)
        );

        if (hasKink)
        {
            return new UserError("The user already has a preference for that kink.");
        }

        var getUserResult = await _users.GetOrRegisterUserAsync(discordUser, ct);
        if (!getUserResult.IsSuccess)
        {
            return Result<UserKink>.FromError(getUserResult);
        }

        var user = getUserResult.Entity;

        var userKink = _database.CreateProxy<UserKink>(user, kink);
        _database.UserKinks.Update(userKink);

        await _database.SaveChangesAsync(ct);

        return userKink;
    }

    /// <summary>
    /// Gets all available kink categories from the database.
    /// </summary>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A list of kink categories.</returns>
    [Pure]
    public async Task<IEnumerable<KinkCategory>> GetKinkCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await _database.Kinks.ServersideQueryAsync
        (
            q => q
                .Select(k => k.Category)
                .Distinct(),
            ct
        );

        return categories.OrderBy(k => k.ToString());
    }

    /// <summary>
    /// Gets a kink by its F-list ID.
    /// </summary>
    /// <param name="onlineKinkID">The F-List kink ID.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    [Pure]
    public async Task<Result<Kink>> GetKinkByFListIDAsync
    (
        long onlineKinkID,
        CancellationToken ct = default
    )
    {
        var kink = await _database.Kinks.ServersideQueryAsync
        (
            q => q
                .Where(k => k.FListID == onlineKinkID)
                .SingleOrDefaultAsync(ct)
        );

        if (kink is not null)
        {
            return kink;
        }

        return new UserError("No kink with that ID found.");
    }

    /// <summary>
    /// Gets a list of all kinks in a given category.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    [Pure]
    public async Task<Result<IReadOnlyList<Kink>>> GetKinksByCategoryAsync
    (
        KinkCategory category,
        CancellationToken ct = default
    )
    {
        var kinks = await _database.Kinks.ServersideQueryAsync
        (
            q => q
                .Where(k => k.Category == category),
            ct
        );

        if (!kinks.Any())
        {
            return new UserError
            (
                "There are no kinks in that category."
            );
        }

        return Result<IReadOnlyList<Kink>>.FromSuccess(kinks);
    }

    /// <summary>
    /// Resets the user's kink preferences.
    /// </summary>
    /// <param name="discordUser">The user.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A task that must be awaited.</returns>
    public async Task<Result> ResetUserKinksAsync
    (
        Snowflake discordUser,
        CancellationToken ct = default
    )
    {
        var getUserResult = await _users.GetOrRegisterUserAsync(discordUser, ct);
        if (!getUserResult.IsSuccess)
        {
            return Result.FromError(getUserResult);
        }

        var user = getUserResult.Entity;

        var kinksToRemove = await _database.UserKinks.ServersideQueryAsync
        (
            q => q.Where(k => k.User == user),
            ct
        );

        _database.UserKinks.RemoveRange(kinksToRemove);
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <summary>
    /// Gets the first kink that the given uses does not have a set preference for in the given category.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="category">The category.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    [Pure]
    public async Task<Result<Kink?>> GetFirstKinkWithoutPreferenceInCategoryAsync
    (
        Snowflake user,
        KinkCategory category,
        CancellationToken ct = default
    )
    {
        // First, look for something that's already registered, but doesn't have a preference
        var kinkWithoutPreference = await QueryDatabaseAsync
        (
            q => q
                .Where(k => k.User.DiscordID == user)
                .Where(k => k.Kink.Category == category)
                .FirstOrDefaultAsync(k => k.Preference == KinkPreference.NoPreference, ct)
        );

        if (kinkWithoutPreference is not null)
        {
            return kinkWithoutPreference.Kink;
        }

        // Okay, no kink registered - let's find the last one
        var lastKink = await QueryDatabaseAsync
        (
            q => q
                .Where(k => k.User.DiscordID == user)
                .Where(k => k.Kink.Category == category)
                .OrderBy(k => k.Kink.FListID)
                .LastOrDefaultAsync(ct)
        );

        var getKinksResult = await GetKinksByCategoryAsync(category, ct);
        if (!getKinksResult.IsSuccess)
        {
            return Result<Kink?>.FromError(getKinksResult);
        }

        var kinks = getKinksResult.Entity;

        // The user doesn't have any set kinks in this category; grab the first one
        // Still nothing? Just pick the first one without a preference, or return nothing
        return lastKink is null
            ? kinks[0]
            : kinks.FirstOrDefault(k => k.FListID != lastKink.Kink.FListID);
    }

    /// <summary>
    /// Gets the first kink in the given category.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    [Pure]
    public async Task<Result<Kink>> GetFirstKinkInCategoryAsync
    (
        KinkCategory category,
        CancellationToken ct = default
    )
    {
        var getKinksResult = await GetKinksByCategoryAsync(category, ct);

        return getKinksResult.IsSuccess
            ? Result<Kink>.FromSuccess(getKinksResult.Entity[0])
            : Result<Kink>.FromError(getKinksResult);
    }

    /// <summary>
    /// Gets the next kink in its category by its predecessor's F-List ID.
    /// </summary>
    /// <param name="precedingFListID">The F-List ID of the preceding kink.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>The next kink, or null if the kink is the last kink in the category.</returns>
    [Pure]
    public async Task<Result<Kink?>> GetNextKinkByCurrentFListIDAsync
    (
        long precedingFListID,
        CancellationToken ct = default
    )
    {
        var getKinkResult = await GetKinkByFListIDAsync(precedingFListID, ct);
        if (!getKinkResult.IsSuccess)
        {
            return Result<Kink?>.FromError(getKinkResult);
        }

        var currentKink = getKinkResult.Entity;
        var getKinksResult = await GetKinksByCategoryAsync(currentKink.Category, ct);
        if (!getKinksResult.IsSuccess)
        {
            return Result<Kink?>.FromError(getKinksResult);
        }

        var group = getKinksResult.Entity;
        return group.SkipUntil(k => k.FListID == precedingFListID).FirstOrDefault();
    }

    /// <summary>
    /// Updates the kink database, adding in new entries. Duplicates are not added.
    /// </summary>
    /// <param name="newKinks">The new kinks.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>The number of updated kinks.</returns>
    public async Task<int> UpdateKinksAsync
    (
        IEnumerable<Kink> newKinks,
        CancellationToken ct = default
    )
    {
        var alteredKinks = newKinks
            .Select(kink => _database.Kinks.Update(kink))
            .Count(entry => entry.State != EntityState.Unchanged);

        await _database.SaveChangesAsync(ct);

        return alteredKinks;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserKink>> QueryDatabaseAsync
    (
        Func<IQueryable<UserKink>, IQueryable<UserKink>>? query = default,
        CancellationToken ct = default
    )
    {
        query ??= q => q;
        return await _database.UserKinks.ServersideQueryAsync(query, ct);
    }

    /// <inheritdoc />
    public async Task<TOut> QueryDatabaseAsync<TOut>(Func<IQueryable<UserKink>, Task<TOut>> query)
    {
        return await _database.UserKinks.ServersideQueryAsync(query);
    }
}
