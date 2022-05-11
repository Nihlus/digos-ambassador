//
//  IsServerKnownAsync.cs
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

using System.Threading.Tasks;
using Remora.Rest.Core;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Core;

public static partial class ServerServiceTests
{
    public class IsServerKnownAsync : ServerServiceTestBase
    {
        private readonly Snowflake _discordGuild;

        public IsServerKnownAsync()
        {
            _discordGuild = new Snowflake(0);
        }

        [Fact]
        public async Task ReturnsFalseIfServerHasNotBeenRegistered()
        {
            var result = await this.Servers.IsServerKnownAsync(_discordGuild);

            Assert.False(result);
        }

        [Fact]
        public async Task ReturnsTrueIfServerHasBeenRegistered()
        {
            await this.Servers.AddServerAsync(_discordGuild);

            var result = await this.Servers.IsServerKnownAsync(_discordGuild);

            Assert.True(result);
        }
    }
}
