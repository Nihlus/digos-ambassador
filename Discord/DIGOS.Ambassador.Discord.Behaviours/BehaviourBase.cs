//
//  BehaviourBase.cs
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

using System.Threading.Tasks;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace DIGOS.Ambassador.Discord.Behaviours
{
    /// <summary>
    /// Acts as a base class for behaviours in the client.
    /// </summary>
    public abstract class BehaviourBase : IBehaviour
    {
        /// <inheritdoc />
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets the client associated with the behaviour.
        /// </summary>
        protected DiscordSocketClient Client { get; }

        /// <summary>
        /// Gets or sets the scope in which this behaviour lives.
        /// </summary>
        private IServiceScope ServiceScope { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BehaviourBase"/> class.
        /// </summary>
        /// <param name="client">The client that the behaviour is associated with.</param>
        protected BehaviourBase(DiscordSocketClient client)
        {
            this.Client = client;
        }

        /// <inheritdoc/>
        [NotNull]
        public async Task StartAsync()
        {
            if (this.IsRunning)
            {
                return;
            }

            this.IsRunning = true;
            await OnStartingAsync();
        }

        /// <inheritdoc/>
        public void WithScope(IServiceScope serviceScope)
        {
            this.ServiceScope = serviceScope;
        }

        /// <summary>
        /// User-implementable logic that runs during behaviour startup.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        protected virtual Task OnStartingAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        [NotNull]
        public async Task StopAsync()
        {
            if (!this.IsRunning)
            {
                return;
            }

            this.IsRunning = false;
            await OnStoppingAsync();
        }

        /// <summary>
        /// User-implementable logic that runs during behaviour shutdown.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        protected virtual Task OnStoppingAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            this.ServiceScope.Dispose();
        }
    }
}
