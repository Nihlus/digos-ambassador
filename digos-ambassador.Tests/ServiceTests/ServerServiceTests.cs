//
//  ServerServiceTests.cs
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
using DIGOS.Ambassador.Tests.TestBases;
using DIGOS.Ambassador.Tests.Utility;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591

namespace DIGOS.Ambassador.Tests.ServiceTests
{
    public class ServerServiceTests
    {
        public class GetDescription : ServerServiceTestBase
        {
            private Server _server;

            public override async Task InitializeAsync()
            {
                var serverMock = MockHelper.CreateDiscordGuild(0);
                _server = await this.Servers.GetOrRegisterServerAsync(serverMock);
            }

            [Fact]
            public void ReturnsErrorIfDescriptionIsNull()
            {
                var result = this.Servers.GetDescription(_server);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public void ReturnsErrorIfDescriptionIsEmpty()
            {
                _server.Description = string.Empty;

                var result = this.Servers.GetDescription(_server);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public void ReturnsErrorIfDescriptionIsWhitespace()
            {
                _server.Description = "      ";

                var result = this.Servers.GetDescription(_server);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public void CanGetDescription()
            {
                const string expected = "oogabooga";
                _server.Description = expected;

                var result = this.Servers.GetDescription(_server);

                Assert.True(result.IsSuccess);
                Assert.Equal(expected, result.Entity);
            }
        }

        public class SetDescriptionAsync : ServerServiceTestBase
        {
            private Server _server;

            public override async Task InitializeAsync()
            {
                var serverMock = MockHelper.CreateDiscordGuild(0);
                _server = await this.Servers.GetOrRegisterServerAsync(serverMock);
            }

            [Fact]
            public async Task ReturnsErrorIfNewDescriptionIsNull()
            {
                var result = await this.Servers.SetDescriptionAsync(_server, null);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsErrorIfNewDescriptionIsEmpty()
            {
                var result = await this.Servers.SetDescriptionAsync(_server, string.Empty);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsErrorIfNewDescriptionIsWhitespace()
            {
                var result = await this.Servers.SetDescriptionAsync(_server, "     ");

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsErrorIfNewDescriptionIsSameAsOldDescription()
            {
                var old = "oogabooga";
                _server.Description = old;

                var result = await this.Servers.SetDescriptionAsync(_server, old);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsErrorIfNewDescriptionIsTooLong()
            {
                var newDescription = new string('a', 801);
                var result = await this.Servers.SetDescriptionAsync(_server, newDescription);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task CanSetDescription()
            {
                var newDescription = "oogabooga";
                var result = await this.Servers.SetDescriptionAsync(_server, newDescription);

                Assert.True(result.IsSuccess);
                Assert.Equal(newDescription, _server.Description);
            }
        }

        public class GetJoinMessage : ServerServiceTestBase
        {
            private Server _server;

            public override async Task InitializeAsync()
            {
                var serverMock = MockHelper.CreateDiscordGuild(0);
                _server = await this.Servers.GetOrRegisterServerAsync(serverMock);
            }

            [Fact]
            public void ReturnsErrorIfJoinMessageIsNull()
            {
                var result = this.Servers.GetJoinMessage(_server);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public void ReturnsErrorIfJoinMessageIsEmpty()
            {
                _server.JoinMessage = string.Empty;

                var result = this.Servers.GetJoinMessage(_server);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public void ReturnsErrorIfJoinMessageIsWhitespace()
            {
                _server.JoinMessage = "      ";

                var result = this.Servers.GetJoinMessage(_server);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public void CanGetJoinMessage()
            {
                const string expected = "oogabooga";
                _server.JoinMessage = expected;

                var result = this.Servers.GetJoinMessage(_server);

                Assert.True(result.IsSuccess);
                Assert.Equal(expected, result.Entity);
            }
        }

        public class SetJoinMessageAsync : ServerServiceTestBase
        {
            private Server _server;

            public override async Task InitializeAsync()
            {
                var serverMock = MockHelper.CreateDiscordGuild(0);
                _server = await this.Servers.GetOrRegisterServerAsync(serverMock);
            }

            [Fact]
            public async Task ReturnsErrorIfNewJoinMessageIsNull()
            {
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
                var old = "oogabooga";
                _server.JoinMessage = old;

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

        public class SetIsNSFWAsync : ServerServiceTestBase
        {
            private Server _server;

            public override async Task InitializeAsync()
            {
                var serverMock = MockHelper.CreateDiscordGuild(0);
                _server = await this.Servers.GetOrRegisterServerAsync(serverMock);
            }

            [Fact]
            public async Task ReturnsErrorIfValueIsSameAsCurrent()
            {
                var result = await this.Servers.SetIsNSFWAsync(_server, true);
                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task CanSetValue()
            {
                var result = await this.Servers.SetIsNSFWAsync(_server, false);

                Assert.True(result.IsSuccess);
                Assert.False(_server.IsNSFW);
            }
        }

        public class SetSendJoinMessageAsync : ServerServiceTestBase
        {
            private Server _server;

            public override async Task InitializeAsync()
            {
                var serverMock = MockHelper.CreateDiscordGuild(0);
                _server = await this.Servers.GetOrRegisterServerAsync(serverMock);
            }

            [Fact]
            public async Task ReturnsErrorIfValueIsSameAsCurrent()
            {
                var result = await this.Servers.SetSendJoinMessageAsync(_server, false);
                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task CanSetValue()
            {
                var result = await this.Servers.SetSendJoinMessageAsync(_server, true);

                Assert.True(result.IsSuccess);
                Assert.True(_server.SendJoinMessage);
            }
        }
    }
}
