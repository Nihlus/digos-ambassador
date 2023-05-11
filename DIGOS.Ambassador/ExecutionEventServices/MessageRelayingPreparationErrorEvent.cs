//
//  MessageRelayingPreparationErrorEvent.cs
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

using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Services;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Results;

namespace DIGOS.Ambassador.ExecutionEventServices;

/// <summary>
/// Displays some error messages directly to the user.
/// </summary>
public class MessageRelayingPreparationErrorEvent : IPreparationErrorEvent
{
    private readonly MessageRelayService _messageRelay;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageRelayingPreparationErrorEvent"/> class.
    /// </summary>
    /// <param name="messageRelay">The message relay service.</param>
    public MessageRelayingPreparationErrorEvent(MessageRelayService messageRelay)
    {
        _messageRelay = messageRelay;
    }

    /// <inheritdoc />
    public Task<Result> PreparationFailed
    (
        IOperationContext context,
        IResult preparationResult,
        CancellationToken ct = default
    ) => _messageRelay.RelayResultAsync(context, preparationResult, ct);
}
