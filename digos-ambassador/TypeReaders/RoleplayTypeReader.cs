//
//  RoleplayTypeReader.cs
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
using DIGOS.Ambassador.Database.Roleplaying;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Services;

using Discord;
using Discord.Commands;

using Microsoft.Extensions.DependencyInjection;

namespace DIGOS.Ambassador.TypeReaders
{
	/// <summary>
	/// Reads owned roleplays as command arguments. The name "current" is reserved, and will retrieve the current
	/// roleplay.
	/// </summary>
	public sealed class RoleplayTypeReader : OwnedEntityTypeReader<IUser, Roleplay>
	{
		/// <inheritdoc />
		protected override async Task<RetrieveEntityResult<Roleplay>> RetrieveEntityAsync(IUser entityOwner, string entityName, ICommandContext context, IServiceProvider services)
		{
			var roleplayService = services.GetRequiredService<RoleplayService>();
			var db = services.GetRequiredService<GlobalInfoContext>();

			if (!entityName.IsNullOrWhitespace() && string.Equals(entityName, "current", StringComparison.OrdinalIgnoreCase))
			{
				return await roleplayService.GetActiveRoleplayAsync(db, context);
			}

			return await roleplayService.GetBestMatchingRoleplayAsync(db, context, entityOwner, entityName);
		}
	}
}
