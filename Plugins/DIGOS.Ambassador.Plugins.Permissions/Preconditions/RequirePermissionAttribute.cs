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
using DIGOS.Ambassador.Plugins.Permissions.Model;
using DIGOS.Ambassador.Plugins.Permissions.Services;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace DIGOS.Ambassador.Plugins.Permissions.Preconditions
{
    /// <summary>
    /// This attribute can be attached to Discord.Net.Commands module commands to restrict them to certain predefined
    /// permissions.
    /// </summary>
    [PublicAPI]
    public class RequirePermissionAttribute : PrioritizedPreconditionAttribute
    {
        private readonly Type _permissionType;
        private readonly PermissionTarget _target;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class.
        /// </summary>
        /// <param name="permissionType">The required permission.</param>
        /// <param name="target">The required target scope.</param>
        public RequirePermissionAttribute(Type permissionType, PermissionTarget target)
        {
            _permissionType = permissionType;
            _target = target;
        }

        /// <inheritdoc />
        protected override async Task<PreconditionResult> CheckPrioritizedPermissions
        (
            ICommandContext context,
            CommandInfo command,
            IServiceProvider services
        )
        {
            if (context.Guild is null || !(context.User is SocketGuildUser guildUser))
            {
                return PreconditionResult.FromError("Permissions are only supported on guild commands.");
            }

            var permissionService = services.GetRequiredService<PermissionService>();
            var permissionRegistry = services.GetRequiredService<PermissionRegistryService>();

            var getPermissionResult = permissionRegistry.GetPermission(_permissionType);
            if (!getPermissionResult.IsSuccess)
            {
                return PreconditionResult.FromError(getPermissionResult.ErrorReason);
            }

            var permission = getPermissionResult.Entity;

            var hasPermissionResult = await permissionService.HasPermissionAsync
            (
                context.Guild,
                guildUser,
                permission,
                _target
            );

            if (hasPermissionResult.IsSuccess)
            {
                return PreconditionResult.FromSuccess();
            }

            return PreconditionResult.FromError(hasPermissionResult.ErrorReason);
        }
    }
}
