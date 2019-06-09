//
//  TransformationServiceTestBase.cs
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
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Users;
using Xunit;

namespace DIGOS.Ambassador.Tests.TestBases
{
    /// <summary>
    /// Serves as a test base for transformation service tests.
    /// </summary>
    public abstract class TransformationServiceTestBase : DatabaseDependantTestBase, IAsyncLifetime
    {
        /// <summary>
        /// Gets the transformation service object.
        /// </summary>
        protected TransformationService Transformations { get; }

        /// <summary>
        /// Gets the user service.
        /// </summary>
        protected UserService Users { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationServiceTestBase"/> class.
        /// </summary>
        protected TransformationServiceTestBase()
        {
            this.Transformations = new TransformationService(new ContentService(), new UserService());
            this.Users = new UserService();
        }

        /// <inheritdoc />
        public virtual async Task InitializeAsync()
        {
            await this.Transformations.UpdateTransformationDatabaseAsync(this.Database);
        }

        /// <inheritdoc />
        public virtual Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
