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

using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Interactivity.Messages;

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
        private readonly UserFeedbackService Feedback;

        [NotNull]
        private readonly HelpService Help;

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
        private readonly IReadOnlyList<ModuleInfo> Modules;

        private readonly IReadOnlyList<IReadOnlyList<EmbedFieldBuilder>> ModuleListPages;

        private readonly IDictionary<ModuleInfo, IReadOnlyList<IReadOnlyList<EmbedFieldBuilder>>> CommandListPages;

        [NotNull]
        private readonly Embed LoadingEmbed;

        /*
         * State fields
         */

        private HelpWizardState State;

        /*
         * Module list state
         */

        private int ModuleListOffset;

        /*
         * Command list state
         */

        [CanBeNull]
        private ModuleInfo CurrentModule;

        private int CommandListOffset;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelpWizard"/> class.
        /// </summary>
        /// <param name="modules">The modules available in the bot.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="help">The help service.</param>
        public HelpWizard
        (
            [NotNull, ItemNotNull] IReadOnlyList<ModuleInfo> modules,
            [NotNull] UserFeedbackService feedback,
            [NotNull] HelpService help
        )
        {
            this.Modules = modules;
            this.Feedback = feedback;
            this.Help = help;

            var eb = new EmbedBuilder();
            eb.WithTitle("Help & Information");
            eb.WithDescription("Loading...");

            this.LoadingEmbed = eb.Build();

            this.ModuleListPages = BuildModuleListPages();
            this.CommandListPages = new Dictionary<ModuleInfo, IReadOnlyList<IReadOnlyList<EmbedFieldBuilder>>>();
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
            foreach (var module in this.Modules)
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

                if (module == this.Modules.Last() && !pages.Contains(currentPage))
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

            var commandGroups = module.Commands.GroupBy(c => c.Aliases.OrderByDescending(a => a).First()).ToList();

            foreach (var commandGroup in commandGroups)
            {
                var helpField = this.Help.CreateCommandInfoEmbedField(commandGroup.First());

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
            return await channel.SendMessageAsync(string.Empty, embed: this.LoadingEmbed)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task UpdateAsync()
        {
            if (this.Message is null)
            {
                return;
            }

            await this.Message.ModifyAsync(m => m.Embed = this.LoadingEmbed);

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
            switch (this.State)
            {
                case HelpWizardState.ModuleListing:
                {
                    var eb = this.Feedback.CreateEmbedBase();
                    eb.WithTitle("Available command modules");
                    eb.WithDescription($"Click {EnterModule} and type in a module's name to see available commands.");

                    var page = this.ModuleListPages[this.ModuleListOffset];
                    eb.WithFields(page);

                    eb.WithFooter
                    (
                       $"Page {this.ModuleListOffset + 1} of {this.ModuleListPages.Count} " +
                       $"({this.Modules.Count} modules)"
                    );

                    return Task.FromResult(eb.Build());
                }
                case HelpWizardState.CommandListing:
                {
                    var eb = this.Feedback.CreateEmbedBase();
                    eb.WithTitle($"Available commands in {this.CurrentModule.Name}");

                    var description = $"Click {EnterModule} and type in a command to see detailed information." +
                                      "\n\n" +
                                      $"{this.CurrentModule.Remarks}";

                    eb.WithDescription(description);

                    var pages = this.CommandListPages[this.CurrentModule];
                    var page = pages[this.CommandListOffset];
                    eb.WithFields(page);

                    eb.WithFooter
                    (
                        $"Page {this.CommandListOffset + 1} of {pages.Count} " +
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

            switch (this.State)
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
                this.State = HelpWizardState.ModuleListing;
                await UpdateAsync();

                return;
            }

            if (emote.Equals(Next))
            {
                if (this.CommandListOffset + 1 > this.CommandListPages[this.CurrentModule].Count - 1)
                {
                    return;
                }

                this.CommandListOffset++;
            }
            else if (emote.Equals(Previous))
            {
                if (this.CommandListOffset - 1 < 0)
                {
                    return;
                }

                this.CommandListOffset--;
            }
            else if (emote.Equals(First))
            {
                this.CommandListOffset = 0;
            }
            else if (emote.Equals(Last))
            {
                this.CommandListOffset = this.CommandListPages[this.CurrentModule].Count - 1;
            }
            else if (emote.Equals(EnterModule))
            {
                bool Filter(IUserMessage m) => m.Author.Id == reaction.UserId;

                if (!this.CurrentModule.Commands.Any())
                {
                    await this.Feedback.SendWarningAndDeleteAsync
                    (
                        this.MessageContext,
                        "There aren't any commands available in the module.",
                        TimeSpan.FromSeconds(10)
                    );

                    return;
                }

                await this.Feedback.SendConfirmationAndDeleteAsync
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
                    var commandSearchTerms = this.CurrentModule.Commands.Select(c => c.GetActualName());

                    var findCommandResult = commandSearchTerms.BestLevenshteinMatch(searchText, 0.5);
                    if (findCommandResult.IsSuccess)
                    {
                        var foundName = findCommandResult.Entity;

                        var commandGroup = this.CurrentModule.Commands
                            .Where(c => c.GetActualName() == foundName)
                            .GroupBy(c => c.Aliases.OrderByDescending(a => a).First())
                            .First();

                        var eb = this.Help.CreateDetailedCommandInfoEmbed(commandGroup);

                        await this.Feedback.SendEmbedAndDeleteAsync
                        (
                            this.MessageContext.Channel,
                            eb.Build(),
                            TimeSpan.FromSeconds(45)
                        );
                    }
                    else
                    {
                        var eb = this.Feedback.CreateEmbedBase(Color.Orange);
                        eb.WithDescription("I couldn't find a sufficiently close command to that.");

                        await this.Feedback.SendEmbedAndDeleteAsync
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
                if (this.ModuleListOffset + 1 > this.ModuleListPages.Count - 1)
                {
                    return;
                }

                this.ModuleListOffset++;
            }
            else if (emote.Equals(Previous))
            {
                if (this.ModuleListOffset - 1 < 0)
                {
                    return;
                }

                this.ModuleListOffset--;
            }
            else if (emote.Equals(First))
            {
                this.ModuleListOffset = 0;
            }
            else if (emote.Equals(Last))
            {
                this.ModuleListOffset = this.ModuleListPages.Count - 1;
            }
            else if (emote.Equals(EnterModule))
            {
                bool Filter(IUserMessage m) => m.Author.Id == reaction.UserId;

                if (!this.Modules.Any())
                {
                    await this.Feedback.SendWarningAndDeleteAsync
                    (
                        this.MessageContext,
                        "There aren't any modules available in the bot.",
                        TimeSpan.FromSeconds(10)
                    );

                    return;
                }

                await this.Feedback.SendConfirmationAndDeleteAsync
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
                        await this.Feedback.SendWarningAndDeleteAsync
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
        public Task<ExecuteResult> OpenModule(string moduleName)
        {
            var moduleSearchTerms = this.Modules.Select
            (
                m => new List<string>(m.Aliases) { m.Name }
            )
            .SelectMany(t => t);

            var getModuleResult = moduleSearchTerms.BestLevenshteinMatch(moduleName, 0.75);
            if (!getModuleResult.IsSuccess)
            {
                return Task.FromResult(ExecuteResult.FromError(getModuleResult));
            }

            var bestMatch = getModuleResult.Entity;

            var module = this.Modules.First(m => m.Name == bestMatch || m.Aliases.Contains(bestMatch));
            if (!this.CommandListPages.ContainsKey(module))
            {
                var commandPages = BuildCommandListPages(module);
                this.CommandListPages.Add(module, commandPages);
            }

            this.CommandListOffset = 0;
            this.CurrentModule = module;

            this.State = HelpWizardState.CommandListing;

            return Task.FromResult(ExecuteResult.FromSuccess());
        }

        [SuppressMessage("Style", "SA1118", Justification = "Large text blocks.")]
        private async Task DisplayHelpTextAsync()
        {
            var eb = new EmbedBuilder();
            eb.WithColor(Color.DarkPurple);

            switch (this.State)
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

            await this.Feedback.SendEmbedAndDeleteAsync(this.MessageContext.Channel, eb.Build(), TimeSpan.FromSeconds(30));
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
            switch (this.State)
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
            switch (this.State)
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
