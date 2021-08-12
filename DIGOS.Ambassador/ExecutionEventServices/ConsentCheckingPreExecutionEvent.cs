//
//  ConsentCheckingPreExecutionEvent.cs
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
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Core.Attributes;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Results;
using Microsoft.Extensions.Options;
using Remora.Commands.Extensions;
using Remora.Commands.Services;
using Remora.Commands.Signatures;
using Remora.Commands.Tokenization;
using Remora.Commands.Trees;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Commands.Services;
using Remora.Results;

namespace DIGOS.Ambassador.ExecutionEventServices
{
    /// <summary>
    /// Ensures the user has consented to data processing before allowing a command to be executed.
    /// </summary>
    public class ConsentCheckingPreExecutionEvent : IPreExecutionEvent
    {
        private readonly ICommandResponderOptions _options;
        private readonly PrivacyService _privacy;
        private readonly CommandService _commandService;
        private readonly IDiscordRestInteractionAPI _interactionAPI;
        private readonly TokenizerOptions _tokenizerOptions;
        private readonly TreeSearchOptions _treeSearchOptions;
        private readonly FeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsentCheckingPreExecutionEvent"/> class.
        /// </summary>
        /// <param name="privacy">The privacy service.</param>
        /// <param name="commandService">The command service.</param>
        /// <param name="options">The responder options.</param>
        /// <param name="interactionAPI">The interaction API.</param>
        /// <param name="tokenizerOptions">The tokenizer options.</param>
        /// <param name="treeSearchOptions">The tree search options.</param>
        /// <param name="feedback">The feedback service.</param>
        public ConsentCheckingPreExecutionEvent
        (
            PrivacyService privacy,
            CommandService commandService,
            IOptions<CommandResponderOptions> options,
            IDiscordRestInteractionAPI interactionAPI,
            IOptions<TokenizerOptions> tokenizerOptions,
            IOptions<TreeSearchOptions> treeSearchOptions,
            FeedbackService feedback
        )
        {
            _privacy = privacy;
            _commandService = commandService;
            _interactionAPI = interactionAPI;
            _feedback = feedback;
            _options = options.Value;
            _tokenizerOptions = tokenizerOptions.Value;
            _treeSearchOptions = treeSearchOptions.Value;
        }

        /// <inheritdoc />
        public async Task<Result> BeforeExecutionAsync(ICommandContext context, CancellationToken ct = default)
        {
            var hasConsented = await _privacy.HasUserConsentedAsync(context.User.ID, ct);

            IReadOnlyList<BoundCommandNode> potentialCommands;
            switch (context)
            {
                case MessageContext messageContext:
                {
                    if (!messageContext.Message.Content.HasValue)
                    {
                        potentialCommands = Array.Empty<BoundCommandNode>();
                        break;
                    }

                    var content = messageContext.Message.Content.Value;

                    // Strip off the prefix
                    if (_options.Prefix is not null)
                    {
                        content = content
                        [
                            (content.IndexOf(_options.Prefix, StringComparison.Ordinal) + _options.Prefix.Length)..
                        ];
                    }

                    potentialCommands = _commandService.Tree
                        .Search(content, _tokenizerOptions, _treeSearchOptions)
                        .ToList();

                    break;
                }
                case InteractionContext interactionContext:
                {
                    interactionContext.Data.UnpackInteraction(out var command, out var parameters);
                    potentialCommands = _commandService.Tree
                        .Search(command, parameters, _tokenizerOptions, _treeSearchOptions)
                        .ToList();

                    break;
                }
                default:
                {
                    throw new InvalidOperationException();
                }
            }

            var atLeastOneRequiresConsent = potentialCommands.Any
            (
                c => c.Node.CommandMethod.GetCustomAttribute<PrivacyExemptAttribute>() is null
            );

            if (hasConsented || !atLeastOneRequiresConsent)
            {
                return Result.FromSuccess();
            }

            var requestConsent = await _privacy.RequestConsentAsync(context.User.ID, ct);
            if (!requestConsent.IsSuccess)
            {
                return requestConsent;
            }

            // Delete the original message
            if (context is not InteractionContext deletionContext || !_feedback.HasEditedOriginalMessage)
            {
                return new NoConsentError();
            }

            var deleteOriginal = await _interactionAPI.DeleteOriginalInteractionResponseAsync
            (
                deletionContext.ApplicationID,
                deletionContext.Token,
                ct
            );

            if (!deleteOriginal.IsSuccess)
            {
                return deleteOriginal;
            }

            return new NoConsentError();
        }
    }
}
