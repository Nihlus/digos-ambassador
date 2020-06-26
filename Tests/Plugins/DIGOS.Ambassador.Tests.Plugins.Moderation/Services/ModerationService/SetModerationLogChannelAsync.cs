//
//  SetModerationLogChannelAsync.cs
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

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

using System.Threading.Tasks;
using DIGOS.Ambassador.Tests.Plugins.Moderation.Bases;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Moderation.Services.ModerationService
{
    public partial class ModerationServiceTests
    {
        public class SetModerationLogChannelAsync : ModerationServiceTestBase
        {
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(0);
            private readonly ITextChannel _channel = MockHelper.CreateDiscordTextChannel(0);
            private readonly ITextChannel _anotherChannel = MockHelper.CreateDiscordTextChannel(1);

            [Fact]
            public async Task ReturnsSuccessfulIfNoChannelIsSet()
            {
                var result = await this.Moderation.SetModerationLogChannelAsync(_guild, _channel);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulIfAnotherChannelIsSet()
            {
                await this.Moderation.SetModerationLogChannelAsync(_guild, _anotherChannel);

                var result = await this.Moderation.SetModerationLogChannelAsync(_guild, _channel);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulIfSameChannelIsSet()
            {
                await this.Moderation.SetModerationLogChannelAsync(_guild, _channel);

                var result = await this.Moderation.SetModerationLogChannelAsync(_guild, _channel);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ActuallySetsChannel()
            {
                await this.Moderation.SetModerationLogChannelAsync(_guild, _channel);

                var settings = (await this.Moderation.GetServerSettingsAsync(_guild)).Entity;

                Assert.Equal((long)_channel.Id, settings.ModerationLogChannel);
            }
        }
    }
}
