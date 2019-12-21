//
//  ServiceProvidingTestBase.cs
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
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

using NotNullAttribute = JetBrains.Annotations.NotNullAttribute;

namespace DIGOS.Ambassador.Tests.TestBases
{
    /// <summary>
    /// Represents a test base that can provide services to running tests.
    /// </summary>
    public abstract class ServiceProvidingTestBase
    {
        /// <summary>
        /// Gets the available services.
        /// </summary>
        [NotNull]
        protected IServiceProvider Services { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProvidingTestBase"/> class.
        /// </summary>
        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor", Justification = "Required.")]
        protected ServiceProvidingTestBase()
        {
            var serviceCollection = new ServiceCollection();

            RegisterServices(serviceCollection);
            this.Services = serviceCollection.BuildServiceProvider();

            ConfigureServices(this.Services);
        }

        /// <summary>
        /// Registers services provided by the test base in the test base's service provider.
        /// </summary>
        /// <param name="serviceCollection">The service collection to register services in.</param>
        protected abstract void RegisterServices([NotNull] IServiceCollection serviceCollection);

        /// <summary>
        /// Configures the test base using the registered services.
        /// </summary>
        /// <param name="serviceProvider">The available services.</param>
        protected abstract void ConfigureServices([NotNull] IServiceProvider serviceProvider);
    }
}
