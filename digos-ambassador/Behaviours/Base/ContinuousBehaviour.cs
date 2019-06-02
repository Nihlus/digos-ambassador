//
//  ContinuousBehaviour.cs
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
using Discord.WebSocket;
using JetBrains.Annotations;
using log4net;

namespace DIGOS.Ambassador.Behaviours
{
    /// <summary>
    /// Abstract base class for a behaviour that continuously performs an action.
    /// </summary>
    public abstract class ContinuousBehaviour : BehaviourBase
    {
        /// <summary>
        /// Gets the logging instance for this behaviour.
        /// </summary>
        [NotNull]
        private ILog Log { get; }

        /// <summary>
        /// Gets or sets the cancellation source for the continuous action task.
        /// </summary>
        [NotNull]
        private CancellationTokenSource CancellationSource { get; set; }

        /// <summary>
        /// Gets or sets the continuous action task.
        /// </summary>
        [NotNull]
        private Task ContinuousActionTask { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuousBehaviour"/> class.
        /// </summary>
        /// <param name="client">The client.</param>
        protected ContinuousBehaviour(DiscordSocketClient client)
            : base(client)
        {
            this.Log = LogManager.GetLogger(GetType());

            this.CancellationSource = new CancellationTokenSource();
            this.ContinuousActionTask = Task.CompletedTask;
        }

        /// <summary>
        /// Implements the body that should run on each tick of the behaviour. Usually, having some sort of delay in
        /// this method takes strain off of the system.
        /// </summary>
        /// <param name="ct">The cancellation token for the behaviour.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        protected abstract Task OnTickAsync(CancellationToken ct);

        /// <summary>
        /// Continuously runs <see cref="OnTickAsync"/> until the behaviour stops.
        /// </summary>
        /// <param name="ct">The cancellation token for the behaviour.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        private async Task RunContinuousActionAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await OnTickAsync(ct);
                }
                catch (Exception e)
                {
                    // Nom nom nom
                    this.Log.Error($"Error in behaviour tick.", e);
                }
            }
        }

        /// <inheritdoc/>
        /// <remarks>You must call this base implementation in any derived methods.</remarks>
        protected override Task OnStartingAsync()
        {
            this.CancellationSource = new CancellationTokenSource();
            this.ContinuousActionTask = RunContinuousActionAsync(this.CancellationSource.Token);

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        /// <remarks>You must call this base implementation in any derived methods.</remarks>
        protected override async Task OnStoppingAsync()
        {
            this.CancellationSource.Cancel();
            await this.ContinuousActionTask;

            this.CancellationSource.Dispose();
        }
    }
}
