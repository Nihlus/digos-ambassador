//
//  ShiftBodypartResult.cs
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
using DIGOS.Ambassador.Services.Base;
using Discord.Commands;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Services
{
    /// <summary>
    /// Represents an attempt to shift a part of a character's body.
    /// </summary>
    public class ShiftBodypartResult : ResultBase<ShiftBodypartResult>
    {
        /// <summary>
        /// Gets the shifting message.
        /// </summary>
        public string ShiftMessage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShiftBodypartResult"/> class.
        /// </summary>
        /// <param name="shiftMessage">The message to display to the user when shifting.</param>
        private ShiftBodypartResult([CanBeNull] string shiftMessage)
        {
            this.ShiftMessage = shiftMessage;
        }

        /// <inheritdoc cref="ResultBase{TResultType}(CommandError?,string,Exception)"/>
        [UsedImplicitly]
        private ShiftBodypartResult
        (
            [CanBeNull] CommandError? error,
            [CanBeNull] string errorReason,
            [CanBeNull] Exception exception = null
        )
            : base(error, errorReason, exception)
        {
        }

        /// <summary>
        /// Creates a new successful result.
        /// </summary>
        /// <returns>A successful result.</returns>
        /// <param name="shiftMessage">The message to display to the user when shifting.</param>
        [Pure]
        public static ShiftBodypartResult FromSuccess([NotNull] string shiftMessage)
        {
            return new ShiftBodypartResult(shiftMessage);
        }
    }
}
