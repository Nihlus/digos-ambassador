//
//  AmbassadorBotService.cs
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
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Behaviours.Services;
using Remora.Discord.Hosted;
using Remora.Plugins.Services;
using Remora.Results;

namespace DIGOS.Ambassador
{
    /// <summary>
    /// Main service for the bot itself. Handles high-level functionality.
    /// </summary>
    public class AmbassadorBotService : HostedDiscordBotService<AmbassadorBotService>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AmbassadorBotService"/> class.
        /// </summary>
        /// <param name="discordClient">The Discord client.</param>
        /// <param name="commandService">The command service.</param>
        /// <param name="behaviourService">The behaviour service.</param>
        /// <param name="pluginService">The plugin service.</param>
        /// <param name="hostConfiguration">The host configuration.</param>
        /// <param name="hostEnvironment">The host environment.</param>
        /// <param name="log">The logging instance.</param>
        /// <param name="applicationLifetime">The application lifetime.</param>
        /// <param name="services">The available services.</param>
        public AmbassadorBotService
        (
            DiscordSocketClient discordClient,
            CommandService commandService,
            BehaviourService behaviourService,
            PluginService pluginService,
            IConfiguration hostConfiguration,
            IHostEnvironment hostEnvironment,
            ILogger<AmbassadorBotService> log,
            IHostApplicationLifetime applicationLifetime,
            IServiceProvider services
        )
            : base
            (
                discordClient,
                pluginService,
                behaviourService,
                hostConfiguration,
                hostEnvironment,
                log,
                applicationLifetime,
                services
            )
        {
            commandService.Log += OnDiscordLogEvent;
        }

        /// <inheritdoc/>
        protected override async Task<RetrieveEntityResult<string>> GetTokenAsync()
        {
            var contentService = this.Services.GetRequiredService<ContentService>();

            var getTokenResult = await contentService.GetBotTokenAsync();
            if (!getTokenResult.IsSuccess)
            {
                return RetrieveEntityResult<string>.FromError(getTokenResult);
            }

            var token = getTokenResult.Entity.Trim();
            return token;
        }
    }
}
