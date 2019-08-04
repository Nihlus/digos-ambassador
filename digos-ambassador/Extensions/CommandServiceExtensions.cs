//
//  CommandServiceExtensions.cs
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

using DIGOS.Ambassador.Discord.TypeReaders;
using DIGOS.Ambassador.TypeReaders;
using Discord.Commands;

namespace DIGOS.Ambassador.Extensions
{
    /// <summary>
    /// Extensions to the <see cref="CommandService"/> class.
    /// </summary>
    public static class CommandServiceExtensions
    {
        /// <summary>
        /// Adds a type reader to the command service based on a <see cref="HumanizerEnumTypeReader{T}"/>.
        /// </summary>
        /// <param name="this">The command service.</param>
        /// <typeparam name="TEnum">The enum type to add.</typeparam>
        public static void AddEnumReader<TEnum>(this CommandService @this) where TEnum : struct, System.Enum
        {
            @this.AddTypeReader<TEnum>(new HumanizerEnumTypeReader<TEnum>());
        }
    }
}
