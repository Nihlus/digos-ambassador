//
//  ContextConfigurationService.cs
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
using System.Collections.Generic;
using DIGOS.Ambassador.Core.Database.Context;
using DIGOS.Ambassador.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;

namespace DIGOS.Ambassador.Core.Database.Services;

/// <summary>
/// Serves functionality for schema-aware database contexts.
/// </summary>
public class ContextConfigurationService
{
    private readonly ContentService _content;
    private readonly IConfiguration _configuration;

    private readonly Dictionary<Type, string> _knownSchemas;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextConfigurationService"/> class.
    /// </summary>
    /// <param name="content">The content service.</param>
    /// <param name="configuration">The application configuration.</param>
    public ContextConfigurationService(ContentService content, IConfiguration configuration)
    {
        _content = content;
        _configuration = configuration;

        _knownSchemas = new Dictionary<Type, string>();
    }

    private void EnsureSchemaIsCached<TContext>() where TContext : AmbassadorDbContext
    {
        if (_knownSchemas.ContainsKey(typeof(TContext)))
        {
            return;
        }

        var dummyOptions = new DbContextOptionsBuilder<TContext>().Options;
        if (Activator.CreateInstance(typeof(TContext), dummyOptions) is not TContext dummyContext)
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
        where TContext : AmbassadorDbContext
    {
        EnsureSchemaIsCached<TContext>();
        var schema = _knownSchemas[typeof(TContext)];

        optionsBuilder
            .UseLazyLoadingProxies()
            .UseNpgsql
            (
                _configuration.GetConnectionString("Amby"),
                b => b.MigrationsHistoryTable(HistoryRepository.DefaultTableName + schema)
            )
            .UseSnakeCaseNamingConvention();
    }
}
