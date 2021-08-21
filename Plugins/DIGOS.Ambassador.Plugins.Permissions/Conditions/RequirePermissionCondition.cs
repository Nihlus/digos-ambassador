//
//  RequirePermissionCondition.cs
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

using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Permissions.Services;
using Remora.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Permissions.Conditions
{
    /// <summary>
    /// Marks a command as requiring a certain permission.
    /// </summary>
    public class RequirePermissionCondition : ICondition<RequirePermissionAttribute>
    {
        private readonly PermissionService _permissions;
        private readonly PermissionRegistryService _permissionRegistry;
        private readonly ICommandContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequirePermissionCondition"/> class.
        /// </summary>
        /// <param name="permissions">The permissions service.</param>
        /// <param name="permissionRegistry">The permissions registry.</param>
        /// <param name="context">The command context.</param>
        public RequirePermissionCondition
        (
            PermissionService permissions,
            PermissionRegistryService permissionRegistry,
            ICommandContext context
        )
        {
            _permissions = permissions;
            _permissionRegistry = permissionRegistry;
            _context = context;
        }

        /// <inheritdoc />
        public async ValueTask<Result> CheckAsync(RequirePermissionAttribute attribute, CancellationToken ct = default)
        {
            if (!_context.GuildID.IsDefined(out var guildID))
            {
                return new InvalidOperationError("This condition must be executed in a guild.");
            }

            var getPermissionResult = _permissionRegistry.GetPermission(attribute.Type);
            if (!getPermissionResult.IsSuccess)
            {
                return Result.FromError(getPermissionResult);
            }

            var permission = getPermissionResult.Entity;

            return await _permissions.HasPermissionAsync
            (
                guildID,
                _context.User.ID,
                permission,
                attribute.Target,
                ct
            );
        }
    }
}
