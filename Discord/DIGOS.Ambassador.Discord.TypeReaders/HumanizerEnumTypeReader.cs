﻿//
//  HumanizerEnumTypeReader.cs
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
using Discord.Commands;
using Humanizer;
using Humanizer.Configuration;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Discord.TypeReaders
{
    /// <summary>
    /// Reads enums using Humanizer's DehumanizeTo function.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    public class HumanizerEnumTypeReader<T> : TypeReader where T : struct, IComparable, IFormattable
    {
        /// <inheritdoc />
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var defaultLocator = Configurator.EnumDescriptionPropertyLocator;
            Configurator.EnumDescriptionPropertyLocator = info => false;

            try
            {
                var result = input.DehumanizeTo<T>();
                return Task.FromResult(TypeReaderResult.FromSuccess(result));
            }
            catch (NoMatchFoundException)
            {
                var message = $"\"{input}\" isn't something I recognize as a " +
                              $"{typeof(T).Name.Humanize().Transform(To.LowerCase)}.";

                return Task.FromResult(TypeReaderResult.FromError(CommandError.Unsuccessful, message));
            }
            finally
            {
                Configurator.EnumDescriptionPropertyLocator = defaultLocator;
            }
        }
    }
}
