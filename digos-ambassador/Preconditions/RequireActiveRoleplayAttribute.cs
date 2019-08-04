//
//  RequireActiveRoleplayAttribute.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Services;
using Discord.Commands;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace DIGOS.Ambassador.Preconditions
{
    /// <summary>
    /// Restricts the usage of a command to the owner of the currently active roleplay. Furthermore, it also requires a
    /// roleplay to be current.
    /// </summary>
    public class RequireActiveRoleplayAttribute : PreconditionAttribute
    {
        private readonly bool _requireOwner;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireActiveRoleplayAttribute"/> class.
        /// </summary>
        /// <param name="requireOwner">Whether or not it is required that the current roleplay is owned by the invoker.</param>
        public RequireActiveRoleplayAttribute(bool requireOwner = false)
        {
            _requireOwner = requireOwner;
        }

        /// <inheritdoc />
        public override async Task<PreconditionResult> CheckPermissionsAsync([NotNull] ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var roleplayService = services.GetRequiredService<RoleplayService>();
            var db = services.GetRequiredService<AmbyDatabaseContext>();

            var result = await roleplayService.GetActiveRoleplayAsync(db, context.Channel);
            if (!result.IsSuccess)
            {
                return PreconditionResult.FromError(result.ErrorReason);
            }

            if (_requireOwner)
            {
                var roleplay = result.Entity;
                if (roleplay.Owner.DiscordID != (long)context.User.Id)
                {
                    return PreconditionResult.FromError("Only the roleplay owner can do that.");
                }
            }

            return PreconditionResult.FromSuccess();
        }
    }
}
