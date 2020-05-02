//
//  DatabaseFacadeExtensions.cs
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

using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DIGOS.Ambassador.Tests.Extensions
{
    /// <summary>
    /// Extension methods for the <see cref="DatabaseFacade"/> class.
    /// </summary>
    [PublicAPI]
    public static class DatabaseFacadeExtensions
    {
        /// <summary>
        /// Directly creates the schema of the database.
        /// </summary>
        /// <param name="facade">The database.</param>
        public static void Create(this DatabaseFacade facade)
        {
            facade.ExecuteSqlRaw(facade.GenerateCreateScript());
        }

        /// <summary>
        /// Directly creates the schema of the database.
        /// </summary>
        /// <param name="facade">The database.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task CreateAsync(this DatabaseFacade facade)
        {
            return facade.ExecuteSqlRawAsync(facade.GenerateCreateScript());
        }
    }
}
