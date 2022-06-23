//
//  KinkWizardEntity.cs
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
using DIGOS.Ambassador.Plugins.Kinks.Model;
using DIGOS.Ambassador.Plugins.Kinks.Services;
using DIGOS.Ambassador.Plugins.Kinks.Wizards;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Rest.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Kinks.InteractiveEntities;

/// <summary>
/// Handles interactions with kink wizards.
/// </summary>
public class KinkWizardEntity : InMemoryPersistentInteractiveEntity<KinkWizard>, IButtonInteractiveEntity
{
    /// <inheritdoc />
    public override string Nonce => $"kink-wizard::{_context.Message.Value.ID.ToString()}";

    private readonly KinkService _kinks;
    private readonly FeedbackService _feedback;
    private readonly IDiscordRestChannelAPI _channelAPI;
    private readonly IDiscordRestInteractionAPI _interactionAPI;
    private readonly InteractionContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinkWizardEntity"/> class.
    /// </summary>
    /// <param name="kinks">The kink service.</param>
    /// <param name="feedback">The user feedback service.</param>
    /// <param name="channelAPI">The channel API.</param>
    /// <param name="interactionAPI">The interaction API.</param>
    /// <param name="context">The interaction context.</param>
    public KinkWizardEntity
    (
        KinkService kinks,
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
        _channelAPI = channelAPI;
    }

    /// <inheritdoc />
    public override Task<Result<bool>> IsInterestedAsync
    (
        ComponentType? componentType,
        string customID,
        CancellationToken ct = default
    )
    {
        if (_context.User.ID != this.Data.SourceUserID)
        {
            return Task.FromResult<Result<bool>>(false);
        }

        return Task.FromResult<Result<bool>>
        (
            componentType is ComponentType.Button
            && this.Data.Buttons.Any(b => b.CustomID.IsDefined(out var buttonID) && buttonID == customID)
        );
    }

    /// <inheritdoc />
    public async Task<Result> HandleInteractionAsync(IUser user, string customID, CancellationToken ct = default)
    {
        var button = this.Data.Buttons.Single(b => b.CustomID.IsDefined(out var buttonID) && buttonID == customID);

        // Special actions
        if (button == this.Data.Exit)
        {
            this.DeleteData = true;
            return await _interactionAPI.DeleteOriginalInteractionResponseAsync
            (
                _context.ApplicationID,
                _context.Token,
                ct
            );
        }

        if (button == this.Data.Info)
        {
            return await DisplayHelpTextAsync(ct);
        }

        return this.Data.State switch
        {
            KinkWizardState.CategorySelection => await ConsumeCategoryInteractionAsync(button, ct),
            KinkWizardState.KinkPreference => await ConsumePreferenceInteractionAsync(button, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(this.Data.State))
        };
    }

    private async Task<Result> ConsumePreferenceInteractionAsync
    (
        ButtonComponent button,
        CancellationToken ct = default
    )
    {
        if (button == this.Data.Back)
        {
            this.Data.GoToCategorySelection();
            return await UpdateAsync(ct);
        }

        var preference = button switch
        {
            _ when button == this.Data.Favourite => KinkPreference.Favourite,
            _ when button == this.Data.Like => KinkPreference.Like,
            _ when button == this.Data.Maybe => KinkPreference.Maybe,
            _ when button == this.Data.No => KinkPreference.No,
            _ when button == this.Data.NoPreference => KinkPreference.NoPreference,
            _ => throw new ArgumentOutOfRangeException(nameof(button))
        };

        var setPreference = await SetCurrentKinkPreference(preference);
        if (!setPreference.IsSuccess)
        {
            return setPreference;
        }

        var moveNext = await this.Data.MoveToNextKinkInCategoryAsync(_kinks, ct);
        if (!moveNext.IsDefined(out var hasMore))
        {
            return (Result)moveNext;
        }

        if (hasMore)
        {
            return await UpdateAsync(ct);
        }

        this.Data.GoToCategorySelection();

        var send = await _feedback.SendContextualNeutralAsync
        (
            "All done in that category!",
            this.Data.SourceUserID,
            new FeedbackMessageOptions(MessageFlags: MessageFlags.Ephemeral),
            ct: ct
        );

        if (!send.IsSuccess)
        {
            return Result.FromError(send);
        }

        return await UpdateAsync(ct);
    }

