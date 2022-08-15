//
//  SnowflakeConverter.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Remora.Rest.Core;

namespace DIGOS.Ambassador.Core.Database.Converters;

/// <summary>
/// Converts <see cref="Snowflake"/> instances to and from a database provider representation.
/// </summary>
[PublicAPI]
public class SnowflakeConverter : ValueConverter<Snowflake, long>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnowflakeConverter"/> class.
    /// </summary>
    public SnowflakeConverter()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SnowflakeConverter"/> class.
    /// </summary>
    /// <param name="mappingHints">The mapping hints.</param>
    public SnowflakeConverter
    (
        ConverterMappingHints? mappingHints = null
    )
        : base
        (
            v => (long)v.Value,
            v => new Snowflake((ulong)v, 1420070400000), // the discord epoch
            mappingHints
        )
    {
    }
}
