//
//  MessageRelayingPostExecutionEvent.cs
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

using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Results;
using Remora.Commands.Results;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Commands.Services;
using Remora.Discord.Rest.Results;
using Remora.Results;

namespace DIGOS.Ambassador.ExecutionEventServices
{
    /// <summary>
    /// Relays returned messages to the user upon completion of a command.
    /// </summary>
    public class MessageRelayingPostExecutionEvent : IPostExecutionEvent
    {
        private readonly IDiscordRestWebhookAPI _webhookAPI;
        private readonly FeedbackService _userFeedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageRelayingPostExecutionEvent"/> class.
        /// </summary>
        /// <param name="webhookAPI">The webhook API.</param>
        /// <param name="userFeedback">The feedback service.</param>
        public MessageRelayingPostExecutionEvent(IDiscordRestWebhookAPI webhookAPI, FeedbackService userFeedback)
        {
            _webhookAPI = webhookAPI;
            _userFeedback = userFeedback;
        }

        /// <inheritdoc />
        public async Task<Result> AfterExecutionAsync
        (
            ICommandContext context,
            IResult commandResult,
            CancellationToken ct = default
        )
        {
            if (commandResult.IsSuccess)
            {
                if (commandResult is not Result<FeedbackMessage> messageResult)
                {
                    if (context is not InteractionContext interactionContext)
                    {
                        return Result.FromSuccess();
                    }

                    // Check if the original interaction has been edited
                    var getOriginal = await _webhookAPI.GetOriginalInteractionResponseAsync
                    (
                        interactionContext.ApplicationID,
                        interactionContext.Token,
                        ct
                    );

                    if (!getOriginal.IsSuccess)
                    {
                        return Result.FromError(getOriginal);
                    }

                    var original = getOriginal.Entity;
                    var originalIsEdited = original.EditedTimestamp.HasValue
                                           || original.Embeds.Count > 0
                                           || original.Content != string.Empty;

                    if (originalIsEdited)
                    {
                        return Result.FromSuccess();
                    }

                    // Erase the original interaction
                    return await _webhookAPI.DeleteOriginalInteractionResponseAsync
                    (
                        interactionContext.ApplicationID,
                        interactionContext.Token,
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

                    if (!sendError.IsSuccess)
                    {
                        return Result.FromError(sendError);
                    }

                    break;
                }
                case CommandNotFoundError:
                {
                    var sendError = await _userFeedback.SendContextualErrorAsync
                    (
                        "No matching command found.",
                        context.User.ID,
                        ct
                    );

                    if (!sendError.IsSuccess)
                    {
                        return Result.FromError(sendError);
                    }

                    break;
                }
            }

            return Result.FromError(result.Error!);
        }
    }
}
