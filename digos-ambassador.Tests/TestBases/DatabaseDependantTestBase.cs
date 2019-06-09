﻿//
//  DatabaseDependantTestBase.cs
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
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Tests.Database;

namespace DIGOS.Ambassador.Tests.TestBases
{
    /// <summary>
    /// Serves as a test base for tests that depend on a database context.
    /// </summary>
    public abstract class DatabaseDependantTestBase : IDisposable
    {
        private readonly MockedDatabase _databaseMock;

        /// <summary>
        /// Gets the mocked database connection for this test.
        /// </summary>
        protected AmbyDatabaseContext Database { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseDependantTestBase"/> class.
        /// </summary>
        protected DatabaseDependantTestBase()
        {
            _databaseMock = new MockedDatabase();
            this.Database = _databaseMock.GetDatabaseContext();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Database?.Dispose();
            _databaseMock?.Dispose();
        }
    }
}
