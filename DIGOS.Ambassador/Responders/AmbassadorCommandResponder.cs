//
//  AmbassadorCommandResponder.cs
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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using DIGOS.Ambassador.Core.Database;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Feedback.Errors;
using DIGOS.Ambassador.Discord.Feedback.Results;
using DIGOS.Ambassador.Plugins.Core.Attributes;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Remora.Commands.Results;
using Remora.Commands.Services;
using Remora.Commands.Trees;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Commands.Results;
using Remora.Discord.Commands.Services;
using Remora.Discord.Core;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DIGOS.Ambassador.Responders
{
    /// <summary>
    /// Responds to commands.
    /// </summary>
    [UsedImplicitly]
    public class AmbassadorCommandResponder : IResponder<IMessageCreate>, IResponder<IMessageUpdate>
    {
        private readonly CommandService _commandService;
        private readonly ICommandResponderOptions _options;
        private readonly ExecutionEventCollectorService _eventCollector;
        private readonly IServiceProvider _services;
        private readonly UserFeedbackService _userFeedback;
        private readonly ContextInjectionService _contextInjection;
        private readonly PrivacyService _privacy;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbassadorCommandResponder"/> class.
        /// </summary>
        /// <param name="commandService">The command service.</param>
        /// <param name="options">The command responder options.</param>
        /// <param name="eventCollector">The event collector.</param>
        /// <param name="services">The available services.</param>
        /// <param name="userFeedback">The user feedback service.</param>
        /// <param name="contextInjection">The context injection service.</param>
        /// <param name="privacy">The privacy service.</param>
        public AmbassadorCommandResponder
        (
            CommandService commandService,
            IOptions<AmbassadorCommandResponderOptions> options,
            ExecutionEventCollectorService eventCollector,
            IServiceProvider services,
            UserFeedbackService userFeedback,
            ContextInjectionService contextInjection,
            PrivacyService privacy
        )
        {
            _commandService = commandService;
            _services = services;
            _userFeedback = userFeedback;
            _contextInjection = contextInjection;
            _privacy = privacy;
            _eventCollector = eventCollector;
            _options = options.Value;
        }

        /// <inheritdoc/>
        public async Task<Result> RespondAsync
        (
            IMessageCreate? gatewayEvent,
            CancellationToken ct = default
        )
        {
            if (gatewayEvent is null)
            {
                return Result.FromSuccess();
            }

            if (_options.Prefix is not null)
            {
                if (!gatewayEvent.Content.StartsWith(_options.Prefix))
                {
                    return Result.FromSuccess();
                }
            }

            var author = gatewayEvent.Author;
            if (author.IsBot.HasValue && author.IsBot.Value)
            {
                return Result.FromSuccess();
            }

            if (author.IsSystem.HasValue && author.IsSystem.Value)
            {
                return Result.FromSuccess();
            }

            var context = new MessageContext
            (
                gatewayEvent.ChannelID,
                author,
                gatewayEvent.ID,
                new PartialMessage
                (
                    gatewayEvent.ID,
                    gatewayEvent.ChannelID,
                    gatewayEvent.GuildID,
                    new Optional<IUser>(gatewayEvent.Author),
                    gatewayEvent.Member,
                    gatewayEvent.Content,
                    gatewayEvent.Timestamp,
                    gatewayEvent.EditedTimestamp,
                    gatewayEvent.IsTTS,
                    gatewayEvent.MentionsEveryone,
                    new Optional<IReadOnlyList<IUserMention>>(gatewayEvent.Mentions),
                    new Optional<IReadOnlyList<Snowflake>>(gatewayEvent.MentionedRoles),
                    gatewayEvent.MentionedChannels,
                    new Optional<IReadOnlyList<IAttachment>>(gatewayEvent.Attachments),
                    new Optional<IReadOnlyList<IEmbed>>(gatewayEvent.Embeds),
                    gatewayEvent.Reactions,
                    gatewayEvent.Nonce,
                    gatewayEvent.IsPinned,
                    gatewayEvent.WebhookID,
                    gatewayEvent.Type,
                    gatewayEvent.Activity,
                    gatewayEvent.Application,
                    gatewayEvent.ApplicationID,
                    gatewayEvent.MessageReference,
                    gatewayEvent.Flags,
                    gatewayEvent.ReferencedMessage,
                    gatewayEvent.Interaction,
                    gatewayEvent.Thread,
                    gatewayEvent.Components,
                    gatewayEvent.StickerItems
                )
            );

            _contextInjection.Context = context;
            return await RelayResultToUserAsync
            (
                context,
                await ExecuteCommandAsync(gatewayEvent.Content, context, ct),
                ct
            );
        }

        /// <inheritdoc/>
        public async Task<Result> RespondAsync
        (
            IMessageUpdate? gatewayEvent,
            CancellationToken ct = default
        )
        {
            if (gatewayEvent is null)
            {
                return Result.FromSuccess();
            }

            if (!gatewayEvent.Content.HasValue)
            {
                return Result.FromSuccess();
            }

            if (_options.Prefix is not null)
            {
                if (!gatewayEvent.Content.Value.StartsWith(_options.Prefix))
                {
                    return Result.FromSuccess();
                }
            }

            if (!gatewayEvent.Author.HasValue)
            {
                return Result.FromSuccess();
            }

            var author = gatewayEvent.Author.Value!;
            if (author.IsBot.HasValue && author.IsBot.Value)
            {
                return Result.FromSuccess();
            }

            if (author.IsSystem.HasValue && author.IsSystem.Value)
            {
                return Result.FromSuccess();
            }

            var context = new MessageContext
            (
                gatewayEvent.ChannelID.Value,
                author,
                gatewayEvent.ID.Value,
                gatewayEvent
            );

            _contextInjection.Context = context;
            return await RelayResultToUserAsync
            (
                context,
                await ExecuteCommandAsync(gatewayEvent.Content.Value!, context, ct),
                ct
            );
        }

        private async Task<IResult> ExecuteCommandAsync
        (
            string content,
            ICommandContext commandContext,
            CancellationToken ct = default
        )
        {
            using var transaction = TransactionFactory.Create();

            // Strip off the prefix
            if (_options.Prefix is not null)
            {
                content = content
                [
                    (content.IndexOf(_options.Prefix, StringComparison.Ordinal) + _options.Prefix.Length)..
                ];
            }

            // First of all, check user consent
            var hasConsented = await _privacy.HasUserConsentedAsync(commandContext.User.ID, ct);

            var searchOptions = new TreeSearchOptions(StringComparison.OrdinalIgnoreCase);
            var potentialCommands = _commandService.Tree.Search(content, searchOptions).ToList();
            var atLeastOneRequiresConsent = potentialCommands.Any
            (
                c =>
                c.Node.CommandMethod.GetCustomAttribute<PrivacyExemptAttribute>() is null
            );

            if (!hasConsented && atLeastOneRequiresConsent)
            {
                var requestConsent = await _privacy.RequestConsentAsync(commandContext.User.ID, ct);
                if (!requestConsent.IsSuccess)
                {
                    return requestConsent;
                }

                return Result.FromSuccess();
            }

            // Run any user-provided pre execution events
            var preExecution = await _eventCollector.RunPreExecutionEvents(commandContext, ct);
            if (!preExecution.IsSuccess)
            {
                return preExecution;
            }

            // Run the actual command
            var executeResult = await _commandService.TryExecuteAsync
            (
                content,
                searchOptions: searchOptions,
                services: _services,
                ct: ct
            );

            if (!executeResult.IsSuccess)
            {
                return Result.FromError(executeResult);
            }

            // Run any user-provided post execution events
            var postExecution = await _eventCollector.RunPostExecutionEvents
            (
                commandContext,
                executeResult.Entity,
                ct
            );

            if (!postExecution.IsSuccess)
            {
                return postExecution;
            }

            if (executeResult.Entity.IsSuccess)
            {
                transaction.Complete();
            }

            return executeResult.Entity;
        }

        private async Task<Result> RelayResultToUserAsync<TResult>
        (
            ICommandContext context,
            TResult commandResult,
            CancellationToken ct = default
        )
            where TResult : IResult
        {
            if (commandResult.IsSuccess)
            {
                if (commandResult is not Result<UserMessage> messageResult)
                {
                    return Result.FromSuccess();
                }

                // Relay the message to the user
                var sendMessage = await _userFeedback.SendContextualMessageAsync
                (
                    context.User.ID,
                    messageResult.Entity!,
                    ct
                );

                return !sendMessage.IsSuccess
                    ? Result.FromError(sendMessage)
                    : Result.FromSuccess();
            }

            var error = commandResult.Error;
            switch (error)
            {
                case ParameterParsingError:
                case AmbiguousCommandInvocationError:
                case ConditionNotSatisfiedError:
                case UserError:
                case { } when error.GetType().IsGenericType &&
                              error.GetType().GetGenericTypeDefinition() == typeof(ParsingError<>):
                {
                    // Alert the user, and don't complete the transaction
                    var sendError = await _userFeedback.SendContextualErrorAsync
                    (
                        context.User.ID,
                        error.Message,
                        ct
                    );

                    return sendError.IsSuccess
                        ? Result.FromSuccess()
                        : Result.FromError(sendError);
                }
                default:
                {
                    return Result.FromError(commandResult.Error!);
                }
            }
        }
    }
}
