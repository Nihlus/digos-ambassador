//
//  KinkCommands.cs
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

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Kinks;
using DIGOS.Ambassador.Services;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using static Discord.Commands.RunMode;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Modules
{
	/// <summary>
	/// Commands for viewing and configuring user kinks.
	/// </summary>
	[Group("kink")]
	[Summary("Commands for viewing and configuring user kinks.")]
	public class KinkCommands : InteractiveBase<SocketCommandContext>
	{
		private readonly KinkService Kinks;
		private readonly UserFeedbackService Feedback;

		/// <summary>
		/// Initializes a new instance of the <see cref="KinkCommands"/> class.
		/// </summary>
		/// <param name="kinks">The application's kink service.</param>
		/// <param name="feedback">The application's feedback service.</param>
		public KinkCommands(KinkService kinks, UserFeedbackService feedback)
		{
			this.Kinks = kinks;
			this.Feedback = feedback;
		}

		/// <summary>
		/// Shows information about the named kink.
		/// </summary>
		/// <param name="name">The name of the kink.</param>
		[UsedImplicitly]
		[Command("show", RunMode = Async)]
		[Summary("Shows information about the named kink.")]
		public async Task ShowKinkAsync([Remainder] [NotNull] string name)
		{
			using (var db = new GlobalInfoContext())
			{
				var getKinkInfoResult = await this.Kinks.GetKinkByNameAsync(db, name);
				if (!getKinkInfoResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, getKinkInfoResult.ErrorReason);
					return;
				}

				var kink = getKinkInfoResult.Entity;
				var display = this.Kinks.BuildKinkInfoEmbed(kink);

				await this.Feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, display);
			}
		}

		/// <summary>
		/// Shows your preference for the named kink.
		/// </summary>
		/// <param name="name">The name of the kink.</param>
		[UsedImplicitly]
		[Command("preference", RunMode = Async)]
		[Summary("Shows your preference for the named kink.")]
		public async Task ShowKinkPreferenceAsync([Remainder] [NotNull] string name) => await ShowKinkPreferenceAsync(this.Context.User, name);

		/// <summary>
		/// Shows the user's preference for the named kink.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="name">The name of the kink.</param>
		[UsedImplicitly]
		[Command("preference", RunMode = Async)]
		[Summary("Shows the user's preference for the named kink.")]
		public async Task ShowKinkPreferenceAsync([NotNull] IUser user, [Remainder] [NotNull] string name)
		{
			using (var db = new GlobalInfoContext())
			{
				var getUserKinkResult = await this.Kinks.GetUserKinkByNameAsync(db, user, name);
				if (!getUserKinkResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, getUserKinkResult.ErrorReason);
					return;
				}

				var userKink = getUserKinkResult.Entity;
				var display = this.Kinks.BuildKinkPreferenceEmbed(userKink);

				await this.Feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, display);
			}
		}

		/// <summary>
		/// Shows the kinks which overlap between you and the given user.
		/// </summary>
		/// <param name="otherUser">The other user.</param>
		[UsedImplicitly]
		[Command("overlap", RunMode = Async)]
		[Summary("Shows the kinks which overlap between you and the given user.")]
		public async Task ShowKinkOverlap([NotNull] IUser otherUser)
		{
			using (var db = new GlobalInfoContext())
			{
				var userKinks = await this.Kinks.GetUserKinksAsync(db, this.Context.User);
				var otherUserKinks = await this.Kinks.GetUserKinksAsync(db, otherUser);

				var overlap = userKinks.Intersect(otherUserKinks, new UserKinkOverlapEqualityComparer());

				if (!await overlap.AnyAsync())
				{
					await this.Feedback.SendErrorAsync(this.Context, "You don't overlap anywhere.");
					return;
				}

				var display = this.Kinks.BuildKinkOverlapEmbed(this.Context.User, otherUser, overlap);

				await this.Feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, display);
			}
		}

		/// <summary>
		/// Shows your kinks with the given preference.
		/// </summary>
		/// <param name="preference">The preference.</param>
		[UsedImplicitly]
		[Command("by-preference", RunMode = Async)]
		[Summary("Shows your kinks with the given preference.")]
		public async Task ShowKinksByPreferenceAsync(KinkPreference preference) =>
			await ShowKinksByPreferenceAsync(this.Context.User, preference);

		/// <summary>
		/// Shows the given user's kinks with the given preference.
		/// </summary>
		/// <param name="otherUser">The user.</param>
		/// <param name="preference">The preference.</param>
		[UsedImplicitly]
		[Command("by-preference", RunMode = Async)]
		[Summary("Shows the given user's kinks with the given preference.")]
		public async Task ShowKinksByPreferenceAsync([NotNull] IUser otherUser, KinkPreference preference)
		{
			using (var db = new GlobalInfoContext())
			{
				var userKinks = await this.Kinks.GetUserKinksAsync(db, otherUser);
				var withPreference = userKinks.Where(k => k.Preference == preference);

				if (!await withPreference.AnyAsync())
				{
					await this.Feedback.SendErrorAsync(this.Context, "The user doesn't have any kinks with that preference.");
					return;
				}

				var paginatedKinks = this.Kinks.BuildPaginatedUserKinkEmbed(withPreference);

			}
		}

		/// <summary>
		/// Sets your preference for the given kink.
		/// </summary>
		/// <param name="name">The name of the kink.</param>
		/// <param name="preference">The preference for the kink.</param>
		[UsedImplicitly]
		[Command("preference", RunMode = Async)]
		[Summary("Sets your preference for the given kink.")]
		public async Task SetKinkPreferenceAsync([NotNull] string name, KinkPreference preference)
		{

		}

		/// <summary>
		/// Runs an interactive wizard for setting kink preferences.
		/// </summary>
		[UsedImplicitly]
		[Command("wizard", RunMode = Async)]
		[Summary("Runs an interactive wizard for setting kink preferences.")]
		public async Task RunKinkWizardAsync()
		{

		}

		/// <summary>
		/// Resets all your kink preferences.
		/// </summary>
		[UsedImplicitly]
		[Command("reset", RunMode = Async)]
		[Summary("Resets all your kink preferences.")]
		public async Task ResetKinksAsync()
		{

		}

		/// <summary>
		/// Sets the visbility level of your kinks; that is, who can see them. Valid choices are All, Friends, and Whitelist.
		/// </summary>
		/// <param name="visibility">The new visibility.</param>
		[UsedImplicitly]
		[Command("visibility", RunMode = Async)]
		[Summary("Sets the visbility level of your kinks; that is, who can see them. Valid choices are All, Friends, and Whitelist.")]
		public async Task SetKinkVisibilityAsync(KinkVisibility visibility)
		{

		}

		/// <summary>
		/// Adds the given user to your visibility whitelist.
		/// </summary>
		/// <param name="otherUser">The user to add.</param>
		[UsedImplicitly]
		[Command("whitelist", RunMode = Async)]
		[Summary("Adds the given user to your visibility whitelist.")]
		public async Task AddUserToWhitelistAsync([NotNull] IUser otherUser)
		{

		}
	}
}
