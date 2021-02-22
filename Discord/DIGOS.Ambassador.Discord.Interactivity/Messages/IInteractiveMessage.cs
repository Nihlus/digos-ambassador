//
//  IInteractiveMessage.cs
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
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Discord.Interactivity.Messages
{
    /// <summary>
    /// Represents the public interface of an interactive message.
    /// </summary>
    public interface IInteractiveMessage
    {
        /// <summary>
        /// Gets the ID of the channel the message is in.
        /// </summary>
        Snowflake ChannelID { get; }

        /// <summary>
        /// Gets the ID of the message.
        /// </summary>
        Snowflake ID { get; }

        /// <summary>
        /// Handles an added reaction.
        /// </summary>
        /// <param name="userID">The ID of the user who added the reaction.</param>
        /// <param name="emoji">The emoji.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        public Task<Result> OnReactionAddedAsync(Snowflake userID, IPartialEmoji emoji, CancellationToken ct = default);

        /// <summary>
        /// Handles a removed reaction.
        /// </summary>
        /// <param name="userID">The ID of the user who removed the reaction.</param>
        /// <param name="emoji">The emoji.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        public Task<Result> OnReactionRemovedAsync
        (
            Snowflake userID,
            IPartialEmoji emoji,
            CancellationToken ct = default
        );

        /// <summary>
        /// Handles a complete removal of all reactions.
        /// </summary>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        public Task<Result> OnAllReactionsRemovedAsync(CancellationToken ct = default);

        /// <summary>
        /// Forces an update of the interactive message.
        /// </summary>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        public Task<Result> UpdateAsync(CancellationToken ct = default);
    }
}
