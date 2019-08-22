//
//  PrioritizedPreconditionAttribute.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Permissions.Preconditions
{
    /// <summary>
    /// Represents a prioritized precondition that is ordered and tested by its priority.
    /// </summary>
    public abstract class PrioritizedPreconditionAttribute : PreconditionAttribute
    {
        /// <summary>
        /// Gets or sets the priority of this precondition. Higher priorities are tested first.
        /// </summary>
        public int Priority { get; set; }

        /// <inheritdoc />
        public sealed override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, [NotNull] CommandInfo command, IServiceProvider services)
        {
            var prioPreconditions = command.Preconditions.Where(a => a is PrioritizedPreconditionAttribute).Cast<PrioritizedPreconditionAttribute>();

            foreach (var prioConditionGroup in prioPreconditions.GroupBy(p => p.Group, StringComparer.Ordinal))
            {
                if (prioConditionGroup.Key is null)
                {
                    // Just check the permissions as normal
                    foreach (var prioPrecondition in prioConditionGroup.OrderBy(p => p.Priority))
                    {
                        var checkResult = await prioPrecondition.CheckPrioritizedPermissions(context, command, services);
                        if (!checkResult.IsSuccess)
                        {
                            return checkResult;
                        }
                    }
                }
                else
                {
                    var results = new List<PreconditionResult>();
                    foreach (var prioPrecondition in prioConditionGroup.OrderBy(p => p.Priority))
                    {
                        results.Add(await prioPrecondition.CheckPrioritizedPermissions(context, command, services));
                    }

                    if (!results.Any(p => p.IsSuccess))
                    {
                        return PreconditionGroupResult.FromError($"Prioritized precondition group \"{prioConditionGroup.Key}\" failed.", results);
                    }
                }
            }

            return PreconditionResult.FromSuccess();
        }

        /// <summary>
        /// Checks the permissions of this specific attribute. Calling <see cref="CheckPermissionsAsync"/> will hijack the
        /// process and check all prioritized preconditions as well.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="command">The invoked command.</param>
        /// <param name="services">The services.</param>
        /// <returns>The result of the permission check.</returns>
        protected abstract Task<PreconditionResult> CheckPrioritizedPermissions(ICommandContext context, CommandInfo command, IServiceProvider services);
    }
}
