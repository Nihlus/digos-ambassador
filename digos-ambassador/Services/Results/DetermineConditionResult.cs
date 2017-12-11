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
using Discord.Commands;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Represents an attempt to retrieve a roleplay from the database.
	/// </summary>
	public struct DetermineConditionResult : IResult
	{
		/// <inheritdoc />
		public CommandError? Error { get; }

		/// <inheritdoc />
		public string ErrorReason { get; }

		/// <inheritdoc />
		public bool IsSuccess { get; }

		/// <summary>
		/// Gets the exception that caused the error, if any.
		/// </summary>
		[CanBeNull]
		public Exception Exception { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DetermineConditionResult"/> struct.
		/// </summary>
		/// <param name="wasSuccessful">Whether or not the condition passed or not.</param>
		/// <param name="error">The error (if any).</param>
		/// <param name="errorReason">A more detailed error description.</param>
		/// <param name="exception">The exception that caused the error (if any).</param>
		private DetermineConditionResult(bool wasSuccessful, [CanBeNull] CommandError? error, [CanBeNull] string errorReason, [CanBeNull] Exception exception = null)
		{
			this.IsSuccess = wasSuccessful;
			this.Error = error;
			this.ErrorReason = errorReason;
			this.Exception = exception;
		}

		/// <summary>
		/// Creates a new successful result.
		/// </summary>
		/// <returns>A successful result.</returns>
		[Pure]
		public static DetermineConditionResult FromSuccess()
		{
			return new DetermineConditionResult(true, null, null);
		}

		/// <summary>
		/// Creates a failed result.
		/// </summary>
		/// <param name="reason">A more detailed error reason.</param>
		/// <returns>A failed result.</returns>
		[Pure]
		public static DetermineConditionResult FromError([NotNull] string reason)
		{
			return new DetermineConditionResult(false, CommandError.UnmetPrecondition, reason);
		}

		/// <summary>
		/// Creates a failed result.
		/// </summary>
		/// <param name="error">The error that caused the failure.</param>
		/// <param name="reason">A more detailed error reason.</param>
		/// <returns>A failed result.</returns>
		[Pure]
		public static DetermineConditionResult FromError(CommandError error, [NotNull] string reason)
		{
			return new DetermineConditionResult(false, error, reason);
		}

		/// <summary>
		/// Creates a failed result based on another result.
		/// </summary>
		/// <param name="result">The result to base this result off of.</param>
		/// <returns>A failed result.</returns>
		[Pure]
		public static DetermineConditionResult FromError([NotNull] IResult result)
		{
			return new DetermineConditionResult(false, result.Error, result.ErrorReason);
		}

		/// <summary>
		/// Creates a failed result based on an exception.
		/// </summary>
		/// <param name="exception">The exception to base this result off of.</param>
		/// <param name="reason">The reason for the exception. Optional, defaults to the exception message.</param>
		/// <returns>A failed result.</returns>
		[Pure]
		public static DetermineConditionResult FromError([NotNull] Exception exception, string reason = null)
		{
			reason = reason ?? exception.Message;

			return new DetermineConditionResult(false, CommandError.Exception, reason, exception);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.IsSuccess ? "Success" : $"{this.Error}: {this.ErrorReason}";
		}
	}
}
