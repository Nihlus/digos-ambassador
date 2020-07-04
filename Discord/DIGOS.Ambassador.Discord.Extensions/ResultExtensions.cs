//
//  ResultExtensions.cs
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

using DIGOS.Ambassador.Discord.Extensions.Results;
using Discord.Commands;
using IResult = Remora.Results.IResult;

namespace DIGOS.Ambassador.Discord.Extensions
{
    /// <summary>
    /// Contains extension methods for result types.
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// Converts the given Remora result to a Discord.NET command result.
        /// </summary>
        /// <param name="this">The Remora result.</param>
        /// <returns>The converted result.</returns>
        public static RuntimeResult ToRuntimeResult(this IResult @this)
        {
            return new RuntimeCommandResult(@this);
        }
    }
}
