//
//  KinkWizardInteractions.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Kinks.Model;
using DIGOS.Ambassador.Plugins.Kinks.Services;
using DIGOS.Ambassador.Plugins.Kinks.Wizards;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Discord.Interactivity.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Kinks.Interactions;

/// <summary>
/// Defines interaction logic for kink wizards.
/// </summary>
[Group("kink-wizard")]
internal class KinkWizardInteractions : InteractionGroup
{
    private readonly KinkService _kinks;
    private readonly InMemoryDataService<Snowflake, KinkWizard> _dataService;
    private readonly FeedbackService _feedback;
    private readonly IDiscordRestInteractionAPI _interactionAPI;
    private readonly InteractionContext _context;
    private readonly IDiscordRestChannelAPI _channelAPI;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinkWizardInteractions"/> class.
    /// </summary>
    /// <param name="kinks">The kink service.</param>
    /// <param name="dataService">The in-memory data service.</param>
    /// <param name="feedback">The user feedback service.</param>
    /// <param name="channelAPI">The channel API.</param>
    /// <param name="interactionAPI">The interaction API.</param>
    /// <param name="context">The interaction context.</param>
    public KinkWizardInteractions
    (
        KinkService kinks,
        InMemoryDataService<Snowflake, KinkWizard> dataService,
        FeedbackService feedback,
        IDiscordRestChannelAPI channelAPI,
        IDiscordRestInteractionAPI interactionAPI,
        InteractionContext context
    )
    {
        _kinks = kinks;
        _feedback = feedback;
        _interactionAPI = interactionAPI;
        _context = context;
        _dataService = dataService;
        _channelAPI = channelAPI;
    }

    /// <summary>
    /// Sets the user's preference for the displayed kink as a favourite.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Button("favourite")]
    public Task<Result> FavouriteClickedAsync() => SetCurrentKinkPreference(KinkPreference.Favourite);

    /// <summary>
    /// Sets the user's preference for the displayed kink as something they like.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Button("like")]
    public Task<Result> LikeClickedAsync() => SetCurrentKinkPreference(KinkPreference.Like);

    /// <summary>
    /// Sets the user's preference for the displayed kink as something they might enjoy.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Button("maybe")]
    public Task<Result> MaybeClickedAsync() => SetCurrentKinkPreference(KinkPreference.Maybe);

    /// <summary>
    /// Sets the user's preference for the displayed kink as something they will never interact with.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Button("no")]
    public Task<Result> NoClickedAsync() => SetCurrentKinkPreference(KinkPreference.No);

    /// <summary>
    /// Sets the user's preference for the displayed kink as having no strong feelings one way or the other.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Button("no-preference")]
    public Task<Result> NoPreferenceClickedAsync() => SetCurrentKinkPreference(KinkPreference.NoPreference);

    private async Task<Result> SetCurrentKinkPreference(KinkPreference preference)
    {
        var leaseData = await _dataService.LeaseDataAsync(_context.Message.Value.ID, this.CancellationToken);
        if (!leaseData.IsSuccess)
        {
            return (Result)leaseData;
        }

        await using var lease = leaseData.Entity;

        if (lease.Data.CurrentFListKinkID is null)
        {
            throw new InvalidOperationException();
        }

        var getUserKinkResult = await _kinks.GetUserKinkByFListIDAsync
        (
            lease.Data.SourceUserID,
            lease.Data.CurrentFListKinkID.Value
        );

        if (!getUserKinkResult.IsSuccess)
        {
            return Result.FromError(getUserKinkResult);
        }

        var userKink = getUserKinkResult.Entity;
        var setPreference = await _kinks.SetKinkPreferenceAsync(userKink, preference);
        if (!setPreference.IsSuccess)
        {
            return setPreference;
        }

        var moveNext = await lease.Data.MoveToNextKinkInCategoryAsync(_kinks, this.CancellationToken);
        if (!moveNext.IsDefined(out var hasMore))
        {
            return (Result)moveNext;
        }

        if (hasMore)
        {
            return await UpdateAsync(lease.Data);
        }

        lease.Data.GoToCategorySelection();

        var send = await _feedback.SendContextualNeutralAsync
        (
            "All done in that category!",
            lease.Data.SourceUserID,
            new FeedbackMessageOptions(MessageFlags: MessageFlags.Ephemeral),
            ct: this.CancellationToken
        );

        return send.IsSuccess
            ? await UpdateAsync(lease.Data)
            : (Result)send;
    }

