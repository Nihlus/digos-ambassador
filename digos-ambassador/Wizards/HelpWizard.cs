//
//  HelpWizard.cs
//
//  Author:
//        Jarl Gullberg <jarl.gullberg@gmail.com>
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
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity.Messages;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Plugins.Kinks.Extensions;
using DIGOS.Ambassador.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

using JetBrains.Annotations;

namespace DIGOS.Ambassador.Wizards
{
    /// <summary>
    /// Acts as an interactive help page.
    /// </summary>
    public class HelpWizard : InteractiveMessage, IWizard
    {
        [NotNull]
        private readonly UserFeedbackService _feedback;

        [NotNull]
        private readonly HelpService _help;

        private static readonly Emoji First = new Emoji("\x23EE");
        private static readonly Emoji Next = new Emoji("\x25B6");
        private static readonly Emoji Previous = new Emoji("\x25C0");
        private static readonly Emoji Last = new Emoji("\x23ED");
        private static readonly Emoji EnterModule = new Emoji("\xD83D\xDD22");

        private static readonly Emoji Back = new Emoji("\x23EB");
        private static readonly Emoji Exit = new Emoji("\x23F9");
        private static readonly Emoji Info = new Emoji("\x2139");

        /// <summary>
        /// Gets the currently accepted emotes.
        /// </summary>
        [NotNull]
        private IReadOnlyCollection<IEmote> AcceptedEmotes => GetCurrentPageEmotes().ToList();

        /// <summary>
        /// Gets the emotes that are currently rejected by the wizard.
        /// </summary>
        [NotNull]
        private IReadOnlyCollection<IEmote> CurrrentlyRejectedEmotes => GetCurrentPageRejectedEmotes().ToList();

        [NotNull, ItemNotNull]
        private readonly IReadOnlyList<ModuleInfo> _modules;

        private readonly IReadOnlyList<IReadOnlyList<EmbedFieldBuilder>> _moduleListPages;

        private readonly IDictionary<ModuleInfo, IReadOnlyList<IReadOnlyList<EmbedFieldBuilder>>> _commandListPages;

        [NotNull]
        private readonly Embed _loadingEmbed;

        /*
         * State fields
         */

        private HelpWizardState _state;

        /*
         * Module list state
         */

        private int _moduleListOffset;

        /*
         * Command list state
         */

        [CanBeNull]
        private ModuleInfo _currentModule;

        private int _commandListOffset;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelpWizard"/> class.
        /// </summary>
        /// <param name="modules">The modules available in the bot.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="help">The help service.</param>
        /// <param name="sourceUser">The user who caused the interactive message to be created.</param>
        public HelpWizard
        (
            [NotNull, ItemNotNull] IReadOnlyList<ModuleInfo> modules,
            [NotNull] UserFeedbackService feedback,
            [NotNull] HelpService help,
            [NotNull] IUser sourceUser
        )
            : base(sourceUser)
        {
            _modules = modules;
            _feedback = feedback;
            _help = help;

            var eb = new EmbedBuilder();
            eb.WithTitle("Help & Information");
            eb.WithDescription("Loading...");

            _loadingEmbed = eb.Build();

            _moduleListPages = BuildModuleListPages();
            _commandListPages = new Dictionary<ModuleInfo, IReadOnlyList<IReadOnlyList<EmbedFieldBuilder>>>();
        }

        /// <summary>
        /// Builds a set of pre-formatted pages for the command modules.
        /// </summary>
        /// <returns>A list of pages, where each page is a list of embed fields.</returns>
        [NotNull, ItemNotNull]
        private IReadOnlyList<IReadOnlyList<EmbedFieldBuilder>> BuildModuleListPages()
        {
            var pages = new List<IReadOnlyList<EmbedFieldBuilder>>();

            var currentPage = new List<EmbedFieldBuilder>();
            var currentContentLength = 0;
            foreach (var module in _modules)
            {
                var moduleContentLength = module.Name.Length + module.Summary.Length;
                if (currentPage.Count >= 3 || currentContentLength + moduleContentLength > 1300)
                {
                    pages.Add(currentPage);

                    currentPage = new List<EmbedFieldBuilder>();
                    currentContentLength = 0;
                }

                var ebf = new EmbedFieldBuilder().WithName(module.Name).WithValue(module.Summary);
                currentPage.Add(ebf);

                currentContentLength += moduleContentLength;

                if (module == _modules.Last() && !pages.Contains(currentPage))
                {
                    pages.Add(currentPage);
                }
            }

            return pages;
        }

