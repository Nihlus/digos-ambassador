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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Plugins.Amby.Services;
using OneOf;
using Remora.Commands.Results;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Commands.Services;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.ExecutionEventServices
{
    /// <summary>
    /// Relays returned messages to the user upon completion of a command.
    /// </summary>
    public class MessageRelayingPostExecutionEvent : IPostExecutionEvent
    {
        private readonly IDiscordRestInteractionAPI _interactionAPI;
        private readonly ContentService _content;
        private readonly PortraitService _portraits;
        private readonly FeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageRelayingPostExecutionEvent"/> class.
        /// </summary>
        /// <param name="interactionAPI">The webhook API.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="portraits">The portrait service.</param>
        /// <param name="content">The content service.</param>
        public MessageRelayingPostExecutionEvent
        (
            IDiscordRestInteractionAPI interactionAPI,
            FeedbackService feedback,
            PortraitService portraits,
            ContentService content
        )
        {
            _interactionAPI = interactionAPI;
            _feedback = feedback;
            _portraits = portraits;
            _content = content;
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
                    if (context is not InteractionContext interactionContext || _feedback.HasEditedOriginalMessage)
                    {
                        return Result.FromSuccess();
                    }

                    // Erase the original interaction
                    return await _interactionAPI.DeleteOriginalInteractionResponseAsync
                    (
                        interactionContext.ApplicationID,
                        interactionContext.Token,
                        ct
                    );
                }

                // Relay the message to the user
                var sendMessage = await _feedback.SendContextualMessageAsync
                (
                    messageResult.Entity,
                    context.User.ID,
                    ct: ct
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
                    var sendError = await _feedback.SendContextualErrorAsync
                    (
                        error.Message,
                        context.User.ID,
                        ct: ct
                    );

                    if (!sendError.IsSuccess)
                    {
                        return Result.FromError(sendError);
                    }

                    break;
                }
                case CommandNotFoundError:
                {
                    var sendError = await _feedback.SendContextualErrorAsync
                    (
                        "No matching command found.",
                        context.User.ID,
                        ct: ct
                    );

                    if (!sendError.IsSuccess)
                    {
                        return Result.FromError(sendError);
                    }

                    break;
                }
                default:
                {
                    var errorEmbed = new Embed("Internal Error")
                    {
                        Description =
                            "Oops! Looks like you've found a bug in the bot - fear not, though. If we " +
                            "work together, we'll have it licked in no time at all.\n" +
                            "\n" +
                            "I've prepared a short report of the technical details of what happened, but it's not " +
                            "going to be worth much without your help. In order to fix this problem, it would be " +
                            "extremely helpful if you wrote down the exact steps you did to encounter this error, " +
                            "and post them along with the generated report on the GitHub repository. You can go " +
                            "there by clicking the link in this message.\n" +
                            "\n" +
                            "The report contains some information about you, so please check it and erase anything " +
                            "you don't want to share before passing it on.\n" +
                            "\n" +
                            "Your assistance is essential, and very much appreciated!",
                        Colour = _feedback.Theme.FaultOrDanger,
                        Timestamp = DateTimeOffset.UtcNow,
                        Thumbnail = new EmbedThumbnail(_portraits.BrokenAmbyUri.ToString())
                    };

                    var sendError = await _feedback.SendPrivateEmbedAsync(context.User.ID, errorEmbed, ct: ct);
                    if (!sendError.IsSuccess)
                    {
                        return Result.FromError(sendError);
                    }

                    var reportEmbed = new Embed("Click here to create a new issue")
                    {
                        Url = _content.AutomaticBugReportCreationUri.ToString(),
                        Colour = _feedback.Theme.Primary
                    };

                    await using var ms = new MemoryStream();
                    var now = DateTime.UtcNow;

                    await using (var sw = new StreamWriter(ms, Encoding.Default, 1024, true))
                    {
                        await sw.WriteLineAsync("Automatic bug report");
                        await sw.WriteLineAsync("====================");
                        await sw.WriteLineAsync();
                        await sw.WriteLineAsync($"Generated at: {now}");

                        var entryAssembly = Assembly.GetEntryAssembly();
                        if (entryAssembly is not null)
                        {
                            await sw.WriteLineAsync($"Bot version: {entryAssembly.GetName().Version}");
                        }

                        string command;
                        switch (context)
                        {
                            case MessageContext messageContext:
                            {
                                command = messageContext.Message.Content.IsDefined(out var messageContent)
                                    ? messageContent
                                    : "Unknown";
                                break;
                            }
                            case InteractionContext interactionContext:
                            {
                                interactionContext.Data.UnpackInteraction(out var commandPath, out var parameters);
                                command = string.Join(" ", commandPath) +
                                          " " +
                                          string.Join(" ", parameters.Select(kvp => string.Join(" ", kvp.Key, string.Join(" ", kvp.Value))));
                                break;
                            }
                            default:
                            {
                                command = "Unknown";
                                break;
                            }
                        }

                        await sw.WriteLineAsync($"Full command: {command}");
                        await sw.WriteLineAsync();
                        await sw.WriteLineAsync("### Error");
                        var errorJson = JsonSerializer.Serialize
                        (
                            result,
                            new JsonSerializerOptions { WriteIndented = true }
                        );

                        await sw.WriteLineAsync(errorJson);
                    }

                    ms.Position = 0;

                    var date = now.ToShortDateString();
                    var time = now.ToShortTimeString();
                    var sendReport = await _feedback.SendPrivateEmbedAsync
                    (
                        context.User.ID,
                        reportEmbed,
                        new FeedbackMessageOptions(Attachments: new List<OneOf<FileData, IPartialAttachment>>
                        {
                            new FileData($"bug-report-{date}-{time}.md", ms, "Generated bug report")
                        }),
                        ct
                    );

                    if (!sendReport.IsSuccess)
                    {
                        return Result.FromError(sendReport);
                    }

                    if (context is not InteractionContext inter || _feedback.HasEditedOriginalMessage)
                    {
                        return Result.FromSuccess();
                    }

                    // Erase the original interaction
                    return await _interactionAPI.DeleteOriginalInteractionResponseAsync
                    (
                        inter.ApplicationID,
                        inter.Token,
                        ct
                    );
                }
            }

            return Result.FromError(result.Error!);
        }
    }
}
