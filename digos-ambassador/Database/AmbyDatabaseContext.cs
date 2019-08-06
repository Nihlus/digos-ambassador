//
//  AmbyDatabaseContext.cs
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

using System.IO;
using DIGOS.Ambassador.Core.Services;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Database
{
    /// <summary>
    /// Database context for global information.
    /// </summary>
    public class AmbyDatabaseContext : DbContext
    {/// <summary>
        /// Initializes a new instance of the <see cref="AmbyDatabaseContext"/> class.
        /// </summary>
        /// <param name="options">The context options.</param>
        public AmbyDatabaseContext([NotNull] DbContextOptions<AmbyDatabaseContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Configures the given options builder to match the settings required for the <see cref="AmbyDatabaseContext"/>.
        /// </summary>
        /// <param name="optionsBuilder">The builder to configure.</param>
        /// <returns>The builder, configured.</returns>
        [NotNull]
        public static DbContextOptionsBuilder ConfigureOptions([NotNull] DbContextOptionsBuilder optionsBuilder)
        {
            var contentService = new ContentService();
            var passfilePath = contentService.DatabaseCredentialsPath;
            if (!File.Exists(passfilePath))
            {
                throw new FileNotFoundException("Could not find PostgreSQL credentials.", passfilePath);
            }

            var passfileContents = File.ReadAllText(passfilePath).Split(':');
            if (passfileContents.Length != 5)
            {
                throw new InvalidDataException("The credential file was of an invalid format.");
            }

            optionsBuilder
                .UseLazyLoadingProxies()
                .UseNpgsql
                (
                    $"Server={passfileContents[0]};" +
                    $"Port={ushort.Parse(passfileContents[1])};" +
                    $"Database={passfileContents[2]};" +
                    $"Username={passfileContents[3]};" +
                    $"Password={passfileContents[4]}"
                );

            return optionsBuilder;
        }
    }
}
