//
//  KinkCommands.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Plugins.Kinks.FList.Kinks;
using DIGOS.Ambassador.Plugins.Kinks.Model;
using DIGOS.Ambassador.Plugins.Kinks.Services;
using DIGOS.Ambassador.Plugins.Kinks.Wizards;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity.Services;
using Remora.Discord.Pagination.Extensions;
using Remora.Rest.Core;
using Remora.Rest.Json.Policies;
using Remora.Results;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Kinks.CommandModules;

/// <summary>
/// Commands for viewing and configuring user kinks.
/// </summary>
[Group("kink")]
[Description("Commands for viewing and configuring user kinks.")]
internal class KinkCommands : CommandGroup
{
    private readonly KinkService _kinks;
    private readonly FeedbackService _feedback;
    private readonly InMemoryDataService<Snowflake, KinkWizard> _dataService;
    private readonly ICommandContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinkCommands"/> class.
    /// </summary>
    /// <param name="kinks">The application's kink service.</param>
    /// <param name="feedback">The application's feedback service.</param>
    /// <param name="dataService">The in-memory data service.</param>
    /// <param name="context">The command context.</param>
    public KinkCommands
    (
        KinkService kinks,
        FeedbackService feedback,
        InMemoryDataService<Snowflake, KinkWizard> dataService,
        ICommandContext context
    )
    {
        _kinks = kinks;
        _feedback = feedback;
        _dataService = dataService;
        _context = context;
    }

    /// <summary>
    /// Shows information about the named kink.
    /// </summary>
    /// <param name="kinkName">The name of the kink.</param>
    [UsedImplicitly]
    [Command("info")]
    [Description("Shows information about the named kink.")]
    public async Task<IResult> ShowKinkAsync(string kinkName)
    {
        var getKinkInfoResult = await _kinks.GetKinkByNameAsync(kinkName);
        if (!getKinkInfoResult.IsSuccess)
        {
            return getKinkInfoResult;
        }

        var kink = getKinkInfoResult.Entity;
        var display = _kinks.BuildKinkInfoEmbed(kink);

        return await _feedback.SendPrivateEmbedAsync(_context.ChannelID, display);
    }

    /// <summary>
    /// Shows the user's preference for the named kink.
    /// </summary>
    /// <param name="kinkName">The name of the kink.</param>
    /// <param name="user">The user.</param>
    [UsedImplicitly]
    [Command("show")]
    [Description("Shows the user's preference for the named kink.")]
    public async Task<IResult> ShowKinkPreferenceAsync(string kinkName, IUser? user = null)
    {
        user ??= _context.User;

        var getUserKinkResult = await _kinks.GetUserKinkByNameAsync(user.ID, kinkName);
        if (!getUserKinkResult.IsSuccess)
        {
            return getUserKinkResult;
        }

        var userKink = getUserKinkResult.Entity;
        var display = _kinks.BuildUserKinkInfoEmbedBase(userKink);

        return await _feedback.SendPrivateEmbedAsync(_context.ChannelID, display);
    }

    /// <summary>
    /// Shows the kinks which overlap between you and the given user.
    /// </summary>
    /// <param name="otherUser">The other user.</param>
    [UsedImplicitly]
    [Command("overlap")]
    [Description("Shows the kinks which overlap between you and the given user.")]
    public async Task<IResult> ShowKinkOverlap(IUser otherUser)
    {
        var overlappingKinks = await _kinks.QueryDatabaseAsync
        (
            q =>
            {
                var otherUserKinks = q
                    .Where(k => k.User.DiscordID == otherUser.ID);

                return q
                    .Where(k => k.User.DiscordID == _context.User.ID)
                    .Where
                    (
                        k => otherUserKinks
                            .Any(ok => ok.Preference == k.Preference && ok.Kink.FListID == k.Kink.FListID)
                    );
            }
        );

        if (!overlappingKinks.Any())
        {
            return Result<FeedbackMessage>.FromSuccess(new FeedbackMessage("You don't overlap anywhere.", _feedback.Theme.Secondary));
        }

        var pages = _kinks.BuildKinkOverlapEmbeds(_context.User.ID, otherUser.ID, overlappingKinks);
        return await _feedback.SendContextualPaginatedMessageAsync(_context.User.ID, pages, ct: this.CancellationToken);
    }

    /// <summary>
    /// Shows the given user's kinks with the given preference. Defaults to yourself.
    /// </summary>
    /// <param name="preference">The preference.</param>
    /// <param name="user">The user.</param>
    [UsedImplicitly]
    [Command("by-preference")]
    [Description("Shows the given user's kinks with the given preference. Defaults to yourself.")]
    public async Task<IResult> ShowKinksByPreferenceAsync
    (
        KinkPreference preference,
        IUser? user = null
    )
    {
        user ??= _context.User;

        var kinksWithPreference = await _kinks.QueryDatabaseAsync
        (
            q => q
                .Where(k => k.User.DiscordID == user.ID)
                .Where(k => k.Preference == preference)
        );

        if (!kinksWithPreference.Any())
        {
            return Result<FeedbackMessage>.FromSuccess
            (
                new FeedbackMessage
                (
                    "The user doesn't have any kinks with that preference.",
                    _feedback.Theme.Warning
                )
            );
        }

        var pages = _kinks.BuildPaginatedUserKinkEmbeds(kinksWithPreference);
        return await _feedback.SendContextualPaginatedMessageAsync
        (
            _context.User.ID,
            pages,
            ct: this.CancellationToken
        );
    }

