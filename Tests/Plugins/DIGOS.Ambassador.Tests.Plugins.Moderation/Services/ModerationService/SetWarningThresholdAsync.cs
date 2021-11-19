//
//  SetWarningThresholdAsync.cs
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

namespace DIGOS.Ambassador.Tests.Plugins.Moderation.Services.ModerationService;

public partial class ModerationServiceTests
{
    public class SetWarningThresholdAsync : ModerationServiceTestBase
    {
        private readonly Snowflake _guild = new(0);

        [Fact]
        public async Task ReturnsSuccessfulIfThresholdIsDifferent()
        {
            var result = await this.Moderation.SetWarningThresholdAsync(_guild, 1);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsUnsuccessfulIfThresholdIsSame()
        {
            await this.Moderation.SetWarningThresholdAsync(_guild, 1);

            var result = await this.Moderation.SetWarningThresholdAsync(_guild, 1);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ActuallySetsThreshold()
        {
            await this.Moderation.SetWarningThresholdAsync(_guild, 1);

            var settings = (await this.Moderation.GetServerSettingsAsync(_guild)).Entity;

            Assert.Equal(1, settings.WarningThreshold);
        }
    }
}
