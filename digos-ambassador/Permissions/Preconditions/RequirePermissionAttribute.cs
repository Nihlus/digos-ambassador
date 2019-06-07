//
//  RequirePermissionAttribute.cs
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

namespace DIGOS.Ambassador.Permissions.Preconditions
{
    /// <summary>
    /// This attribute can be attached to Discord.Net.Commands module commands to restrict them to certain predefined
    /// permissions.
    /// </summary>
    public class RequirePermissionAttribute : PrioritizedPreconditionAttribute
    {
        private readonly (Permission Permission, PermissionTarget Target) _requiredPermission;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class.
        /// </summary>
        /// <param name="permission">The required permission.</param>
        /// <param name="target">The required target scope.</param>
        public RequirePermissionAttribute(Permission permission, PermissionTarget target = PermissionTarget.Self)
        {
            _requiredPermission = (permission, target);
        }

        /// <inheritdoc />
        protected override async Task<PreconditionResult> CheckPrioritizedPermissions([NotNull] ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var permissionService = services.GetRequiredService<PermissionService>();
            var db = services.GetRequiredService<GlobalInfoContext>();

            if (await permissionService.HasPermissionAsync(db, context.Guild, context.User, _requiredPermission))
            {
                return PreconditionResult.FromSuccess();
            }

            return PreconditionResult.FromError("You don't have permission to run that command.");
        }
    }
}
