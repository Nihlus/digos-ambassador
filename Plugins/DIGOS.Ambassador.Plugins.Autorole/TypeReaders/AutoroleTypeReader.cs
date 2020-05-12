//
//  AutoroleTypeReader.cs
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
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace DIGOS.Ambassador.Plugins.Autorole.TypeReaders
{
    /// <summary>
    /// A type parser for autorole configurations.
    /// </summary>
    public class AutoroleTypeReader : RoleTypeReader<SocketRole>
    {
        /// <inheritdoc />
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var baseResult = await base.ReadAsync(context, input, services);
            if (!baseResult.IsSuccess)
            {
                return baseResult;
            }

            if (!(baseResult.Values.FirstOrDefault().Value is IRole discordRole))
            {
                return TypeReaderResult.FromError
                (
                    CommandError.Unsuccessful,
                    "Failed to retrieve a role from the parsing result."
                );
            }

            var autoroles = services.GetRequiredService<AutoroleService>();

            var getAutorole = await autoroles.GetAutoroleAsync(discordRole);
            if (!getAutorole.IsSuccess)
            {
                return TypeReaderResult.FromError(CommandError.Unsuccessful, getAutorole.ErrorReason);
            }

            var autorole = getAutorole.Entity;
            return TypeReaderResult.FromSuccess(autorole);
        }
    }
}
