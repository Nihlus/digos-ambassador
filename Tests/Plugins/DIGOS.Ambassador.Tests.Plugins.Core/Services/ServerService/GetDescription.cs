//
//  GetDescription.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using Remora.Rest.Core;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Core;

public static partial class ServerServiceTests
{
    public class GetDescription : ServerServiceTestBase
    {
        private Server _server = null!;

        public override async Task InitializeAsync()
        {
            var serverMock = new Snowflake(0);
            _server = (await this.Servers.GetOrRegisterServerAsync(serverMock)).Entity;
        }

        [Fact]
        public void ReturnsErrorIfDescriptionIsNull()
        {
            var result = Ambassador.Plugins.Core.Services.Servers.ServerService.GetDescription(_server);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void ReturnsErrorIfDescriptionIsEmpty()
        {
            _server.Description = string.Empty;

            var result = Ambassador.Plugins.Core.Services.Servers.ServerService.GetDescription(_server);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void ReturnsErrorIfDescriptionIsWhitespace()
        {
            _server.Description = "      ";

            var result = Ambassador.Plugins.Core.Services.Servers.ServerService.GetDescription(_server);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void CanGetDescription()
        {
            const string expected = "oogabooga";
            _server.Description = expected;

            var result = Ambassador.Plugins.Core.Services.Servers.ServerService.GetDescription(_server);

            Assert.True(result.IsSuccess);
            Assert.Equal(expected, result.Entity);
        }
    }
}
