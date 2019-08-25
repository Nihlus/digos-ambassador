//
//  IsEntityNameValid.cs
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
using DIGOS.Ambassador.Discord;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Plugins.Characters.CommandModules;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Plugins.Characters.TypeReaders;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Core
{
    public partial class OwnedEntityServiceTests
    {
        public class IsEntityNameValid : OwnedEntityServiceTestBase, IAsyncLifetime
        {
            private ModuleInfo _commandModule;

            protected override void RegisterServices(IServiceCollection serviceCollection)
            {
                base.RegisterServices(serviceCollection);

                serviceCollection
                    .AddDbContext<CharactersDatabaseContext>(ConfigureOptions<CharactersDatabaseContext>);

                serviceCollection
                    .AddScoped<ServerService>()
                    .AddSingleton<PronounService>()
                    .AddScoped<CharacterService>()
                    .AddScoped<ContentService>()
                    .AddScoped<CommandService>()
                    .AddScoped<DiscordService>()
                    .AddScoped<UserFeedbackService>()
                    .AddScoped<InteractivityService>()
                    .AddScoped<BaseSocketClient>(p => new DiscordSocketClient())
                    .AddScoped<Random>();
            }

            public async Task InitializeAsync()
            {
                var charactersDatabase = this.Services.GetRequiredService<CharactersDatabaseContext>();
                charactersDatabase.Database.Migrate();

                var commands = this.Services.GetRequiredService<CommandService>();

                commands.AddTypeReader<Character>(new CharacterTypeReader());
                _commandModule = await commands.AddModuleAsync<CharacterCommands>(this.Services);
            }

            [Fact]
            public void ReturnsFailureForNullNames()
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                var result = this.Entities.IsEntityNameValid(_commandModule, null);

                Assert.False(result.IsSuccess);
            }

            [Theory]
            [InlineData(':')]
            public void ReturnsFailureIfNameContainsInvalidCharacters(char invalidCharacter)
            {
                var result = this.Entities.IsEntityNameValid(_commandModule, $"Test{invalidCharacter}");

                Assert.False(result.IsSuccess);
            }

            [Theory]
            [InlineData("current")]
            public void ReturnsFailureIfNameIsAReservedName(string reservedName)
            {
                var result = this.Entities.IsEntityNameValid(_commandModule, reservedName);

                Assert.False(result.IsSuccess);
            }

            [Theory]
            [InlineData("create")]
            [InlineData("show")]
            [InlineData("character show")]
            [InlineData("create Test Testsson")]
            [InlineData("set name Amby")]
            public void ReturnsFailureIfNameContainsACommandName(string commandName)
            {
                var result = this.Entities.IsEntityNameValid(_commandModule, commandName);

                Assert.False(result.IsSuccess);
            }

            [Theory]
            [InlineData("Norm")]
            [InlineData("Tali'Zorah")]
            [InlineData("August Strindberg")]
            public void ReturnsSuccessForNormalNames(string name)
            {
                var result = this.Entities.IsEntityNameValid(_commandModule, name);

                Assert.True(result.IsSuccess);
            }

            public Task DisposeAsync()
            {
                return Task.CompletedTask;
            }
        }
    }
}
