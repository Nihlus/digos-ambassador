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
using System.Diagnostics.CodeAnalysis;
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
using Discord.WebSocket;
using Humanizer;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Wizards
{
    /// <summary>
    /// Acts as an interactive wizard for interactively setting the kink preferences of users.
    /// </summary>
    public class KinkWizard : InteractiveMessage, IWizard
    {
        [ProvidesContext]
        private readonly GlobalInfoContext Database;
        private readonly UserFeedbackService Feedback;
        private readonly KinkService Kinks;

        private static readonly Emoji Next = new Emoji("\x25B6");
        private static readonly Emoji Previous = new Emoji("\x25C0");
        private static readonly Emoji First = new Emoji("\x23EE");
        private static readonly Emoji Last = new Emoji("\x23ED");
        private static readonly Emoji EnterCategory = new Emoji("\xD83D\xDD22");

        private static readonly Emoji Fave = new Emoji("\x2764");
        private static readonly Emoji Like = new Emoji("\x2705");
        private static readonly Emoji Maybe = new Emoji("\x26A0");
        private static readonly Emoji Never = new Emoji("\x26D4");
        private static readonly Emoji NoPreference = new Emoji("🤷");

        private static readonly Emoji Back = new Emoji("\x23EB");
        private static readonly Emoji Exit = new Emoji("\x23F9");
        private static readonly Emoji Info = new Emoji("\x2139");

        /// <inheritdoc />
        [NotNull]
        public IReadOnlyCollection<IEmote> AcceptedEmotes => GetCurrentPageEmotes().ToList();

        /// <summary>
        /// Gets the emotes that are currently rejected by the wizard.
        /// </summary>
        [NotNull]
        public IReadOnlyCollection<IEmote> CurrrentlyRejectedEmotes => GetCurrentPageRejectedEmotes().ToList();

        private readonly Embed LoadingEmbed;

        private int CurrentFListKinkID;

        private KinkWizardState State;

        private IReadOnlyList<KinkCategory> Categories;

        private int CurrentCategoryOffset;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinkWizard"/> class.
        /// </summary>
        /// <param name="database">A database context from the context pool.</param>
        /// <param name="context">The context.</param>
        /// <param name="feedback">The user feedback service.</param>
        /// <param name="kinkService">The kink service.</param>
        /// <param name="interactiveService">The interactive service.</param>
        public KinkWizard(GlobalInfoContext database, SocketCommandContext context, UserFeedbackService feedback, KinkService kinkService, InteractiveService interactiveService)
            : base(context, interactiveService)
        {
            this.Database = database;
            this.Feedback = feedback;
            this.Kinks = kinkService;

            this.State = KinkWizardState.CategorySelection;

            var eb = new EmbedBuilder();
            eb.WithTitle("Kink Wizard");
            eb.WithDescription("Loading...");

            this.LoadingEmbed = eb.Build();
        }

        /// <inheritdoc />
        public override async Task<IUserMessage> DisplayAsync([NotNull] ISocketMessageChannel channel)
        {
            if (!(this.Message is null))
            {
                throw new InvalidOperationException("The wizard is already active in a channel.");
            }

            this.Categories = (await this.Kinks.GetKinkCategoriesAsync(this.Database)).ToList();

            this.State = KinkWizardState.CategorySelection;

            this.Message = await channel.SendMessageAsync(string.Empty, embed: this.LoadingEmbed).ConfigureAwait(false);
            this.ReactionCallback = new WizardCallback(this.Context, this);

            await UpdateVisibleMessage();

            return this.Message;
        }

        private async Task UpdateVisibleMessage(bool shouldModifyContents = true)
        {
            if (shouldModifyContents)
            {
                await this.Message.ModifyAsync(m => m.Embed = this.LoadingEmbed);
            }

            foreach (var emote in this.CurrrentlyRejectedEmotes)
            {
                if (!this.Message.Reactions.ContainsKey(emote) || !this.Message.Reactions[emote].IsMe)
                {
                    continue;
                }

                await this.Message.RemoveReactionAsync(emote, this.Context.Client.CurrentUser);
            }

            foreach (var emote in this.AcceptedEmotes)
            {
                if (this.Message.Reactions.ContainsKey(emote) && this.Message.Reactions[emote].IsMe)
                {
                    continue;
                }

                await this.Message.AddReactionAsync(emote);
            }

            if (shouldModifyContents)
            {
                var newEmbed = await GetCurrentPageAsync();
                await this.Message.ModifyAsync(m => m.Embed = newEmbed);
            }
        }

        /// <inheritdoc />
        public async Task<bool> ConsumeEmoteAsync([NotNull] IEmote emote)
        {
            if (emote.Equals(Exit))
            {
                this.Interactive.RemoveReactionCallback(this.Message);
                await this.Message.DeleteAsync().ConfigureAwait(false);

                return true;
            }

            if (emote.Equals(Info))
            {
                await DisplayHelpTextAsync();
                return false;
            }

            switch (this.State)
            {
                case KinkWizardState.CategorySelection:
                {
                    await ConsumeCategoryInteractionAsync(emote);
                    break;
                }
                case KinkWizardState.KinkPreference:
                {
                    await ConsumePreferenceInteractionAsync(emote);
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            return false;
        }

        private async Task ConsumePreferenceInteractionAsync([NotNull] IEmote emote)
        {
            if (emote.Equals(Back))
            {
                this.State = KinkWizardState.CategorySelection;
                await UpdateVisibleMessage();
                return;
            }

            KinkPreference? preference = null;
            if (emote.Equals(Fave))
            {
                preference = KinkPreference.Favourite;
            }

            if (emote.Equals(Like))
            {
                preference = KinkPreference.Like;
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

                var getNextKinkResult = await this.Kinks.GetNextKinkByCurrentFListIDAsync(this.Database, this.CurrentFListKinkID);
                if (!getNextKinkResult.IsSuccess)
                {
                    this.CurrentFListKinkID = -1;
                    this.State = KinkWizardState.CategorySelection;
                    await this.Feedback.SendConfirmationAndDeleteAsync(this.Context, this.Interactive, "All done in that category!");
                }
                else
                {
                    this.CurrentFListKinkID = (int)getNextKinkResult.Entity.FListID;
                }

                await UpdateVisibleMessage();
            }
        }

        private async Task ConsumeCategoryInteractionAsync([NotNull] IEmote emote)
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
                if (this.CurrentCategoryOffset - 3 < 0)
                {
                    this.CurrentCategoryOffset = 0;
                    return;
                }

                this.CurrentCategoryOffset -= 3;
            }
            else if (emote.Equals(First))
            {
                if (this.CurrentCategoryOffset == 0)
                {
                    return;
                }

                this.CurrentCategoryOffset = 0;
            }
            else if (emote.Equals(Last))
            {
                int newOffset;
                if (this.Categories.Count % 3 == 0)
                {
                    newOffset = this.Categories.Count - 3;
                }
                else
                {
                    newOffset = this.Categories.Count - (this.Categories.Count % 3);
                }

                if (newOffset <= this.CurrentCategoryOffset)
                {
                    return;
                }

                this.CurrentCategoryOffset = newOffset;
            }
            else if (emote.Equals(EnterCategory))
            {
                if (!this.Categories.Any())
                {
                    await this.Feedback.SendWarningAndDeleteAsync
                    (
                        this.Context,
                        this.Interactive,
                        "There aren't any categories in the database.",
                        TimeSpan.FromSeconds(10)
                    );

                    return;
                }

                await this.Feedback.SendConfirmationAndDeleteAsync
                (
                    this.Context,
                    this.Interactive,
                    "Please enter a category name.",
                    TimeSpan.FromSeconds(45)
                );

                var message = await this.Interactive.NextMessageAsync(this.Context, timeout: TimeSpan.FromSeconds(45));

                var tryStartCategoryResult = await OpenCategory(message.Content);
                if (!tryStartCategoryResult.IsSuccess)
                {
                    await this.Feedback.SendWarningAndDeleteAsync
                    (
                        this.Context,
                        this.Interactive,
                        tryStartCategoryResult.ErrorReason,
                        TimeSpan.FromSeconds(10)
                    );

                    return;
                }
            }

            await UpdateVisibleMessage();
        }

        private async Task<ExecuteResult> OpenCategory(string categoryName)
        {
            var getCategoryResult = this.Categories.Select(c => c.ToString()).BestLevenshteinMatch(categoryName, 0.75);
            if (!getCategoryResult.IsSuccess)
            {
                return ExecuteResult.FromError(getCategoryResult);
            }

            if (!Enum.TryParse<KinkCategory>(getCategoryResult.Entity, true, out var category))
            {
                return ExecuteResult.FromError(CommandError.ParseFailed, "Could not parse kink category.");
            }

            var getKinkResult = await this.Kinks.GetFirstKinkWithoutPreferenceInCategoryAsync(this.Database, this.Context.User, category);
            if (!getKinkResult.IsSuccess)
            {
                getKinkResult = await this.Kinks.GetFirstKinkInCategoryAsync(this.Database, category);
            }

            if (!getKinkResult.IsSuccess)
            {
                return ExecuteResult.FromError(getKinkResult);
            }

            var kink = getKinkResult.Entity;
            this.CurrentFListKinkID = (int)kink.FListID;

            this.State = KinkWizardState.KinkPreference;

            return ExecuteResult.FromSuccess();
        }

        [SuppressMessage("Style", "SA1118", Justification = "Large text blocks.")]
        private async Task DisplayHelpTextAsync()
        {
            var eb = new EmbedBuilder();
            eb.WithColor(Color.DarkPurple);

            switch (this.State)
            {
                case KinkWizardState.CategorySelection:
                {
                    eb.WithTitle("Help: Category selection");
                    eb.AddField
                    (
                        "Usage",
                        $"Use the navigation buttons to scroll through the available categories. Select a category by " +
                        $"pressing {EnterCategory} and typing in the name. The search algorithm is quite lenient, so " +
                        $"you may find that things work fine even with typos.\n" +
                        $"\n" +
                        $"You can quit at any point by pressing {Exit}."
                    );
                    break;
                }
                case KinkWizardState.KinkPreference:
                {
                    eb.WithTitle("Help: Kink preference");
                    eb.AddField
                    (
                        "Usage",
                        $"Set your preference for this kink by pressing one of the following buttons:" +
                        $"\n{Fave} : Favourite" +
                        $"\n{Like} : Like" +
                        $"\n{Maybe} : Maybe" +
                        $"\n{Never} : Never" +
                        $"\n{NoPreference} : No preference\n" +
                        $"\n" +
                        $"\nPress {Back} to go back to the categories." +
                        $"\nYou can quit at any point by pressing {Exit}."
                    );
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            await this.Interactive.ReplyAndDeleteAsync(this.Context, string.Empty, false, eb.Build(), TimeSpan.FromSeconds(30));
        }

        private async Task SetCurrentKinkPreference(KinkPreference preference)
        {
            var getUserKinkResult = await this.Kinks.GetUserKinkByFListIDAsync(this.Database, this.Context.User, this.CurrentFListKinkID);
            if (!getUserKinkResult.IsSuccess)
            {
                await this.Feedback.SendErrorAndDeleteAsync(this.Context, this.Interactive, getUserKinkResult.ErrorReason);
                return;
            }

            var userKink = getUserKinkResult.Entity;
            var setPreferenceResult = await this.Kinks.SetKinkPreferenceAsync(this.Database, userKink, preference);
            if (!setPreferenceResult.IsSuccess)
            {
                await this.Feedback.SendErrorAndDeleteAsync(this.Context, this.Interactive, setPreferenceResult.ErrorReason);
            }
        }

        /// <summary>
        /// Gets the emotes that are associated with the current page.
        /// </summary>
        /// <returns>A set of emotes.</returns>
        [NotNull]
        public IEnumerable<IEmote> GetCurrentPageEmotes()
        {
            switch (this.State)
            {
                case KinkWizardState.CategorySelection:
                {
                    return new[] { Exit, Info, First, Previous, Next, Last, EnterCategory };
                }
                case KinkWizardState.KinkPreference:
                {
                    return new[] { Exit, Info, Back, Fave, Like, Maybe, Never, NoPreference };
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        [NotNull]
        private IEnumerable<IEmote> GetCurrentPageRejectedEmotes()
        {
            switch (this.State)
            {
                case KinkWizardState.CategorySelection:
                {
                    return new[] { Back, Fave, Like, Maybe, Never, NoPreference };
                }
                case KinkWizardState.KinkPreference:
                {
                    return new[] { Next, Previous, First, Last, EnterCategory };
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
                    var eb = this.Feedback.CreateEmbedBase();
                    eb.WithTitle("Category selection");

                    if (this.Categories.Any())
                    {
                        eb.WithDescription("Select from one of the categories below.");
                        var categories = this.Categories.Skip(this.CurrentCategoryOffset).Take(3).ToList();
                        foreach (var category in categories)
                        {
                            eb.AddField(category.ToString().Humanize().Transform(To.TitleCase), category.Humanize());
                        }

                        eb.WithFooter($"Categories {this.CurrentCategoryOffset}-{this.CurrentCategoryOffset + categories.Count} / {this.Categories.Count}");
                    }
                    else
                    {
                        eb.WithDescription("There aren't any categories in the database.");
                    }

                    return eb.Build();
                }
                case KinkWizardState.KinkPreference:
                {
                    var getUserKinkResult = await this.Kinks.GetUserKinkByFListIDAsync(this.Database, this.Context.User, this.CurrentFListKinkID);
                    if (!getUserKinkResult.IsSuccess)
                    {
                        await this.Feedback.SendErrorAndDeleteAsync(this.Context, this.Interactive, "Failed to get the user kink.", TimeSpan.FromSeconds(10));
                        this.State = KinkWizardState.CategorySelection;

                        // Recursively calling at this point is safe, since we will get the emotes from the category page.
                        return await GetCurrentPageAsync();
                    }

                    var userKink = getUserKinkResult.Entity;
                    return this.Kinks.BuildUserKinkInfoEmbedBase(userKink).Build();
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
