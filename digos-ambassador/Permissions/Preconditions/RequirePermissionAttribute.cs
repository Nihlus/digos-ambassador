//
//  RequirePermissionAttribute.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Permissions;
using DIGOS.Ambassador.Database.ServerInfo;

namespace DIGOS.Ambassador.Permissions.Preconditions
{
	public class RequirePermissionAttribute : PreconditionAttribute
	{
		private readonly Permission Permission;
		private readonly PermissionTarget Target;

		/// <summary>
		/// Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class.
		/// </summary>
		/// <param name="permission">The required permission.</param>
		/// <param name="target">The required target scope.</param>
		public RequirePermissionAttribute(Permission permission, PermissionTarget target = PermissionTarget.Self)
		{
			this.Permission = permission;
			this.Target = target;
		}

		/// <inheritdoc />
		public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
		{
			using (var db = new GlobalUserInfoContext())
			{
				var user = await db.GetOrRegisterUserAsync(context.User);

				var permission = new UserPermission
				{
					Permission = this.Permission,
					Target = this.Target,
					Servers = new List<Server> { Server.CreateDefault(context.Guild) }
				};

				if (PermissionChecker.HasPermission(context.Guild, user, permission))
				{
					return PreconditionResult.FromSuccess();
				}
			}

			return PreconditionResult.FromError("Access denied.");
		}
	}
}
