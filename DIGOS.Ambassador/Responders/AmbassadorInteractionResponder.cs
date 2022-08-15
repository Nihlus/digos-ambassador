//
//  AmbassadorInteractionResponder.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database;
using Microsoft.Extensions.Options;
using Remora.Commands.Services;
using Remora.Commands.Tokenization;
using Remora.Commands.Trees;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Commands.Services;
using Remora.Results;

namespace DIGOS.Ambassador.Responders;

/// <summary>
/// Extends the vanilla responder with an ambient transaction.
/// </summary>
public class AmbassadorInteractionResponder : InteractionResponder
{
    /// <inheritdoc cref="InteractionResponder"/>
    public AmbassadorInteractionResponder
    (
        CommandService commandService,
        IOptions<InteractionResponderOptions> options,
        IDiscordRestInteractionAPI interactionAPI,
        ExecutionEventCollectorService eventCollector,
        IServiceProvider services,
        ContextInjectionService contextInjection,
        IOptions<TokenizerOptions> tokenizerOptions,
        IOptions<TreeSearchOptions> treeSearchOptions
    )
        : base
        (
            commandService,
            options,
            interactionAPI,
            eventCollector,
            services,
            contextInjection,
            tokenizerOptions,
            treeSearchOptions
        )
    {
    }

    /// <inheritdoc />
    public override async Task<Result> RespondAsync
    (
        IInteractionCreate gatewayEvent,
        CancellationToken ct = default
    )
    {
        using var transaction = TransactionFactory.Create();

        var executionResult = await base.RespondAsync(gatewayEvent, ct);
        if (executionResult.IsSuccess)
        {
            transaction.Complete();
        }

        return executionResult;
    }
}
