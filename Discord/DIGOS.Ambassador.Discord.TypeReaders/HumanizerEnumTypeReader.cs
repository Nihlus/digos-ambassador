//
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
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using Humanizer.Configuration;
using Remora.Commands.Parsers;
using Remora.Commands.Results;
using Remora.Results;

namespace DIGOS.Ambassador.Discord.TypeReaders;

/// <summary>
/// Reads enums using Humanizer's DehumanizeTo function.
/// </summary>
/// <typeparam name="T">The enum type.</typeparam>
public class HumanizerEnumTypeReader<T> : AbstractTypeParser<T> where T : struct, Enum
{
    /// <inheritdoc />
    public override ValueTask<Result<T>> TryParseAsync(string value, CancellationToken ct = default)
    {
        value = value.Trim();

        var defaultLocator = Configurator.EnumDescriptionPropertyLocator;
        Configurator.EnumDescriptionPropertyLocator = _ => false;

        try
        {
            var result = value.DehumanizeTo<T>();
            return new ValueTask<Result<T>>(result);
        }
        catch (NoMatchFoundException)
        {
            return new ValueTask<Result<T>>(new ParsingError<T>(value));
        }
        finally
        {
            Configurator.EnumDescriptionPropertyLocator = defaultLocator;
        }
    }
}
