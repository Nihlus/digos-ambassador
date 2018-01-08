//
//  RequireEntityOwnerOrPermissionAttribute.cs
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
using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Services;

using Discord.Commands;

using Microsoft.Extensions.DependencyInjection;

namespace DIGOS.Ambassador.Permissions.Preconditions
{
	/// <summary>
	/// Acts as a precondition for owned entities, limiting their use to their owners or users with explicit permission.
	/// </summary>
	public class RequireEntityOwnerOrPermissionAttribute : ParameterPreconditionAttribute
	{
		/// <summary>
		/// Gets the required permission.
		/// </summary>
		public Permission Permission { get;  }

		/// <summary>
		/// Gets the required target.
		/// </summary>
		public PermissionTarget Target { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RequireEntityOwnerOrPermissionAttribute"/> class.
		/// </summary>
		/// <param name="permission">The permission to require.</param>
		/// <param name="target">The target to require.</param>
		public RequireEntityOwnerOrPermissionAttribute(Permission permission, PermissionTarget target)
		{
			this.Permission = permission;
			this.Target = target;
		}

		/// <inheritdoc />
		public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, ParameterInfo parameter, object value, IServiceProvider services)
		{
			if (!(value is IOwnedNamedEntity entity))
			{
				return PreconditionResult.FromError("The value isn't an owned entity.");
			}

			var permissionService = services.GetRequiredService<PermissionService>();
			var db = services.GetRequiredService<GlobalInfoContext>();

			if (entity.IsOwner(context.User))
			{
				return PreconditionResult.FromSuccess();
			}

			bool hasPermission = await permissionService.HasPermissionAsync
			(
				db,
				context.Guild,
				context.User,
				(this.Permission, this.Target)
			);

			if (!hasPermission)
			{
				return PreconditionResult.FromError("You don't have permission to do that.");
			}

			return PreconditionResult.FromSuccess();
		}
	}
}
