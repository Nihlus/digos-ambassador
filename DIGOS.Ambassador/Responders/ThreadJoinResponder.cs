//
//  ThreadJoinResponder.cs
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
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DIGOS.Ambassador.Responders
{
    /// <summary>
    /// Automatically joins created threads, ensuring that bot functionality remains available.
    /// </summary>
    public class ThreadJoinResponder : IResponder<IThreadCreate>
    {
        private readonly IDiscordRestChannelAPI _channelAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadJoinResponder"/> class.
        /// </summary>
        /// <param name="channelAPI">The channel API.</param>
        public ThreadJoinResponder(IDiscordRestChannelAPI channelAPI)
        {
            _channelAPI = channelAPI;
        }

        /// <inheritdoc />
        public Task<Result> RespondAsync(IThreadCreate gatewayEvent, CancellationToken ct = default)
        {
            return _channelAPI.JoinThreadAsync(gatewayEvent.ID, ct);
        }
    }
}