        /// <summary>
        /// Builds a set of pre-formatted pages for the commands in the given module.
        /// </summary>
        /// <param name="module">The module that contains the commands.</param>
        /// <returns>A list of pages, where each page is a list of embed fields.</returns>
        private IReadOnlyList<IReadOnlyList<EmbedFieldBuilder>> BuildCommandListPages([NotNull] ModuleInfo module)
        {
            var pages = new List<IReadOnlyList<EmbedFieldBuilder>>();

            var currentPage = new List<EmbedFieldBuilder>();
            var currentContentLength = 0;

            var commandGroups = module
                .GetAllCommands()
                .GroupBy(c => c.Aliases.OrderByDescending(a => a).First())
                .ToList();

            foreach (var commandGroup in commandGroups)
            {
                var helpField = _help.CreateCommandInfoEmbedField(commandGroup.First());

                var commandContentLength = helpField.Name.Length + ((string)helpField.Value).Length;

                if (commandGroup.Count() > 1)
                {
                    var hint = "*This command has multiple variants.*";

                    helpField.WithValue((string)helpField.Value + "\n" + hint);
                    commandContentLength += hint.Length;
                }

                if (currentPage.Count >= 5 || (currentContentLength + commandContentLength) > 1300)
                {
                    pages.Add(currentPage);

                    currentPage = new List<EmbedFieldBuilder>();
                    currentContentLength = 0;
                }

                currentPage.Add(helpField);

                currentContentLength += commandContentLength;

                if (commandGroup == commandGroups.Last() && !pages.Contains(currentPage))
                {
                    pages.Add(currentPage);
                }
            }

            return pages;
        }

