//
//  WatchdogBehaviour.cs
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
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Behaviours
{
    /// <summary>
    /// Acts as a watchdog for disconnection events, reconnecting the client if Discord.NET fails to reestablish on its
    /// own.
    /// </summary>
    public class WatchdogBehaviour : BehaviourBase
    {
        private readonly AmbassadorClient Ambassador;

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchdogBehaviour"/> class.
        /// </summary>
        /// <param name="client">The discord client.</param>
        /// <param name="ambassador">The ambassador instance.</param>
        public WatchdogBehaviour(DiscordSocketClient client, AmbassadorClient ambassador)
            : base(client)
        {
            this.Ambassador = ambassador;
        }

        /// <inheritdoc />
        protected override Task OnStartingAsync()
        {
            this.Client.Disconnected += OnClientDisconnected;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task OnStoppingAsync()
        {
            this.Client.Disconnected -= OnClientDisconnected;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles a disconnection event.
        /// </summary>
        /// <param name="exception">The exception that caused the disconnection, if any.</param>
        /// <returns>A task representing the disconnection event.</returns>
        [NotNull]
        private Task OnClientDisconnected(Exception exception)
        {
            _ = Task.Run
            (
                async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    if (this.Client.ConnectionState != ConnectionState.Connecting &&
                        this.Client.ConnectionState != ConnectionState.Connected)
                    {
                        await this.Ambassador.StopAsync();
                        await this.Ambassador.StartAsync();
                    }
                }
            );

            return Task.CompletedTask;
        }
    }
}
