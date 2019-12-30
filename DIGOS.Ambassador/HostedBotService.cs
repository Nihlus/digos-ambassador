//
//  HostedBotService.cs
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DIGOS.Ambassador
{
    /// <summary>
    /// Acts as a base class for hosted bots.
    /// </summary>
    /// <typeparam name="TBotService">The implementing bot service.</typeparam>
    public abstract class HostedBotService<TBotService> : IHostedService
        where TBotService : HostedBotService<TBotService>
    {
        /// <summary>
        /// Gets the available services.
        /// </summary>
        protected IServiceProvider Services { get; }

        /// <summary>
        /// Gets the application lifetime.
        /// </summary>
        protected IHostApplicationLifetime Lifetime { get; }

        /// <summary>
        /// Gets the logging instance for this service.
        /// </summary>
        protected ILogger<TBotService> Log { get; }

        /// <summary>
        /// Gets the host environment for this service.
        /// </summary>
        protected IHostEnvironment HostEnvironment { get; }

        /// <summary>
        /// Gets the host configuration for this service.
        /// </summary>
        protected IConfiguration HostConfiguration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HostedBotService{TBotService}"/> class.
        /// </summary>
        /// <param name="hostConfiguration">The host configuration.</param>
        /// <param name="hostEnvironment">The host environment.</param>
        /// <param name="log">The logging instance.</param>
        /// <param name="applicationLifetime">The application lifetime.</param>
        /// <param name="services">The available services.</param>
        protected HostedBotService
        (
            IConfiguration hostConfiguration,
            IHostEnvironment hostEnvironment,
            ILogger<TBotService> log,
            IHostApplicationLifetime applicationLifetime,
            IServiceProvider services
        )
        {
            this.HostConfiguration = hostConfiguration;
            this.Log = log;
            this.Lifetime = applicationLifetime;
            this.Services = services;
            this.HostEnvironment = hostEnvironment;
        }

        /// <inheritdoc />
        public abstract Task StartAsync(CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract Task StopAsync(CancellationToken cancellationToken);
    }
}
