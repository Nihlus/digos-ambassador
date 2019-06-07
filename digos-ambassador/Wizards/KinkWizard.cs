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
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Interactivity.Messages;

using Discord;
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
        private readonly GlobalInfoContext _database;
        private readonly UserFeedbackService _feedback;
        private readonly KinkService _kinks;

        private readonly IUser _targetUser;

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

        /// <summary>
        /// Gets the currently accepted emotes.
        /// </summary>
        [NotNull]
        private IReadOnlyCollection<IEmote> AcceptedEmotes => GetCurrentPageEmotes().ToList();

        /// <summary>
        /// Gets the emotes that are currently rejected by the wizard.
        /// </summary>
        [NotNull]
        private IReadOnlyCollection<IEmote> CurrrentlyRejectedEmotes => GetCurrentPageRejectedEmotes().ToList();

        private readonly Embed _loadingEmbed;

        private int _currentFListKinkID;

        private KinkWizardState _state;

        private IReadOnlyList<KinkCategory> _categories;

        private int _currentCategoryOffset;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinkWizard"/> class.
        /// </summary>
        /// <param name="database">A database context from the context pool.</param>
        /// <param name="feedback">The user feedback service.</param>
        /// <param name="kinkService">The kink service.</param>
        /// <param name="targetUser">The target user.</param>
        public KinkWizard
        (
            GlobalInfoContext database,
            UserFeedbackService feedback,
            KinkService kinkService,
            IUser targetUser
        )
            : base(targetUser)
        {
            this._database = database;
            this._feedback = feedback;
            this._kinks = kinkService;

            this._targetUser = targetUser;

            this._state = KinkWizardState.CategorySelection;

            var eb = new EmbedBuilder();
            eb.WithTitle("Kink Wizard");
            eb.WithDescription("Loading...");

            this._loadingEmbed = eb.Build();
        }

        /// <inheritdoc />
        protected override async Task<IUserMessage> DisplayAsync([NotNull] IMessageChannel channel)
        {
            if (!(this.Message is null))
            {
                throw new InvalidOperationException("The wizard is already active in a channel.");
            }

            this._categories = (await this._kinks.GetKinkCategoriesAsync(this._database)).ToList();
            this._state = KinkWizardState.CategorySelection;

            return await channel.SendMessageAsync(string.Empty, embed: this._loadingEmbed).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task UpdateAsync()
        {
            if (this.Message is null)
            {
                return;
            }

            await this.Message.ModifyAsync(m => m.Embed = this._loadingEmbed);

            foreach (var emote in this.CurrrentlyRejectedEmotes)
            {
                if (!this.Message.Reactions.ContainsKey(emote) || !this.Message.Reactions[emote].IsMe)
                {
                    continue;
                }

                await this.Message.RemoveReactionAsync(emote, this.Interactivity.Client.CurrentUser);
            }

            foreach (var emote in this.AcceptedEmotes)
            {
                if (this.Message.Reactions.ContainsKey(emote) && this.Message.Reactions[emote].IsMe)
                {
                    continue;
                }

                await this.Message.AddReactionAsync(emote);
            }

            var newEmbed = await GetCurrentPageAsync();
            await this.Message.ModifyAsync(m => m.Embed = newEmbed);
        }

        /// <inheritdoc />
        public override Task HandleAddedInteractionAsync(SocketReaction reaction)
        {
            if (reaction.Emote.Equals(Exit))
            {
                this.Interactivity.DeleteInteractiveMessageAsync(this);
            }

            if (reaction.Emote.Equals(Info))
            {
                return DisplayHelpTextAsync();
            }

            switch (this._state)
            {
                case KinkWizardState.CategorySelection:
                {
                    return ConsumeCategoryInteractionAsync(reaction);
                }
                case KinkWizardState.KinkPreference:
                {
                    return ConsumePreferenceInteractionAsync(reaction);
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        private async Task ConsumePreferenceInteractionAsync([NotNull] SocketReaction reaction)
        {
            var emote = reaction.Emote;

            if (emote.Equals(Back))
            {
                this._state = KinkWizardState.CategorySelection;
                await UpdateAsync();
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

                var getNextKinkResult = await this._kinks.GetNextKinkByCurrentFListIDAsync(this._database, this._currentFListKinkID);
                if (!getNextKinkResult.IsSuccess)
                {
                    this._currentFListKinkID = -1;
                    this._state = KinkWizardState.CategorySelection;
                    await this._feedback.SendConfirmationAndDeleteAsync(this.MessageContext, "All done in that category!");
                }
                else
                {
                    this._currentFListKinkID = (int)getNextKinkResult.Entity.FListID;
                }

                await UpdateAsync();
            }
        }

        private async Task ConsumeCategoryInteractionAsync([NotNull] SocketReaction reaction)
        {
            var emote = reaction.Emote;

            if (emote.Equals(Next))
            {
                if (this._currentCategoryOffset + 3 >= this._categories.Count)
                {
                    return;
                }

                this._currentCategoryOffset += 3;
            }
            else if (emote.Equals(Previous))
            {
                if (this._currentCategoryOffset - 3 < 0)
                {
                    this._currentCategoryOffset = 0;
                    return;
                }

                this._currentCategoryOffset -= 3;
            }
            else if (emote.Equals(First))
            {
                if (this._currentCategoryOffset == 0)
                {
                    return;
                }

                this._currentCategoryOffset = 0;
            }
            else if (emote.Equals(Last))
            {
                int newOffset;
                if (this._categories.Count % 3 == 0)
                {
                    newOffset = this._categories.Count - 3;
                }
                else
                {
                    newOffset = this._categories.Count - (this._categories.Count % 3);
                }

                if (newOffset <= this._currentCategoryOffset)
                {
                    return;
                }

                this._currentCategoryOffset = newOffset;
            }
            else if (emote.Equals(EnterCategory))
            {
                bool Filter(IUserMessage m) => m.Author.Id == reaction.UserId;

                if (!this._categories.Any())
                {
                    await this._feedback.SendWarningAndDeleteAsync
                    (
                        this.MessageContext,
                        "There aren't any categories in the database.",
                        TimeSpan.FromSeconds(10)
                    );

                    return;
                }

                await this._feedback.SendConfirmationAndDeleteAsync
                (
                    this.MessageContext,
                    "Please enter a category name.",
                    TimeSpan.FromSeconds(45)
                );

                var messageResult = await this.Interactivity.GetNextMessageAsync
                (
                    this.MessageContext.Channel,
                    Filter,
                    TimeSpan.FromSeconds(45)
                );

                if (messageResult.IsSuccess)
                {
                    var tryStartCategoryResult = await OpenCategory(messageResult.Entity.Content);
                    if (!tryStartCategoryResult.IsSuccess)
                    {
                        await this._feedback.SendWarningAndDeleteAsync
                        (
                            this.MessageContext,
                            tryStartCategoryResult.ErrorReason,
                            TimeSpan.FromSeconds(10)
                        );

                        return;
                    }
                }
            }

            await UpdateAsync();
        }

        private async Task<ExecuteResult> OpenCategory(string categoryName)
        {
            var getCategoryResult = this._categories.Select(c => c.ToString()).BestLevenshteinMatch(categoryName, 0.75);
            if (!getCategoryResult.IsSuccess)
            {
                return ExecuteResult.FromError(getCategoryResult);
            }

            if (!Enum.TryParse<KinkCategory>(getCategoryResult.Entity, true, out var category))
            {
                return ExecuteResult.FromError(CommandError.ParseFailed, "Could not parse kink category.");
            }

            var getKinkResult = await this._kinks.GetFirstKinkWithoutPreferenceInCategoryAsync(this._database, this._targetUser, category);
            if (!getKinkResult.IsSuccess)
            {
                getKinkResult = await this._kinks.GetFirstKinkInCategoryAsync(this._database, category);
            }

            if (!getKinkResult.IsSuccess)
            {
                return ExecuteResult.FromError(getKinkResult);
            }

            var kink = getKinkResult.Entity;
            this._currentFListKinkID = (int)kink.FListID;

            this._state = KinkWizardState.KinkPreference;

            return ExecuteResult.FromSuccess();
        }

        [SuppressMessage("Style", "SA1118", Justification = "Large text blocks.")]
        private async Task DisplayHelpTextAsync()
        {
            var eb = new EmbedBuilder();
            eb.WithColor(Color.DarkPurple);

            switch (this._state)
            {
                case KinkWizardState.CategorySelection:
                {
                    eb.WithTitle("Help: Category selection");
                    eb.AddField
                    (
                        "Usage",
                        "Use the navigation buttons to scroll through the available categories. Select a category by " +
                        $"pressing {EnterCategory} and typing in the name. The search algorithm is quite lenient, so " +
                        "you may find that things work fine even with typos.\n" +
                        "\n" +
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
                        "Set your preference for this kink by pressing one of the following buttons:" +
                        $"\n{Fave} : Favourite" +
                        $"\n{Like} : Like" +
                        $"\n{Maybe} : Maybe" +
                        $"\n{Never} : Never" +
                        $"\n{NoPreference} : No preference\n" +
                        "\n" +
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

            await this._feedback.SendEmbedAndDeleteAsync(this.MessageContext.Channel, eb.Build(), TimeSpan.FromSeconds(30));
        }

        private async Task SetCurrentKinkPreference(KinkPreference preference)
        {
            var getUserKinkResult = await this._kinks.GetUserKinkByFListIDAsync(this._database, this._targetUser, this._currentFListKinkID);
            if (!getUserKinkResult.IsSuccess)
            {
                await this._feedback.SendErrorAndDeleteAsync(this.MessageContext, getUserKinkResult.ErrorReason);
                return;
            }

            var userKink = getUserKinkResult.Entity;
            var setPreferenceResult = await this._kinks.SetKinkPreferenceAsync(this._database, userKink, preference);
            if (!setPreferenceResult.IsSuccess)
            {
                await this._feedback.SendErrorAndDeleteAsync(this.MessageContext, setPreferenceResult.ErrorReason);
            }
        }

        /// <summary>
        /// Gets the emotes that are associated with the current page.
        /// </summary>
        /// <returns>A set of emotes.</returns>
        public IEnumerable<IEmote> GetCurrentPageEmotes()
        {
            switch (this._state)
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
            switch (this._state)
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
            switch (this._state)
            {
                case KinkWizardState.CategorySelection:
                {
                    var eb = this._feedback.CreateEmbedBase();
                    eb.WithTitle("Category selection");

                    if (this._categories.Any())
                    {
                        eb.WithDescription("Select from one of the categories below.");
                        var categories = this._categories.Skip(this._currentCategoryOffset).Take(3).ToList();
                        foreach (var category in categories)
                        {
                            eb.AddField(category.ToString().Humanize().Transform(To.TitleCase), category.Humanize());
                        }

                        eb.WithFooter($"Categories {this._currentCategoryOffset}-{this._currentCategoryOffset + categories.Count} / {this._categories.Count}");
                    }
                    else
                    {
                        eb.WithDescription("There aren't any categories in the database.");
                    }

                    return eb.Build();
                }
                case KinkWizardState.KinkPreference:
                {
                    var getUserKinkResult = await this._kinks.GetUserKinkByFListIDAsync(this._database, this._targetUser, this._currentFListKinkID);
                    if (!getUserKinkResult.IsSuccess)
                    {
                        await this._feedback.SendErrorAndDeleteAsync(this.MessageContext, "Failed to get the user kink.", TimeSpan.FromSeconds(10));
                        this._state = KinkWizardState.CategorySelection;

                        // Recursively calling at this point is safe, since we will get the emotes from the category page.
                        return await GetCurrentPageAsync();
                    }

                    var userKink = getUserKinkResult.Entity;
                    return this._kinks.BuildUserKinkInfoEmbedBase(userKink).Build();
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
