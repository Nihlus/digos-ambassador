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

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Kinks;
using DIGOS.Ambassador.Database.Users;
using DIGOS.Ambassador.Extensions;
using Discord;
using Discord.Commands;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MoreLinq;

namespace DIGOS.Ambassador.Services
{
    /// <summary>
    /// Service class for user kinks.
    /// </summary>
    public class KinkService
    {
        private readonly UserFeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinkService"/> class.
        /// </summary>
        /// <param name="feedback">The feedback service.</param>
        public KinkService(UserFeedbackService feedback)
        {
            this._feedback = feedback;
        }

        /// <summary>
        /// Gets a kink by its name.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="name">The name of the kink.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<Kink>> GetKinkByNameAsync([NotNull] GlobalInfoContext db, string name)
        {
            return await db.Kinks.SelectFromBestLevenshteinMatchAsync(x => x, k => k.Name, name);
        }

        /// <summary>
        /// Builds an informational embed for the given kink.
        /// </summary>
        /// <param name="kink">The kink.</param>
        /// <returns>An embed.</returns>
        public Embed BuildKinkInfoEmbed([NotNull] Kink kink)
        {
            var eb = this._feedback.CreateEmbedBase();

            eb.WithTitle(kink.Name.Transform(To.TitleCase));
            eb.WithDescription(kink.Description);

            return eb.Build();
        }

        /// <summary>
        /// Gets a user's kink preference by the kink name.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="discordUser">The user.</param>
        /// <param name="name">The kink name.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<UserKink>> GetUserKinkByNameAsync([NotNull] GlobalInfoContext db, [NotNull] IUser discordUser, string name)
        {
            var getUserResult = await db.GetOrRegisterUserAsync(discordUser);
            if (!getUserResult.IsSuccess)
            {
                return RetrieveEntityResult<UserKink>.FromError(getUserResult);
            }

            var user = getUserResult.Entity;

            return user.Kinks.SelectFromBestLevenshteinMatch(x => x, k => k.Kink.Name, name);
        }

        /// <summary>
        /// Builds an embed displaying the user's current preference for a given kink.
        /// </summary>
        /// <param name="userKink">The kink.</param>
        /// <returns>An embed.</returns>
        [NotNull]
        public EmbedBuilder BuildUserKinkInfoEmbedBase([NotNull] UserKink userKink)
        {
            var eb = this._feedback.CreateEmbedBase();

            eb.AddField(userKink.Kink.Name.Transform(To.TitleCase), userKink.Kink.Description);
            eb.AddField("Current preference", userKink.Preference.Humanize());

            return eb;
        }

        /// <summary>
        /// Gets the given user's kink preferences.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="discordUser">The user.</param>
        /// <returns>The user's kinks.</returns>
        public async Task<RetrieveEntityResult<IEnumerable<UserKink>>> GetUserKinksAsync([NotNull] GlobalInfoContext db, [NotNull] IUser discordUser)
        {
            var getUserResult = await db.GetOrRegisterUserAsync(discordUser);
            if (!getUserResult.IsSuccess)
            {
                return RetrieveEntityResult<IEnumerable<UserKink>>.FromError(getUserResult);
            }

            var user = getUserResult.Entity;
            return RetrieveEntityResult<IEnumerable<UserKink>>.FromSuccess(user.Kinks);
        }

