//
//  SetJoinMessageAsync.cs
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
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Tests.Utility;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Core
{
    public static partial class ServerServiceTests
    {
        public class SetJoinMessageAsync : ServerServiceTestBase
        {
            private Server _server;

            public override async Task InitializeAsync()
            {
                var serverMock = MockHelper.CreateDiscordGuild(0);
                _server = (await this.Servers.GetOrRegisterServerAsync(serverMock)).Entity;
            }

            [Fact]
            public async Task ReturnsErrorIfNewJoinMessageIsNull()
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = await this.Servers.SetJoinMessageAsync(_server, null);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsErrorIfNewJoinMessageIsEmpty()
            {
                var result = await this.Servers.SetJoinMessageAsync(_server, string.Empty);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsErrorIfNewJoinMessageIsWhitespace()
            {
                var result = await this.Servers.SetJoinMessageAsync(_server, "     ");

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsErrorIfNewJoinMessageIsSameAsOldJoinMessage()
            {
                const string old = "oogabooga";
                await this.Servers.SetJoinMessageAsync(_server, old);

                var result = await this.Servers.SetJoinMessageAsync(_server, old);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsErrorIfNewJoinMessageIsTooLong()
            {
                var newJoinMessage = new string('a', 1201);
                var result = await this.Servers.SetJoinMessageAsync(_server, newJoinMessage);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task CanSetJoinMessage()
            {
                var newJoinMessage = "oogabooga";
                var result = await this.Servers.SetJoinMessageAsync(_server, newJoinMessage);

                Assert.True(result.IsSuccess);
                Assert.Equal(newJoinMessage, _server.JoinMessage);
            }
        }
    }
}
