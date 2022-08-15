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

using System;
using Remora.Commands.Results;
using Remora.Results;

namespace DIGOS.Ambassador.Extensions;

/// <summary>
/// Defines extension methods for Result types.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Unwraps a result, attempting to find the most relevant error in its chain.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <returns>The most relevant error.</returns>s
    public static IResult GetMostRelevantResult(this IResult result)
    {
        var current = result;
        while (true)
        {
            if (current.IsSuccess)
            {
                throw new InvalidOperationException();
            }

            if (current.Inner is null)
            {
                // No deeper errors, this is the most relevant error
                return current;
            }

            switch (current.Error)
            {
                case ParameterParsingError when current.Inner.Error is ParameterParsingError:
                {
                    current = current.Inner;
                    continue;
                }
                case ConditionNotSatisfiedError when current.Inner.Error is ConditionNotSatisfiedError:
                {
                    current = current.Inner;
                    continue;
                }
                default:
                {
                    return current;
                }
            }
        }
    }
}
