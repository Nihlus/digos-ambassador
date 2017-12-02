//
//  KinkWizard.cs
//
//  Author:
//        Jarl Gullberg <jarl.gullberg@gmail.com>
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
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Kinks;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Interactivity;
using DIGOS.Ambassador.Services;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;

using Humanizer;
using Microsoft.EntityFrameworkCore;
using MoreLinq;

namespace DIGOS.Ambassador.Wizards
{
	/// <summary>
	/// Acts as an interactive wizard for interactively setting the kink preferences of users.
	/// </summary>
	public class KinkWizard : InteractiveMessage, IWizard
	{
		private readonly UserFeedbackService Feedback;
		private readonly KinkService Kinks;

		private static readonly Emote Next = Emote.Parse("arrow_forward");
		private static readonly Emote Previous = Emote.Parse("arrow_back");
		private static readonly Emote First = Emote.Parse("previous_track");
		private static readonly Emote Last = Emote.Parse("next_track");
		private static readonly Emote EnterCategory = Emote.Parse("abc");

		private static readonly Emote Fave = Emote.Parse("heart");
		private static readonly Emote Like = Emote.Parse("white_check_mark");
		private static readonly Emote Maybe = Emote.Parse("warning");
		private static readonly Emote Never = Emote.Parse("no_entry");
		private static readonly Emote NoPreference = Emote.Parse("shrug");

		private static readonly Emote Exit = Emote.Parse("stop_button");
		private static readonly Emote Info = Emote.Parse("information_source");

		/// <inheritdoc />
		public IReadOnlyCollection<IEmote> AcceptedEmotes => GetCurrentPageEmotes().ToList();

		private int CurrentFListKinkID;

		private KinkWizardState State;

		private IReadOnlyList<KinkCategory> Categories;

		private int CurrentCategoryOffset;

		/// <summary>
		/// Initializes a new instance of the <see cref="KinkWizard"/> class.
		/// </summary>
		/// <param name="context">The message context.</param>
		/// <param name="feedback">The user feedback service.</param>
		/// <param name="kinkService">The kink service.</param>
		/// <param name="interactiveService">The interactive service.</param>
		public KinkWizard(SocketCommandContext context, UserFeedbackService feedback, KinkService kinkService, InteractiveService interactiveService)
			: base(context, interactiveService)
		{
			this.Feedback = feedback;
			this.Kinks = kinkService;

			this.ReactionCallback = new WizardCallback(context, this);
			this.State = KinkWizardState.CategorySelection;
		}

		/// <inheritdoc />
		public override async Task<IUserMessage> DisplayAsync(IMessageChannel channel)
		{
			if (!(this.Message is null))
			{
				throw new InvalidOperationException("The wizard is already active in a channel.");
			}

			this.State = KinkWizardState.CategorySelection;

			var homepage = await GetHomePageAsync();
			this.Message = await channel.SendMessageAsync(string.Empty, embed: homepage).ConfigureAwait(false);

			using (var db = new GlobalInfoContext())
			{
				this.Categories = (await this.Kinks.GetKinkCategoriesAsync(db)).ToList();
			}

			return this.Message;
		}

		private async Task UpdateMessage(bool shouldModifyContents = true)
		{
			if (shouldModifyContents)
			{
				await this.Message.ModifyAsync(async m => m.Embed = await GetCurrentPageAsync());
			}

			// Reactions take a while to add, don't wait for them
			_ = Task.Run(async () =>
			{
				foreach (var emote in this.AcceptedEmotes)
				{
					if (this.Message.Reactions.ContainsKey(emote) && this.Message.Reactions[emote].IsMe)
					{
						continue;
					}

					await this.Message.AddReactionAsync(emote);
				}
			});
		}

		/// <inheritdoc />
		public async Task ConsumeAsync(IEmote emote)
		{
			if (emote.Equals(Exit))
			{
				await QuitWizardAsync();
				return;
			}

			if (emote.Equals(Info))
			{
				await DisplayHelpTextAsync();
				return;
			}

			switch (this.State)
			{
				case KinkWizardState.CategorySelection:
				{
					await ConsumeCategoryInteractionAsync(emote);
					return;
				}
				case KinkWizardState.KinkPreference:
				{
					await ConsumePreferenceInteractionAsync(emote);
					return;
				}
				default:
				{
					throw new ArgumentOutOfRangeException();
				}
			}
		}

		private async Task ConsumePreferenceInteractionAsync(IEmote emote)
		{
			KinkPreference? preference = null;
			if (emote.Equals(Fave))
			{
				preference = KinkPreference.Favourite;
			}

			if (emote.Equals(Like))
			{
				preference = KinkPreference.Yes;
			}

			if (emote.Equals(Maybe))
			{
				preference = KinkPreference.Maybe;
			}

			if (emote.Equals(Never))
			{
				preference = KinkPreference.No;
			}

			if (emote.Equals(NoPreference))
			{
				preference = KinkPreference.NoPreference;
			}

			if (!(preference is null))
			{
				await SetCurrentKinkPreference(preference.Value);

				var getNextKinkResult = await GetNextKinkByCurrentFListIDAsync(this.CurrentFListKinkID);
				if (!getNextKinkResult.IsSuccess)
				{
					this.CurrentFListKinkID = -1;
				}
				else
				{
					this.CurrentFListKinkID = (int)getNextKinkResult.Entity.FListID;
				}

				await UpdateMessage();
			}
		}

		private async Task<RetrieveEntityResult<Kink>> GetFirstKinkWithoutPreferenceInCategory(KinkCategory category)
		{
			throw new NotImplementedException();
		}

