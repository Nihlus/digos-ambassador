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
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity.Messages;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Plugins.Kinks.Model;
using DIGOS.Ambassador.Plugins.Kinks.Services;
using Discord;
using Discord.WebSocket;
using Humanizer;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Kinks.Wizards
{
    /// <summary>
    /// Acts as an interactive wizard for interactively setting the kink preferences of users.
    /// </summary>
    public class KinkWizard : InteractiveMessage, IWizard
    {
        [ProvidesContext]
        private readonly KinksDatabaseContext _database;
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
            KinksDatabaseContext database,
            UserFeedbackService feedback,
            KinkService kinkService,
            IUser targetUser
        )
            : base(targetUser)
        {
            _database = database;
            _feedback = feedback;
            _kinks = kinkService;

            _targetUser = targetUser;

            _state = KinkWizardState.CategorySelection;

            var eb = new EmbedBuilder();
            eb.WithTitle("Kink Wizard");
            eb.WithDescription("Loading...");

            _loadingEmbed = eb.Build();
        }

        /// <inheritdoc />
        protected override async Task<IUserMessage> DisplayAsync([NotNull] IMessageChannel channel)
        {
            if (!(this.Message is null))
            {
                throw new InvalidOperationException("The wizard is already active in a channel.");
            }

            _categories = (await _kinks.GetKinkCategoriesAsync()).ToList();
            _state = KinkWizardState.CategorySelection;

            return await channel.SendMessageAsync(string.Empty, embed: _loadingEmbed).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task UpdateAsync()
        {
            if (this.Message is null)
            {
                return;
            }

            await this.Message.ModifyAsync(m => m.Embed = _loadingEmbed);

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

            switch (_state)
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
                _state = KinkWizardState.CategorySelection;
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

                var getNextKinkResult = await _kinks.GetNextKinkByCurrentFListIDAsync(_currentFListKinkID);
                if (!getNextKinkResult.IsSuccess)
                {
                    _currentFListKinkID = -1;
                    _state = KinkWizardState.CategorySelection;
                    await _feedback.SendConfirmationAndDeleteAsync(this.MessageContext, "All done in that category!");
                }
                else
                {
                    _currentFListKinkID = (int)getNextKinkResult.Entity.FListID;
                }

                await UpdateAsync();
            }
        }

        private async Task ConsumeCategoryInteractionAsync([NotNull] SocketReaction reaction)
        {
            var emote = reaction.Emote;

            if (emote.Equals(Next))
            {
                if (_currentCategoryOffset + 3 >= _categories.Count)
                {
                    return;
                }

                _currentCategoryOffset += 3;
            }
            else if (emote.Equals(Previous))
            {
                if (_currentCategoryOffset - 3 < 0)
                {
                    _currentCategoryOffset = 0;
                    return;
                }

                _currentCategoryOffset -= 3;
            }
            else if (emote.Equals(First))
            {
                if (_currentCategoryOffset == 0)
                {
                    return;
                }

                _currentCategoryOffset = 0;
            }
            else if (emote.Equals(Last))
            {
                int newOffset;
                if (_categories.Count % 3 == 0)
                {
                    newOffset = _categories.Count - 3;
                }
                else
                {
                    newOffset = _categories.Count - (_categories.Count % 3);
                }

                if (newOffset <= _currentCategoryOffset)
                {
                    return;
                }

                _currentCategoryOffset = newOffset;
            }
            else if (emote.Equals(EnterCategory))
            {
                bool Filter(IUserMessage m) => m.Author.Id == reaction.UserId;

                if (!_categories.Any())
                {
                    await _feedback.SendWarningAndDeleteAsync
                    (
                        this.MessageContext,
                        "There aren't any categories in the database.",
                        TimeSpan.FromSeconds(10)
                    );

                    return;
                }

                await _feedback.SendConfirmationAndDeleteAsync
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
                        await _feedback.SendWarningAndDeleteAsync
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

        private async Task<ModifyEntityResult> OpenCategory(string categoryName)
        {
            var getCategoryResult = _categories.Select(c => c.ToString()).BestLevenshteinMatch(categoryName, 0.75);
            if (!getCategoryResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getCategoryResult);
            }

            if (!Enum.TryParse<KinkCategory>(getCategoryResult.Entity, true, out var category))
            {
                return ModifyEntityResult.FromError("Could not parse kink category.");
            }

            var getKinkResult = await _kinks.GetFirstKinkWithoutPreferenceInCategoryAsync(_targetUser, category);
            if (!getKinkResult.IsSuccess)
            {
                getKinkResult = await _kinks.GetFirstKinkInCategoryAsync(category);
            }

            if (!getKinkResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getKinkResult);
            }

            var kink = getKinkResult.Entity;
            _currentFListKinkID = (int)kink.FListID;

            _state = KinkWizardState.KinkPreference;

            return ModifyEntityResult.FromSuccess();
        }

        [SuppressMessage("Style", "SA1118", Justification = "Large text blocks.")]
        private async Task DisplayHelpTextAsync()
        {
            var eb = new EmbedBuilder();
            eb.WithColor(Color.DarkPurple);

            switch (_state)
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

            await _feedback.SendEmbedAndDeleteAsync(this.MessageContext.Channel, eb.Build(), TimeSpan.FromSeconds(30));
        }

        private async Task SetCurrentKinkPreference(KinkPreference preference)
        {
            var getUserKinkResult = await _kinks.GetUserKinkByFListIDAsync(_targetUser, _currentFListKinkID);
            if (!getUserKinkResult.IsSuccess)
            {
                await _feedback.SendErrorAndDeleteAsync(this.MessageContext, getUserKinkResult.ErrorReason);
                return;
            }

            var userKink = getUserKinkResult.Entity;
            var setPreferenceResult = await _kinks.SetKinkPreferenceAsync(userKink, preference);
            if (!setPreferenceResult.IsSuccess)
            {
                await _feedback.SendErrorAndDeleteAsync(this.MessageContext, setPreferenceResult.ErrorReason);
            }
        }

        /// <summary>
        /// Gets the emotes that are associated with the current page.
        /// </summary>
        /// <returns>A set of emotes.</returns>
        public IEnumerable<IEmote> GetCurrentPageEmotes()
        {
            switch (_state)
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
            switch (_state)
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
            switch (_state)
            {
                case KinkWizardState.CategorySelection:
                {
                    var eb = _feedback.CreateEmbedBase();
                    eb.WithTitle("Category selection");

                    if (_categories.Any())
                    {
                        eb.WithDescription("Select from one of the categories below.");
                        var categories = _categories.Skip(_currentCategoryOffset).Take(3).ToList();
                        foreach (var category in categories)
                        {
                            eb.AddField(category.ToString().Humanize().Transform(To.TitleCase), category.Humanize());
                        }

                        eb.WithFooter($"Categories {_currentCategoryOffset}-{_currentCategoryOffset + categories.Count} / {_categories.Count}");
                    }
                    else
                    {
                        eb.WithDescription("There aren't any categories in the database.");
                    }

                    return eb.Build();
                }
                case KinkWizardState.KinkPreference:
                {
                    var getUserKinkResult = await _kinks.GetUserKinkByFListIDAsync(_targetUser, _currentFListKinkID);
                    if (!getUserKinkResult.IsSuccess)
                    {
                        await _feedback.SendErrorAndDeleteAsync(this.MessageContext, "Failed to get the user kink.", TimeSpan.FromSeconds(10));
                        _state = KinkWizardState.CategorySelection;

                        // Recursively calling at this point is safe, since we will get the emotes from the category page.
                        return await GetCurrentPageAsync();
                    }

                    var userKink = getUserKinkResult.Entity;
                    return _kinks.BuildUserKinkInfoEmbedBase(userKink).Build();
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
