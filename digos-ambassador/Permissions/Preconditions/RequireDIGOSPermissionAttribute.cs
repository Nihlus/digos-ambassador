//
//  RequireDIGOSPermissionAttribute.cs
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
using System.Threading.Tasks;
using Discord.Commands;
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Permissions;

namespace DIGOS.Ambassador.Permissions.Preconditions
{
	public class RequireDIGOSPermissionAttribute : PreconditionAttribute
	{
		private DIGOSPermission Permission;
		private DIGOSPermissionTarget Target;

		public RequireDIGOSPermissionAttribute(DIGOSPermission permission, DIGOSPermissionTarget target = DIGOSPermissionTarget.Self)
		{
			this.Permission = permission;
			this.Target = target;
		}

		public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
		{
			using (var db = new GlobalUserInfoContext())
			{
				var user = await db.GetOrRegisterUserAsync(context.User);

				var permission = new UserPermission
				{
					Permission = this.Permission,
					Target = this.Target,
					ServerID = context.Guild.Id
				};

				if (user.HasPermission(permission))
				{
					return PreconditionResult.FromSuccess();
				}
			}

			return PreconditionResult.FromError("Access denied.");
		}
	}
}
