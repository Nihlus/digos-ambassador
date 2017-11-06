//
//  RequireActiveRoleplayAttribute.cs
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
using DIGOS.Ambassador.Database;
using Discord.Commands;

namespace DIGOS.Ambassador.Permissions.Preconditions
{
	/// <summary>
	/// Restricts the usage of a command to the owner of the currently active roleplay. Furthermore, it also requires a
	/// roleplay to be current.
	/// </summary>
	public class RequireActiveRoleplayAttribute : PreconditionAttribute
	{
		private readonly bool RequireOwner;

		/// <summary>
		/// Initializes a new instance of the <see cref="RequireActiveRoleplayAttribute"/> class.
		/// </summary>
		/// <param name="requireOwner">Whether or not it is required that the current roleplay is owned by the invoker.</param>
		public RequireActiveRoleplayAttribute(bool requireOwner = false)
		{
			this.RequireOwner = requireOwner;
		}

		/// <inheritdoc />
		public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
		{
			using (var db = new GlobalInfoContext())
			{
				if (!await db.HasActiveRoleplayAsync(context.Channel))
				{
					return PreconditionResult.FromError(
						"No roleplay is currently active in this channel. Please make one active, or specify the name of one.");
				}

				if (this.RequireOwner)
				{
					var roleplay = await db.GetActiveRoleplayAsync(context.Channel);
					if (roleplay.Owner.DiscordID != context.User.Id)
					{
						return PreconditionResult.FromError("Only the roleplay owner can do that.");
					}
				}
			}

			return PreconditionResult.FromSuccess();
		}
	}
}
