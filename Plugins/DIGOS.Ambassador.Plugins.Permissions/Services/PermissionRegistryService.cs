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
using System.Reflection;
using DIGOS.Ambassador.Discord.Feedback.Errors;
using Microsoft.Extensions.DependencyInjection;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Permissions.Services
{
    /// <summary>
    /// Handles runtime registration of available permissions.
    /// </summary>
    public sealed class PermissionRegistryService
    {
        private readonly Dictionary<Type, IPermission> _registeredPermissions = new Dictionary<Type, IPermission>();

        /// <summary>
        /// Gets the permissions that have been registered in the service.
        /// </summary>
        public IEnumerable<IPermission> RegisteredPermissions => _registeredPermissions.Values;

        /// <summary>
        /// Registers the permissions available in the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly to register permissions from.</param>
        /// <param name="services">The services.</param>
        /// <returns>true if all permissions were successfully registered; otherwise, false.</returns>
        public Result RegisterPermissions(Assembly assembly, IServiceProvider services)
        {
            var permissionTypes = assembly.DefinedTypes.Where
            (
                t =>
                    t.ImplementedInterfaces.Contains(typeof(IPermission)) &&
                    !t.IsAbstract
            );

            foreach (var permissionType in permissionTypes)
            {
                var result = RegisterPermission(permissionType, services);
                if (!result.IsSuccess)
                {
                    return Result.FromError(result);
                }
            }

            return Result.FromSuccess();
        }

        /// <summary>
        /// Registers the given permission type, making it available to the system.
        /// </summary>
        /// <param name="services">The application's services.</param>
        /// <typeparam name="TPermission">The permission type.</typeparam>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public Result<TPermission> RegisterPermission<TPermission>(IServiceProvider services)
            where TPermission : class, IPermission
        {
            var permissionType = typeof(TPermission);
            var registerPermissionResult = RegisterPermission(permissionType, services);

            return !registerPermissionResult.IsSuccess
                ? Result<TPermission>.FromError(registerPermissionResult)
                : Result<TPermission>.FromSuccess((TPermission)registerPermissionResult.Entity);
        }

        /// <summary>
        /// Registers the given permission type, making it available to the system.
        /// </summary>
        /// <param name="permissionType">The permission type.</param>
        /// <param name="services">The application's services.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public Result<IPermission> RegisterPermission
        (
            Type permissionType,
            IServiceProvider services
        )
        {
            if (_registeredPermissions.ContainsKey(permissionType))
            {
                return new UserError("The given permission has already been registered.");
            }

            IPermission permissionInstance;
            try
            {
                permissionInstance = (IPermission)ActivatorUtilities.CreateInstance(services, permissionType);
            }
            catch (Exception e)
            {
                return Result<IPermission>.FromError(e);
            }

            if (_registeredPermissions.Values.Any(p => p.UniqueIdentifier == permissionInstance.UniqueIdentifier))
            {
                return new UserError
                (
                    "A permission with that identifier has already been registered."
                );
            }

            _registeredPermissions.Add(permissionType, permissionInstance);
            return Result<IPermission>.FromSuccess(permissionInstance);
        }

        /// <summary>
        /// Retrieves the permission instance of the given type from the registry.
        /// </summary>
        /// <typeparam name="TPermission">The permission type.</typeparam>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public Result<TPermission> GetPermission<TPermission>()
            where TPermission : class, IPermission
        {
            var result = GetPermission(typeof(TPermission));

            return !result.IsSuccess
                ? Result<TPermission>.FromError(result)
                : Result<TPermission>.FromSuccess((TPermission)result.Entity);
        }

        /// <summary>
        /// Retrieves the permission instance of the given type from the registry.
        /// </summary>
        /// <param name="permissionType">The permission type.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public Result<IPermission> GetPermission(Type permissionType)
        {
            return !_registeredPermissions.TryGetValue(permissionType, out var permission)
                ? new UserError("No permission of that type has been registered.")
                : Result<IPermission>.FromSuccess(permission);
        }

        /// <summary>
        /// Retrieves the permission instance with the given name from the registry. The name is matched on a
        /// case-insensitive basis.
        /// </summary>
        /// <param name="permissionName">The friendly name of the permission type.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public Result<IPermission> GetPermission(string permissionName)
        {
            var permission = this.RegisteredPermissions.FirstOrDefault
            (
                p => p.FriendlyName.Equals(permissionName, StringComparison.OrdinalIgnoreCase)
            );

            return permission is null
                ? new UserError("No permission of that type has been registered.")
                : Result<IPermission>.FromSuccess(permission);
        }
    }
}