    /// <summary>
    /// Moves back to the category selection.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Button("back")]
    public Task<Result> BackClickedAsync() => NavigateAsync(w => w.GoToCategorySelection());

    /// <summary>
    /// Moves the category view to the first page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Button("first")]
    public Task<Result> FirstClickedAsync() => NavigateAsync(w => w.MoveFirst());

    /// <summary>
    /// Moves the category view to the previous page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Button("previous")]
    public Task<Result> PreviousClickedAsync() => NavigateAsync(w => w.MovePrevious());

    /// <summary>
    /// Moves the category view to the next page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Button("next")]
    public Task<Result> NextClickedAsync() => NavigateAsync(w => w.MoveNext());

    /// <summary>
    /// Moves the category view to the last page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Button("last")]
    public Task<Result> LastClickedAsync() => NavigateAsync(w => w.MoveLast());

    /// <summary>
    /// Enters a category provided by the user.
    /// </summary>
    /// <param name="values">The selected values.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [SelectMenu("category-selection")]
    public async Task<Result> EnterCategoryClickedAsync(IReadOnlyList<string> values)
    {
        var leaseData = await _dataService.LeaseDataAsync(_context.Message.Value.ID, this.CancellationToken);
        if (!leaseData.IsSuccess)
        {
            return (Result)leaseData;
        }

        await using var lease = leaseData.Entity;

        if (!lease.Data.Categories.Any())
        {
            var sendWarning = await _feedback.SendContextualWarningAsync
            (
                "There aren't any categories in the database.",
                lease.Data.SourceUserID,
                new FeedbackMessageOptions(MessageFlags: MessageFlags.Ephemeral),
                ct: this.CancellationToken
            );

            return sendWarning.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(sendWarning);
        }

        var categoryName = values.Single();

        var tryStartCategoryResult = await lease.Data.OpenCategoryAsync(_kinks, categoryName, this.CancellationToken);
        if (tryStartCategoryResult.IsSuccess)
        {
            return await UpdateAsync(lease.Data);
        }

        var send = await _feedback.SendContextualWarningAsync
        (
            tryStartCategoryResult.Error.Message,
            lease.Data.SourceUserID,
            new FeedbackMessageOptions(MessageFlags: MessageFlags.Ephemeral),
            ct: this.CancellationToken
        );

        return !send.IsSuccess
            ? Result.FromError(send)
            : tryStartCategoryResult;
    }

    /// <summary>
    /// Closes the wizard.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Button("exit")]
    public async Task<Result> ExitClickedAsync()
    {
        var leaseData = await _dataService.LeaseDataAsync(_context.Message.Value.ID, this.CancellationToken);
        if (!leaseData.IsSuccess)
        {
            return (Result)leaseData;
        }

        await using var lease = leaseData.Entity;

        lease.Delete();

        if (lease.Data.WasCreatedWithInteraction)
        {
            return await _interactionAPI.DeleteOriginalInteractionResponseAsync
            (
                _context.ApplicationID,
                _context.Token,
                this.CancellationToken
            );
        }

        var message = _context.Message.Value;
        return await _channelAPI.DeleteMessageAsync(message.ChannelID, message.ID, ct: this.CancellationToken);
    }

    /// <summary>
    /// Displays help related to the wizard's current page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Button("info")]
    public async Task<Result> HelpClickedAsync()
    {
        var leaseData = await _dataService.LeaseDataAsync(_context.Message.Value.ID, this.CancellationToken);
        if (!leaseData.IsSuccess)
        {
            return (Result)leaseData;
        }

        await using var lease = leaseData.Entity;

        return await DisplayHelpTextAsync(lease.Data);
    }

