//
//  SchemaAwareDbContext.cs
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
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DIGOS.Ambassador.Database.Abstractions
{
    /// <summary>
    /// Acts as a base class for EF Core database contexts that take the schema of the model into account.
    /// </summary>
    public abstract class SchemaAwareDbContext : DbContext
    {
        private readonly string _schema;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaAwareDbContext"/> class.
        /// </summary>
        /// <param name="schema">The schema.</param>
        protected SchemaAwareDbContext(string schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void OnConfiguring([NotNull] DbContextOptionsBuilder optionsBuilder)
        {
            var passfilePath = Path.Combine("Content", "database.credentials");
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
                    $"Password={passfileContents[4]}",
                    b => b.MigrationsHistoryTable(HistoryRepository.DefaultTableName + _schema)
                );

            optionsBuilder.ReplaceService<IMigrationsModelDiffer, SchemaAwareMigrationsModelDiffer>();
        }

        /// <inheritdoc />
        protected override void OnModelCreating([NotNull] ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema(_schema);
        }
    }
}
