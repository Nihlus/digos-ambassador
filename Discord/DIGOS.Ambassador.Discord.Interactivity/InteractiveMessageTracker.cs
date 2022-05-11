//
//  InteractiveMessageTracker.cs
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

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Interactivity.Messages;
using Remora.Rest.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Discord.Interactivity;

/// <summary>
/// Singleton service for tracking interactive messages.
/// </summary>
public class InteractiveMessageTracker
{
    /// <summary>
    /// Holds a mapping of message IDs to tracked messages.
    /// </summary>
    private readonly ConcurrentDictionary<string, IInteractiveMessage> _trackedMessages = new();

    /// <summary>
    /// Begins tracking the given message.
    /// </summary>
    /// <param name="message">The interactive message.</param>
    /// <returns>A result which may or may not have succeeded.</returns>
    public Result TrackMessage(IInteractiveMessage message)
    {
        return _trackedMessages.TryAdd(message.Nonce, message)
            ? Result.FromSuccess()
            : new InvalidOperationError("A message with that ID is already tracked.");
    }

    /// <summary>
    /// Gets a registered interactive entity that matches the given nonce.
    /// </summary>
    /// <param name="nonce">The entity's unique identifier.</param>
    /// <param name="entity">The entity, or null if none exists.</param>
    /// <typeparam name="TEntity">The concrete entity type.</typeparam>
    /// <returns>true if a matching entity was successfully found; otherwise, false.</returns>
    public bool TryGetInteractiveEntity<TEntity>(string nonce, [NotNullWhen(true)] out TEntity? entity)
        where TEntity : IInteractiveEntity
    {
        entity = default;

        if (!_trackedMessages.TryGetValue(nonce, out var untypedEntity))
        {
            return false;
        }

        if (untypedEntity is not TEntity typedEntity)
        {
            return false;
        }

        entity = typedEntity;
        return true;
    }

    /// <summary>
    /// Ceases tracking the message with the given ID.
    /// </summary>
    /// <param name="id">The ID of the message.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A result which may or may not have succeeded.</returns>
    public async Task<Result> UntrackMessageAsync(Snowflake id, CancellationToken ct = default)
    {
        if (!_trackedMessages.TryRemove(id.ToString(), out var trackedMessage))
        {
            // The message is already removed
            return Result.FromSuccess();
        }

        await trackedMessage.Semaphore.WaitAsync(ct);
        trackedMessage.Dispose();

        return Result.FromSuccess();
    }

    /// <summary>
    /// Ceases tracking the message with the given ID.
    /// </summary>
    /// <param name="id">The ID of the message.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A result which may or may not have succeeded.</returns>
    public Task<Result> OnMessageDeletedAsync(Snowflake id, CancellationToken ct = default)
        => UntrackMessageAsync(id, ct);
}
