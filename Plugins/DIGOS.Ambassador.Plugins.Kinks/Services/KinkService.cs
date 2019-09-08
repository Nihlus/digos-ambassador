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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Kinks.Extensions;
using DIGOS.Ambassador.Plugins.Kinks.Model;
using Discord;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MoreLinq.Extensions;

namespace DIGOS.Ambassador.Plugins.Kinks.Services
{
    /// <summary>
    /// Service class for user kinks.
    /// </summary>
    [PublicAPI]
    public sealed class KinkService
    {
        [NotNull]
        private readonly KinksDatabaseContext _database;

        [NotNull]
        private readonly UserService _users;

        [NotNull]
        private readonly UserFeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinkService"/> class.
        /// </summary>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="users">The user service.</param>
        /// <param name="database">The database.</param>
        public KinkService
        (
            [NotNull] UserFeedbackService feedback,
            [NotNull] UserService users,
            [NotNull] KinksDatabaseContext database
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
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<Kink>> GetKinkByNameAsync(string name)
        {
            return await _database.Kinks.SelectFromBestLevenshteinMatchAsync(x => x, k => k.Name, name);
        }

        /// <summary>
        /// Builds an informational embed for the given kink.
        /// </summary>
        /// <param name="kink">The kink.</param>
        /// <returns>An embed.</returns>
        [Pure, NotNull]
        public Embed BuildKinkInfoEmbed([NotNull] Kink kink)
        {
            var eb = _feedback.CreateEmbedBase();

            eb.WithTitle(kink.Name.Transform(To.TitleCase));
            eb.WithDescription(kink.Description);

            return eb.Build();
        }

        /// <summary>
        /// Gets a user's kink preference by the kink name.
        /// </summary>
        /// <param name="discordUser">The user.</param>
        /// <param name="name">The kink name.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<UserKink>> GetUserKinkByNameAsync
        (
            [NotNull] IUser discordUser,
            [NotNull] string name
        )
        {
            var getUserKinksResult = await GetUserKinksAsync(discordUser);
            if (!getUserKinksResult.IsSuccess)
            {
                return RetrieveEntityResult<UserKink>.FromError(getUserKinksResult);
            }

            var userKinks = getUserKinksResult.Entity;

            return userKinks.SelectFromBestLevenshteinMatch(x => x, k => k.Kink.Name, name);
        }

        /// <summary>
        /// Builds an embed displaying the user's current preference for a given kink.
        /// </summary>
        /// <param name="userKink">The kink.</param>
        /// <returns>An embed.</returns>
        [Pure, NotNull]
        public EmbedBuilder BuildUserKinkInfoEmbedBase([NotNull] UserKink userKink)
        {
            var eb = _feedback.CreateEmbedBase();

            eb.AddField(userKink.Kink.Name.Transform(To.TitleCase), userKink.Kink.Description);
            eb.AddField("Current preference", userKink.Preference.Humanize());

            return eb;
        }

        /// <summary>
        /// Gets the given user's kink preferences.
        /// </summary>
        /// <param name="discordUser">The user.</param>
        /// <returns>The user's kinks.</returns>
        [Pure, NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<IQueryable<UserKink>>> GetUserKinksAsync([NotNull] IUser discordUser)
        {
            var getUserResult = await _users.GetOrRegisterUserAsync(discordUser);
            if (!getUserResult.IsSuccess)
            {
                return RetrieveEntityResult<IQueryable<UserKink>>.FromError(getUserResult);
            }

            var user = getUserResult.Entity;
            var userKinks = _database.UserKinks.Where(k => k.User == user);

            return RetrieveEntityResult<IQueryable<UserKink>>.FromSuccess(userKinks);
        }

        /// <summary>
        /// Builds a paginated embed displaying the given overlapping kinks.
        /// </summary>
        /// <param name="firstUser">The first user.</param>
        /// <param name="secondUser">The second user.</param>
        /// <param name="kinks">The kinks.</param>
        /// <returns>A paginated embed.</returns>
        [Pure, NotNull, ItemNotNull]
        public IEnumerable<EmbedBuilder> BuildKinkOverlapEmbeds
        (
            [NotNull] IUser firstUser,
            [NotNull] IUser secondUser,
            [NotNull, ItemNotNull] IEnumerable<UserKink> kinks
        )
        {
            var pages =
            (
                from batch in kinks.Batch(3)
                from kink in batch
                select BuildUserKinkInfoEmbedBase(kink).WithTitle
                (
                    $"Matching kinks between {firstUser.Mention} and {secondUser.Mention}"
                )
            )
            .ToList();

            return pages;
        }

        /// <summary>
        /// Builds a paginated embed displaying the given kinks.
        /// </summary>
        /// <param name="kinks">The kinks.</param>
        /// <returns>A paginated embed.</returns>
        [Pure, NotNull, ItemNotNull]
        public IEnumerable<EmbedBuilder> BuildPaginatedUserKinkEmbeds
        (
            [NotNull, ItemNotNull] IEnumerable<UserKink> kinks
        )
        {
            var pages =
            (
                from batch in kinks.Batch(3)
                from kink in batch
                select BuildUserKinkInfoEmbedBase(kink)
            )
            .ToList();

            return pages;
        }

        /// <summary>
        /// Sets the user's preference for the given kink.
        /// </summary>
        /// <param name="userKink">The user's kink.</param>
        /// <param name="preference">The new preference.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> SetKinkPreferenceAsync
        (
            [NotNull] UserKink userKink,
            KinkPreference preference
        )
        {
            userKink.Preference = preference;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets a user's kink preferences by the F-List kink ID.
        /// </summary>
        /// <param name="discordUser">The discord user.</param>
        /// <param name="onlineKinkID">The F-List kink ID.</param>
        /// <returns>The user's kink preference.</returns>
        [Pure, NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<UserKink>> GetUserKinkByFListIDAsync
        (
            [NotNull] IUser discordUser,
            int onlineKinkID
        )
        {
            var getKinkResult = await GetKinkByFListIDAsync(onlineKinkID);
            if (!getKinkResult.IsSuccess)
            {
                return RetrieveEntityResult<UserKink>.FromError(getKinkResult);
            }

            var getUserKinksResult = await GetUserKinksAsync(discordUser);
            if (!getUserKinksResult.IsSuccess)
            {
                return RetrieveEntityResult<UserKink>.FromError(getUserKinksResult);
            }

            var userKinks = getUserKinksResult.Entity;
            var userKink = await userKinks.FirstOrDefaultAsync(k => k.Kink.FListID == onlineKinkID);

            if (!(userKink is null))
            {
                return RetrieveEntityResult<UserKink>.FromSuccess(userKink);
            }

            var kink = getKinkResult.Entity;
            var addKinkResult = await AddUserKinkAsync(discordUser, kink);
            if (!addKinkResult.IsSuccess)
            {
                return RetrieveEntityResult<UserKink>.FromError(addKinkResult);
            }

            return RetrieveEntityResult<UserKink>.FromSuccess(addKinkResult.Entity);
        }

        /// <summary>
        /// Adds a kink to a user's preference list.
        /// </summary>
        /// <param name="discordUser">The user.</param>
        /// <param name="kink">The kink.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<CreateEntityResult<UserKink>> AddUserKinkAsync
        (
            [NotNull] IUser discordUser,
            [NotNull] Kink kink
        )
        {
            var getUserKinksResult = await GetUserKinksAsync(discordUser);
            if (!getUserKinksResult.IsSuccess)
            {
                return CreateEntityResult<UserKink>.FromError(getUserKinksResult);
            }

            var userKinks = getUserKinksResult.Entity;

            if (userKinks.Any(k => k.Kink.FListID == kink.FListID))
            {
                return CreateEntityResult<UserKink>.FromError("The user already has a preference for that kink.");
            }

            var getUserResult = await _users.GetOrRegisterUserAsync(discordUser);
            if (!getUserResult.IsSuccess)
            {
                return CreateEntityResult<UserKink>.FromError(getUserResult);
            }

            var user = getUserResult.Entity;

            var userKink = new UserKink(user, kink);

            _database.UserKinks.Update(userKink);

            await _database.SaveChangesAsync();
            return CreateEntityResult<UserKink>.FromSuccess(userKink);
        }

        /// <summary>
        /// Gets all available kink categories from the database.
        /// </summary>
        /// <returns>A list of kink categories.</returns>
        [Pure, NotNull, ItemNotNull]
        public Task<IQueryable<KinkCategory>> GetKinkCategoriesAsync()
        {
            return Task.FromResult(_database.Kinks.Select(k => k.Category).OrderBy(k => k.ToString()).Distinct());
        }

        /// <summary>
        /// Gets a kink by its F-list ID.
        /// </summary>
        /// <param name="onlineKinkID">The F-List kink ID.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<Kink>> GetKinkByFListIDAsync(int onlineKinkID)
        {
            var kink = await _database.Kinks.FirstOrDefaultAsync(k => k.FListID == onlineKinkID);
            if (kink is null)
            {
                return RetrieveEntityResult<Kink>.FromError("No kink with that ID found.");
            }

            return RetrieveEntityResult<Kink>.FromSuccess(kink);
        }

        /// <summary>
        /// Gets a list of all kinks in a given category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<IEnumerable<Kink>>> GetKinksByCategoryAsync(KinkCategory category)
        {
            var group = await _database.Kinks.GroupBy(k => k.Category).FirstOrDefaultAsync(g => g.Key == category);
            if (group is null)
            {
                return RetrieveEntityResult<IEnumerable<Kink>>.FromError
                (
                    "There are no kinks in that category."
                );
            }

            return RetrieveEntityResult<IEnumerable<Kink>>.FromSuccess(group.OrderBy(g => g.Category.ToString()));
        }

        /// <summary>
        /// Gets a list of all a user's kinks in a given category.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="category">The category.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<IEnumerable<UserKink>>> GetUserKinksByCategoryAsync
        (
            [NotNull] IUser user,
            KinkCategory category
        )
        {
            var getUserKinksResult = await GetUserKinksAsync(user);
            if (!getUserKinksResult.IsSuccess)
            {
                return RetrieveEntityResult<IEnumerable<UserKink>>.FromError(getUserKinksResult);
            }

            var userKinks = getUserKinksResult.Entity;

            var groups = userKinks.GroupBy(k => k.Kink.Category).ToList();
            var first = groups.FirstOrDefault(g => g.Key == category);

            if (first is null)
            {
                return RetrieveEntityResult<IEnumerable<UserKink>>.FromSuccess(new UserKink[] { });
            }

            return RetrieveEntityResult<IEnumerable<UserKink>>.FromSuccess(first.OrderBy(uk => uk.Kink.ToString()));
        }

        /// <summary>
        /// Resets the user's kink preferences.
        /// </summary>
        /// <param name="discordUser">The user.</param>
        /// <returns>A task that must be awaited.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> ResetUserKinksAsync([NotNull] IUser discordUser)
        {
            var getUserResult = await _users.GetOrRegisterUserAsync(discordUser);
            if (!getUserResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getUserResult);
            }

            var user = getUserResult.Entity;

            _database.UserKinks.RemoveRange(_database.UserKinks.Where(k => k.User == user));

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets the first kink that the given uses does not have a set preference for in the given category.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="category">The category.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<Kink>> GetFirstKinkWithoutPreferenceInCategoryAsync
        (
            [NotNull] IUser user,
            KinkCategory category
        )
        {
            var getKinksResult = await GetKinksByCategoryAsync(category);
            if (!getKinksResult.IsSuccess)
            {
                return RetrieveEntityResult<Kink>.FromError(getKinksResult);
            }

            var kinks = getKinksResult.Entity;

            var getKinksByCategoryResult = await GetUserKinksByCategoryAsync(user, category);
            if (!getKinksByCategoryResult.IsSuccess)
            {
                return RetrieveEntityResult<Kink>.FromError(getKinksByCategoryResult);
            }

            var userKinks = getKinksByCategoryResult.Entity.ToList();

            // Find the first kink that the user either has in their list with no preference, or does not exist
            // in their list
            var kinkWithoutPreference = kinks.FirstOrDefault
            (
                k =>
                    userKinks.Any
                    (
                        uk =>
                            k.FListID == uk.Kink.FListID && uk.Preference == KinkPreference.NoPreference
                    ) ||
                    userKinks.All
                    (
                        uk =>
                            k.FListID != uk.Kink.FListID
                    )
            );

            if (kinkWithoutPreference is null)
            {
                return RetrieveEntityResult<Kink>.FromError("No kink without a set preference found.");
            }

            return RetrieveEntityResult<Kink>.FromSuccess(kinkWithoutPreference);
        }

        /// <summary>
        /// Gets the first kink in the given category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<Kink>> GetFirstKinkInCategoryAsync(KinkCategory category)
        {
            var getKinksResult = await GetKinksByCategoryAsync(category);
            if (!getKinksResult.IsSuccess)
            {
                return RetrieveEntityResult<Kink>.FromError(getKinksResult);
            }

            return RetrieveEntityResult<Kink>.FromSuccess(getKinksResult.Entity.First());
        }

        /// <summary>
        /// Gets the next kink in its category by its predecessor's F-List ID.
        /// </summary>
        /// <param name="precedingFListID">The F-List ID of the preceding kink.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<Kink>> GetNextKinkByCurrentFListIDAsync(int precedingFListID)
        {
            var getKinkResult = await GetKinkByFListIDAsync(precedingFListID);
            if (!getKinkResult.IsSuccess)
            {
                return getKinkResult;
            }

            var currentKink = getKinkResult.Entity;
            var getKinksResult = await GetKinksByCategoryAsync(currentKink.Category);
            if (!getKinksResult.IsSuccess)
            {
                return RetrieveEntityResult<Kink>.FromError(getKinksResult);
            }

            var group = getKinksResult.Entity;
            var nextKink = group.SkipUntil(k => k.FListID == precedingFListID).FirstOrDefault();

            if (nextKink is null)
            {
                return RetrieveEntityResult<Kink>.FromError("The current kink was the last one in the category.");
            }

            return RetrieveEntityResult<Kink>.FromSuccess(nextKink);
        }

        /// <summary>
        /// Updates the kink database, adding in new entries. Duplicates are not added.
        /// </summary>
        /// <param name="newKinks">The new kinks.</param>
        /// <returns>The number of updated kinks.</returns>
        [NotNull]
        public async Task<int> UpdateKinksAsync([NotNull, ItemNotNull] IEnumerable<Kink> newKinks)
        {
            foreach (var kink in newKinks)
            {
                if (!await _database.Kinks.AnyAsync(k => k.FListID == kink.FListID))
                {
                    await _database.Kinks.AddAsync(kink);
                }
            }

            return await _database.SaveChangesAsync();
        }
    }
}
