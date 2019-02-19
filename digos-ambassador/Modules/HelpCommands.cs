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
using Discord.Commands;
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
        private readonly CommandService Commands;
        private readonly UserFeedbackService Feedback;
        private readonly InteractivityService Interactive;
        private readonly HelpService Help;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelpCommands"/> class.
        /// </summary>
        /// <param name="commands">The command service.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="interactive">The interactive service.</param>
        /// <param name="help">The help service.</param>
        public HelpCommands
        (
            [NotNull] CommandService commands,
            [NotNull] UserFeedbackService feedback,
            [NotNull] InteractivityService interactive,
            [NotNull] HelpService help
        )
        {
            this.Commands = commands;
            this.Feedback = feedback;
            this.Interactive = interactive;
            this.Help = help;
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
            var helpWizard = new HelpWizard(modules, this.Feedback, this.Help);

            await this.Interactive.SendPrivateInteractiveMessageAndDeleteAsync
            (
                this.Context,
                this.Feedback,
                helpWizard,
                TimeSpan.FromMinutes(30)
            );
        }

        /// <summary>
        /// Lists available commands based on a search string.
        /// </summary>
        /// <param name="searchText">The text to search the command handler for.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [UsedImplicitly]
        [Command(RunMode = RunMode.Async)]
        [Summary("Lists available commands that match the given search text.")]
        public async Task HelpAsync([CanBeNull, Remainder] string searchText)
        {
            searchText = searchText.Unquote();

            var modules = this.Commands.Modules.Where(m => !m.IsSubmodule).ToList();

            var moduleSearchTerms = modules.Select
            (
                m => new List<string>(m.Aliases) { m.Name }
            )
            .SelectMany(t => t);

            var getModuleResult = moduleSearchTerms.BestLevenshteinMatch(searchText, 0.5);
            if (getModuleResult.IsSuccess)
            {
                var helpWizard = new HelpWizard(modules, this.Feedback, this.Help);
                await helpWizard.OpenModule(getModuleResult.Entity);
                await this.Interactive.SendPrivateInteractiveMessageAndDeleteAsync
                (
                    this.Context,
                    this.Feedback,
                    helpWizard,
                    TimeSpan.FromMinutes(30)
                );

                return;
            }

            var commandSearchTerms = modules.SelectMany(m => m.GetAllCommands().SelectMany(c => c.Aliases));
            var findCommandResult = commandSearchTerms.BestLevenshteinMatch(searchText, 0.5);
            if (findCommandResult.IsSuccess)
            {
                var foundAlias = findCommandResult.Entity;

                var commandGroup = modules
                    .Select(m => m.GetAllCommands().Where(c => c.Aliases.Contains(findCommandResult.Entity)))
                    .First(l => l.Any())
                    .Where(c => c.Aliases.Contains(foundAlias))
                    .GroupBy(c => c.Aliases.OrderByDescending(a => a).First())
                    .First();

                var eb = this.Help.CreateDetailedCommandInfoEmbed(commandGroup);

                await this.Feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, eb.Build());
            }
        }
    }
}
