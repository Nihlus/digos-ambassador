//
//  GetServerSettingsAsync.cs
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
using Remora.Discord.Core;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Moderation.Services.ModerationService
{
    public partial class ModerationServiceTests
    {
        public class GetServerSettingsAsync : ModerationServiceTestBase
        {
            private readonly Snowflake _guild = new Snowflake(0);

            [Fact]
            public async Task ReturnsSuccessfulIfSettingsExist()
            {
                await this.Moderation.CreateServerSettingsAsync(_guild);

                var result = await this.Moderation.GetServerSettingsAsync(_guild);

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsCorrectSettings()
            {
                var expected = (await this.Moderation.CreateServerSettingsAsync(_guild)).Entity;

                var actual = (await this.Moderation.GetServerSettingsAsync(_guild)).Entity;

                Assert.Same(expected, actual);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulIfSettingsDoNotExist()
            {
                var result = await this.Moderation.GetServerSettingsAsync(_guild);

                Assert.False(result.IsSuccess);
            }
        }
    }
}
