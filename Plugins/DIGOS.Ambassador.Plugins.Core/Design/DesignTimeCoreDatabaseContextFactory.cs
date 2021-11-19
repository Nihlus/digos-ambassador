﻿//
//  DesignTimeCoreDatabaseContextFactory.cs
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

using DIGOS.Ambassador.Core.Database.Design;
using DIGOS.Ambassador.Core.Database.Services;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Plugins.Core.Model;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

[assembly: DesignTimeServicesReference("DIGOS.Ambassador.Core.Database.Design." + nameof(AutoGeneratedDesignTimeServices) + ", DIGOS.Ambassador.Core.Database")]

namespace DIGOS.Ambassador.Plugins.Core.Design;

/// <summary>
/// Design-time factory for <see cref="CoreDatabaseContext"/> instances.
/// </summary>
[UsedImplicitly]
public class DesignTimeCoreDatabaseContextFactory : IDesignTimeDbContextFactory<CoreDatabaseContext>
{
    /// <inheritdoc />
    public CoreDatabaseContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CoreDatabaseContext>();
        new ContextConfigurationService(new ContentService(FileSystemFactory.CreateContentFileSystem()))
            .ConfigureSchemaAwareContext<CoreDatabaseContext>(optionsBuilder);

        return new CoreDatabaseContext(optionsBuilder.Options);
    }
}
