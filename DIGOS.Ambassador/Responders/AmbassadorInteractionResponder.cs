//
//  AmbassadorInteractionResponder.cs
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
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Plugins.Core.Attributes;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using JetBrains.Annotations;
using Remora.Commands.Extensions;
using Remora.Commands.Results;
using Remora.Commands.Services;
using Remora.Commands.Trees;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Commands.Results;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DIGOS.Ambassador.Responders
{
    /// <summary>
    /// Responds to interactions.
    /// </summary>
    [UsedImplicitly]
    public class AmbassadorInteractionResponder : IResponder<IInteractionCreate>
    {
        private readonly CommandService _commandService;
        private readonly IDiscordRestInteractionAPI _interactionAPI;
        private readonly ExecutionEventCollectorService _eventCollector;
        private readonly IServiceProvider _services;
        private readonly FeedbackService _userFeedback;
        private readonly IDiscordRestWebhookAPI _webhookAPI;
        private readonly ContextInjectionService _contextInjection;
        private readonly PrivacyService _privacy;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbassadorInteractionResponder"/> class.
        /// </summary>
        /// <param name="commandService">The command service.</param>
        /// <param name="interactionAPI">The interaction API.</param>
        /// <param name="eventCollector">The event collector.</param>
        /// <param name="services">The available services.</param>
        /// <param name="userFeedback">The user feedback service.</param>
        /// <param name="webhookAPI">The webhook API.</param>
        /// <param name="contextInjection">The context injection service.</param>
        /// <param name="privacy">The privacy service.</param>
        public AmbassadorInteractionResponder
        (
            CommandService commandService,
            IDiscordRestInteractionAPI interactionAPI,
            ExecutionEventCollectorService eventCollector,
            IServiceProvider services,
            FeedbackService userFeedback,
            IDiscordRestWebhookAPI webhookAPI,
            ContextInjectionService contextInjection,
            PrivacyService privacy
        )
        {
            _commandService = commandService;
            _interactionAPI = interactionAPI;
            _eventCollector = eventCollector;
            _services = services;
            _userFeedback = userFeedback;
            _webhookAPI = webhookAPI;
            _contextInjection = contextInjection;
            _privacy = privacy;
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync
        (
            IInteractionCreate? gatewayEvent,
            CancellationToken ct = default
        )
        {
            if (gatewayEvent is null)
            {
                return Result.FromSuccess();
            }

            if (gatewayEvent.Type != InteractionType.ApplicationCommand)
            {
                return Result.FromSuccess();
            }

            if (!gatewayEvent.Data.HasValue)
            {
                return Result.FromSuccess();
            }

            if (!gatewayEvent.ChannelID.HasValue)
            {
                return Result.FromSuccess();
            }

            var user = gatewayEvent.User.HasValue
                ? gatewayEvent.User.Value
                : gatewayEvent.Member.HasValue
                    ? gatewayEvent.Member.Value.User.HasValue
                        ? gatewayEvent.Member.Value.User.Value
                        : null
                    : null;

            if (user is null)
            {
                return Result.FromSuccess();
            }

            // Signal Discord that we'll be handling this one asynchronously
            var response = new InteractionResponse
            (
                InteractionCallbackType.DeferredChannelMessageWithSource
            );

            var interactionResponse = await _interactionAPI.CreateInteractionResponseAsync
            (
                gatewayEvent.ID,
                gatewayEvent.Token,
                response,
                ct
            );

            if (!interactionResponse.IsSuccess)
            {
                return interactionResponse;
            }

            var context = new InteractionContext
            (
                gatewayEvent.GuildID,
                gatewayEvent.ChannelID.Value,
                user,
                gatewayEvent.Member,
                gatewayEvent.Token,
                gatewayEvent.ID,
                gatewayEvent.ApplicationID,
                gatewayEvent.Data.Value.Resolved
            );

            _contextInjection.Context = context;
            return await RelayResultToUserAsync
            (
                context,
                await TryExecuteCommandAsync(context, gatewayEvent.Data.Value, ct),
                ct
            );
        }

        private async Task<IResult> TryExecuteCommandAsync
        (
            ICommandContext context,
            IApplicationCommandInteractionData data,
            CancellationToken ct = default
        )
        {
            using var transaction = TransactionFactory.Create();
            data.UnpackInteraction(out var command, out var parameters);

            // First of all, check user consent
            var hasConsented = await _privacy.HasUserConsentedAsync(context.User.ID, ct);

            var searchOptions = new TreeSearchOptions(StringComparison.OrdinalIgnoreCase);
            var potentialCommands = _commandService.Tree.Search(command, parameters, searchOptions).ToList();
            var atLeastOneRequiresConsent = potentialCommands.Any
            (
                c =>
                    c.Node.CommandMethod.GetCustomAttribute<PrivacyExemptAttribute>() is null
            );

            if (!hasConsented && atLeastOneRequiresConsent)
            {
                var requestConsent = await _privacy.RequestConsentAsync(context.User.ID, ct);
                if (!requestConsent.IsSuccess)
                {
                    return requestConsent;
                }

                return Result.FromSuccess();
            }

            // Run any user-provided pre execution events
            var preExecution = await _eventCollector.RunPreExecutionEvents(context, ct);
            if (!preExecution.IsSuccess)
            {
                return preExecution;
            }

            // Run the actual command
            var executeResult = await _commandService.TryExecuteAsync
            (
                command,
                parameters,
                _services,
                searchOptions: searchOptions,
                ct: ct
            );

            if (!executeResult.IsSuccess)
            {
                return Result.FromError(executeResult);
            }

            // Run any user-provided post execution events
            var postExecution = await _eventCollector.RunPostExecutionEvents
            (
                context,
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
            InteractionContext context,
            TResult commandResult,
            CancellationToken ct = default
        )
            where TResult : IResult
        {
            if (commandResult.IsSuccess)
            {
                if (commandResult is not Result<FeedbackMessage> messageResult)
                {
                    if (commandResult is Result)
                    {
                        // Most likely some kind of custom embed
                        return Result.FromSuccess();
                    }

                    // Erase the original interaction
                    return await _webhookAPI.DeleteOriginalInteractionResponseAsync
                    (
                        context.ApplicationID,
                        context.Token,
                        ct
                    );
                }

                // Relay the message to the user
                var sendMessage = await _userFeedback.SendContextualMessageAsync
                (
                    messageResult.Entity!,
                    context.User.ID,
                    ct
                );

                return !sendMessage.IsSuccess
                    ? Result.FromError(sendMessage)
                    : Result.FromSuccess();
            }

            IResult result = commandResult;
            while (result.Error is ParameterParsingError or ConditionNotSatisfiedError && result.Inner is not null)
            {
                result = result.Inner;
            }

            var error = result.Error;
            switch (error)
            {
                case AmbiguousCommandInvocationError:
                case UserError:
                case { } e when
                    e.GetType().IsGenericType && e.GetType().GetGenericTypeDefinition() == typeof(ParsingError<>):
                {
                    var sendError = await _userFeedback.SendContextualErrorAsync
                    (
                        error.Message,
                        context.User.ID,
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
