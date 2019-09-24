//
//  CommandBehaviour.cs
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Discord.Behaviours;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Core.Attributes;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Services;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;

using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace DIGOS.Ambassador.Behaviours
{
    /// <summary>
    /// Acts as a behaviour for invoking commands, and logging their results.
    /// </summary>
    public class CommandBehaviour : ClientEventBehaviour
    {
        private readonly IServiceProvider _services;

        private readonly UserFeedbackService _feedback;
        private readonly PrivacyService _privacy;
        private readonly ContentService _content;
        private readonly CommandService _commands;
        private readonly HelpService _help;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandBehaviour"/> class.
        /// </summary>
        /// <param name="client">The discord client.</param>
        /// <param name="services">The available services.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="privacy">The privacy service.</param>
        /// <param name="content">The content service.</param>
        /// <param name="commands">The command service.</param>
        /// <param name="help">The help service.</param>
        public CommandBehaviour
        (
            DiscordSocketClient client,
            IServiceProvider services,
            UserFeedbackService feedback,
            PrivacyService privacy,
            ContentService content,
            CommandService commands,
            HelpService help
        )
            : base(client)
        {
            _services = services;
            _feedback = feedback;
            _privacy = privacy;
            _content = content;
            _commands = commands;
            _help = help;
        }

        /// <inheritdoc />
        protected override async Task MessageUpdated
        (
            Cacheable<IMessage, ulong> oldMessage,
            [CanBeNull] SocketMessage updatedMessage,
            ISocketMessageChannel messageChannel
        )
        {
            if (updatedMessage is null)
            {
                return;
            }

            // Ignore all changes except text changes
            var isTextUpdate = updatedMessage.EditedTimestamp.HasValue &&
                               updatedMessage.EditedTimestamp.Value > DateTimeOffset.Now - 1.Minutes();

            if (!isTextUpdate)
            {
                return;
            }

            await MessageReceived(updatedMessage);
        }

        /// <inheritdoc />
        protected override async Task MessageReceived(SocketMessage arg)
        {
            if (!(arg is SocketUserMessage message))
            {
                return;
            }

            if (arg.Author.IsBot || arg.Author.IsWebhook)
            {
                return;
            }

            var argumentPos = 0;
            if (!(message.HasCharPrefix('!', ref argumentPos) || message.HasMentionPrefix(this.Client.CurrentUser, ref argumentPos)))
            {
                return;
            }

            if (message.Content.Length < 2)
            {
                return;
            }

            if (!message.Content.Any(char.IsLetterOrDigit))
            {
                return;
            }

            var context = new SocketCommandContext(this.Client, message);

            // Perform first-time user checks, making sure the user has their default permissions, has consented, etc
            if (!await _privacy.HasUserConsentedAsync(context.User) && !IsPrivacyExemptCommand(context, argumentPos))
            {
                // Ask for consent
                var userDMChannel = await arg.Author.GetOrCreateDMChannelAsync();
                var result = await _privacy.RequestConsentAsync(userDMChannel);
                if (result.IsSuccess)
                {
                    return;
                }

                const string response = "It seems like you're not accepting DMs from non-friends. Please enable " +
                                        "this, so you can read the bot's privacy policy and consent to data " +
                                        "handling and processing.";

                await _feedback.SendWarningAsync(context, response);

                return;
            }

            // Create a service scope for this command
            using (var scope = _services.CreateScope())
            {
                var result = await _commands.ExecuteAsync(context, argumentPos, scope.ServiceProvider);
                await HandleCommandResultAsync(context, result, argumentPos);
            }
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
            var commandSearchResult = _commands.Search(context, argumentPos);
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
        /// Handles the result of a command, alerting the user if errors occurred.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <param name="result">The result of the command.</param>
        /// <param name="argumentPos">The position in the message string where the command starts.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task HandleCommandResultAsync
        (
            [NotNull] ICommandContext context,
            [NotNull] IResult result,
            int argumentPos
        )
        {
            if (result.IsSuccess)
            {
                return;
            }

            switch (result.Error)
            {
                case CommandError.Exception:
                {
                    await HandleInternalErrorAsync(context, result);
                    break;
                }
                case CommandError.UnknownCommand:
                {
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

                        var searchResult = _commands.Search(context, argumentPos);
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
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
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
            [NotNull] ICommandContext context,
            IResult result
        )
        {
            // Log the exception for later debugging purposes
            var executeResult = (ExecuteResult)result;
            this.Log.Error(executeResult.Exception);

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
                eb.WithThumbnailUrl(_content.BrokenAmbyUri.ToString());

                var reportEmbed = _feedback.CreateEmbedBase(Color.Red);
                reportEmbed.WithTitle("Click here to create a new issue");
                reportEmbed.WithUrl(_content.AutomaticBugReportCreationUri.ToString());

                using (var ms = new MemoryStream())
                {
                    var now = DateTime.UtcNow;

                    using (var sw = new StreamWriter(ms, Encoding.Default, 1024, true))
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
                        await sw.WriteLineAsync(executeResult.Exception.ToString());
                    }

                    // Rewind the stream before passing it along
                    ms.Position = 0;
                    await userDMChannel.SendMessageAsync(string.Empty, false, eb.Build());

                    var date = now.ToShortDateString();
                    var time = now.ToShortTimeString();
                    await userDMChannel.SendFileAsync(ms, $"bug-report-{date}-{time}.md", string.Empty, false, reportEmbed.Build());
                }
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
