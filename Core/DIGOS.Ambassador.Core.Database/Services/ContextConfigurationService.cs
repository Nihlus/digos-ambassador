//
//  ContextConfigurationService.cs
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
using DIGOS.Ambassador.Core.Database.Credentials;
using DIGOS.Ambassador.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Remora.EntityFrameworkCore.Modular;
using Remora.EntityFrameworkCore.Modular.Services;

namespace DIGOS.Ambassador.Core.Database.Services
{
    /// <summary>
    /// Serves functionality for schema-aware database contexts.
    /// </summary>
    public class ContextConfigurationService
    {
        private readonly ContentService _content;
        private readonly SchemaAwareDbContextService _schemaAwareDbContextService;
        private readonly Dictionary<Type, string> _knownSchemas;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextConfigurationService"/> class.
        /// </summary>
        /// <param name="content">The content service.</param>
        /// <param name="schemaAwareDbContextService">The schema-aware database context service.</param>
        public ContextConfigurationService
        (
            ContentService content,
            SchemaAwareDbContextService schemaAwareDbContextService
        )
        {
            _content = content;
            _schemaAwareDbContextService = schemaAwareDbContextService;
            _knownSchemas = new Dictionary<Type, string>();
        }

        private void EnsureSchemaIsCached<TContext>() where TContext : SchemaAwareDbContext
        {
            if (_knownSchemas.ContainsKey(typeof(TContext)))
            {
                return;
            }

            var dummyOptions = new DbContextOptionsBuilder<TContext>().Options;
            if (!(Activator.CreateInstance(typeof(TContext), dummyOptions) is TContext dummyContext))
            {
                return;
            }

            var schema = dummyContext.Schema;
            _knownSchemas.Add(typeof(TContext), schema);
        }

        /// <summary>
        /// Configures the options of a schema-aware database context.
        /// </summary>
        /// <param name="optionsBuilder">The unconfigured options builder.</param>
        /// <typeparam name="TContext">The context type.</typeparam>
        public void ConfigureSchemaAwareContext<TContext>(DbContextOptionsBuilder optionsBuilder)
            where TContext : SchemaAwareDbContext
        {
            EnsureSchemaIsCached<TContext>();
            var schema = _knownSchemas[typeof(TContext)];

            var getCredentialStream = _content.GetDatabaseCredentialStream();
            if (!getCredentialStream.IsSuccess)
            {
                throw new InvalidOperationException("Failed to get the database credential stream.");
            }

            DatabaseCredentials? credentials;
            using (var credentialStream = new StreamReader(getCredentialStream.Entity))
            {
                var content = credentialStream.ReadToEnd();
                if (!DatabaseCredentials.TryParse(content, out credentials))
                {
                    throw new InvalidOperationException("Failed to parse the database credentials.");
                }
            }

            optionsBuilder
                .UseLazyLoadingProxies()
                .UseNpgsql
                (
                    credentials.GetConnectionString(),
                    b => b.MigrationsHistoryTable(HistoryRepository.DefaultTableName + schema)
                );

            _schemaAwareDbContextService.ConfigureSchemaAwareContext(optionsBuilder);
        }
    }
}
