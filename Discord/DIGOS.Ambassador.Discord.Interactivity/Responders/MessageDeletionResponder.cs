//
//  MessageDeletionResponder.cs
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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DIGOS.Ambassador.Discord.Interactivity.Responders
{
    /// <summary>
    /// Responds to events required for interactivity.
    /// </summary>
    public class MessageDeletionResponder :
        IResponder<IMessageDelete>,
        IResponder<IMessageDeleteBulk>
    {
        private readonly InteractivityService _interactivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageDeletionResponder"/> class.
        /// </summary>
        /// <param name="interactivity">The interactivity service.</param>
        public MessageDeletionResponder(InteractivityService interactivity)
        {
            _interactivity = interactivity;
        }

        /// <inheritdoc />
        public Task<Result> RespondAsync(IMessageDelete gatewayEvent, CancellationToken ct = default)
            => _interactivity.OnMessageDeletedAsync(gatewayEvent.ID, ct);

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IMessageDeleteBulk gatewayEvent, CancellationToken ct = default)
        {
            var deletions = gatewayEvent.IDs.Select(id => _interactivity.OnMessageDeletedAsync(id, ct));
            var results = await Task.WhenAll(deletions);

            var firstFail = results.FirstOrDefault(r => !r.IsSuccess);
            return firstFail.Equals(default)
                ? Result.FromSuccess()
                : firstFail;
        }
    }
}
