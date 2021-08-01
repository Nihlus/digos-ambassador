//
//  PrivacyServiceTestBase.cs
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
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Tests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Commands.Feedback.Themes;
using Remora.Discord.Commands.Services;
using Xunit;

#pragma warning disable SA1648

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Core
{
    /// <summary>
    /// Serves as a test base for privacy service tests.
    /// </summary>
    public abstract class PrivacyServiceTestBase : DatabaseProvidingTestBase, IAsyncLifetime
    {
        /// <summary>
        /// Gets the privacy service object.
        /// </summary>
        protected PrivacyService Privacy { get; private set; } = null!;

        /// <summary>
        /// Gets the database.
        /// </summary>
        protected CoreDatabaseContext Database { get; private set; } = null!;

        /// <inheritdoc />
        protected override void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddDbContext<CoreDatabaseContext>(ConfigureOptions<CoreDatabaseContext>);

            var channelAPIMock = new Mock<IDiscordRestChannelAPI>();
            var userAPIMock = new Mock<IDiscordRestUserAPI>();
            var webhookAPIMock = new Mock<IDiscordRestWebhookAPI>();

            serviceCollection
                .AddSingleton(FileSystemFactory.CreateContentFileSystem())
                .AddScoped<ContentService>()
                .AddScoped<FeedbackService>()
                .AddScoped<PrivacyService>()
                .AddScoped<ContextInjectionService>()
                .AddSingleton(FeedbackTheme.DiscordDark)
                .AddSingleton(channelAPIMock.Object)
                .AddSingleton(userAPIMock.Object)
                .AddSingleton(webhookAPIMock.Object)
                .AddLogging(c => c.AddProvider(NullLoggerProvider.Instance));
        }

        /// <inheritdoc />
        protected override void ConfigureServices(IServiceProvider serviceProvider)
        {
            this.Database = serviceProvider.GetRequiredService<CoreDatabaseContext>();
            this.Database.Database.EnsureCreated();

            this.Privacy = serviceProvider.GetRequiredService<PrivacyService>();
        }

        /// <inheritdoc />
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
