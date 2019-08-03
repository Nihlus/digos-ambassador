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
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Attributes;
using DIGOS.Ambassador.Core.Services.Content;
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Servers;
using DIGOS.Ambassador.Services.Users;

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
    public class CommandBehaviour : ContinuousBehaviour
    {
        private readonly AmbyDatabaseContext _database;

        private readonly IServiceProvider _services;

        private readonly ServerService _servers;
        private readonly UserService _users;
        private readonly UserFeedbackService _feedback;
        private readonly PrivacyService _privacy;
        private readonly ContentService _content;
        private readonly CommandService _commands;
        private readonly PermissionService _permissions;
        private readonly HelpService _help;

        /// <summary>
        /// Gets the commands that are currently running.
        /// </summary>
        private ConcurrentQueue<Task> RunningCommands { get; }

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
        /// <param name="users">The user service.</param>
        /// <param name="servers">The server service.</param>
        public CommandBehaviour
        (
            DiscordSocketClient client,
            AmbyDatabaseContext database,
            IServiceProvider services,
            UserFeedbackService feedback,
            PrivacyService privacy,
            ContentService content,
            CommandService commands,
            PermissionService permissions,
            HelpService help,
            UserService users,
            ServerService servers
        )
            : base(client)
        {
            _database = database;
            _services = services;
            _feedback = feedback;
            _privacy = privacy;
            _content = content;
            _commands = commands;
            _permissions = permissions;
            _help = help;
            _users = users;
            _servers = servers;

            this.RunningCommands = new ConcurrentQueue<Task>();
        }

        /// <inheritdoc/>
        protected override async Task OnTickAsync(CancellationToken ct)
        {
            if (this.RunningCommands.TryDequeue(out var command))
            {
                if (command.IsCompleted)
                {
                    try
                    {
                        await command;
                    }
                    catch (Exception e)
                    {
                        // Nom nom nom
                        this.Log.Error("Error in command.", e);
                    }
                }
                else
                {
                    // If it's not done yet, stick it back on the queue.
                    this.RunningCommands.Enqueue(command);
                }
            }

            // And we'll also run a short delay so we don't eat all the CPU time
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }

        private async Task SaneExecuteCommandWrapperAsync(ICommandContext context, int argumentPos)
        {
            // Create a service scope for this command
            using (var scope = _services.CreateScope())
            {
                var result = await _commands.ExecuteAsync(context, argumentPos, scope.ServiceProvider);
                await HandleCommandResultAsync(context, result, argumentPos);
            }
        }

        /// <inheritdoc />
        protected override Task OnStartingAsync()
        {
            base.OnStartingAsync();

            this.Client.MessageReceived += OnMessageReceived;
            this.Client.MessageUpdated += OnMessageUpdated;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override async Task OnStoppingAsync()
        {
            await base.OnStoppingAsync();

            this.Client.MessageReceived -= OnMessageReceived;
            this.Client.MessageUpdated -= OnMessageUpdated;
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

            // Perform first-time user checks, making sure the user has their default permissions, has consented, etc
            if (!await _privacy.HasUserConsentedAsync(_database, context.User) && !IsPrivacyExemptCommand(context, argumentPos))
            {
                // Ask for consent
                var userDMChannel = await arg.Author.GetOrCreateDMChannelAsync();
                var result = await _privacy.RequestConsentAsync(userDMChannel, _content, _feedback);
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

            var guild = (message.Channel as SocketGuildChannel)?.Guild;
            if (guild != null)
            {
                var registerUserResult = await _users.GetOrRegisterUserAsync(_database, arg.Author);
                if (!registerUserResult.IsSuccess)
                {
                    return;
                }

                var user = registerUserResult.Entity;

                var server = await _servers.GetOrRegisterServerAsync(_database, guild);

                // Grant permissions to new users
                if (!server.IsUserKnown(arg.Author))
                {
                    await _permissions.GrantDefaultPermissionsAsync(_database, guild, arg.Author);
                    server.KnownUsers.Add(user);

                    await _database.SaveChangesAsync();
                }
            }

            // Run the command asynchronously, but we'll await it later
            this.RunningCommands.Enqueue(SaneExecuteCommandWrapperAsync(context, argumentPos));
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
    }
}
