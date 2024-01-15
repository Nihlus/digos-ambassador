//
//  ConfigurationBuilderExtensions.cs
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

using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace DIGOS.Ambassador.Core.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="IConfigurationBuilder"/> interface.
/// </summary>
[PublicAPI]
public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds various configuration paths used by the program.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <returns>The builder, with the paths.</returns>
    public static IConfigurationBuilder AddAmbassadorConfigurationFiles(this IConfigurationBuilder builder)
    {
        builder
            .AddEnvironmentVariables()
            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"), optional: true);

        var systemConfigBase = OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD()
            ? Path.Combine("/", "etc", "digos-ambassador")
            : Directory.GetCurrentDirectory();

        builder
            .AddJsonFile(Path.Combine(systemConfigBase, "appsettings.json"), optional: true);

        var systemConfigDropInDirectory = Path.Combine(systemConfigBase, "conf.d");
        if (Directory.Exists(systemConfigDropInDirectory))
        {
            var dropInFiles = Directory.EnumerateFiles(systemConfigDropInDirectory, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var dropInFile in dropInFiles.OrderBy(Path.GetFileNameWithoutExtension))
            {
                builder.AddJsonFile(dropInFile, true);
            }
        }

        return builder;
    }
}
