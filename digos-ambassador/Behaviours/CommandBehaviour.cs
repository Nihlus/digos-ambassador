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

using DIGOS.Ambassador.Attributes;
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Users;

using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;

using Humanizer;
using JetBrains.Annotations;
using log4net;

namespace DIGOS.Ambassador.Behaviours
{
    /// <summary>
    /// Acts as a behaviour for invoking commands, and logging their results.
    /// </summary>
    public class CommandBehaviour : BehaviourBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CommandBehaviour));

        private readonly GlobalInfoContext Database;

        private readonly IServiceProvider Services;

        private readonly UserFeedbackService Feedback;
        private readonly PrivacyService Privacy;
        private readonly ContentService Content;
        private readonly CommandService Commands;
        private readonly PermissionService Permissions;
        private readonly HelpService Help;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandBehaviour"/> class.
        /// </summary>
        /// <param name="client">The discord client.</param>
        /// <param name="database">The database.</param>
        /// <param name="services">The available services.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="privacy">The privacy service.</param>
        /// <param name="content">The content service.</param>
        /// <param name="commands">The command service.</param>
        /// <param name="permissions">The permission service.</param>
        /// <param name="help">The help service.</param>
        public CommandBehaviour
        (
            DiscordSocketClient client,
            GlobalInfoContext database,
            IServiceProvider services,
            UserFeedbackService feedback,
            PrivacyService privacy,
            ContentService content,
            CommandService commands,
            PermissionService permissions,
            HelpService help
        )
            : base(client)
        {
            this.Database = database;
            this.Services = services;
            this.Feedback = feedback;
            this.Privacy = privacy;
            this.Content = content;
            this.Commands = commands;
            this.Permissions = permissions;
            this.Help = help;
        }

        /// <inheritdoc />
        protected override Task OnStartingAsync()
        {
            this.Client.MessageReceived += OnMessageReceived;
            this.Client.MessageUpdated += OnMessageUpdated;

            this.Commands.CommandExecuted += OnCommandExecuted;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task OnStoppingAsync()
        {
            this.Client.MessageReceived -= OnMessageReceived;
            this.Client.MessageUpdated -= OnMessageUpdated;

            this.Commands.CommandExecuted -= OnCommandExecuted;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles incoming messages, passing them to the command context handler.
        /// </summary>
        /// <param name="arg">The message coming in from the socket client.</param>
        /// <returns>A task representing the message handling.</returns>
        private async Task OnMessageReceived(SocketMessage arg)
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

            var context = new SocketCommandContext(this.Client, message);

            // Perform first-time user checks, making sure the user has their default permissions
            var guild = (message.Channel as SocketGuildChannel)?.Guild;
            if (guild != null)
            {
                if (!await this.Privacy.HasUserConsentedAsync(this.Database, context.User))
                {
                    // We need to gather consent from the user
                    var commandSearchResult = this.Commands.Search(context, argumentPos);
                    if (!commandSearchResult.IsSuccess)
                    {
                        return;
                    }

                    // Some command we recognize as being exempt from the privacy regulations
                    // (mostly privacy commands) - if this is one of them, just run it
                    var potentialPrivacyCommand = commandSearchResult.Commands.FirstOrDefault().Command;
                    if (potentialPrivacyCommand.Attributes.Any(a => a is PrivacyExemptAttribute))
                    {
                        var privacyExemptCommandResult = await this.Commands.ExecuteAsync
                        (
                            context,
                            argumentPos,
                            this.Services
                        );

                        await HandleCommandResultAsync(context, privacyExemptCommandResult, argumentPos);
                        return;
                    }

                    // else, ask for consent
                    var userDMChannel = await arg.Author.GetOrCreateDMChannelAsync();
                    var result = await this.Privacy.RequestConsentAsync(userDMChannel, this.Content, this.Feedback);
                    if (result.IsSuccess)
                    {
                        return;
                    }

                    const string response = "It seems like you're not accepting DMs from non-friends. Please enable " +
                                            "this, so you can read the bot's privacy policy and consent to data " +
                                            "handling and processing.";

                    await this.Feedback.SendWarningAsync(context, response);

                    return;
                }

                var registerUserResult = await this.Database.GetOrRegisterUserAsync(arg.Author);
                if (!registerUserResult.IsSuccess)
                {
                    return;
                }

                var user = registerUserResult.Entity;

                var server = await this.Database.GetOrRegisterServerAsync(guild);

                // Grant permissions to new users
                if (!server.IsUserKnown(arg.Author))
                {
                    await this.Permissions.GrantDefaultPermissionsAsync(this.Database, guild, arg.Author);
                    server.KnownUsers.Add(user);

                    await this.Database.SaveChangesAsync();
                }
            }

            await this.Commands.ExecuteAsync(context, argumentPos, this.Services);
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
                        var errorEmbed = this.Feedback.CreateFeedbackEmbed
                        (
                            context.User,
                            Color.Red,
                            result.ErrorReason
                        );

                        await userDMChannel.SendMessageAsync(string.Empty, false, errorEmbed);

                        var searchResult = this.Commands.Search(context, argumentPos);
                        if (searchResult.Commands.Any())
                        {
                            await userDMChannel.SendMessageAsync
                            (
                                string.Empty,
                                false,
                                this.Help.CreateCommandUsageEmbed(searchResult.Commands)
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
        /// <param name="command">The command that failed.</param>
        /// <param name="context">The context of the command.</param>
        /// <param name="result">The result of the command.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task HandleInternalErrorAsync
        (
            [NotNull] CommandInfo command,
            [NotNull] ICommandContext context,
            IResult result
        )
        {
            // Log the exception for later debugging purposes
            var executeResult = (ExecuteResult)result;
            Log.Error(executeResult.Exception);

            // Alert the user, explain what happened, and ask them to make a bug report.
            var userDMChannel = await context.Message.Author.GetOrCreateDMChannelAsync();

            try
            {
                var eb = this.Feedback.CreateEmbedBase(Color.Red);
                eb.WithTitle("Internal Error");
                eb.WithDescription
                (
                    "Oops! Looks like you've found a bug in the bot - fear not, though. If we work together, we'll " +
                    "have it licked in no time at all.\n" +
                    "\n" +
                    "I've prepared a short report of the technical details of what happened, but it's not going" +
                    "to be worth much without your help. In order to fix this problem, it would be extremely " +
                    "helpful if you wrote down the exact steps you did to encounter this error, and post them along" +
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
                eb.WithThumbnailUrl(this.Content.BrokenAmbyUri.ToString());

                eb.WithUrl(this.Content.AutomaticBugReportCreationUri.ToString());

                using (var ms = new MemoryStream())
                {
                    using (var sw = new StreamWriter(ms, Encoding.Default, 1024, true))
                    {
                        await sw.WriteLineAsync("Automatic bug report");
                        await sw.WriteLineAsync("====================");
                        await sw.WriteLineAsync();
                        await sw.WriteLineAsync($"Generated at: {DateTime.UtcNow}");
                        await sw.WriteLineAsync($"Bot version: {Assembly.GetEntryAssembly().GetName().Version}");
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
                    await userDMChannel.SendFileAsync(ms, "bug-report.md", string.Empty, false, eb.Build());
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

        /// <summary>
        /// Handles reparsing of edited messages.
        /// </summary>
        /// <param name="oldMessage">The old message.</param>
        /// <param name="updatedMessage">The new message.</param>
        /// <param name="messageChannel">The channel of the message.</param>
        private async Task OnMessageUpdated
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

            await OnMessageReceived(updatedMessage);
        }

        /// <summary>
        /// Handles the execution result of an asynchronous command, letting errors be handled properly.
        /// </summary>
        /// <param name="command">The command that was executed.</param>
        /// <param name="context">The context of the executed command.</param>
        /// <param name="result">The result of the execution.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task OnCommandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!command.IsSpecified)
            {
                return;
            }

            var message = context.Message;

            var argumentPos = 0;
            if (!(message.HasCharPrefix('!', ref argumentPos) || message.HasMentionPrefix(this.Client.CurrentUser, ref argumentPos)))
            {
                return;
            }

            if (result.Error == CommandError.Exception)
            {
                await HandleInternalErrorAsync(command.Value, context, result);
            }
            else
            {
                await HandleCommandResultAsync(context, result, argumentPos);
            }
        }
    }
}
