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
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Core.TypeReaders;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Roleplaying.TypeReaders
{
    /// <summary>
    /// Reads owned roleplays as command arguments. The name "current" is reserved, and will retrieve the current
    /// roleplay.
    /// </summary>
    [PublicAPI]
    public sealed class RoleplayTypeReader : OwnedEntityTypeReader<Roleplay>
    {
        /// <inheritdoc />
        protected override async Task<RetrieveEntityResult<Roleplay>> RetrieveEntityAsync
        (
            IUser? entityOwner,
            string? entityName,
            ICommandContext context,
            IServiceProvider services
        )
        {
            var roleplayService = services.GetRequiredService<RoleplayService>();

            if (!entityName.IsNullOrWhitespace() && string.Equals(entityName, "current", StringComparison.OrdinalIgnoreCase))
            {
                return await roleplayService.GetActiveRoleplayAsync(context.Channel);
            }

            return await roleplayService.GetBestMatchingRoleplayAsync
            (
                (ITextChannel)context.Channel,
                context.Guild,
                entityOwner,
                entityName
            );
        }
    }
}
