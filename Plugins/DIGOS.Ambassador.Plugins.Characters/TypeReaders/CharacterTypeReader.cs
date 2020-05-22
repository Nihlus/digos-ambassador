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
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services;
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
            var characterService = services.GetRequiredService<CharacterDiscordService>();

            // Special case
            if (!string.Equals(entityName, "current", StringComparison.OrdinalIgnoreCase))
            {
                return await characterService.GetBestMatchingCharacterAsync
                (
                    context.Guild,
                    entityOwner as IGuildUser,
                    entityName
                );
            }

            if (!(context.User is IGuildUser invoker))
            {
                return RetrieveEntityResult<Character>.FromError("The current user isn't a guild user.");
            }

            return await characterService.GetCurrentCharacterAsync(invoker);
        }
    }
}
