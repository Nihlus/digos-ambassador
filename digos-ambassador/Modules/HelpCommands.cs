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
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Services;
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
        private readonly CommandService _commands;
        private readonly UserFeedbackService _feedback;
        private readonly InteractivityService _interactive;
        private readonly HelpService _help;

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
            _commands = commands;
            _feedback = feedback;
            _interactive = interactive;
            _help = help;
        }

        /// <summary>
        /// Lists available commands modules.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [UsedImplicitly]
        [Command]
        [Summary("Lists available command modules.")]
        public async Task HelpAsync()
        {
            var modules = _commands.Modules.Where(m => !m.IsSubmodule).ToList();
            var helpWizard = new HelpWizard(modules, _feedback, _help, this.Context.User);

            await _interactive.SendPrivateInteractiveMessageAndDeleteAsync
            (
                this.Context,
                _feedback,
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
        [Command]
        [Summary("Lists available commands that match the given search text.")]
        public async Task HelpAsync([CanBeNull, Remainder] string searchText)
        {
            searchText = searchText.Unquote();

            var topLevelModules = _commands.Modules.Where(m => !m.IsSubmodule).ToList();

            var moduleSearchTerms = _commands.Modules.Select
            (
                m => new List<string>(m.Aliases) { m.Name }
            )
            .SelectMany(t => t);

            var getModuleAliasResult = moduleSearchTerms.BestLevenshteinMatch(searchText, 0.5);
            if (getModuleAliasResult.IsSuccess)
            {
                var module = _commands.Modules.First(m => m.Aliases.Contains(getModuleAliasResult.Entity));
                if (module.IsSubmodule)
                {
                    module = module.GetTopLevelModule();
                }

                var helpWizard = new HelpWizard(topLevelModules, _feedback, _help, this.Context.User);
                await helpWizard.OpenModule(module.Name);
                await _interactive.SendPrivateInteractiveMessageAndDeleteAsync
                (
                    this.Context,
                    _feedback,
                    helpWizard,
                    TimeSpan.FromMinutes(30)
                );

                return;
            }

            var commandSearchTerms = topLevelModules.SelectMany(m => m.GetAllCommands().SelectMany(c => c.Aliases));
            var findCommandResult = commandSearchTerms.BestLevenshteinMatch(searchText, 0.5);
            if (findCommandResult.IsSuccess)
            {
                var foundAlias = findCommandResult.Entity;

                var commandGroup = topLevelModules
                    .Select(m => m.GetAllCommands().Where(c => c.Aliases.Contains(findCommandResult.Entity)))
                    .First(l => l.Any())
                    .Where(c => c.Aliases.Contains(foundAlias))
                    .GroupBy(c => c.Aliases.OrderByDescending(a => a).First())
                    .First();

                var eb = _help.CreateDetailedCommandInfoEmbed(commandGroup);

                await _feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, eb.Build());
            }
        }
    }
}