    private async Task<Result> ConsumeCategoryInteractionAsync
    (
        ButtonComponent button,
        CancellationToken ct = default
    )
    {
        var didPageChange = false;
        if (button == this.Data.Next)
        {
            didPageChange = this.Data.MoveNext();
        }
        else if (button == this.Data.Previous)
        {
            didPageChange = this.Data.MovePrevious();
        }
        else if (button == this.Data.First)
        {
            didPageChange = this.Data.MoveFirst();
        }
        else if (button == this.Data.Last)
        {
            didPageChange = this.Data.MoveLast();
        }
        else if (button == this.Data.EnterCategory)
        {
            if (!this.Data.Categories.Any())
            {
                var sendWarning = await _feedback.SendContextualWarningAsync
                (
                    "There aren't any categories in the database.",
                    this.Data.SourceUserID,
                    new FeedbackMessageOptions(MessageFlags: MessageFlags.Ephemeral),
                    ct: ct
                );

                return sendWarning.IsSuccess
                    ? Result.FromSuccess()
                    : Result.FromError(sendWarning);
            }

            var sendConfirmation = await _feedback.SendContextualNeutralAsync
            (
                "Please enter a category name.",
                this.Data.SourceUserID,
                new FeedbackMessageOptions(MessageFlags: MessageFlags.Ephemeral),
                ct: ct
            );

            if (!sendConfirmation.IsSuccess)
            {
                return Result.FromError(sendConfirmation);
            }

            var getNextMessage = await GetNextMessageAsync
            (
                this.Data.ChannelID,
                ct: ct
            );

            if (!getNextMessage.IsSuccess)
            {
                return Result.FromError(getNextMessage);
            }

            var nextMessage = getNextMessage.Entity;

            if (nextMessage is null)
            {
                return await UpdateAsync(ct);
            }

            var tryStartCategoryResult = await this.Data.OpenCategoryAsync(_kinks, nextMessage.Content, ct);
            if (tryStartCategoryResult.IsSuccess)
            {
                return await UpdateAsync(ct);
            }

            var send = await _feedback.SendContextualWarningAsync
            (
                tryStartCategoryResult.Error.Message,
                this.Data.SourceUserID,
                new FeedbackMessageOptions(MessageFlags: MessageFlags.Ephemeral),
                ct: ct
            );

            return !send.IsSuccess
                ? Result.FromError(send)
                : tryStartCategoryResult;
        }

        return didPageChange
            ? await UpdateAsync(ct)
            : Result.FromSuccess();
    }

