//
//  AutoroleCommands.cs
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
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Plugins.Autorole.Model;
using DIGOS.Ambassador.Plugins.Autorole.Permissions;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using DIGOS.Ambassador.Plugins.Permissions.Preconditions;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Autorole.CommandModules
{
    /// <summary>
    /// Commands for creating, using, and interacting with autoroles.
    /// </summary>
    [UsedImplicitly]
    [Alias("autorole", "at")]
    [Group("autorole")]
    [Summary("Commands for creating, editing, and interacting with automatic roles.")]
    public partial class AutoroleCommands : ModuleBase
    {
        private readonly AutoroleService _autoroles;
        private readonly UserFeedbackService _feedback;
        private readonly InteractivityService _interactivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoroleCommands"/> class.
        /// </summary>
        /// <param name="autoroles">The autorole service.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        public AutoroleCommands
        (
            AutoroleService autoroles,
            UserFeedbackService feedback,
            InteractivityService interactivity
        )
        {
            _autoroles = autoroles;
            _feedback = feedback;
            _interactivity = interactivity;
        }

        /// <summary>
        /// Creates a new autorole configuration for the given Discord role.
        /// </summary>
        /// <param name="discordRole">The discord role.</param>
        [UsedImplicitly]
        [Alias("create")]
        [Command("create")]
        [Summary("Creates a new autorole configuration for the given Discord role.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(CreateAutorole), PermissionTarget.Self)]
        public async Task CreateAutoroleAsync(IRole discordRole)
        {
            var create = await _autoroles.CreateAutoroleAsync(discordRole);
            if (!create.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, create.ErrorReason);
            }

            await _feedback.SendConfirmationAsync(this.Context, "Autorole configuration created.");
        }

        /// <summary>
        /// Deletes an existing autorole configuration for the given Discord role.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        [UsedImplicitly]
        [Alias("delete")]
        [Command("delete")]
        [Summary("Deletes an existing autorole configuration for the given Discord role.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(DeleteAutorole), PermissionTarget.Self)]
        public async Task DeleteAutoroleAsync(AutoroleConfiguration autorole)
        {
            var deleteAutorole = await _autoroles.DeleteAutoroleAsync(autorole);
            if (!deleteAutorole.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, deleteAutorole.ErrorReason);
            }

            await _feedback.SendConfirmationAsync(this.Context, "Autorole configuration deleted.");
        }

        /// <summary>
        /// Enables the given autorole, allowing it to be added to users.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        [UsedImplicitly]
        [Alias("enable")]
        [Command("enable")]
        [Summary("Enables the given autorole, allowing it to be added to users.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(EditAutorole), PermissionTarget.Self)]
        public async Task EnableAutoroleAsync(AutoroleConfiguration autorole)
        {
            var enableAutorole = await _autoroles.EnableAutoroleAsync(autorole);
            if (!enableAutorole.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, enableAutorole.ErrorReason);
            }

            await _feedback.SendConfirmationAsync(this.Context, "Autorole enabled.");
        }

        /// <summary>
        /// Disables the given autorole, preventing it from being added to users.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        [UsedImplicitly]
        [Alias("disable")]
        [Command("disable")]
        [Summary("Disables the given autorole, preventing it from being added to users.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(EditAutorole), PermissionTarget.Self)]
        public async Task DisableAutoroleAsync(AutoroleConfiguration autorole)
        {
            var disableAutorole = await _autoroles.DisableAutoroleAsync(autorole);
            if (!disableAutorole.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, disableAutorole.ErrorReason);
            }

            await _feedback.SendConfirmationAsync(this.Context, "Autorole disabled.");
        }

        /// <summary>
        /// Show the settings for the given autorole.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        [UsedImplicitly]
        [Alias("show", "view")]
        [Command("show")]
        [Summary("Show the settings for the given autorole.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(ViewAutorole), PermissionTarget.Self)]
        public async Task ShowAutoroleAsync(AutoroleConfiguration autorole)
        {
            var paginatedEmbed = new PaginatedEmbed(_feedback, _interactivity, this.Context.User)
            {
                Appearance = PaginatedAppearanceOptions.Default
            };

            var baseEmbed = paginatedEmbed.Appearance.CreateEmbedBase()
                .WithTitle("Autorole Configuration")
                .WithDescription(MentionUtils.MentionRole((ulong)autorole.DiscordRoleID))
                .AddField("Requires confirmation", autorole.RequiresConfirmation, true)
                .AddField("Is enabled", autorole.IsEnabled, true);

            if (!autorole.Conditions.Any())
            {
                baseEmbed.AddField("Conditions", "No conditions");
                baseEmbed.Footer = null;

                await _feedback.SendEmbedAsync(this.Context.Channel, baseEmbed.Build());
                return;
            }

            var conditionFields = autorole.Conditions.Select
            (
                c => new EmbedFieldBuilder()
                    .WithName($"Condition #{autorole.Conditions.IndexOf(c)} (ID {c.ID})")
                    .WithValue(c.GetDescriptiveUIText())
            );

            var pages = PageFactory.FromFields(conditionFields, pageBase: baseEmbed);
            paginatedEmbed.WithPages(pages);

            await _interactivity.SendInteractiveMessageAndDeleteAsync
            (
                this.Context.Channel,
                paginatedEmbed,
                TimeSpan.FromMinutes(5)
            );
        }

        /// <summary>
        /// Lists configured autoroles.
        /// </summary>
        [UsedImplicitly]
        [Alias("list")]
        [Command("list")]
        [Summary("Lists configured autoroles.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(ViewAutorole), PermissionTarget.Self)]
        public async Task ListAutorolesAsync()
        {
            var autoroles = new List<AutoroleConfiguration>();
            foreach (var role in this.Context.Guild.Roles)
            {
                var getAutorole = await _autoroles.GetAutoroleAsync(role);
                if (!getAutorole.IsSuccess)
                {
                    continue;
                }

                autoroles.Add(getAutorole.Entity);
            }

            var pager = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                _interactivity,
                this.Context.User,
                autoroles,
                at => $"@{this.Context.Guild.Roles.First(r => r.Id == (ulong)at.DiscordRoleID).Name}",
                at => at.IsEnabled ? "Enabled" : "Disabled",
                "There are no autoroles configured."
            );

            await _interactivity.SendInteractiveMessageAndDeleteAsync
            (
                this.Context.Channel,
                pager,
                TimeSpan.FromMinutes(5)
            );
        }

        /// <summary>
        /// Affirms a user's qualification for an autorole.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="user">The user.</param>
        [UsedImplicitly]
        [Alias("affirm", "confirm")]
        [Command("affirm")]
        [Summary("Affirms a user's qualification for an autorole.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(AffirmDenyAutorole), PermissionTarget.All)]
        public async Task AffirmAutoroleForUserAsync
        (
            AutoroleConfiguration autorole,
            IUser user
        )
        {
            var affirmResult = await _autoroles.AffirmAutoroleAsync(autorole, user);
            if (!affirmResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, affirmResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "Qualification affirmed.");
        }

        /// <summary>
        /// Affirms all currently qualifying users for the given autorole.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        [UsedImplicitly]
        [Alias("affirm-all", "confirm-all")]
        [Summary("Affirms all currently qualifying users for the given autorole.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(AffirmDenyAutorole), PermissionTarget.All)]
        public async Task AffirmAutoroleForAllAsync(AutoroleConfiguration autorole)
        {
            var affirmResult = await _autoroles.AffirmAutoroleForAllAsync(autorole);
            if (!affirmResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, affirmResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "Qualifications affirmed.");
        }

        /// <summary>
        /// Denies a user's qualification for an autorole.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="user">The user.</param>
        [UsedImplicitly]
        [Alias("deny")]
        [Command("deny")]
        [Summary("Denies a user's qualification for an autorole.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(AffirmDenyAutorole), PermissionTarget.Self)]
        public async Task DenyAutoroleForUserAsync
        (
            AutoroleConfiguration autorole,
            IUser user
        )
        {
            var denyResult = await _autoroles.DenyAutoroleAsync(autorole, user);
            if (!denyResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, denyResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "Qualification denied.");
        }

        /// <summary>
        /// Sets whether the given autorole require confirmation for the assignment after a user has qualified.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="requireAffirmation">Whether confirmation is required.</param>
        [UsedImplicitly]
        [Alias("require-affirmation", "require-confirmation")]
        [Command("require-confirmation")]
        [Summary("Sets whether the given autorole require confirmation for the assignment after a user has qualified.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(EditAutorole), PermissionTarget.Self)]
        public async Task SetAffirmationRequirementAsync
        (
            AutoroleConfiguration autorole,
            bool requireAffirmation = true
        )
        {
            var setRequirementResult = await _autoroles.SetAffirmationRequiredAsync(autorole, requireAffirmation);
            if (!setRequirementResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, setRequirementResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync
            (
                this.Context,
                requireAffirmation ? "Affirmation is now required." : "Affirmation is no longer required."
            );
        }
    }
}
