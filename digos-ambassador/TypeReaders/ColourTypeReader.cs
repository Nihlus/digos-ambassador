//
//  ColourTypeReader.cs
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
using DIGOS.Ambassador.Database.Transformations.Appearances;
using Discord.Commands;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.TypeReaders
{
    /// <summary>
    /// Reads owned characters as command arguments.
    /// </summary>
    public sealed class ColourTypeReader : TypeReader
    {
        /// <inheritdoc />
        [NotNull]
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            if (!Colour.TryParse(input, out var colour))
            {
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Failed to parse a valid colour."));
            }

            return Task.FromResult(TypeReaderResult.FromSuccess(colour));
        }
    }
}
