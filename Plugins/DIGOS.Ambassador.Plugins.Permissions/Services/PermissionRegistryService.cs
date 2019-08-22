//
//  PermissionRegistryService.cs
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
using DIGOS.Ambassador.Core.Results;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace DIGOS.Ambassador.Plugins.Permissions.Services
{
    /// <summary>
    /// Handles runtime registration of available permissions.
    /// </summary>
    [PublicAPI]
    public class PermissionRegistryService
    {
        private readonly Dictionary<Type, IPermission> _registeredPermissions = new Dictionary<Type, IPermission>();

        /// <summary>
        /// Gets the permissions that have been registered in the service.
        /// </summary>
        public IEnumerable<IPermission> RegisteredPermissions => _registeredPermissions.Values;

        /// <summary>
        /// Registers the given permission type, making it available to the system.
        /// </summary>
        /// <param name="services">The application's services.</param>
        /// <typeparam name="TPermission">The permission type.</typeparam>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        [NotNull]
        public CreateEntityResult<TPermission> RegisterPermission<TPermission>([NotNull] IServiceProvider services)
            where TPermission : class, IPermission
        {
            var permissionType = typeof(TPermission);
            if (_registeredPermissions.ContainsKey(permissionType))
            {
                return CreateEntityResult<TPermission>.FromError("The given permission has already been registered.");
            }

            TPermission permissionInstance;
            try
            {
                permissionInstance = ActivatorUtilities.CreateInstance<TPermission>(services);
            }
            catch (Exception e)
            {
                return CreateEntityResult<TPermission>.FromError(e);
            }

            if (_registeredPermissions.Values.Any(p => p.UniqueIdentifier == permissionInstance.UniqueIdentifier))
            {
                return CreateEntityResult<TPermission>.FromError
                (
                    "A permission with that identifier has already been registered."
                );
            }

            _registeredPermissions.Add(permissionType, permissionInstance);
            return CreateEntityResult<TPermission>.FromSuccess(permissionInstance);
        }

        /// <summary>
        /// Retrieves the permission instance of the given type from the registry.
        /// </summary>
        /// <typeparam name="TPermission">The permission type.</typeparam>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [NotNull]
        public RetrieveEntityResult<TPermission> GetPermission<TPermission>()
            where TPermission : class, IPermission
        {
            var result = GetPermission(typeof(TPermission));
            if (!result.IsSuccess)
            {
                return RetrieveEntityResult<TPermission>.FromError(result);
            }

            return RetrieveEntityResult<TPermission>.FromSuccess((TPermission)result.Entity);
        }

        /// <summary>
        /// Retrieves the permission instance of the given type from the registry.
        /// </summary>
        /// <param name="permissionType">The permission type.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [NotNull]
        public RetrieveEntityResult<IPermission> GetPermission([NotNull] Type permissionType)
        {
            if (!_registeredPermissions.TryGetValue(permissionType, out var permission))
            {
                return RetrieveEntityResult<IPermission>.FromError("No permission of that type has been registered.");
            }

            return RetrieveEntityResult<IPermission>.FromSuccess(permission);
        }

        /// <summary>
        /// Retrieves the permission instance with the given name from the registry. The name is matched on a
        /// case-insensitive basis.
        /// </summary>
        /// <param name="permissionName">The friendly name of the permission type.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [NotNull]
        public RetrieveEntityResult<IPermission> GetPermission([NotNull] string permissionName)
        {
            var permission = this.RegisteredPermissions.FirstOrDefault
            (
                p => p.FriendlyName.Equals(permissionName, StringComparison.OrdinalIgnoreCase)
            );

            if (permission is null)
            {
                return RetrieveEntityResult<IPermission>.FromError("No permission of that type has been registered.");
            }

            return RetrieveEntityResult<IPermission>.FromSuccess(permission);
        }
    }
}