        /// <summary>
        /// Builds a paginated embed displaying the given overlapping kinks.
        /// </summary>
        /// <param name="firstUser">The first user.</param>
        /// <param name="secondUser">The second user.</param>
        /// <param name="kinks">The kinks.</param>
        /// <returns>A paginated embed.</returns>
        [NotNull]
        public IEnumerable<EmbedBuilder> BuildKinkOverlapEmbeds(IUser firstUser, IUser secondUser, IEnumerable<UserKink> kinks)
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
        [NotNull]
        public IEnumerable<EmbedBuilder> BuildPaginatedUserKinkEmbeds(IEnumerable<UserKink> kinks)
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
        /// <param name="db">The database.</param>
        /// <param name="userKink">The user's kink.</param>
        /// <param name="preference">The new preference.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetKinkPreferenceAsync([NotNull] GlobalInfoContext db, [NotNull] UserKink userKink, KinkPreference preference)
        {
            userKink.Preference = preference;
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets a user's kink preferences by the F-List kink ID.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="discordUser">The discord user.</param>
        /// <param name="onlineKinkID">The F-List kink ID.</param>
        /// <returns>The user's kink preference.</returns>
        public async Task<RetrieveEntityResult<UserKink>> GetUserKinkByFListIDAsync([NotNull] GlobalInfoContext db, IUser discordUser, int onlineKinkID)
        {
            var getKinkResult = await GetKinkByFListIDAsync(db, onlineKinkID);
            if (!getKinkResult.IsSuccess)
            {
                return RetrieveEntityResult<UserKink>.FromError(getKinkResult);
            }

            var getUserResult = await db.GetOrRegisterUserAsync(discordUser);
            if (!getUserResult.IsSuccess)
            {
                return RetrieveEntityResult<UserKink>.FromError(getUserResult);
            }

            var user = getUserResult.Entity;

            var userKink = user.Kinks.FirstOrDefault(k => k.Kink.FListID == onlineKinkID);

            if (!(userKink is null))
            {
                return RetrieveEntityResult<UserKink>.FromSuccess(userKink);
            }

            var kink = getKinkResult.Entity;
            var addKinkResult = await AddUserKinkAsync(db, discordUser, kink);
            if (!addKinkResult.IsSuccess)
            {
                return RetrieveEntityResult<UserKink>.FromError(addKinkResult);
            }

            return RetrieveEntityResult<UserKink>.FromSuccess(addKinkResult.Entity);
        }

        /// <summary>
        /// Adds a kink to a user's preference list.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="discordUser">The user.</param>
        /// <param name="kink">The kink.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<UserKink>> AddUserKinkAsync([NotNull] GlobalInfoContext db, [NotNull] IUser discordUser, Kink kink)
        {
            var getUserResult = await db.GetOrRegisterUserAsync(discordUser);
            if (!getUserResult.IsSuccess)
            {
                return CreateEntityResult<UserKink>.FromError(getUserResult);
            }

            var user = getUserResult.Entity;

            if (user.Kinks.Any(k => k.Kink.FListID == kink.FListID))
            {
                return CreateEntityResult<UserKink>.FromError(CommandError.MultipleMatches, "The user already has a preference for that kink.");
            }

            var userKink = UserKink.CreateFrom(kink);
            user.Kinks.Add(userKink);

            await db.SaveChangesAsync();
            return CreateEntityResult<UserKink>.FromSuccess(userKink);
        }

        /// <summary>
        /// Gets all available kink categories from the database.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <returns>A list of kink categories.</returns>
        [NotNull]
        public Task<IQueryable<KinkCategory>> GetKinkCategoriesAsync([NotNull] GlobalInfoContext db)
        {
            return Task.FromResult(db.Kinks.Select(k => k.Category).Distinct());
        }

        /// <summary>
        /// Gets a kink by its F-list ID.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="onlineKinkID">The F-List kink ID.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<Kink>> GetKinkByFListIDAsync([NotNull] GlobalInfoContext db, int onlineKinkID)
        {
            var kink = await db.Kinks.FirstOrDefaultAsync(k => k.FListID == onlineKinkID);
            if (kink is null)
            {
                return RetrieveEntityResult<Kink>.FromError(CommandError.ObjectNotFound, "No kink with that ID found.");
            }

            return RetrieveEntityResult<Kink>.FromSuccess(kink);
        }

        /// <summary>
        /// Gets a list of all kinks in a given category.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="category">The category.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<IEnumerable<Kink>>> GetKinksByCategoryAsync([NotNull] GlobalInfoContext db, KinkCategory category)
        {
            var group = await db.Kinks.GroupBy(k => k.Category).FirstOrDefaultAsync(g => g.Key == category);
            if (group is null)
            {
                return RetrieveEntityResult<IEnumerable<Kink>>.FromError
                (
                    CommandError.ObjectNotFound,
                    "There are no kinks in that category."
                );
            }

            return RetrieveEntityResult<IEnumerable<Kink>>.FromSuccess(group);
        }

        /// <summary>
        /// Gets a list of all a user's kinks in a given category.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="user">The user.</param>
        /// <param name="category">The category.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<IEnumerable<UserKink>>> GetUserKinksByCategoryAsync([NotNull] GlobalInfoContext db, [NotNull] IUser user, KinkCategory category)
        {
            var getUserKinksResult = await GetUserKinksAsync(db, user);
            if (!getUserKinksResult.IsSuccess)
            {
                return RetrieveEntityResult<IEnumerable<UserKink>>.FromError(getUserKinksResult);
            }

            var userKinks = getUserKinksResult.Entity;

            var group = userKinks.GroupBy(k => k.Kink.Category).FirstOrDefault(g => g.Key == category);

            if (group is null)
            {
                return RetrieveEntityResult<IEnumerable<UserKink>>.FromSuccess(new UserKink[] { });
            }

            return RetrieveEntityResult<IEnumerable<UserKink>>.FromSuccess(group);
        }

        /// <summary>
        /// Resets the user's kink preferences.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="discordUser">The user.</param>
        /// <returns>A task that must be awaited.</returns>
        public async Task<ModifyEntityResult> ResetUserKinksAsync([NotNull] GlobalInfoContext db, [NotNull] IUser discordUser)
        {
            var getUserResult = await db.GetOrRegisterUserAsync(discordUser);
            if (!getUserResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getUserResult);
            }

            var user = getUserResult.Entity;
            user.Kinks.Clear();

            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets the first kink that the given uses does not have a set preference for in the given category.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="user">The user.</param>
        /// <param name="category">The category.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<Kink>> GetFirstKinkWithoutPreferenceInCategoryAsync([NotNull] GlobalInfoContext db, IUser user, KinkCategory category)
        {
            var getKinksResult = await GetKinksByCategoryAsync(db, category);
            if (!getKinksResult.IsSuccess)
            {
                return RetrieveEntityResult<Kink>.FromError(getKinksResult);
            }

            var kinks = getKinksResult.Entity;

            var getKinksByCategoryResult = await GetUserKinksByCategoryAsync(db, user, category);
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
                return RetrieveEntityResult<Kink>.FromError(CommandError.ObjectNotFound, "No kink without a set preference found.");
            }

            return RetrieveEntityResult<Kink>.FromSuccess(kinkWithoutPreference);
        }

