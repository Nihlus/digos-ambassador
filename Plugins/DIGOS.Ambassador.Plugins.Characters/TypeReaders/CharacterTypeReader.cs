﻿//
//  CharacterTypeReader.cs
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
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Core.TypeReaders;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Characters.TypeReaders
{
    /// <summary>
    /// Reads owned characters as command arguments.
    /// </summary>
    [PublicAPI]
    public sealed class CharacterTypeReader : OwnedEntityTypeReader<Character>
    {
        /// <inheritdoc />
        protected override async Task<RetrieveEntityResult<Character>> RetrieveEntityAsync
        (
            IUser? entityOwner,
            string? entityName,
            ICommandContext context,
            IServiceProvider services
        )
        {
            var characterService = services.GetRequiredService<CharacterService>();
            var userService = services.GetRequiredService<UserService>();

            var getInvokerResult = await userService.GetOrRegisterUserAsync(context.User);
            if (!getInvokerResult.IsSuccess)
            {
                return RetrieveEntityResult<Character>.FromError(getInvokerResult);
            }

            var invoker = getInvokerResult.Entity;
            if (!entityName.IsNullOrWhitespace() && string.Equals(entityName, "current", StringComparison.OrdinalIgnoreCase))
            {
                return await characterService.GetCurrentCharacterAsync(context, invoker);
            }

            if (entityOwner is null)
            {
                return await characterService.GetBestMatchingCharacterAsync(context, null, entityName);
            }

            var getOwnerResult = await userService.GetOrRegisterUserAsync(entityOwner);
            if (!getOwnerResult.IsSuccess)
            {
                return RetrieveEntityResult<Character>.FromError(getOwnerResult);
            }

            var owner = getOwnerResult.Entity;
            return await characterService.GetBestMatchingCharacterAsync(context, owner, entityName);
        }
    }
}
