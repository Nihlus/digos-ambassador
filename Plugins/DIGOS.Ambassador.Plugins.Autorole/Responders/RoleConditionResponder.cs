//
//  RoleConditionResponder.cs
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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Autorole.Responders
{
    /// <summary>
    /// Responds to role changes.
    /// </summary>
    public class RoleConditionResponder : IResponder<IGuildMemberUpdate>
    {
        private readonly AutoroleService _autoroles;
        private readonly AutoroleUpdateService _autoroleUpdates;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleConditionResponder"/> class.
        /// </summary>
        /// <param name="autoroles">The autorole service.</param>
        /// <param name="autoroleUpdates">The autorole update service.</param>
        public RoleConditionResponder(AutoroleService autoroles, AutoroleUpdateService autoroleUpdates)
        {
            _autoroles = autoroles;
            _autoroleUpdates = autoroleUpdates;
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IGuildMemberUpdate gatewayEvent, CancellationToken ct = default)
        {
            var guild = gatewayEvent.GuildID;

            var autoroles = await _autoroles.GetAutorolesAsync
            (
                guild,
                q => q
                    .Where(a => a.IsEnabled)
                    .Where
                    (
                        a => a.Conditions.Any
                        (
                            c =>
                                c.GetType() == typeof(RoleCondition)
                        )
                    ),
                ct
            );

            foreach (var autorole in autoroles)
            {
                using var transaction = TransactionFactory.Create();

                var rolesToLookFor = autorole.Conditions
                    .Where(c => c is RoleCondition)
                    .Cast<RoleCondition>()
                    .Select(c => c.RoleID);

                var userHasAutorole = gatewayEvent.Roles.Contains(autorole.DiscordRoleID);
                var userHasRelevantRole = gatewayEvent.Roles.Any(r => rolesToLookFor.Contains(r));

                if (userHasAutorole || userHasRelevantRole)
                {
                    var updateAutorole = await _autoroleUpdates.UpdateAutoroleForUserAsync
                    (
                        autorole,
                        guild,
                        gatewayEvent.User.ID,
                        ct
                    );

                    if (!updateAutorole.IsSuccess)
                    {
                        return Result.FromError(updateAutorole);
                    }
                }

                transaction.Complete();
            }

            return Result.FromSuccess();
        }
    }
}
