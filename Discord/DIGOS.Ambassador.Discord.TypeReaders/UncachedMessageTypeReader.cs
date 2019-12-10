﻿//
//  UncachedMessageTypeReader.cs
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
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DIGOS.Ambassador.Discord.TypeReaders
{
    /// <summary>
    /// Reads an IMessage, downloading it if necessary.
    /// </summary>
    /// <typeparam name="T">A type implementing <see cref="IMessage"/>.</typeparam>
    public class UncachedMessageTypeReader<T> : TypeReader where T : class, IMessage
    {
        /// <inheritdoc />
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            if (ulong.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            {
                if (await context.Channel.GetMessageAsync(id) is T message)
                {
                    return TypeReaderResult.FromSuccess(message);
                }
            }

            return TypeReaderResult.FromError(CommandError.Unsuccessful, "Message not found.");
        }
    }
}