        /// <summary>
        /// Gets the first kink in the given category.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="category">The category.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<Kink>> GetFirstKinkInCategoryAsync([NotNull] GlobalInfoContext db, KinkCategory category)
        {
            var getKinksResult = await GetKinksByCategoryAsync(db, category);
            if (!getKinksResult.IsSuccess)
            {
                return RetrieveEntityResult<Kink>.FromError(getKinksResult);
            }

            return RetrieveEntityResult<Kink>.FromSuccess(getKinksResult.Entity.First());
        }

        /// <summary>
        /// Gets the next kink in its category by its predecessor's F-List ID.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="precedingFListID">The F-List ID of the preceding kink.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<Kink>> GetNextKinkByCurrentFListIDAsync([NotNull] GlobalInfoContext db, int precedingFListID)
        {
            var getKinkResult = await GetKinkByFListIDAsync(db, precedingFListID);
            if (!getKinkResult.IsSuccess)
            {
                return getKinkResult;
            }

            var currentKink = getKinkResult.Entity;
            var getKinksResult = await GetKinksByCategoryAsync(db, currentKink.Category);
            if (!getKinksResult.IsSuccess)
            {
                return RetrieveEntityResult<Kink>.FromError(getKinksResult);
            }

            var group = getKinksResult.Entity;
            var nextKink = group.SkipUntil(k => k.FListID == precedingFListID).FirstOrDefault();

            if (nextKink is null)
            {
                return RetrieveEntityResult<Kink>.FromError(CommandError.ObjectNotFound, "The current kink was the last one in the category.");
            }

            return RetrieveEntityResult<Kink>.FromSuccess(nextKink);
        }
    }
}
