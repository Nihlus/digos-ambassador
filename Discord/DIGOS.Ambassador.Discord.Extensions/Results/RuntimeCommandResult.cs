//
//  RuntimeCommandResult.cs
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

using Discord.Commands;
using IResult = Remora.Results.IResult;

namespace DIGOS.Ambassador.Discord.Extensions.Results
{
    /// <summary>
    /// Represents the result of a command. Used for bubbling up errors to the command handler.
    /// </summary>
    public class RuntimeCommandResult : RuntimeResult, IResult
    {
        /// <inheritdoc />
        string IResult.ErrorReason => this.Reason;

        /// <summary>
        /// Gets the message that should be displayed to the user if the result is successful. Null if no message should
        /// be displayed.
        /// </summary>
        public string? SuccessMessage { get; }

        /// <summary>
        /// Gets the base result.
        /// </summary>
        public IResult? BaseResult { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeCommandResult"/> class.
        /// </summary>
        /// <param name="successMessage">The message that is displayed in the case of success.</param>
        private RuntimeCommandResult(string? successMessage)
            : base(null, null)
        {
            this.SuccessMessage = successMessage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeCommandResult"/> class.
        /// </summary>
        /// <param name="error">The error type.</param>
        /// <param name="errorMessage">The error message.</param>
        private RuntimeCommandResult(CommandError error, string errorMessage)
            : base(error, errorMessage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeCommandResult"/> class.
        /// </summary>
        /// <param name="baseResult">The error type.</param>
        public RuntimeCommandResult(IResult baseResult)
            : base
            (
                baseResult.IsSuccess ? (CommandError?)null : CommandError.Unsuccessful,
                baseResult.IsSuccess ? null : baseResult.ErrorReason
            )
        {
            this.BaseResult = baseResult;
        }

        /// <summary>
        /// Creates a new failed result.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>The failed result.</returns>
        public static RuntimeCommandResult FromError(string errorMessage)
        {
            return new RuntimeCommandResult(CommandError.Unsuccessful, errorMessage);
        }

        /// <summary>
        /// Creates a new successful result.
        /// </summary>
        /// <param name="successMessage">The message to be shown on success, or null.</param>
        /// <returns>The successful result.</returns>
        public static RuntimeCommandResult FromSuccess(string? successMessage = null)
        {
            return new RuntimeCommandResult(successMessage);
        }
    }
}