    [SuppressMessage("Style", "SA1118", Justification = "Large text blocks.")]
    private async Task<Result> DisplayHelpTextAsync(CancellationToken ct = default)
    {
        var eb = new Embed
        {
            Colour = Color.MediumPurple
        };

        switch (this.Data.State)
        {
            case KinkWizardState.CategorySelection:
            {
                var fields = new List<IEmbedField>
                {
                    new EmbedField
                    (
                        "Usage",
                        "Use the navigation buttons to scroll through the available categories. Select a "
                        + $"category by pressing {this.Data.EnterCategory.Emoji.Value.Name.Value} and typing in the name. "
                        + "The search algorithm is quite lenient, so you may find that things work fine even with "
                        + "typos.\n"
                        + "\n"
                        + $"You can quit at any point by pressing {this.Data.Exit.Emoji.Value.Name.Value}."
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
                        + $"\n{this.Data.Favourite.Emoji.Value.Name.Value} : Favourite"
                        + $"\n{this.Data.Like.Emoji.Value.Name.Value} : Like"
                        + $"\n{this.Data.Maybe.Emoji.Value.Name.Value} : Maybe"
                        + $"\n{this.Data.No.Emoji.Value.Name.Value} : Never"
                        + $"\n{this.Data.NoPreference.Emoji.Value.Name.Value} : No preference\n"
                        + "\n"
                        + $"\nPress {this.Data.Back.Emoji.Value.Name.Value} to go back to the categories."
                        + $"\nYou can quit at any point by pressing {this.Data.Exit.Emoji.Value.Name.Value}."
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
                throw new ArgumentOutOfRangeException(nameof(this.Data.State));
            }
        }

        return (Result)await _feedback.SendContextualEmbedAsync
        (
            eb,
            new FeedbackMessageOptions(MessageFlags: MessageFlags.Ephemeral),
            ct
        );
    }

    private async Task<Result> SetCurrentKinkPreference(KinkPreference preference)
    {
        if (this.Data.CurrentFListKinkID is null)
        {
            throw new InvalidOperationException();
        }

        var getUserKinkResult = await _kinks.GetUserKinkByFListIDAsync
        (
            this.Data.SourceUserID,
            this.Data.CurrentFListKinkID.Value
        );

        if (!getUserKinkResult.IsSuccess)
        {
            return Result.FromError(getUserKinkResult);
        }

        var userKink = getUserKinkResult.Entity;
        return await _kinks.SetKinkPreferenceAsync(userKink, preference);
    }

    /// <summary>
    /// Gets the next message sent in the given channel.
    /// </summary>
    /// <param name="channelID">The channel to watch.</param>
    /// <param name="from">The user to filter on.</param>
    /// <param name="timeout">The timeout after which the method gives up. Defaults to 45 seconds.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The message, or null if no message was sent within the given timespan.</returns>
    private async Task<Result<IMessage?>> GetNextMessageAsync
    (
        Snowflake channelID,
        Snowflake? from = null,
        TimeSpan? timeout = null,
        CancellationToken ct = default
    )
    {
        timeout ??= TimeSpan.FromSeconds(45);

        var now = DateTimeOffset.UtcNow;
        var timeoutTime = now + timeout;
        var after = Snowflake.CreateTimestampSnowflake(now, Constants.DiscordEpoch);

        while (now <= timeoutTime)
        {
            var getMessage = await _channelAPI.GetChannelMessagesAsync
            (
                channelID,
                after: after,
                limit: 5,
                ct: ct
            );

            if (!getMessage.IsDefined(out var messages))
            {
                return Result<IMessage?>.FromError(getMessage);
            }

            var message = messages.FirstOrDefault(m => from is null || m.Author.ID == from);
            if (message is not null)
            {
                return Result<IMessage?>.FromSuccess(message);
            }

            // No matches, keep looking
            now = DateTimeOffset.UtcNow;
            after = messages.Select<IMessage, Snowflake?>(m => m.ID).LastOrDefault()
                    ?? Snowflake.CreateTimestampSnowflake(now, Constants.DiscordEpoch);

            await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
        }

        return Result<IMessage?>.FromSuccess(null);
    }

    /// <summary>
    /// Updates the contents of the wizard.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A result which may or may not have succeeded.</returns>
    private async Task<Result> UpdateAsync(CancellationToken ct = default)
    {
        var getPage = await this.Data.GetCurrentPageAsync(_kinks, ct);
        if (!getPage.IsSuccess)
        {
            return Result.FromError(getPage);
        }

        var page = getPage.Entity;

        return (Result)await _interactionAPI.EditOriginalInteractionResponseAsync
        (
            _context.ApplicationID,
            _context.Token,
            embeds: new[] { page },
            components: new Optional<IReadOnlyList<IMessageComponent>?>(this.Data.GetCurrentPageComponents()),
            ct: ct
        );
    }
}