        /// <inheritdoc/>
        protected override async Task<IUserMessage> DisplayAsync([NotNull] IMessageChannel channel)
        {
            return await channel.SendMessageAsync(string.Empty, embed: _loadingEmbed)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task UpdateAsync()
        {
            if (this.Message is null)
            {
                return;
            }

            await this.Message.ModifyAsync(m => m.Embed = _loadingEmbed);

            foreach (var emote in this.CurrrentlyRejectedEmotes)
            {
                if (!this.Message.Reactions.ContainsKey(emote) || !this.Message.Reactions[emote].IsMe)
                {
                    continue;
                }

                await this.Message.RemoveReactionAsync(emote, this.Interactivity.Client.CurrentUser);
            }

            foreach (var emote in this.AcceptedEmotes)
            {
                if (this.Message.Reactions.ContainsKey(emote) && this.Message.Reactions[emote].IsMe)
                {
                    continue;
                }

                await this.Message.AddReactionAsync(emote);
            }

            var newEmbed = await GetCurrentPageAsync();
            await this.Message.ModifyAsync(m => m.Embed = newEmbed);
        }

        /// <inheritdoc/>
        public Task<Embed> GetCurrentPageAsync()
        {
            switch (_state)
            {
                case HelpWizardState.ModuleListing:
                {
                    var eb = _feedback.CreateEmbedBase();
                    eb.WithTitle("Available command modules");
                    eb.WithDescription($"Click {EnterModule} and type in a module's name to see available commands.");

                    var page = _moduleListPages[_moduleListOffset];
                    eb.WithFields(page);

                    eb.WithFooter
                    (
                       $"Page {_moduleListOffset + 1} of {_moduleListPages.Count} " +
                       $"({_modules.Count} modules)"
                    );

                    return Task.FromResult(eb.Build());
                }
                case HelpWizardState.CommandListing:
                {
                    var eb = _feedback.CreateEmbedBase();
                    eb.WithTitle($"Available commands in {_currentModule.Name}");

                    var description = $"Click {EnterModule} and type in a command to see detailed information." +
                                      "\n\n" +
                                      $"{_currentModule.Remarks}";

                    eb.WithDescription(description);

                    var pages = _commandListPages[_currentModule];
                    var page = pages[_commandListOffset];
                    eb.WithFields(page);

                    eb.WithFooter
                    (
                        $"Page {_commandListOffset + 1} of {pages.Count} " +
                        $"({pages.Sum(p => p.Count)} commands)"
                    );

                    return Task.FromResult(eb.Build());
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <inheritdoc/>
        public override Task HandleAddedInteractionAsync(SocketReaction reaction)
        {
            if (reaction.Emote.Equals(Exit))
            {
                this.Interactivity.DeleteInteractiveMessageAsync(this);
            }

            if (reaction.Emote.Equals(Info))
            {
                return DisplayHelpTextAsync();
            }

            switch (_state)
            {
                case HelpWizardState.ModuleListing:
                {
                    return ConsumeModuleListInteractionAsync(reaction);
                }
                case HelpWizardState.CommandListing:
                {
                    return ConsumeCommandListInteractionAsync(reaction);
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        private async Task ConsumeCommandListInteractionAsync([NotNull] SocketReaction reaction)
        {
            var emote = reaction.Emote;

            if (!this.AcceptedEmotes.Contains(emote))
            {
                return;
            }

            if (emote.Equals(Back))
            {
                _state = HelpWizardState.ModuleListing;
                await UpdateAsync();

                return;
            }

            if (emote.Equals(Next))
            {
                if (_commandListOffset + 1 > _commandListPages[_currentModule].Count - 1)
                {
                    return;
                }

                _commandListOffset++;
            }
            else if (emote.Equals(Previous))
            {
                if (_commandListOffset - 1 < 0)
                {
                    return;
                }

                _commandListOffset--;
            }
            else if (emote.Equals(First))
            {
                _commandListOffset = 0;
            }
            else if (emote.Equals(Last))
            {
                _commandListOffset = _commandListPages[_currentModule].Count - 1;
            }
            else if (emote.Equals(EnterModule))
            {
                bool Filter(IUserMessage m) => m.Author.Id == reaction.UserId;

                if (!_currentModule.Commands.Any())
                {
                    await _feedback.SendWarningAndDeleteAsync
                    (
                        this.MessageContext,
                        "There aren't any commands available in the module.",
                        TimeSpan.FromSeconds(10)
                    );

                    return;
                }

                await _feedback.SendConfirmationAndDeleteAsync
                (
                    this.MessageContext,
                    "Please enter a command name.",
                    TimeSpan.FromSeconds(45)
                );

                var messageResult = await this.Interactivity.GetNextMessageAsync
                (
                    this.MessageContext.Channel,
                    Filter,
                    TimeSpan.FromSeconds(45)
                );

                if (messageResult.IsSuccess)
                {
                    var searchText = messageResult.Entity.Content;
                    var commandSearchTerms = _currentModule
                        .GetAllCommands()
                        .Select(c => c.GetFullCommand());

                    var findCommandResult = commandSearchTerms.BestLevenshteinMatch(searchText, 0.5);
                    if (findCommandResult.IsSuccess)
                    {
                        var foundName = findCommandResult.Entity;

                        var commandGroup = _currentModule.Commands
                            .Where(c => c.GetFullCommand() == foundName)
                            .GroupBy(c => c.Aliases.OrderByDescending(a => a).First())
                            .First();

                        var eb = _help.CreateDetailedCommandInfoEmbed(commandGroup);

                        await _feedback.SendEmbedAndDeleteAsync
                        (
                            this.MessageContext.Channel,
                            eb.Build(),
                            TimeSpan.FromSeconds(45)
                        );
                    }
                    else
                    {
                        var eb = _feedback.CreateEmbedBase(Color.Orange);
                        eb.WithDescription("I couldn't find a sufficiently close command to that.");

                        await _feedback.SendEmbedAndDeleteAsync
                        (
                            this.MessageContext.Channel,
                            eb.Build()
                        );
                    }
                }
            }

            await UpdateAsync();
        }

        private async Task ConsumeModuleListInteractionAsync([NotNull] SocketReaction reaction)
        {
            var emote = reaction.Emote;

            if (!this.AcceptedEmotes.Contains(emote))
            {
                return;
            }

            if (emote.Equals(Next))
            {
                if (_moduleListOffset + 1 > _moduleListPages.Count - 1)
                {
                    return;
                }

                _moduleListOffset++;
            }
            else if (emote.Equals(Previous))
            {
                if (_moduleListOffset - 1 < 0)
                {
                    return;
                }

                _moduleListOffset--;
            }
            else if (emote.Equals(First))
            {
                _moduleListOffset = 0;
            }
            else if (emote.Equals(Last))
            {
                _moduleListOffset = _moduleListPages.Count - 1;
            }
            else if (emote.Equals(EnterModule))
            {
                bool Filter(IUserMessage m) => m.Author.Id == reaction.UserId;

                if (!_modules.Any())
                {
                    await _feedback.SendWarningAndDeleteAsync
                    (
                        this.MessageContext,
                        "There aren't any modules available in the bot.",
                        TimeSpan.FromSeconds(10)
                    );

                    return;
                }

                await _feedback.SendConfirmationAndDeleteAsync
                (
                    this.MessageContext,
                    "Please enter a module name.",
                    TimeSpan.FromSeconds(45)
                );

                var messageResult = await this.Interactivity.GetNextMessageAsync
                (
                    this.MessageContext.Channel,
                    Filter,
                    TimeSpan.FromSeconds(45)
                );

                if (messageResult.IsSuccess)
                {
                    var tryStartCategoryResult = await OpenModule(messageResult.Entity.Content);
                    if (!tryStartCategoryResult.IsSuccess)
                    {
                        await _feedback.SendWarningAndDeleteAsync
                        (
                            this.MessageContext,
                            tryStartCategoryResult.ErrorReason,
                            TimeSpan.FromSeconds(10)
                        );

                        return;
                    }
                }
            }

            await UpdateAsync();
        }

        /// <summary>
        /// Attempts to open the information page for the given module in the wizard.
        /// </summary>
        /// <param name="moduleName">The name of the module.</param>
        /// <returns>A execution result which may or may not have succeeded.</returns>
        public Task<ModifyEntityResult> OpenModule(string moduleName)
        {
            var moduleSearchTerms = _modules.Select
            (
                m => new List<string>(m.Aliases) { m.Name }
            )
            .SelectMany(t => t);

            var getModuleResult = moduleSearchTerms.BestLevenshteinMatch(moduleName, 0.75);
            if (!getModuleResult.IsSuccess)
            {
                return Task.FromResult(ModifyEntityResult.FromError(getModuleResult));
            }

            var bestMatch = getModuleResult.Entity;

            var module = _modules.First(m => m.Name == bestMatch || m.Aliases.Contains(bestMatch));
            if (!_commandListPages.ContainsKey(module))
            {
                var commandPages = BuildCommandListPages(module);
                _commandListPages.Add(module, commandPages);
            }

            _commandListOffset = 0;
            _currentModule = module;

            _state = HelpWizardState.CommandListing;

            return Task.FromResult(ModifyEntityResult.FromSuccess());
        }

        [SuppressMessage("Style", "SA1118", Justification = "Large text blocks.")]
        private async Task DisplayHelpTextAsync()
        {
            var eb = new EmbedBuilder();
            eb.WithColor(Color.DarkPurple);

            switch (_state)
            {
                case HelpWizardState.ModuleListing:
                {
                    eb.WithTitle("Help: Command modules");
                    eb.AddField
                    (
                        "Usage",
                        "Use the navigation buttons to scroll through the available modules. To view " +
                        $"commands in a module, press {EnterModule} and type in the name. The search algorithm is " +
                        "quite lenient, so you may find that things work fine even with typos.\n" +
                        "\n" +
                        $"You can quit at any point by pressing {Exit}."
                    );
                    break;
                }
                case HelpWizardState.CommandListing:
                {
                    eb.WithTitle("Help: Module commands");
                    eb.AddField
                    (
                        "Usage",
                        "Use the navigation buttons to scroll through the available commands. To view " +
                        $"detailed information about a command, press {EnterModule} and type in the name. To go back " +
                        $"to the module list, press {Back}." +
                        "\n" +
                        $"You can quit at any point by pressing {Exit}."
                    );
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            await _feedback.SendEmbedAndDeleteAsync(this.MessageContext.Channel, eb.Build(), TimeSpan.FromSeconds(30));
        }

        /// <remarks>
        /// This method forwards to the added interaction handler, resulting in removals being treated the same as
        /// additions. Click for click, tat for tot.
        /// </remarks>
        /// <inheritdoc/>
        public override Task HandleRemovedInteractionAsync(SocketReaction reaction)
            => HandleAddedInteractionAsync(reaction);

        /// <inheritdoc/>
        public IEnumerable<IEmote> GetCurrentPageEmotes()
        {
            switch (_state)
            {
                case HelpWizardState.ModuleListing:
                {
                    return new[] { First, Previous, Next, Last, Info, Exit, EnterModule };
                }
                case HelpWizardState.CommandListing:
                {
                    return new[] { First, Previous, Next, Last, Info, Exit, EnterModule,  Back };
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        [NotNull]
        private IEnumerable<IEmote> GetCurrentPageRejectedEmotes()
        {
            switch (_state)
            {
                case HelpWizardState.ModuleListing:
                {
                    return new[] { Back };
                }
                case HelpWizardState.CommandListing:
                {
                    return new IEmote[] { };
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
