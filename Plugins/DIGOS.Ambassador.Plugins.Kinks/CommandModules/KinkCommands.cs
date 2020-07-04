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

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Extensions.Results;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Discord.TypeReaders;
using DIGOS.Ambassador.Plugins.Kinks.FList.Kinks;
using DIGOS.Ambassador.Plugins.Kinks.Model;
using DIGOS.Ambassador.Plugins.Kinks.Services;
using DIGOS.Ambassador.Plugins.Kinks.Wizards;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Newtonsoft.Json;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Kinks.CommandModules
{
    /// <summary>
    /// Commands for viewing and configuring user kinks.
    /// </summary>
    [Group("kink")]
    [Summary("Commands for viewing and configuring user kinks.")]
    public class KinkCommands : ModuleBase
    {
        private readonly KinkService _kinks;
        private readonly UserFeedbackService _feedback;

        private readonly InteractivityService _interactivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinkCommands"/> class.
        /// </summary>
        /// <param name="kinks">The application's kink service.</param>
        /// <param name="feedback">The application's feedback service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        public KinkCommands
        (
            KinkService kinks,
            UserFeedbackService feedback,
            InteractivityService interactivity
        )
        {
            _kinks = kinks;
            _feedback = feedback;
            _interactivity = interactivity;
        }

        /// <summary>
        /// Shows information about the named kink.
        /// </summary>
        /// <param name="kinkName">The name of the kink.</param>
        [UsedImplicitly]
        [Command("info")]
        [Summary("Shows information about the named kink.")]
        public async Task<RuntimeResult> ShowKinkAsync(string kinkName)
        {
            var getKinkInfoResult = await _kinks.GetKinkByNameAsync(kinkName);
            if (!getKinkInfoResult.IsSuccess)
            {
                return getKinkInfoResult.ToRuntimeResult();
            }

            var kink = getKinkInfoResult.Entity;
            var display = _kinks.BuildKinkInfoEmbed(kink);

            await _feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, display);
            return RuntimeCommandResult.FromSuccess();
        }

        /// <summary>
        /// Shows your preference for the named kink.
        /// </summary>
        /// <param name="kinkName">The name of the kink.</param>
        [UsedImplicitly]
        [Alias("show", "preference")]
        [Command("show")]
        [Summary("Shows your preference for the named kink.")]
        public async Task<RuntimeResult> ShowKinkPreferenceAsync(string kinkName)
            => await ShowKinkPreferenceAsync(this.Context.User, kinkName);

        /// <summary>
        /// Shows the user's preference for the named kink.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="kinkName">The name of the kink.</param>
        [UsedImplicitly]
        [Alias("show", "preference")]
        [Command("show")]
        [Summary("Shows the user's preference for the named kink.")]
        public async Task<RuntimeResult> ShowKinkPreferenceAsync(IUser user, string kinkName)
        {
            var getUserKinkResult = await _kinks.GetUserKinkByNameAsync(user, kinkName);
            if (!getUserKinkResult.IsSuccess)
            {
                return getUserKinkResult.ToRuntimeResult();
            }

            var userKink = getUserKinkResult.Entity;
            var display = _kinks.BuildUserKinkInfoEmbedBase(userKink);

            await _feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, display.Build());
            return RuntimeCommandResult.FromSuccess();
        }

        /// <summary>
        /// Shows the kinks which overlap between you and the given user.
        /// </summary>
        /// <param name="otherUser">The other user.</param>
        [UsedImplicitly]
        [Command("overlap")]
        [Summary("Shows the kinks which overlap between you and the given user.")]
        public async Task<RuntimeResult> ShowKinkOverlap(IUser otherUser)
        {
            var getUserKinksResult = await _kinks.GetUserKinksAsync(this.Context.User);
            if (!getUserKinksResult.IsSuccess)
            {
                return getUserKinksResult.ToRuntimeResult();
            }

            var userKinks = getUserKinksResult.Entity;

            var getOtherUserKinksResult = await _kinks.GetUserKinksAsync(otherUser);
            if (!getOtherUserKinksResult.IsSuccess)
            {
                return getOtherUserKinksResult.ToRuntimeResult();
            }

            var otherUserKinks = getOtherUserKinksResult.Entity;

            var overlap = userKinks.Intersect(otherUserKinks, new UserKinkOverlapEqualityComparer()).ToList();

            if (!overlap.Any())
            {
                return RuntimeCommandResult.FromSuccess("You don't overlap anywhere.");
            }

            var kinkOverlapPages = _kinks.BuildKinkOverlapEmbeds(this.Context.User, otherUser, overlap);
            var paginatedMessage = new PaginatedEmbed(_feedback, _interactivity, this.Context.User)
                .WithPages(kinkOverlapPages);

            await _interactivity.SendPrivateInteractiveMessageAsync(this.Context, _feedback, paginatedMessage);
            return RuntimeCommandResult.FromSuccess();
        }

        /// <summary>
        /// Shows your kinks with the given preference.
        /// </summary>
        /// <param name="preference">The preference.</param>
        [UsedImplicitly]
        [Command("by-preference")]
        [Summary("Shows your kinks with the given preference.")]
        public async Task<RuntimeResult> ShowKinksByPreferenceAsync
        (
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<KinkPreference>))]
            KinkPreference preference
        )
        => await ShowKinksByPreferenceAsync(this.Context.User, preference);

        /// <summary>
        /// Shows the given user's kinks with the given preference.
        /// </summary>
        /// <param name="otherUser">The user.</param>
        /// <param name="preference">The preference.</param>
        [UsedImplicitly]
        [Command("by-preference")]
        [Summary("Shows the given user's kinks with the given preference.")]
        public async Task<RuntimeResult> ShowKinksByPreferenceAsync
        (
            IUser otherUser,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<KinkPreference>))]
            KinkPreference preference
        )
        {
            var getUserKinksResult = await _kinks.GetUserKinksAsync(otherUser);
            if (!getUserKinksResult.IsSuccess)
            {
                return getUserKinksResult.ToRuntimeResult();
            }

            var userKinks = getUserKinksResult.Entity;

            var withPreference = userKinks.Where(k => k.Preference == preference).ToList();

            if (!withPreference.Any())
            {
                return RuntimeCommandResult.FromError("The user doesn't have any kinks with that preference.");
            }

            var paginatedKinkPages = _kinks.BuildPaginatedUserKinkEmbeds(withPreference);
            var paginatedMessage = new PaginatedEmbed(_feedback, _interactivity, this.Context.User)
                .WithPages(paginatedKinkPages);

            await _interactivity.SendPrivateInteractiveMessageAsync(this.Context, _feedback, paginatedMessage);
            return RuntimeCommandResult.FromSuccess();
        }

        /// <summary>
        /// Sets your preference for the given kink.
        /// </summary>
        /// <param name="kinkName">The name of the kink.</param>
        /// <param name="preference">The preference for the kink.</param>
        [UsedImplicitly]
        [Command("preference")]
        [Summary("Sets your preference for the given kink.")]
        public async Task<RuntimeResult> SetKinkPreferenceAsync
        (
            string kinkName,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<KinkPreference>))]
            KinkPreference preference
        )
        {
            var getUserKinkResult = await _kinks.GetUserKinkByNameAsync(this.Context.User, kinkName);
            if (!getUserKinkResult.IsSuccess)
            {
                return getUserKinkResult.ToRuntimeResult();
            }

            var userKink = getUserKinkResult.Entity;
            var setKinkPreferenceResult = await _kinks.SetKinkPreferenceAsync(userKink, preference);
            if (!setKinkPreferenceResult.IsSuccess)
            {
                return setKinkPreferenceResult.ToRuntimeResult();
            }

            return RuntimeCommandResult.FromSuccess("Preference set.");
        }

        /// <summary>
        /// Runs an interactive wizard for setting kink preferences.
        /// </summary>
        [UsedImplicitly]
        [Command("wizard")]
        [Summary("Runs an interactive wizard for setting kink preferences.")]
        public async Task<RuntimeResult> RunKinkWizardAsync()
        {
            var wizard = new KinkWizard
            (
                _feedback,
                _interactivity,
                _kinks,
                this.Context.User
            );

            await _interactivity.SendPrivateInteractiveMessageAsync(this.Context, _feedback, wizard);
            return RuntimeCommandResult.FromSuccess();
        }

        /// <summary>
        /// Updates the kink database with data from F-list.
        /// </summary>
        /// <returns>A task wrapping the update action.</returns>
        [UsedImplicitly]
        [Command("update-db")]
        [Summary("Updates the kink database with data from F-list.")]
        [RequireContext(ContextType.DM)]
        [RequireOwner]
        public async Task<RuntimeResult> UpdateKinkDatabaseAsync()
        {
            await _feedback.SendConfirmationAsync(this.Context, "Updating kinks...");

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
                    return RuntimeCommandResult.FromError("Could not connect to F-list: Operation timed out.");
                }
            }

            var kinkCollection = JsonConvert.DeserializeObject<KinkCollection>(json);
            if (kinkCollection.KinkCategories is null)
            {
                return RuntimeCommandResult.FromError($"Received an error from F-List: {kinkCollection.Error}");
            }

            foreach (var kinkSection in kinkCollection.KinkCategories)
            {
                if (!Enum.TryParse<KinkCategory>(kinkSection.Key, out var kinkCategory))
                {
                    return RuntimeCommandResult.FromError("Failed to parse kink category.");
                }

                updatedKinkCount += await _kinks.UpdateKinksAsync(kinkSection.Value.Kinks.Select
                (
                    k => new Kink(k.Name, k.Description, k.KinkId, kinkCategory)
                ));
            }

            return RuntimeCommandResult.FromSuccess($"Done. {updatedKinkCount} kinks updated.");
        }

        /// <summary>
        /// Resets all your kink preferences.
        /// </summary>
        [UsedImplicitly]
        [Command("reset")]
        [Summary("Resets all your kink preferences.")]
        public async Task<RuntimeResult> ResetKinksAsync()
        {
            var resetResult = await _kinks.ResetUserKinksAsync(this.Context.User);
            if (!resetResult.IsSuccess)
            {
                return resetResult.ToRuntimeResult();
            }

            return RuntimeCommandResult.FromSuccess("Preferences reset.");
        }
    }
}
