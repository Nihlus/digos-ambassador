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
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Feedback.Errors;
using DIGOS.Ambassador.Discord.Feedback.Results;
using DIGOS.Ambassador.Plugins.Core.Services;
using JetBrains.Annotations;
using Remora.Commands.Results;
using Remora.Commands.Services;
using Remora.Commands.Trees;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
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
        private readonly UserFeedbackService _userFeedback;
        private readonly IDiscordRestWebhookAPI _webhookAPI;
        private readonly IdentityInformationService _identityInformation;
        private readonly ContextInjectionService _contextInjection;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbassadorInteractionResponder"/> class.
        /// </summary>
        /// <param name="commandService">The command service.</param>
        /// <param name="interactionAPI">The interaction API.</param>
        /// <param name="eventCollector">The event collector.</param>
        /// <param name="services">The available services.</param>
        /// <param name="userFeedback">The user feedback service.</param>
        /// <param name="webhookAPI">The webhook API.</param>
        /// <param name="identityInformation">The identity information.</param>
        /// <param name="contextInjection">The context injection service.</param>
        public AmbassadorInteractionResponder
        (
            CommandService commandService,
            IDiscordRestInteractionAPI interactionAPI,
            ExecutionEventCollectorService eventCollector,
            IServiceProvider services,
            UserFeedbackService userFeedback,
            IDiscordRestWebhookAPI webhookAPI,
            IdentityInformationService identityInformation,
            ContextInjectionService contextInjection
        )
        {
            _commandService = commandService;
            _interactionAPI = interactionAPI;
            _eventCollector = eventCollector;
            _services = services;
            _userFeedback = userFeedback;
            _webhookAPI = webhookAPI;
            _identityInformation = identityInformation;
            _contextInjection = contextInjection;
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
                InteractionResponseType.DeferredChannelMessageWithSource
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
                gatewayEvent.ID
            );

            _contextInjection.Context = context;

            return await RelayResultToUserAsync
            (
                context,
                await TryExecuteCommandAsync(context, gatewayEvent.Data.Value, ct),
                ct
            );
        }

        private async Task<Result> TryExecuteCommandAsync
        (
            ICommandContext context,
            IApplicationCommandInteractionData data,
            CancellationToken ct = default
        )
        {
            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            data.UnpackInteraction(out var command, out var parameters);

            // Run any user-provided pre execution events
            var preExecution = await _eventCollector.RunPreExecutionEvents(context, ct);
            if (!preExecution.IsSuccess)
            {
                return preExecution;
            }

            // Run the actual command
            var searchOptions = new TreeSearchOptions(StringComparison.OrdinalIgnoreCase);
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

            return Result.FromSuccess();
        }

        private async Task<Result> RelayResultToUserAsync<TResult>
        (
            InteractionContext context,
            TResult commandResult,
            CancellationToken ct = default
        )
            where TResult : struct, IResult
        {
            if (commandResult.IsSuccess)
            {
                if (commandResult is not Result<UserMessage> messageResult)
                {
                    // All good? Erase the original interaction message
                    var eraseOriginal = await _webhookAPI.DeleteOriginalInteractionResponseAsync
                    (
                        _identityInformation.ApplicationID,
                        context.Token,
                        ct
                    );

                    return !eraseOriginal.IsSuccess
                        ? eraseOriginal
                        : Result.FromSuccess();
                }

                // Relay the message to the user
                var sendMessage = await _userFeedback.SendMessageAsync
                (
                    context.ChannelID,
                    context.User.ID,
                    messageResult.Entity!,
                    ct
                );

                return !sendMessage.IsSuccess
                    ? Result.FromError(sendMessage)
                    : Result.FromSuccess();
            }

            var error = commandResult.Unwrap();
            switch (error)
            {
                case AmbiguousCommandInvocationError:
                case ConditionNotSatisfiedError:
                case UserError:
                case { } when error.GetType().IsGenericType &&
                              error.GetType().GetGenericTypeDefinition() == typeof(ParsingError<>):
                {
                    // Alert the user, and don't complete the transaction
                    var sendError = await _webhookAPI.EditOriginalInteractionResponseAsync
                    (
                        _identityInformation.ApplicationID,
                        context.Token,
                        embeds: new[]
                        {
                            _userFeedback.CreateEmbedBase(Color.OrangeRed) with
                            {
                                Description = error.Message
                            }
                        },
                        ct: ct
                    );

                    return sendError.IsSuccess
                        ? Result.FromSuccess()
                        : Result.FromError(sendError);
                }
                default:
                {
                    return Result.FromError(commandResult.Unwrap());
                }
            }
        }
    }
}
