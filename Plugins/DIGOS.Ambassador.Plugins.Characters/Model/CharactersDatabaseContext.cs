//
//  CharactersDatabaseContext.cs
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

using DIGOS.Ambassador.Core.Database.Context;
using DIGOS.Ambassador.Plugins.Characters.Model.Data;
using Microsoft.EntityFrameworkCore;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Characters.Model;

/// <summary>
/// Represents the database model of the dossier plugin.
/// </summary>
public class CharactersDatabaseContext : AmbassadorDbContext
{
    private const string _schemaName = "CharacterModule";

    /// <summary>
    /// Gets the table where characters are stored.
    /// </summary>
    public DbSet<Character> Characters => Set<Character>();

    /// <summary>
    /// Gets the table where images are stored.
    /// </summary>
    public DbSet<Image> Images => Set<Image>();

    /// <summary>
    /// Gets the table where character roles are stored.
    /// </summary>
    public DbSet<CharacterRole> CharacterRoles => Set<CharacterRole>();

    /// <summary>
    /// Initializes a new instance of the <see cref="CharactersDatabaseContext"/> class.
    /// </summary>
    /// <param name="contextOptions">The context options.</param>
    public CharactersDatabaseContext(DbContextOptions<CharactersDatabaseContext> contextOptions)
        : base(_schemaName, contextOptions)
    {
    }
}
