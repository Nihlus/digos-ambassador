//
//  AmbassadorCommandBehaviour.cs
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Amby.Services;
using DIGOS.Ambassador.Plugins.Core.Attributes;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Help.Services;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Discord.Commands.Behaviours;

namespace DIGOS.Ambassador.Plugins.Amby.Behaviours
{
    /// <summary>
    /// Acts as a behaviour for invoking commands, and logging their results.
    /// </summary>
    public class AmbassadorCommandBehaviour : CommandBehaviour
    {
        private readonly UserFeedbackService _feedback;
        private readonly PrivacyService _privacy;
        private readonly ContentService _content;
        private readonly PortraitService _portraits;
        private readonly HelpService _help;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbassadorCommandBehaviour"/> class.
        /// </summary>
        /// <param name="client">The discord client.</param>
        /// <param name="serviceScope">The service scope in use.</param>
        /// <param name="logger">The logging instance for this type.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="privacy">The privacy service.</param>
        /// <param name="content">The content service.</param>
        /// <param name="commands">The command service.</param>
        /// <param name="help">The help service.</param>
        /// <param name="portraits">The portrait service.</param>
        public AmbassadorCommandBehaviour
        (
            DiscordSocketClient client,
            IServiceScope serviceScope,
            ILogger<AmbassadorCommandBehaviour> logger,
            UserFeedbackService feedback,
            PrivacyService privacy,
            ContentService content,
            CommandService commands,
            HelpService help,
            PortraitService portraits
        )
            : base(client, serviceScope, logger, commands)
        {
            _feedback = feedback;
            _privacy = privacy;
            _content = content;
            _help = help;
            _portraits = portraits;
        }

        /// <inheritdoc />
        protected override async Task ConfigureFiltersAsync(List<Func<SocketCommandContext, Task<bool>>> commandFilters)
        {
            // Include the base filters
            await base.ConfigureFiltersAsync(commandFilters);

            commandFilters.Add(HasConsentedOrCommandDoesNotRequireConsentAsync);
        }

