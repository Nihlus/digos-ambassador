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
using System.Linq;
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

namespace DIGOS.Ambassador.Behaviours
{
    /// <summary>
    /// Acts as a behaviour for invoking commands, and logging their results.
    /// </summary>
    public class CommandBehaviour : BehaviourBase
    {
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

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task OnStoppingAsync()
        {
            this.Client.MessageReceived -= OnMessageReceived;
            this.Client.MessageUpdated -= OnMessageUpdated;

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

            var commandResult = await this.Commands.ExecuteAsync(context, argumentPos, this.Services);
            await HandleCommandResultAsync(context, commandResult, argumentPos);
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
                case CommandError.Exception:
                {
                    var userDMChannel = await context.Message.Author.GetOrCreateDMChannelAsync();

                    var errorEmbed = this.Feedback.CreateFeedbackEmbed(context.User, Color.Red, result.ErrorReason);
                    var searchResult = this.Commands.Search(context, argumentPos);

                    try
                    {
                        await userDMChannel.SendMessageAsync(string.Empty, false, errorEmbed);
                        await userDMChannel.SendMessageAsync
                        (
                            string.Empty,
                            false,
                            this.Help.CreateCommandUsageEmbed(searchResult.Commands)
                        );
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

            await HandleCommandResultAsync(context, result, argumentPos);
        }
    }
}
