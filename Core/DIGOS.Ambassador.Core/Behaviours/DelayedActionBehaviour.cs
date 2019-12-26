//
//  DelayedActionBehaviour.cs
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
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Services;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Discord.Behaviours;

namespace DIGOS.Ambassador.Core
{
    /// <summary>
    /// Represents a behaviour that does things at a later date.
    /// </summary>
    public class DelayedActionBehaviour : ContinuousDiscordBehaviour<DelayedActionBehaviour>
    {
        /// <summary>
        /// Gets the events that are currently running.
        /// </summary>
        private readonly DelayedActionService _delayedActions;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayedActionBehaviour"/> class.
        /// </summary>
        /// <param name="client">The Discord client.</param>
        /// <param name="serviceScope">The service scope in use.</param>
        /// <param name="logger">The logging instance for this type.</param>
        /// <param name="delayedActions">The do-later service.</param>
        public DelayedActionBehaviour
        (
            DiscordSocketClient client,
            [NotNull] IServiceScope serviceScope,
            [NotNull] ILogger<DelayedActionBehaviour> logger,
            DelayedActionService delayedActions
        )
            : base(client, serviceScope, logger)
        {
            _delayedActions = delayedActions;
        }

        /// <inheritdoc />
        protected override async Task OnTickAsync(CancellationToken ct)
        {
            if (_delayedActions.RunningTimeouts.TryDequeue(out var timeout))
            {
                if (timeout.IsCompleted)
                {
                    try
                    {
                        await timeout;

                        // Get and perform the actual task
                        var taskFactory = _delayedActions.ScheduledTasks[timeout];
                        await taskFactory();
                    }
                    catch (Exception e)
                    {
                        // Nom nom nom
                        this.Log.LogError("Error in delayed action.", e);
                    }
                }
                else
                {
                    _delayedActions.RunningTimeouts.Enqueue(timeout);
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), ct);
        }
    }
}