        /// <inheritdoc />
        protected override async Task OnCommandFailedAsync
        (
            SocketCommandContext context,
            int commandStart,
            ExecuteResult result
        )
        {
            switch (result.Error)
            {
                case CommandError.UnknownCommand:
                {
                    break;
                }
                case CommandError.Exception:
                {
                    await HandleInternalErrorAsync(context, result);
                    break;
                }
                case CommandError.ObjectNotFound:
                case CommandError.MultipleMatches:
                case CommandError.Unsuccessful:
                case CommandError.UnmetPrecondition:
                case CommandError.ParseFailed:
                case CommandError.BadArgCount:
                {
                    var userDMChannel = await context.Message.Author.GetOrCreateDMChannelAsync();

                    try
                    {
                        var errorEmbed = _feedback.CreateFeedbackEmbed
                        (
                            context.User,
                            Color.Red,
                            result.ErrorReason
                        );

                        await userDMChannel.SendMessageAsync(string.Empty, false, errorEmbed);

                        var searchResult = this.Commands.Search(context, commandStart);
                        if (searchResult.Commands.Any())
                        {
                            await userDMChannel.SendMessageAsync
                            (
                                string.Empty,
                                false,
                                _help.CreateCommandUsageEmbed(searchResult.Commands)
                            );
                        }
                    }
                    catch (HttpException hex) when (hex.WasCausedByDMsNotAccepted())
                    {
                    }
                    finally
                    {
                        await userDMChannel.CloseAsync();
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Determines whether or not the user has consented to data storage and handling under the GDPR, or if the
        /// command that is being executed is exempt from GDPR considerations.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>true if the user has consented or the command does not require consent; otherwise, false.</returns>
        private async Task<bool> HasConsentedOrCommandDoesNotRequireConsentAsync(SocketCommandContext context)
        {
            // Find the argument pos
            if (!FindCommandStartPosition(context.Message, out var argumentPos))
            {
                return true;
            }

            if (await _privacy.HasUserConsentedAsync(context.User) || IsPrivacyExemptCommand(context, argumentPos))
            {
                return true;
            }

            // Ask for consent
            var userDMChannel = await context.User.GetOrCreateDMChannelAsync();
            var result = await _privacy.RequestConsentAsync(userDMChannel);
            if (result.IsSuccess)
            {
                return false;
            }

            const string response = "It seems like you're not accepting DMs from non-friends. Please enable " +
                                    "this, so you can read the bot's privacy policy and consent to data " +
                                    "handling and processing.";

            await _feedback.SendWarningAsync(context, response);

            return false;
        }

        /// <summary>
        /// Determines whether the currently running command is exempt from user consent.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="argumentPos">The position in the message where the command begins.</param>
        /// <returns>true if the command is exempt from consent; otherwise, false.</returns>
        private bool IsPrivacyExemptCommand(ICommandContext context, int argumentPos)
        {
            // We need to gather consent from the user
            var commandSearchResult = this.Commands.Search(context, argumentPos);
            if (!commandSearchResult.IsSuccess)
            {
                return false;
            }

            // Some command we recognize as being exempt from the privacy regulations
            // (mostly privacy commands) - if this is one of them, just run it
            var potentialPrivacyCommand = commandSearchResult.Commands.FirstOrDefault().Command;
            if (potentialPrivacyCommand.Attributes.Any(a => a is PrivacyExemptAttribute))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handles the result of an internal error, generating a short error report for the user and instructing them
        /// on how to proceed.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <param name="result">The result of the command.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task HandleInternalErrorAsync
        (
            ICommandContext context,
            ExecuteResult result
        )
        {
            // Log the exception for later debugging purposes
            this.Log.LogError(result.Exception.ToString());

            // Alert the user, explain what happened, and ask them to make a bug report.
            var userDMChannel = await context.Message.Author.GetOrCreateDMChannelAsync();

            try
            {
                var eb = _feedback.CreateEmbedBase(Color.Red);
                eb.WithTitle("Internal Error");
                eb.WithDescription
                (
                    "Oops! Looks like you've found a bug in the bot - fear not, though. If we work together, we'll " +
                    "have it licked in no time at all.\n" +
                    "\n" +
                    "I've prepared a short report of the technical details of what happened, but it's not going " +
                    "to be worth much without your help. In order to fix this problem, it would be extremely " +
                    "helpful if you wrote down the exact steps you did to encounter this error, and post them along " +
                    "with the generated report on the GitHub repository. You can go there by clicking the link " +
                    "in this message.\n" +
                    "\n" +
                    "The report contains some information about you, so please check it and erase anything you don't" +
                    " want to share before passing it on.\n" +
                    "\n" +
                    "Your assistance is essential, and very much appreciated!"
                );

                eb.WithAuthor(context.Client.CurrentUser);
                eb.WithCurrentTimestamp();
                eb.WithFooter
                (
                    "If you don't have an account on github, you can also send a DM to Jax#7487, who is the main" +
                    " developer of the bot."
                );
                eb.WithThumbnailUrl(_portraits.BrokenAmbyUri.ToString());

                var reportEmbed = _feedback.CreateEmbedBase(Color.Red);
                reportEmbed.WithTitle("Click here to create a new issue");
                reportEmbed.WithUrl(_content.AutomaticBugReportCreationUri.ToString());

                await using var ms = new MemoryStream();
                var now = DateTime.UtcNow;

                await using (var sw = new StreamWriter(ms, Encoding.Default, 1024, true))
                {
                    await sw.WriteLineAsync("Automatic bug report");
                    await sw.WriteLineAsync("====================");
                    await sw.WriteLineAsync();
                    await sw.WriteLineAsync($"Generated at: {now}");

                    var entryAssembly = Assembly.GetEntryAssembly();
                    if (!(entryAssembly is null))
                    {
                        await sw.WriteLineAsync($"Bot version: {entryAssembly.GetName().Version}");
                    }

                    await sw.WriteLineAsync($"Command message link: {context.Message.GetJumpUrl()}");
                    await sw.WriteLineAsync
                    (
                        $"Ran by: {context.User.Username}#{context.User.Discriminator} ({context.User.Id})"
                    );
                    await sw.WriteLineAsync
                    (
                        $"In: {(context.Guild is null ? "DM" : $"{context.Guild.Name} ({context.Guild.Id})")}"
                    );
                    await sw.WriteLineAsync($"Full command: {context.Message.Content}");
                    await sw.WriteLineAsync();
                    await sw.WriteLineAsync("### Stack Trace");
                    await sw.WriteLineAsync(result.Exception.ToString());
                }

                // Rewind the stream before passing it along
                ms.Position = 0;
                await userDMChannel.SendMessageAsync(string.Empty, false, eb.Build());

                var date = now.ToShortDateString();
                var time = now.ToShortTimeString();
                await userDMChannel.SendFileAsync(ms, $"bug-report-{date}-{time}.md", string.Empty, false, reportEmbed.Build());
            }
            catch (HttpException hex) when (hex.WasCausedByDMsNotAccepted())
            {
            }
            finally
            {
                await userDMChannel.CloseAsync();
            }
        }
    }
}
