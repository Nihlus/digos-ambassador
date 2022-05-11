//
//  SetWarningReasonAsync.cs
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
#pragma warning disable CS8625

using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using DIGOS.Ambassador.Tests.Plugins.Moderation.Bases;
using Remora.Rest.Core;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Moderation.Services.WarningService;

public partial class WarningService
{
    public class SetWarningReasonAsync : WarningServiceTestBase
    {
        private readonly UserWarning _warning = new(new Server(new Snowflake(0)), new User(new Snowflake(0)), new User(new Snowflake(1)), string.Empty);

        [Fact]
        public async Task ReturnsUnsuccessfulIfNewReasonIsEmpty()
        {
            var result = await this.Warnings.SetWarningReasonAsync(_warning, string.Empty);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsUnsuccessfulIfNewReasonIsTooLong()
        {
            var result = await this.Warnings.SetWarningReasonAsync(_warning, new string('a', 1025));

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsUnsuccessfulIfNewReasonIsIdentical()
        {
            await this.Warnings.SetWarningReasonAsync(_warning, "Dummy thicc");
            var result = await this.Warnings.SetWarningReasonAsync(_warning, "Dummy thicc");

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsSuccessfulIfNewReasonIsWellFormed()
        {
            await this.Warnings.SetWarningReasonAsync(_warning, "Dummy thicc");
            var result = await this.Warnings.SetWarningReasonAsync(_warning, "Not dummy thicc");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ActuallySetsReason()
        {
            await this.Warnings.SetWarningReasonAsync(_warning, "Dummy thicc");

            Assert.Equal("Dummy thicc", _warning.Reason);
        }

        [Fact]
        public async Task SetterUpdatesTimestamp()
        {
            var before = _warning.UpdatedAt;

            await this.Warnings.SetWarningReasonAsync(_warning, "Dummy thicc");

            var after = _warning.UpdatedAt;

            Assert.True(before < after);
        }
    }
}
