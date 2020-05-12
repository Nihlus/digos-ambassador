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

using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Autorole.Model;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoroleCommands"/> class.
        /// </summary>
        /// <param name="autoroles">The autorole service.</param>
        /// <param name="feedback">The feedback service.</param>
        public AutoroleCommands(AutoroleService autoroles, UserFeedbackService feedback)
        {
            _autoroles = autoroles;
            _feedback = feedback;
        }

        /// <summary>
        /// Creates a new autorole configuration for the given Discord role.
        /// </summary>
        /// <param name="discordRole">The discord role.</param>
        [UsedImplicitly]
        [Alias("create")]
        [Command("create")]
        [Summary("Creates a new autorole configuration for the given Discord role.")]
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
        public async Task ShowAutoroleAsync(AutoroleConfiguration autorole)
        {
            var embedBase = _feedback.CreateEmbedBase();

            embedBase
                .WithTitle("Autorole Configuration")
                .WithDescription(MentionUtils.MentionRole((ulong)autorole.DiscordRoleID))
                .AddField("Requires confirmation", autorole.RequiresConfirmation, true)
                .AddField("Is enabled", autorole.IsEnabled, true);

            // TODO: Display conditions in some agnostic manner
            await _feedback.SendEmbedAsync(this.Context.Channel, embedBase.Build());
        }
    }
}
