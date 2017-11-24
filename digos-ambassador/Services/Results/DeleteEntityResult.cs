//
//  DeleteEntityResult.cs
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
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Encapsulates the result of an attempt to delete an entity.
	/// </summary>
	public struct DeleteEntityResult : IResult
	{
		/// <inheritdoc />
		public CommandError? Error { get; }

		/// <inheritdoc />
		public string ErrorReason { get; }

		/// <inheritdoc />
		public bool IsSuccess => !this.Error.HasValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="DeleteEntityResult"/> struct.
		/// </summary>
		/// <param name="error">The error (if any).</param>
		/// <param name="errorReason">A more detailed error description.</param>
		public DeleteEntityResult([CanBeNull] CommandError? error, [CanBeNull] string errorReason)
		{
			this.Error = error;
			this.ErrorReason = errorReason;
		}

		/// <summary>
		/// Creates a new successful result.
		/// </summary>
		/// <returns>A successful result.</returns>
		public static DeleteEntityResult FromSuccess()
		{
			return new DeleteEntityResult(null, null);
		}

		/// <summary>
		/// Creates a failed result.
		/// </summary>
		/// <param name="error">The error that caused the failure.</param>
		/// <param name="reason">A more detailed error reason.</param>
		/// <returns>A failed result.</returns>
		public static DeleteEntityResult FromError(CommandError error, [NotNull] string reason)
		{
			return new DeleteEntityResult(error, reason);
		}

		/// <summary>
		/// Creates a failed result based on another result.
		/// </summary>
		/// <param name="result">The result to base this result off of.</param>
		/// <returns>A failed result.</returns>
		public static DeleteEntityResult FromError([NotNull] IResult result)
		{
			return new DeleteEntityResult(result.Error, result.ErrorReason);
		}
	}
}
