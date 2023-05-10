//
//  ResultExtensions.cs
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

using Remora.Discord.API.Abstractions.Results;
using Remora.Discord.API.Objects;
using Remora.Rest.Results;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Auctions.Extensions;

/// <summary>
/// Defines extension methods for result types.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Determines whether the result failed with the specified Discord error code.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="error">The error code.</param>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <returns>true if the result failed because of the specified error code; otherwise, false.</returns>
    public static bool FailedBecauseOf<TResult>(this TResult result, DiscordError error) where TResult : IResult
    {
        if (result.IsSuccess)
        {
            return false;
        }

        if (result.Error is not RestResultError<RestError> restError)
        {
            return false;
        }

        if (!restError.Error.Code.IsDefined(out var code))
        {
            return false;
        }

        return code == error;
    }
}
