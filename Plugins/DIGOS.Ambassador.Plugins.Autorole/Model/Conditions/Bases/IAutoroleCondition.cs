//
//  IAutoroleCondition.cs
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
using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.Bases
{
    /// <summary>
    /// Defines the public API of an autorole condition.
    /// </summary>
    public interface IAutoroleCondition
    {
        /// <summary>
        /// Gets a piece of text that sufficiently describes what the condition requires, suitable for display in a UI.
        /// </summary>
        /// <returns>The descriptive UI text.</returns>
        string GetDescriptiveUIText();

        /// <summary>
        /// Determines whether this condition has the same conditions as another condition.
        /// </summary>
        /// <param name="autoroleCondition">The other condition.</param>
        /// <returns>true if this condition has the same conditions as another condition; otherwise, false.</returns>
        bool HasSameConditionsAs(IAutoroleCondition autoroleCondition);

        /// <summary>
        /// Determines whether the condition is fulfilled for the given Discord user.
        /// </summary>
        /// <param name="services">The service provider.</param>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>true if the condition is fulfilled; otherwise, false.</returns>
        Task<Result<bool>> IsConditionFulfilledForUserAsync
        (
            IServiceProvider services,
            Snowflake guildID,
            Snowflake userID,
            CancellationToken ct = default
        );
    }
}
