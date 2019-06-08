//
//  DetermineConditionResult.cs
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
    /// Represents an attempt to retrieve a roleplay from the database.
    /// </summary>
    public class DetermineConditionResult : ResultBase<DetermineConditionResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DetermineConditionResult"/> class.
        /// </summary>
        private DetermineConditionResult()
        {
        }

        /// <inheritdoc cref="ResultBase{TResultType}(CommandError?,string,Exception)"/>
        [UsedImplicitly]
        private DetermineConditionResult([CanBeNull] CommandError? error, [CanBeNull] string errorReason, [CanBeNull] Exception exception = null)
            : base(error, errorReason, exception)
        {
        }

        /// <summary>
        /// Creates a new successful result.
        /// </summary>
        /// <returns>A successful result.</returns>
        [Pure]
        public static DetermineConditionResult FromSuccess()
        {
            return new DetermineConditionResult();
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        /// <param name="reason">A more detailed error reason.</param>
        /// <returns>A failed result.</returns>
        [Pure]
        public static DetermineConditionResult FromError([NotNull] string reason)
        {
            return FromError(CommandError.UnmetPrecondition, reason);
        }
    }
}
