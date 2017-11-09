//
//  ModifyEntityResult.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using Discord.Commands;
using static DIGOS.Ambassador.Services.ModifyEntityAction;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Encapsulates the result of an attempt to add or edit an entity.
	/// </summary>
	public class ModifyEntityResult : IResult
	{
		/// <inheritdoc />
		public CommandError? Error { get; }

		/// <inheritdoc />
		public string ErrorReason { get; }

		/// <inheritdoc />
		public bool IsSuccess => !this.Error.HasValue;

		/// <summary>
		/// Gets the action that was taken on the entity.
		/// </summary>
		public ModifyEntityAction ActionTaken { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ModifyEntityResult"/> class.
		/// </summary>
		/// <param name="actionTaken">The action that was taken on the entity.</param>
		/// <param name="error">The error (if any).</param>
		/// <param name="errorReason">A more detailed error description.</param>
		public ModifyEntityResult(ModifyEntityAction actionTaken, CommandError? error, string errorReason)
		{
			this.ActionTaken = actionTaken;
			this.Error = error;
			this.ErrorReason = errorReason;
		}

		/// <summary>
		/// Creates a new successful result.
		/// </summary>
		/// <param name="actionTaken">The action that was taken on the entity.</param>
		/// <returns>A successful result.</returns>
		public static ModifyEntityResult FromSuccess(ModifyEntityAction actionTaken)
		{
			return new ModifyEntityResult(actionTaken, null, null);
		}

		/// <summary>
		/// Creates a failed result.
		/// </summary>
		/// <param name="error">The error that caused the failure.</param>
		/// <param name="reason">A more detailed error reason.</param>
		/// <returns>A failed result.</returns>
		public static ModifyEntityResult FromError(CommandError error, string reason)
		{
			return new ModifyEntityResult(None, error, reason);
		}

		/// <summary>
		/// Creates a failed result based on another result.
		/// </summary>
		/// <param name="result">The result to base this result off of.</param>
		/// <returns>A failed result.</returns>
		public static ModifyEntityResult FromError(IResult result)
		{
			return new ModifyEntityResult(None, result.Error, result.ErrorReason);
		}
	}
}
