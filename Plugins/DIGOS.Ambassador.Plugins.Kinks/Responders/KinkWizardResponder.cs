//
//  KinkWizardResponder.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Interactivity.Responders;
using DIGOS.Ambassador.Discord.Pagination.Extensions;
using DIGOS.Ambassador.Plugins.Kinks.Model;
using DIGOS.Ambassador.Plugins.Kinks.Services;
using DIGOS.Ambassador.Plugins.Kinks.Wizards;
using Humanizer;
using Remora.Commands.Results;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Kinks.Responders
{
    /// <summary>
    /// Handles interactions with kink wizards.
    /// </summary>
    public class KinkWizardResponder :
        InteractivityResponder,
        IResponder<IMessageReactionAdd>,
        IResponder<IMessageReactionRemove>
    {
        private readonly KinkService _kinks;
        private readonly UserFeedbackService _feedback;
        private readonly IDiscordRestChannelAPI _channelAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinkWizardResponder"/> class.
        /// </summary>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="kinks">The kink service.</param>
        /// <param name="channelAPI">The channel API.</param>
        /// <param name="feedback">The user feedback service.</param>
        public KinkWizardResponder
        (
            InteractivityService interactivity,
            KinkService kinks,
            IDiscordRestChannelAPI channelAPI,
            UserFeedbackService feedback
        )
            : base(interactivity)
        {
            _kinks = kinks;
            _channelAPI = channelAPI;
            _feedback = feedback;
        }

        /// <inheritdoc />
        public override async Task<Result> OnCreateAsync(string nonce, CancellationToken ct = default)
        {
            if (!this.Interactivity.TryGetInteractiveEntity<KinkWizard>(nonce, out var message))
            {
                return Result.FromSuccess();
            }

            try
            {
                await message.Semaphore.WaitAsync(ct);

                message.Categories = (await _kinks.GetKinkCategoriesAsync(ct)).ToList();
                message.State = KinkWizardState.CategorySelection;

                return await UpdateAsync(message, ct);
            }
            finally
            {
                message.Semaphore.Release();
            }
        }

        /// <inheritdoc />
        public Task<Result> RespondAsync(IMessageReactionAdd gatewayEvent, CancellationToken ct = default)
            => OnReactionAsync(gatewayEvent.UserID, gatewayEvent.MessageID, gatewayEvent.Emoji, ct);

        /// <inheritdoc />
        public Task<Result> RespondAsync(IMessageReactionRemove gatewayEvent, CancellationToken ct = default)
            => OnReactionAsync
            (
                gatewayEvent.UserID,
                gatewayEvent.MessageID,
                gatewayEvent.Emoji,
                ct
            );

        /// <summary>
        /// Handles an added reaction.
        /// </summary>
        /// <param name="userID">The ID of the reacting user.</param>
        /// <param name="messageID">The ID of the message.</param>
        /// <param name="emoji">The emoji used.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        private async Task<Result> OnReactionAsync
        (
            Snowflake userID,
            Snowflake messageID,
            IPartialEmoji emoji,
            CancellationToken ct = default
        )
        {
            if (!this.Interactivity.TryGetInteractiveEntity<KinkWizard>(messageID.ToString(), out var wizard))
            {
                return Result.FromSuccess();
            }

            try
            {
                await wizard.Semaphore.WaitAsync(ct);

                if (userID != wizard.SourceUserID)
                {
                    // We handled it, but we won't react
                    return Result.FromSuccess();
                }

                var reactionName = emoji.GetEmojiName();
                if (!wizard.ReactionNames.TryGetValue(reactionName, out var knownEmoji))
                {
                    // This isn't an emoji we react to
                    return Result.FromSuccess();
                }

                if (!wizard.GetCurrentPageEmotes().Contains(knownEmoji))
                {
                    // This isn't an emoji we react to (on this page)
                    return Result.FromSuccess();
                }

                // Special actions
                if (knownEmoji.Equals(wizard.Exit))
                {
                    return await _channelAPI.DeleteMessageAsync(wizard.ChannelID, wizard.MessageID, ct);
                }

                if (knownEmoji.Equals(wizard.Info))
                {
                    return await DisplayHelpTextAsync(wizard, ct);
                }

                return wizard.State switch
                {
                    KinkWizardState.CategorySelection => await ConsumeCategoryInteractionAsync(wizard, knownEmoji, ct),
                    KinkWizardState.KinkPreference => await ConsumePreferenceInteractionAsync(wizard, knownEmoji, ct),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            finally
            {
                wizard.Semaphore.Release();
            }
        }

        private async Task<Result> ConsumePreferenceInteractionAsync
        (
            KinkWizard wizard,
            IEmoji emoji,
            CancellationToken ct = default
        )
        {
            if (emoji.Equals(wizard.Back))
            {
                wizard.State = KinkWizardState.CategorySelection;
                return await UpdateAsync(wizard, ct);
            }

            var preference = emoji switch
            {
                _ when emoji.Equals(wizard.Fave) => KinkPreference.Favourite,
                _ when emoji.Equals(wizard.Like) => KinkPreference.Like,
                _ when emoji.Equals(wizard.Maybe) => KinkPreference.Maybe,
                _ when emoji.Equals(wizard.Never) => KinkPreference.No,
                _ when emoji.Equals(wizard.NoPreference) => KinkPreference.NoPreference,
                _ => throw new ArgumentOutOfRangeException()
            };

            var setPreference = await SetCurrentKinkPreference(wizard, preference);
            if (!setPreference.IsSuccess)
            {
                return setPreference;
            }

            if (wizard.CurrentFListKinkID is null)
            {
                throw new InvalidOperationException();
            }

            var getNextKinkResult = await _kinks.GetNextKinkByCurrentFListIDAsync(wizard.CurrentFListKinkID.Value, ct);
            if (!getNextKinkResult.IsSuccess)
            {
                wizard.CurrentFListKinkID = null;
                wizard.State = KinkWizardState.CategorySelection;

                var send = await _feedback.SendConfirmationAsync
                (
                    wizard.ChannelID,
                    wizard.SourceUserID,
                    "All done in that category!",
                    ct
                );

                if (!send.IsSuccess)
                {
                    return Result.FromError(send);
                }
            }
            else
            {
                wizard.CurrentFListKinkID = getNextKinkResult.Entity.FListID;
            }

            return await UpdateAsync(wizard, ct);
        }

        private async Task<Result> ConsumeCategoryInteractionAsync
        (
            KinkWizard wizard,
            IEmoji emoji,
            CancellationToken ct = default
        )
        {
            var didPageChange = false;
            if (emoji.Equals(wizard.Back))
            {
                didPageChange = wizard.MoveNext();
            }
            else if (emoji.Equals(wizard.Previous))
            {
                didPageChange = wizard.MovePrevious();
            }
            else if (emoji.Equals(wizard.First))
            {
                didPageChange = wizard.MoveFirst();
            }
            else if (emoji.Equals(wizard.Last))
            {
                didPageChange = wizard.MoveLast();
            }
            else if (emoji.Equals(wizard.EnterCategory))
            {
                if (!wizard.Categories.Any())
                {
                    var sendWarning = await _feedback.SendWarningAsync
                    (
                        wizard.ChannelID,
                        wizard.SourceUserID,
                        "There aren't any categories in the database.",
                        ct
                    );

                    return sendWarning.IsSuccess
                        ? Result.FromSuccess()
                        : Result.FromError(sendWarning);
                }

                var sendConfirmation = await _feedback.SendConfirmationAsync
                (
                    wizard.ChannelID,
                    wizard.SourceUserID,
                    "Please enter a category name.",
                    ct
                );

                if (!sendConfirmation.IsSuccess)
                {
                    return Result.FromError(sendConfirmation);
                }

                var getNextMessage = await this.Interactivity.GetNextMessageAsync
                (
                    wizard.ChannelID,
                    TimeSpan.FromSeconds(45),
                    ct
                );

                if (!getNextMessage.IsSuccess)
                {
                    return Result.FromError(getNextMessage);
                }

                var nextMessage = getNextMessage.Entity;

                if (nextMessage is null)
                {
                    return await UpdateAsync(wizard, ct);
                }

                var tryStartCategoryResult = await OpenCategory(wizard, nextMessage.Content);
                if (tryStartCategoryResult.IsSuccess)
                {
                    return await UpdateAsync(wizard, ct);
                }

                var send = await _feedback.SendWarningAsync
                (
                    wizard.ChannelID,
                    wizard.SourceUserID,
                    tryStartCategoryResult.Unwrap().Message,
                    ct
                );

                return !send.IsSuccess
                    ? Result.FromError(send)
                    : tryStartCategoryResult;
            }

            return didPageChange
                ? await UpdateAsync(wizard, ct)
                : Result.FromSuccess();
        }

        private async Task<Result> OpenCategory(KinkWizard wizard, string categoryName)
        {
            var getCategoryResult = wizard.Categories.Select(c => c.ToString()).BestLevenshteinMatch(categoryName, 0.75);
            if (!getCategoryResult.IsSuccess)
            {
                return Result.FromError(getCategoryResult);
            }

            if (!Enum.TryParse<KinkCategory>(getCategoryResult.Entity, true, out var category))
            {
                return new ParsingError<KinkCategory>("Could not parse kink category.");
            }

            var getKinkResult = await _kinks.GetFirstKinkWithoutPreferenceInCategoryAsync(wizard.SourceUserID, category);
            if (!getKinkResult.IsSuccess)
            {
                getKinkResult = await _kinks.GetFirstKinkInCategoryAsync(category);
            }

            if (!getKinkResult.IsSuccess)
            {
                return Result.FromError(getKinkResult);
            }

            var kink = getKinkResult.Entity;
            wizard.CurrentFListKinkID = kink.FListID;
            wizard.State = KinkWizardState.KinkPreference;

            return Result.FromSuccess();
        }

        [SuppressMessage("Style", "SA1118", Justification = "Large text blocks.")]
        private async Task<Result> DisplayHelpTextAsync(KinkWizard wizard, CancellationToken ct = default)
        {
            var eb = new Embed
            {
                Colour = Color.MediumPurple
            };

            switch (wizard.State)
            {
                case KinkWizardState.CategorySelection:
                {
                    var fields = new List<IEmbedField>
                    {
                        new EmbedField
                        (
                            "Usage",
                            "Use the navigation buttons to scroll through the available categories. Select a " +
                            $"category by pressing :{wizard.EnterCategory.GetEmojiName()}: and typing in the name. " +
                            "The search algorithm is quite lenient, so you may find that things work fine even with " +
                            "typos.\n" +
                            "\n" +
                            $"You can quit at any point by pressing :{wizard.Exit.GetEmojiName()}:."
                        )
                    };

                    eb = eb with
                    {
                        Title = "Help: Category selection",
                        Fields = fields
                    };

                    break;
                }
                case KinkWizardState.KinkPreference:
                {
                    var fields = new List<IEmbedField>
                    {
                        new EmbedField
                        (
                            "Usage",
                            "Set your preference for this kink by pressing one of the following buttons:" +
                            $"\n:{wizard.Fave.GetEmojiName()}: : Favourite" +
                            $"\n:{wizard.Like.GetEmojiName()}: : Like" +
                            $"\n:{wizard.Maybe.GetEmojiName()}: : Maybe" +
                            $"\n:{wizard.Never.GetEmojiName()}: : Never" +
                            $"\n:{wizard.NoPreference.GetEmojiName()}: : No preference\n" +
                            "\n" +
                            $"\nPress :{wizard.Back.GetEmojiName()}: to go back to the categories." +
                            $"\nYou can quit at any point by pressing :{wizard.Exit.GetEmojiName()}:."
                        )
                    };

                    eb = eb with
                    {
                        Title = "Help: Kink preference",
                        Fields = fields
                    };

                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            var send = await _feedback.SendEmbedAsync(wizard.ChannelID, eb, ct);
            return send.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(send);
        }

        private async Task<Result> SetCurrentKinkPreference(KinkWizard wizard, KinkPreference preference)
        {
            if (wizard.CurrentFListKinkID is null)
            {
                throw new InvalidOperationException();
            }

            var getUserKinkResult = await _kinks.GetUserKinkByFListIDAsync
            (
                wizard.SourceUserID,
                wizard.CurrentFListKinkID.Value
            );

            if (!getUserKinkResult.IsSuccess)
            {
                return Result.FromError(getUserKinkResult);
            }

            var userKink = getUserKinkResult.Entity;
            return await _kinks.SetKinkPreferenceAsync(userKink, preference);
        }

        /// <summary>
        /// Gets an embed that represents the current page.
        /// </summary>
        /// <returns>The embed.</returns>
        private async Task<Result<Embed>> GetCurrentPageAsync(KinkWizard wizard, CancellationToken ct = default)
        {
            switch (wizard.State)
            {
                case KinkWizardState.CategorySelection:
                {
                    var eb = new Embed
                    {
                        Title = "Category selection",
                        Colour = Color.MediumPurple
                    };

                    if (wizard.Categories.Any())
                    {
                        var visibleCategories = wizard.Categories.Skip(wizard.CurrentCategoryOffset).Take(3).ToList();
                        var visibleCategoryFields = visibleCategories.Select
                        (
                            c => new EmbedField(c.ToString().Humanize().Transform(To.TitleCase), c.Humanize())
                        ).ToList();

                        var offset = wizard.CurrentCategoryOffset;
                        eb = eb with
                        {
                            Description = "Select from one of the categories below.",
                            Fields = visibleCategoryFields,
                            Footer = new EmbedFooter
                            (
                                $"Categories {offset}-{offset + categories.Count} / {wizard.Categories.Count}"
                            )
                        };
                    }
                    else
                    {
                        eb = eb with
                        {
                            Description = "There aren't any categories in the database."
                        };
                    }

                    return eb;
                }
                case KinkWizardState.KinkPreference:
                {
                    if (wizard.CurrentFListKinkID is null)
                    {
                        throw new InvalidOperationException();
                    }

                    var getUserKinkResult = await _kinks.GetUserKinkByFListIDAsync
                    (
                        wizard.SourceUserID,
                        wizard.CurrentFListKinkID.Value,
                        ct
                    );

                    if (!getUserKinkResult.IsSuccess)
                    {
                        var sendError = await _feedback.SendErrorAsync
                        (
                            wizard.ChannelID,
                            wizard.SourceUserID,
                            "Failed to get the user kink.",
                            ct
                        );

                        if (!sendError.IsSuccess)
                        {
                            return Result<Embed>.FromError(sendError);
                        }

                        wizard.State = KinkWizardState.CategorySelection;

                        // Recursively calling at this point is safe, since we will get the emojis from the category page.
                        return await GetCurrentPageAsync(wizard, ct);
                    }

                    var userKink = getUserKinkResult.Entity;
                    return _kinks.BuildUserKinkInfoEmbedBase(userKink);
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Updates the contents of the wizard.
        /// </summary>
        /// <param name="wizard">The wizard.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        private async Task<Result> UpdateAsync(KinkWizard wizard, CancellationToken ct = default)
        {
            var updateButtons = await UpdateReactionButtonsAsync(wizard, ct);
            if (!updateButtons.IsSuccess)
            {
                return updateButtons;
            }

            var getPage = await GetCurrentPageAsync(wizard, ct);
            if (!getPage.IsSuccess)
            {
                return Result.FromError(getPage);
            }

            var page = getPage.Entity;

            var modifyMessage = await _channelAPI.EditMessageAsync
            (
                wizard.ChannelID,
                wizard.MessageID,
                embed: page,
                ct: ct
            );

            return modifyMessage.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(modifyMessage);
        }

        /// <summary>
        /// Updates the displayed buttons.
        /// </summary>
        /// <param name="wizard">The wizard.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        private async Task<Result> UpdateReactionButtonsAsync(KinkWizard wizard, CancellationToken ct = default)
        {
            var getMessage = await _channelAPI.GetChannelMessageAsync(wizard.ChannelID, wizard.MessageID, ct);
            if (!getMessage.IsSuccess)
            {
                return Result.FromError(getMessage);
            }

            var message = getMessage.Entity;
            var existingReactions = message.Reactions;

            if (existingReactions.HasValue)
            {
                var removeReactions = await _channelAPI.DeleteAllReactionsAsync
                (
                    wizard.ChannelID,
                    wizard.MessageID,
                    ct
                );

                if (!removeReactions.IsSuccess)
                {
                    return removeReactions;
                }
            }

            foreach (var reaction in wizard.GetCurrentPageEmotes().Select(e => e.GetEmojiName()))
            {
                if (existingReactions.HasValue)
                {
                    if (existingReactions.Value!.Any(r => r.Emoji.GetEmojiName() == reaction))
                    {
                        // This one is already added; skip it
                        continue;
                    }
                }

                var addReaction = await _channelAPI.CreateReactionAsync
                (
                    wizard.ChannelID,
                    wizard.MessageID,
                    reaction,
                    ct
                );

                if (!addReaction.IsSuccess)
                {
                    return addReaction;
                }
            }

            return Result.FromSuccess();
        }
    }
}
