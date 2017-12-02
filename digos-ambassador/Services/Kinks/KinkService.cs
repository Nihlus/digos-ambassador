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
using DIGOS.Ambassador.Pagination;
using Discord;
using Humanizer;

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
			throw new System.NotImplementedException();
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
		/// <param name="user">The user.</param>
		/// <param name="name">The kink name.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public async Task<RetrieveEntityResult<UserKink>> GetUserKinkByNameAsync(GlobalInfoContext db, IUser user, string name)
		{
			throw new System.NotImplementedException();
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
		/// <param name="user">The user.</param>
		/// <returns>The user's kinks.</returns>
		public async Task<IQueryable<UserKink>> GetUserKinksAsync(GlobalInfoContext db, IUser user)
		{
			throw new System.NotImplementedException();
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
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Builds a paginated embed displaying the given kinks.
		/// </summary>
		/// <param name="kinks">The kinks.</param>
		/// <returns>A paginated embed.</returns>
		public PaginatedEmbed BuildPaginatedUserKinkEmbed(IQueryable<UserKink> kinks)
		{
			throw new System.NotImplementedException();
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
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Gets a user's kink preferences by the F-List kink ID.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="onlineKinkID">The F-List kink ID.</param>
		/// <returns>The user's kink preference.</returns>
		public Task<UserKink> GetUserKinkByIDAsync(GlobalInfoContext db, int onlineKinkID)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Gets all available kink categories from the database.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <returns>A list of kink categories.</returns>
		public Task<IEnumerable<KinkCategory>> GetKinkCategoriesAsync(GlobalInfoContext db)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Gets a kink by its F-list ID.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="onlineKinkID">The F-List kink ID.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public Task<RetrieveEntityResult<Kink>> GetKinkByIDAsync(GlobalInfoContext db, int onlineKinkID)
		{
			throw new System.NotImplementedException();
		}
	}
}