		private async Task<RetrieveEntityResult<Kink>> GetNextKinkByCurrentFListIDAsync(int currentFListKinkID)
		{
			using (var db = new GlobalInfoContext())
			{
				var getKinkResult = await this.Kinks.GetKinkByIDAsync(db, currentFListKinkID);
				if (!getKinkResult.IsSuccess)
				{
					return getKinkResult;
				}

				var currentKink = getKinkResult.Entity;
				var byGroup = db.Kinks.GroupBy(k => k.Category);
				var group = await byGroup.FirstAsync(g => g.Key == currentKink.Category);

				var nextKink = group.SkipUntil(k => k.FListID == currentFListKinkID).FirstOrDefault();

				if (nextKink is null)
				{
					return RetrieveEntityResult<Kink>.FromError(CommandError.ObjectNotFound, "The current kink was the last one in the category.");
				}

				return RetrieveEntityResult<Kink>.FromSuccess(nextKink);
			}
		}

		private async Task ConsumeCategoryInteractionAsync(IEmote emote)
		{
			if (emote.Equals(Next))
			{
				if (this.CurrentCategoryOffset + 3 >= this.Categories.Count)
				{
					return;
				}

				this.CurrentCategoryOffset += 3;
			}
			else if (emote.Equals(Previous))
			{
				if (this.CurrentCategoryOffset - 3 > 0)
				{
					this.CurrentCategoryOffset = 0;
					return;
				}

				this.CurrentCategoryOffset -= 3;
			}
			else if (emote.Equals(First))
			{
				this.CurrentCategoryOffset = 0;
			}
			else if (emote.Equals(Last))
			{
				if (this.Categories.Count % 3 == 0)
				{
					this.CurrentCategoryOffset = this.Categories.Count - 3;
				}
				else
				{
					this.CurrentCategoryOffset = this.Categories.Count - (this.Categories.Count % 3);
				}
			}
			else if (emote.Equals(EnterCategory))
			{
				await this.Feedback.SendConfirmationAndDeleteAsync
				(
					this.Context,
					this.Interactive,
					"Please enter a category name.",
					TimeSpan.FromSeconds(45)
				);

				var message = await this.Interactive.NextMessageAsync(this.Context, timeout: TimeSpan.FromSeconds(45));

				var tryStartCategoryResult = await StartCategory(message.Content);
				if (!tryStartCategoryResult.IsSuccess)
				{
					await this.Feedback.SendWarningAndDeleteAsync
					(
						this.Context,
						this.Interactive,
						"No category with that name found.",
						TimeSpan.FromSeconds(10)
					);

					return;
				}
			}

			await UpdateMessage();
		}

		private async Task<ExecuteResult> StartCategory(string categoryName)
		{
			var getCategoryResult = this.Categories.Select(c => c.ToString()).BestLevenshteinMatch(categoryName);
			if (!getCategoryResult.IsSuccess)
			{
				return ExecuteResult.FromError(getCategoryResult);
			}

			var category = Enum.Parse<KinkCategory>(getCategoryResult.Entity, true);
			throw new NotImplementedException();
		}

		private async Task QuitWizardAsync()
		{
			this.Interactive.RemoveReactionCallback(this.Message);
			await this.Message.DeleteAsync().ConfigureAwait(false);
		}

		private Task DisplayHelpTextAsync()
		{
			throw new NotImplementedException();
		}

		private async Task SetCurrentKinkPreference(KinkPreference preference)
		{
			using (var db = new GlobalInfoContext())
			{
				var userKink = await this.Kinks.GetUserKinkByIDAsync(db, this.CurrentFListKinkID);
				var setPreferenceResult = await this.Kinks.SetKinkPreferenceAsync(db, userKink, preference);
				if (!setPreferenceResult.IsSuccess)
				{
					var eb = this.Feedback.CreateBaseEmbed(Color.Red);
					eb.WithDescription("Failed to set the kink preference.");

					await this.Interactive.ReplyAndDeleteAsync(this.Context, string.Empty, false, eb, TimeSpan.FromSeconds(5));
				}
			}
		}

		/// <summary>
		/// Gets the emotes that are associated with the current page.
		/// </summary>
		/// <returns>A set of emotes.</returns>
		public IEnumerable<IEmote> GetCurrentPageEmotes()
		{
			switch (this.State)
			{
				case KinkWizardState.CategorySelection:
				{
					return new[] { Next, Previous, First, Last, EnterCategory, Exit, Info };
				}
				case KinkWizardState.KinkPreference:
				{
					return new[] { Fave, Like, Maybe, Never, NoPreference, Exit, Info };
				}
				default:
				{
					throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <inheritdoc />
		public async Task<Embed> GetCurrentPageAsync()
		{
			switch (this.State)
			{
				case KinkWizardState.CategorySelection:
				{
					var eb = this.Feedback.CreateBaseEmbed();
					var categories = this.Categories.Skip(this.CurrentCategoryOffset).Take(3).ToList();
					foreach (var category in categories)
					{
						eb.AddField(category.ToString().Transform(To.TitleCase), category.Humanize());
					}

					eb.WithFooter($"Categories {this.CurrentCategoryOffset}-{this.CurrentCategoryOffset + categories.Count} / {this.Categories.Count}");

					return eb.Build();
				}
				case KinkWizardState.KinkPreference:
				{
					using (var db = new GlobalInfoContext())
					{
						var userKink = await this.Kinks.GetUserKinkByIDAsync(db, this.CurrentFListKinkID);
						return this.Kinks.BuildKinkPreferenceEmbed(userKink);
					}
				}
				default:
				{
					throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <inheritdoc />
		public Task<Embed> GetHomePageAsync()
		{
			this.State = KinkWizardState.CategorySelection;
			return GetCurrentPageAsync();
		}
	}
}
