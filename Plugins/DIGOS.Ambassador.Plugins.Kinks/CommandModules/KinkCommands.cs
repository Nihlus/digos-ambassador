﻿//
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

using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Feedback.Errors;
using DIGOS.Ambassador.Discord.Feedback.Results;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination.Extensions;
using DIGOS.Ambassador.Plugins.Kinks.FList.Kinks;
using DIGOS.Ambassador.Plugins.Kinks.Model;
using DIGOS.Ambassador.Plugins.Kinks.Services;
using DIGOS.Ambassador.Plugins.Kinks.Wizards;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Json;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Kinks.CommandModules
{
    /// <summary>
    /// Commands for viewing and configuring user kinks.
    /// </summary>
    [Group("kink")]
    [Description("Commands for viewing and configuring user kinks.")]
    public class KinkCommands : CommandGroup
    {
        private readonly KinkService _kinks;
        private readonly UserFeedbackService _feedback;

        private readonly InteractivityService _interactivity;
        private readonly ICommandContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinkCommands"/> class.
        /// </summary>
        /// <param name="kinks">The application's kink service.</param>
        /// <param name="feedback">The application's feedback service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="context">The command context.</param>
        public KinkCommands
        (
            KinkService kinks,
            UserFeedbackService feedback,
            InteractivityService interactivity,
            ICommandContext context
        )
        {
            _kinks = kinks;
            _feedback = feedback;
            _interactivity = interactivity;
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
                return Result<UserMessage>.FromSuccess(new ConfirmationMessage("You don't overlap anywhere."));
            }

            var pages = _kinks.BuildKinkOverlapEmbeds(_context.User.ID, otherUser.ID, overlappingKinks);
            return await _interactivity.SendPrivateInteractiveMessageAsync(_context.User.ID, pages);
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
                return Result<UserMessage>.FromSuccess
                (
                    new WarningMessage("The user doesn't have any kinks with that preference.")
                );
            }

            var pages = _kinks.BuildPaginatedUserKinkEmbeds(kinksWithPreference);
            return await _interactivity.SendPrivateInteractiveMessageAsync
            (
                _context.User.ID,
                pages
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
        public async Task<Result<UserMessage>> SetKinkPreferenceAsync
        (
            string kinkName,
            KinkPreference preference
        )
        {
            var getUserKinkResult = await _kinks.GetUserKinkByNameAsync(_context.User.ID, kinkName);
            if (!getUserKinkResult.IsSuccess)
            {
                return Result<UserMessage>.FromError(getUserKinkResult);
            }

            var userKink = getUserKinkResult.Entity;
            var setKinkPreferenceResult = await _kinks.SetKinkPreferenceAsync(userKink, preference);

            return setKinkPreferenceResult.IsSuccess
                ? new ConfirmationMessage("Preference set.")
                : Result<UserMessage>.FromError(setKinkPreferenceResult);
        }

        /// <summary>
        /// Runs an interactive wizard for setting kink preferences.
        /// </summary>
        [UsedImplicitly]
        [Command("wizard")]
        [Description("Runs an interactive wizard for setting kink preferences.")]
        public async Task<Result> RunKinkWizardAsync()
        {
            return await _interactivity.SendInteractiveMessageAsync
            (
                _context.ChannelID,
                (c, m) => new KinkWizard(c, m, _context.User.ID)
            );
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
        public async Task<Result<UserMessage>> UpdateKinkDatabaseAsync()
        {
            var send = await _feedback.SendContextualConfirmationAsync(_context.User.ID, "Updating kinks...");
            if (!send.IsSuccess)
            {
                return Result<UserMessage>.FromError(send);
            }

            var updatedKinkCount = 0;

            // Get the latest JSON from F-list
            string json;
            using (var web = new HttpClient())
            {
                web.Timeout = TimeSpan.FromSeconds(3);

                var cts = new CancellationTokenSource();
                cts.CancelAfter(web.Timeout);

                try
                {
                    using var response = await web.GetAsync
                    (
                        new Uri("https://www.f-list.net/json/api/kink-list.php"),
                        cts.Token
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

            foreach (var (categoryName, category) in kinkCollection.KinkCategories)
            {
                if (!Enum.TryParse<KinkCategory>(categoryName, out var kinkCategory))
                {
                    return new UserError("Failed to parse kink category.");
                }

                updatedKinkCount += await _kinks.UpdateKinksAsync(category.Kinks.Select
                (
                    k => new Kink(k.Name, k.Description, k.KinkId, kinkCategory)
                ));
            }

            return new ConfirmationMessage($"Done. {updatedKinkCount} kinks updated.");
        }

        /// <summary>
        /// Resets all your kink preferences.
        /// </summary>
        [UsedImplicitly]
        [Command("reset")]
        [Description("Resets all your kink preferences.")]
        public async Task<Result<UserMessage>> ResetKinksAsync()
        {
            var resetResult = await _kinks.ResetUserKinksAsync(_context.User.ID);
            return resetResult.IsSuccess
                ? new ConfirmationMessage("Preferences reset.")
                : Result<UserMessage>.FromError(resetResult);
        }
    }
}
