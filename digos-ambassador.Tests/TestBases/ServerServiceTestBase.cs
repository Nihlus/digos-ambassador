//
//  ServerServiceTestBase.cs
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
using DIGOS.Ambassador.Services.Servers;
using Xunit;

namespace DIGOS.Ambassador.Tests.TestBases
{
    /// <summary>
    /// Serves as a test base for server service tests.
    /// </summary>
    public class ServerServiceTestBase : DatabaseDependantTestBase, IAsyncLifetime
    {
        /// <summary>
        /// Gets the server service object.
        /// </summary>
        protected ServerService Servers { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerServiceTestBase"/> class.
        /// </summary>
        protected ServerServiceTestBase()
        {
            this.Servers = new ServerService();
        }

        /// <inheritdoc/>
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
