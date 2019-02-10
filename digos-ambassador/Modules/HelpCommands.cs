//
//  HelpCommands.cs
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

using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Interactivity;
using DIGOS.Ambassador.Wizards;

using Discord;
using Discord.Commands;
using Discord.Net;

using Humanizer;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Modules
{
    /// <summary>
    /// Help-related commands that explain other commands or modules.
    /// </summary>
    [UsedImplicitly]
    [Group("help")]
    [Alias("help", "halp", "hlep", "commands")]
    [Summary("Help-related commands that explain other commands or modules.")]
    public class HelpCommands : ModuleBase<SocketCommandContext>
    {
        private readonly IServiceProvider Services;

        private readonly CommandService Commands;
        private readonly UserFeedbackService Feedback;
        private readonly InteractivityService Interactive;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelpCommands"/> class.
        /// </summary>
        /// <param name="commands">The command service.</param>
        /// <param name="services">All available services.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="interactive">The interactive service.</param>
        public HelpCommands
        (
            [NotNull] CommandService commands,
            [NotNull] IServiceProvider services,
            [NotNull] UserFeedbackService feedback,
            [NotNull] InteractivityService interactive
        )
        {
            this.Commands = commands;
            this.Services = services;
            this.Feedback = feedback;
            this.Interactive = interactive;
        }

        /// <summary>
        /// Lists available commands modules.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [UsedImplicitly]
        [Command(RunMode = RunMode.Async)]
        [Summary("Lists available command modules.")]
        public async Task HelpAsync()
        {
            var modules = this.Commands.Modules.Where(m => !m.IsSubmodule).ToList();
            var helpWizard = new HelpWizard(modules, this.Feedback);

            await this.Interactive.SendPrivateInteractiveMessageAsync(this.Context, this.Feedback, helpWizard);
        }

        /// <summary>
        /// Lists available commands based on a search string.
        /// </summary>
        /// <param name="searchText">The text to search the command handler for.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [UsedImplicitly]
        [Command(RunMode = RunMode.Async)]
        [Summary("Lists available commands that match the given search text.")]
        public async Task HelpAsync([CanBeNull] string searchText)
        {
            IReadOnlyList<CommandInfo> searchResults;
            if (searchText.IsNullOrEmpty())
            {
                searchResults = this.Commands.Modules.SelectMany(m => m.Commands).ToList();
            }
            else
            {
                searchResults = this.Commands.Commands.Where
                (
                    c =>
                    c.Aliases.Any
                    (
                        a =>
                        a.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    )
                    || c.Module.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                )
                .Distinct().ToList();
            }

            var userChannel = await this.Context.Message.Author.GetOrCreateDMChannelAsync();

            if (searchResults.Count <= 0)
            {
                await this.Feedback.SendWarningAsync(this.Context, "No matching commands found.");
                return;
            }

            var modules = searchResults.Select(ci => ci.Module).GetTopLevelModules().Distinct();

            foreach (var module in modules)
            {
                var availableEmbed = new EmbedBuilder();

                var relevantModuleAliases = module.Aliases.Skip(1);
                var moduleExtraAliases = module.Aliases.Count > 1
                    ? $"(you can also use {relevantModuleAliases.Humanize("or")} instead of {module.Name})"
                    : string.Empty;

                availableEmbed.WithColor(Color.Blue);
                availableEmbed.WithDescription($"Available commands in {module.Name} {moduleExtraAliases}");

                var unavailableEmbed = new EmbedBuilder();

                unavailableEmbed.WithColor(Color.Red);
                unavailableEmbed.WithDescription($"Unavailable commands in {module.Name} {moduleExtraAliases}");

                var matchingCommandsInModule = module.Commands.Union
                (
                    module.Submodules.SelectMany
                    (
                        sm => sm.Commands
                    )
                )
                .Where(c => searchResults.Contains(c));

                foreach (var command in matchingCommandsInModule)
                {
                    var relevantAliases = command.Aliases.Skip(1).Where(a => a.StartsWith(command.Module.Aliases.First())).ToList();
                    var prefix = relevantAliases.Count > 1
                        ? "as well as"
                        : "or";

                    var commandExtraAliases = relevantAliases.Any()
                        ? $"({prefix} {relevantAliases.Humanize("or")})"
                        : string.Empty;

                    var commandDisplayAliases = $"{command.Aliases.First()} {commandExtraAliases}";

                    var hasPermission = await command.CheckPreconditionsAsync(this.Context, this.Services);
                    if (hasPermission.IsSuccess)
                    {
                        availableEmbed.AddField(commandDisplayAliases, $"{command.Summary}\n{this.Feedback.BuildParameterList(command)}");
                    }
                    else
                    {
                        unavailableEmbed.AddField(commandDisplayAliases, $"*{hasPermission.ErrorReason}*\n\n{command.Summary} \n{this.Feedback.BuildParameterList(command)}");
                    }
                }

                try
                {
                    if (availableEmbed.Fields.Count > 0)
                    {
                        await userChannel.SendMessageAsync(string.Empty, false, availableEmbed.Build());
                    }

                    if (unavailableEmbed.Fields.Count > 0)
                    {
                        await userChannel.SendMessageAsync(string.Empty, false, unavailableEmbed.Build());
                    }
                }
                catch (HttpException hex) when (hex.WasCausedByDMsNotAccepted())
                {
                    if (!this.Context.IsPrivate)
                    {
                        await this.Feedback.SendWarningAsync(this.Context, "I can't do that, since you don't accept DMs from non-friends on this server.");
                    }

                    return;
                }
                finally
                {
                    await userChannel.CloseAsync();
                }
            }

            if (!this.Context.IsPrivate)
            {
                await this.Feedback.SendConfirmationAsync(this.Context, "Please check your private messages.");
            }
        }
    }
}
