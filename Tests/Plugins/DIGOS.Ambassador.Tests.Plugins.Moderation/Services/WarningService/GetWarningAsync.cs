//
//  GetWarningAsync.cs
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

namespace DIGOS.Ambassador.Tests.Plugins.Moderation.Services.WarningService
{
    public partial class WarningService
    {
        public class GetWarningAsync : WarningServiceTestBase
        {
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(0);
            private readonly IGuild _otherGuild = MockHelper.CreateDiscordGuild(1);
            private readonly IGuildUser _guildUser = MockHelper.CreateDiscordEntity<IGuildUser>
            (
                0,
                m => m.Setup(gu => gu.Guild.Id).Returns(0)
            );

            private readonly IUser _author = MockHelper.CreateDiscordUser(1);

            [Fact]
            public async Task ReturnsSuccessfulIfWarningExists()
            {
                var warning = (await this.Warnings.CreateWarningAsync(_author, _guildUser, "Dummy thicc")).Entity;

                var result = await this.Warnings.GetWarningAsync(_guild, warning.ID);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulIfNoWarningExists()
            {
                var result = await this.Warnings.GetWarningAsync(_guild, 1);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulIfWarningExistsButServerIsWrong()
            {
                var warning = (await this.Warnings.CreateWarningAsync(_author, _guildUser, "Dummy thicc")).Entity;

                var result = await this.Warnings.GetWarningAsync(_otherGuild, warning.ID);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ActuallyReturnsWarning()
            {
                var warning = (await this.Warnings.CreateWarningAsync(_author, _guildUser, "Dummy thicc")).Entity;

                var result = await this.Warnings.GetWarningAsync(_guild, warning.ID);

                Assert.Same(warning, result.Entity);
            }
        }
    }
}
