//
//  SchemaAwareDbContextService.cs
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
using System.Collections.Generic;
using System.IO;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Core.Services.Content;
using DIGOS.Ambassador.Database.Abstractions.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DIGOS.Ambassador.Database.Abstractions.Services
{
    /// <summary>
    /// Serves functionality for schema-aware database contexts.
    /// </summary>
    public class SchemaAwareDbContextService
    {
        private readonly ContentService _content;
        private readonly Dictionary<Type, string> _knownSchemas;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaAwareDbContextService"/> class.
        /// </summary>
        /// <param name="content">The content service.</param>
        public SchemaAwareDbContextService(ContentService content)
        {
            _content = content;
            _knownSchemas = new Dictionary<Type, string>();
        }

        private void EnsureSchemaIsCached<TContext>() where TContext : SchemaAwareDbContext
        {
            if (_knownSchemas.ContainsKey(typeof(TContext)))
            {
                return;
            }

            var dummyOptions = new DbContextOptionsBuilder().Options;
            var dummyContext = (TContext)Activator.CreateInstance(typeof(TContext), dummyOptions);

            var schema = dummyContext.Schema;
            _knownSchemas.Add(typeof(TContext), schema);
        }

        /// <summary>
        /// Configures the options of a schema-aware database context.
        /// </summary>
        /// <param name="optionsBuilder">The unconfigured options builder.</param>
        /// <typeparam name="TContext">The context type.</typeparam>
        /// <returns>The configured options.</returns>
        public DbContextOptionsBuilder ConfigureSchemaAwareContext<TContext>(DbContextOptionsBuilder optionsBuilder)
            where TContext : SchemaAwareDbContext
        {
            EnsureSchemaIsCached<TContext>();
            var schema = _knownSchemas[typeof(TContext)];

            if (!DatabaseCredentials.TryParse(_content.DatabaseCredentialsPath, out var credentials))
            {
                throw new InvalidOperationException("Failed to get the database credentials.");
            }

            optionsBuilder
                .UseLazyLoadingProxies()
                .UseNpgsql
                (
                    credentials.GetConnectionString(),
                    b => b.MigrationsHistoryTable(HistoryRepository.DefaultTableName + schema)
                );

            optionsBuilder.ReplaceService<IMigrationsModelDiffer, SchemaAwareMigrationsModelDiffer>();

            return optionsBuilder;
        }
    }
}
