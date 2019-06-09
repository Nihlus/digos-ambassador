//
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

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Users;
using Discord;
using Discord.Commands;

using Microsoft.Extensions.DependencyInjection;

namespace DIGOS.Ambassador.TypeReaders
{
    /// <summary>
    /// Reads owned characters as command arguments.
    /// </summary>
    public sealed class CharacterTypeReader : OwnedEntityTypeReader<Character>
    {
        /// <inheritdoc />
        protected override async Task<RetrieveEntityResult<Character>> RetrieveEntityAsync
        (
            IUser entityOwner,
            string entityName,
            ICommandContext context,
            IServiceProvider services
        )
        {
            var characterService = services.GetRequiredService<CharacterService>();
            var userService = services.GetRequiredService<UserService>();
            var db = services.GetRequiredService<GlobalInfoContext>();

            var getInvokerResult = await userService.GetOrRegisterUserAsync(db, context.User);
            if (!getInvokerResult.IsSuccess)
            {
                return RetrieveEntityResult<Character>.FromError(getInvokerResult);
            }

            var invoker = getInvokerResult.Entity;
            if (!entityName.IsNullOrWhitespace() && string.Equals(entityName, "current", StringComparison.OrdinalIgnoreCase))
            {
                return await characterService.GetCurrentCharacterAsync(db, context, invoker);
            }

            if (entityOwner is null)
            {
                return await characterService.GetBestMatchingCharacterAsync(db, context, null, entityName);
            }

            var getOwnerResult = await userService.GetOrRegisterUserAsync(db, entityOwner);
            if (!getOwnerResult.IsSuccess)
            {
                return RetrieveEntityResult<Character>.FromError(getOwnerResult);
            }

            var owner = getOwnerResult.Entity;
            return await characterService.GetBestMatchingCharacterAsync(db, context, owner, entityName);
        }
    }
}