    /// <summary>
    /// Performs some type of navigation action on the data, updating the page afterwards.
    /// </summary>
    /// <param name="navigator">The navigator action.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private Task<Result> NavigateAsync(Action<KinkWizard> navigator) => NavigateAsync
    (
        w =>
        {
            navigator(w);
            return true;
        }
    );

    /// <summary>
    /// Performs some type of navigation action on the data, optionally updating the page afterwards.
    /// </summary>
    /// <param name="navigator">The navigator action.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task<Result> NavigateAsync(Func<KinkWizard, bool> navigator)
    {
        var leaseData = await _dataService.LeaseDataAsync(_context.Message.Value.ID, this.CancellationToken);
        if (!leaseData.IsSuccess)
        {
            return (Result)leaseData;
        }

        await using var lease = leaseData.Entity;
        var didPageChange = navigator(lease.Data);

        if (!didPageChange)
        {
            return Result.FromSuccess();
        }

        return await UpdateAsync(lease.Data);
    }

    /// <summary>
    /// Displays an ephemeral help message relevant to what's currently displayed.
    /// </summary>
    /// <param name="data">The current data state.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [SuppressMessage("Style", "SA1118", Justification = "Large text blocks.")]
    private async Task<Result> DisplayHelpTextAsync(KinkWizard data)
    {
        var eb = new Embed
        {
            Colour = Color.MediumPurple
        };

        switch (data.State)
        {
            case KinkWizardState.CategorySelection:
            {
                var fields = new List<IEmbedField>
                {
                    new EmbedField
                    (
                        "Usage",
                        "Use the navigation buttons to scroll through the available categories. Select a "
                        + "category via the dropdown to start setting preferences for the kinks in that category.\n"
                        + "\n"
                        + $"You can quit at any point by pressing {data.Exit.Emoji.Value.Name.Value}."
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
                        "Set your preference for this kink by pressing one of the following buttons:"
                        + $"\n{data.Favourite.Emoji.Value.Name.Value} : Favourite"
                        + $"\n{data.Like.Emoji.Value.Name.Value} : Like"
                        + $"\n{data.Maybe.Emoji.Value.Name.Value} : Maybe"
                        + $"\n{data.No.Emoji.Value.Name.Value} : Never"
                        + $"\n{data.NoPreference.Emoji.Value.Name.Value} : No preference\n"
                        + "\n"
                        + $"\nPress {data.Back.Emoji.Value.Name.Value} to go back to the categories."
                        + $"\nYou can quit at any point by pressing {data.Exit.Emoji.Value.Name.Value}."
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
                throw new ArgumentOutOfRangeException(nameof(data.State));
            }
        }

        return (Result)await _feedback.SendContextualEmbedAsync
        (
            eb,
            new FeedbackMessageOptions(MessageFlags: MessageFlags.Ephemeral),
            this.CancellationToken
        );
    }

    /// <summary>
    /// Updates the contents of the wizard.
    /// </summary>
    /// <param name="data">The wizard's state.</param>
    /// <returns>A result which may or may not have succeeded.</returns>
    private async Task<Result> UpdateAsync
    (
        KinkWizard data
    )
    {
        var getPage = await data.GetCurrentPageAsync(_kinks, this.CancellationToken);
        if (!getPage.IsSuccess)
        {
            return Result.FromError(getPage);
        }

        var page = getPage.Entity;

        if (data.WasCreatedWithInteraction)
        {
            return (Result)await _interactionAPI.EditOriginalInteractionResponseAsync
            (
                _context.ApplicationID,
                _context.Token,
                embeds: new[] { page },
                components: new Optional<IReadOnlyList<IMessageComponent>?>(data.GetCurrentPageComponents()),
                ct: this.CancellationToken
            );
        }

        var message = _context.Message.Value;
        return (Result)await _channelAPI.EditMessageAsync
        (
            message.ChannelID,
            message.ID,
            embeds: new[] { page },
            components: new Optional<IReadOnlyList<IMessageComponent>?>(data.GetCurrentPageComponents()),
            ct: this.CancellationToken
        );
    }
}
