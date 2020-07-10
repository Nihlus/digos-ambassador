//
//  RequireSelfOrPermissionAttribute.cs
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
using DIGOS.Ambassador.Plugins.Permissions.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

namespace DIGOS.Ambassador.Plugins.Core.Preconditions
{
    /// <summary>
    /// Acts as a precondition for users, allowing access if it's either the invoking user themselves, or if they
    /// have another permission.
    /// </summary>
    [PublicAPI]
    public class RequireSelfOrPermissionAttribute : ParameterPreconditionAttribute
    {
        private readonly Type _permissionType;
        private readonly PermissionTarget _target;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireSelfOrPermissionAttribute"/> class.
        /// </summary>
        /// <param name="permissionType">The permission to require.</param>
        /// <param name="target">The target to require.</param>
        public RequireSelfOrPermissionAttribute(Type permissionType, PermissionTarget target)
        {
            _permissionType = permissionType;
            _target = target;
        }

        /// <inheritdoc />
        public override async Task<PreconditionResult> CheckPermissionsAsync
        (
            ICommandContext context,
            ParameterInfo parameter,
            object value,
            IServiceProvider services
        )
        {
            if (!(value is IUser user))
            {
                return PreconditionResult.FromError("The value isn't a user.");
            }

            if (context.Guild is null || !(context.User is SocketGuildUser guildUser))
            {
                return PreconditionResult.FromError("Permissions are only supported on guild commands.");
            }

            if (user.Id == guildUser.Id)
            {
                return PreconditionResult.FromSuccess();
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

            if (!hasPermissionResult.IsSuccess)
            {
                return PreconditionResult.FromError("You don't have permission to do that.");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}