    /// <summary>
    /// Sets your preference for the given kink.
    /// </summary>
    /// <param name="kinkName">The name of the kink.</param>
    /// <param name="preference">The preference for the kink.</param>
    [UsedImplicitly]
    [Command("preference")]
    [Description("Sets your preference for the given kink.")]
    public async Task<Result<FeedbackMessage>> SetKinkPreferenceAsync
    (
        string kinkName,
        KinkPreference preference
    )
    {
        var getUserKinkResult = await _kinks.GetUserKinkByNameAsync(_context.User.ID, kinkName);
        if (!getUserKinkResult.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(getUserKinkResult);
        }

        var userKink = getUserKinkResult.Entity;
        var setKinkPreferenceResult = await _kinks.SetKinkPreferenceAsync(userKink, preference);

        return setKinkPreferenceResult.IsSuccess
            ? new FeedbackMessage("Preference set.", _feedback.Theme.Secondary)
            : Result<FeedbackMessage>.FromError(setKinkPreferenceResult);
    }

    /// <summary>
    /// Runs an interactive wizard for setting kink preferences.
    /// </summary>
    [UsedImplicitly]
    [Command("wizard")]
    [Description("Runs an interactive wizard for setting kink preferences.")]
    public async Task<Result> RunKinkWizardAsync()
    {
        var categories = await _kinks.GetKinkCategoriesAsync(this.CancellationToken);
        var initialWizard = new KinkWizard
        (
            _context.User.ID,
            categories.ToList(),
            _context is InteractionContext
        );

        var getInitialEmbed = await initialWizard.GetCurrentPageAsync(_kinks, this.CancellationToken);
        if (!getInitialEmbed.IsDefined(out var initialEmbed))
        {
            return (Result)getInitialEmbed;
        }

        var initialComponents = initialWizard.GetCurrentPageComponents();

        var send = await _feedback.SendContextualEmbedAsync
        (
            initialEmbed,
            new FeedbackMessageOptions(MessageComponents: new(initialComponents)),
            ct: this.CancellationToken
        );

        if (!send.IsSuccess)
        {
            return (Result)send;
        }

        var messageID = send.Entity.ID;
        return _dataService.TryAddData(messageID, initialWizard)
            ? Result.FromSuccess()
            : new InvalidOperationError("Failed to add the in-memory data for the kink wizard. Bug?");
    }

    /// <summary>
    /// Updates the kink database with data from F-list.
    /// </summary>
    /// <returns>A task wrapping the update action.</returns>
    [UsedImplicitly]
    [Command("update-db")]
    [Description("Updates the kink database with data from F-list.")]
    [RequireContext(ChannelContext.DM)]
    [RequireOwner]
    public async Task<Result<FeedbackMessage>> UpdateKinkDatabaseAsync()
    {
        var send = await _feedback.SendContextualNeutralAsync("Updating kinks...", _context.User.ID);
        if (!send.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(send);
        }

        // Get the latest JSON from F-list
        string json;
        using (var web = new HttpClient())
        {
            try
            {
                using var response = await web.GetAsync
                (
                    new Uri("https://www.f-list.net/json/api/kink-list.php"),
                    this.CancellationToken
                );

                json = await response.Content.ReadAsStringAsync();
            }
            catch (OperationCanceledException)
            {
                return new UserError("Could not connect to F-list: Operation timed out.");
            }
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new SnakeCaseNamingPolicy()
        };

        var kinkCollection = JsonSerializer.Deserialize<KinkCollection>(json, jsonOptions)
                             ?? throw new InvalidOperationException();

        if (kinkCollection.KinkCategories is null)
        {
            return new UserError($"Received an error from F-List: {kinkCollection.Error}");
        }

        var newKinks = new List<Kink>();

        foreach (var (categoryName, category) in kinkCollection.KinkCategories)
        {
            if (!Enum.TryParse<KinkCategory>(categoryName, out var kinkCategory))
            {
                return new UserError("Failed to parse kink category.");
            }

            newKinks.AddRange(category.Kinks.Select
            (
                k => new Kink(k.Name, k.Description, k.KinkId, kinkCategory)
            ));
        }

        await _kinks.UpdateKinksAsync(newKinks);

        return new FeedbackMessage($"Done.", _feedback.Theme.Secondary);
    }

    /// <summary>
    /// Resets all your kink preferences.
    /// </summary>
    [UsedImplicitly]
    [Command("reset")]
    [Description("Resets all your kink preferences.")]
    public async Task<Result<FeedbackMessage>> ResetKinksAsync()
    {
        var resetResult = await _kinks.ResetUserKinksAsync(_context.User.ID);
        return resetResult.IsSuccess
            ? new FeedbackMessage("Preferences reset.", _feedback.Theme.Secondary)
            : Result<FeedbackMessage>.FromError(resetResult);
    }
}
