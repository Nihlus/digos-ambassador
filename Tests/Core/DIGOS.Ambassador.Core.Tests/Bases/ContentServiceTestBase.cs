//
//  ContentServiceTestBase.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Tests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace DIGOS.Ambassador.Core.Tests.Bases
{
    /// <summary>
    /// Serves as a base class for content service tests.
    /// </summary>
    public abstract class ContentServiceTestBase : ServiceProvidingTestBase, IAsyncLifetime
    {
        /// <summary>
        /// Gets the file system implementation that's in use.
        /// </summary>
        protected IFileSystem FileSystem { get; private set; } = null!;

        /// <summary>
        /// Gets the content service.
        /// </summary>
        protected ContentService ContentService { get; private set; } = null!;

        /// <summary>
        /// Configures the file system.
        /// </summary>
        /// <param name="fileSystem">The file system to configure.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task ConfigureFileSystemAsync(IFileSystem fileSystem)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected sealed override void RegisterServices(IServiceCollection serviceCollection)
        {
            this.FileSystem = new MemoryFileSystem();

            serviceCollection
                .AddSingleton(this.FileSystem)
                .AddSingleton<ContentService>();
        }

        /// <inheritdoc />
        protected sealed override void ConfigureServices(IServiceProvider serviceProvider)
        {
            this.ContentService = this.Services.GetRequiredService<ContentService>();
        }

        /// <inheritdoc />
        public virtual async Task InitializeAsync()
        {
            await ConfigureFileSystemAsync(this.FileSystem);
        }

        /// <inheritdoc />
        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
