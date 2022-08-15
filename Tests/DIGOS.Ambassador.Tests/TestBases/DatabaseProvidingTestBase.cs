//
//  DatabaseProvidingTestBase.cs
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
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DIGOS.Ambassador.Tests.TestBases;

/// <summary>
/// Serves as a test base for tests that depend on a database context.
/// </summary>
[PublicAPI]
public abstract class DatabaseProvidingTestBase : ServiceProvidingTestBase, IDisposable
{
    private SqliteConnection _connection = new("Filename=:memory:");

    /// <summary>
    /// Configures the given options builder to use the underlying test database.
    /// </summary>
    /// <param name="optionsBuilder">The builder to configure.</param>
    /// <param name="schema">The schema of the context to configure.</param>
    protected void ConfigureOptions(DbContextOptionsBuilder optionsBuilder, string schema)
    {
        _connection.Open();

        optionsBuilder
            .UseLazyLoadingProxies()
            .UseSqlite
            (
                _connection,
                b => b.MigrationsHistoryTable(HistoryRepository.DefaultTableName + schema)
            );
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _connection.Dispose();
    }
}
