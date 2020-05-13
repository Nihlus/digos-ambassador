//
//  AutoroleUpdateResult.cs
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
using JetBrains.Annotations;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Autorole.Results
{
    /// <summary>
    /// Represents the result of an autorole update operation. A successful result indicates that no errors were
    /// encountered during the update (exceptions, permission errors, etc), but a more specific status can be retrieved
    /// from <see cref="Status"/>.
    /// </summary>
    public sealed class AutoroleUpdateResult : ResultBase<AutoroleUpdateResult>
    {
        /// <summary>
        /// Holds the actual status value.
        /// </summary>
        private readonly AutoroleUpdateStatus? _status;

        /// <summary>
        /// Gets the status that was retrieved.
        /// </summary>
        [PublicAPI]
        public AutoroleUpdateStatus Status
        {
            get
            {
                if (!this.IsSuccess || _status is null)
                {
                    throw new InvalidOperationException("The result does not contain a valid value.");
                }

                return _status.Value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoroleUpdateResult"/> class.
        /// </summary>
        /// <param name="status">The status.</param>
        private AutoroleUpdateResult(AutoroleUpdateStatus? status)
        {
            _status = status;
        }

        /// <inheritdoc cref="ResultBase{TResultType}(string,Exception)"/>
        [UsedImplicitly]
        private AutoroleUpdateResult
        (
            string? errorReason,
            Exception? exception = null
        )
            : base(errorReason, exception)
        {
        }

        /// <summary>
        /// Creates a new successful result.
        /// </summary>
        /// <param name="status">The status that was retrieved.</param>
        /// <returns>A successful result.</returns>
        [PublicAPI, Pure, NotNull]
        public static AutoroleUpdateResult FromSuccess(AutoroleUpdateStatus status)
        {
            return new AutoroleUpdateResult(status);
        }

        /// <summary>
        /// Implicitly converts a compatible value to a successful result.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <returns>The successful result.</returns>
        [PublicAPI, Pure, NotNull]
        public static implicit operator AutoroleUpdateResult(AutoroleUpdateStatus status)
        {
            return FromSuccess(status);
        }
    }
}
