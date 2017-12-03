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
using DIGOS.Ambassador.Pagination;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using MoreLinq;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Service class for user kinks.
	/// </summary>
	public class KinkService
	{
		private readonly UserFeedbackService Feedback;

		/// <summary>
		/// Initializes a new instance of the <see cref="KinkService"/> class.
		/// </summary>
		/// <param name="feedback">The feedback service.</param>
		public KinkService(UserFeedbackService feedback)
		{
			this.Feedback = feedback;
		}

		/// <summary>
		/// Gets a kink by its name.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="name">The name of the kink.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public async Task<RetrieveEntityResult<Kink>> GetKinkByNameAsync(GlobalInfoContext db, string name)
		{
			return await db.Kinks.SelectFromBestLevenshteinMatchAsync(x => x, k => k.Name, name);
		}

		/// <summary>
		/// Builds an informational embed for the given kink.
		/// </summary>
		/// <param name="kink">The kink.</param>
		/// <returns>An embed.</returns>
		public Embed BuildKinkInfoEmbed(Kink kink)
		{
			var eb = this.Feedback.CreateBaseEmbed();

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
		public async Task<RetrieveEntityResult<UserKink>> GetUserKinkByNameAsync(GlobalInfoContext db, IUser discordUser, string name)
		{
			var user = await db.GetOrRegisterUserAsync(discordUser);
			return user.Kinks.SelectFromBestLevenshteinMatch(x => x, k => k.Kink.Name, name);
		}

		/// <summary>
		/// Builds an embed displaying the user's current preference for a given kink.
		/// </summary>
		/// <param name="userKink">The kink.</param>
		/// <returns>An embed.</returns>
		public Embed BuildKinkPreferenceEmbed(UserKink userKink)
		{
			var eb = this.Feedback.CreateBaseEmbed();

			eb.WithTitle(userKink.Kink.Name.Transform(To.TitleCase));
			eb.WithDescription(userKink.Kink.Description);

			eb.AddField("Current preference", userKink.Preference.Humanize());

			return eb.Build();
		}

		/// <summary>
		/// Gets the given user's kink preferences.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordUser">The user.</param>
		/// <returns>The user's kinks.</returns>
		public async Task<IEnumerable<UserKink>> GetUserKinksAsync(GlobalInfoContext db, IUser discordUser)
		{
			var user = await db.GetOrRegisterUserAsync(discordUser);
			return user.Kinks;
		}

		/// <summary>
		/// Builds a paginated embed displaying the given overlapping kinks.
		/// </summary>
		/// <param name="firstUser">The first user.</param>
		/// <param name="secondUser">The second user.</param>
		/// <param name="kinks">The kinks.</param>
		/// <returns>A paginated embed.</returns>
		public PaginatedEmbed BuildKinkOverlapEmbed(IUser firstUser, IUser secondUser, IEnumerable<UserKink> kinks)
		{
			var pages = new List<EmbedBuilder>();

			foreach (var batch in kinks.Batch(3))
			{
				var eb = new EmbedBuilder();
				eb.WithTitle($"Matching kinks between {firstUser.Mention} and {secondUser.Mention}");
				foreach (var kink in batch)
				{
					eb.AddField(kink.Kink.Name, kink.Preference.Humanize().Transform(To.SentenceCase));
				}

				pages.Add(eb);
			}

			return new PaginatedEmbed(pages);
		}

		/// <summary>
		/// Builds a paginated embed displaying the given kinks.
		/// </summary>
		/// <param name="kinks">The kinks.</param>
		/// <returns>A paginated embed.</returns>
		public PaginatedEmbed BuildPaginatedUserKinkEmbed(IEnumerable<UserKink> kinks)
		{
			var pages = new List<EmbedBuilder>();

			foreach (var batch in kinks.Batch(3))
			{
				var eb = new EmbedBuilder();
				foreach (var kink in batch)
				{
					eb.AddField(kink.Kink.Name, kink.Preference.Humanize().Transform(To.SentenceCase));
				}

				pages.Add(eb);
			}

			return new PaginatedEmbed(pages);
		}

		/// <summary>
		/// Sets the user's preference for the given kink.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="userKink">The user's kink.</param>
		/// <param name="preference">The new preference.</param>
		/// <returns>A modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetKinkPreferenceAsync(GlobalInfoContext db, UserKink userKink, KinkPreference preference)
		{
			userKink.Preference = preference;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Gets a user's kink preferences by the F-List kink ID.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordUser">The discord user.</param>
		/// <param name="onlineKinkID">The F-List kink ID.</param>
		/// <returns>The user's kink preference.</returns>
		public async Task<RetrieveEntityResult<UserKink>> GetUserKinkByFListIDAsync(GlobalInfoContext db, IUser discordUser, int onlineKinkID)
		{
			var getKinkResult = await GetKinkByFListIDAsync(db, onlineKinkID);
			if (!getKinkResult.IsSuccess)
			{
				return RetrieveEntityResult<UserKink>.FromError(getKinkResult);
			}

			var user = await db.GetOrRegisterUserAsync(discordUser);
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
		public async Task<CreateEntityResult<UserKink>> AddUserKinkAsync(GlobalInfoContext db, IUser discordUser, Kink kink)
		{
			var user = await db.GetOrRegisterUserAsync(discordUser);
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
		public Task<IQueryable<KinkCategory>> GetKinkCategoriesAsync(GlobalInfoContext db)
		{
			return Task.FromResult(db.Kinks.Select(k => k.Category).Distinct());
		}

		/// <summary>
		/// Gets a kink by its F-list ID.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="onlineKinkID">The F-List kink ID.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public async Task<RetrieveEntityResult<Kink>> GetKinkByFListIDAsync(GlobalInfoContext db, int onlineKinkID)
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
		public async Task<RetrieveEntityResult<IEnumerable<Kink>>> GetKinksByCategoryAsync(GlobalInfoContext db, KinkCategory category)
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
		public async Task<IEnumerable<UserKink>> GetUserKinksByCategoryAsync(GlobalInfoContext db, IUser user, KinkCategory category)
		{
			var userKinks = await GetUserKinksAsync(db, user);
			var group = userKinks.GroupBy(k => k.Kink.Category).FirstOrDefault(g => g.Key == category);

			if (group is null)
			{
				return new UserKink[] { };
			}

			return group;
		}

		/// <summary>
		/// Resets the user's kink preferences.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordUser">The user.</param>
		/// <returns>A task that must be awaited.</returns>
		public async Task ResetUserKinksAsync(GlobalInfoContext db, IUser discordUser)
		{
			var user = await db.GetOrRegisterUserAsync(discordUser);
			user.Kinks.Clear();

			await db.SaveChangesAsync();
		}
	}
}
