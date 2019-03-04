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

using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Tests.TestBases;
using DIGOS.Ambassador.Tests.Utility;

using Discord.Commands;

using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591

namespace DIGOS.Ambassador.Tests.ServiceTests
{
    public class ServerServiceTests
    {
        public class GetDescription : ServerServiceTestBase
        {
            private Server Server;

            public override async Task InitializeAsync()
            {
                var serverMock = MockHelper.CreateDiscordGuild(0);
                this.Server = await this.Database.GetOrRegisterServerAsync(serverMock);
            }

            [Fact]
            public void ReturnsErrorIfDescriptionIsNull()
            {
                var result = this.Servers.GetDescription(this.Server);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public void ReturnsErrorIfDescriptionIsEmpty()
            {
                this.Server.Description = string.Empty;

                var result = this.Servers.GetDescription(this.Server);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public void ReturnsErrorIfDescriptionIsWhitespace()
            {
                this.Server.Description = "      ";

                var result = this.Servers.GetDescription(this.Server);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public void CanGetDescription()
            {
                const string expected = "oogabooga";
                this.Server.Description = expected;

                var result = this.Servers.GetDescription(this.Server);

                Assert.True(result.IsSuccess);
                Assert.Equal(expected, result.Entity);
            }
        }

        public class SetDescriptionAsync : ServerServiceTestBase
        {
            private Server Server;

            public override async Task InitializeAsync()
            {
                var serverMock = MockHelper.CreateDiscordGuild(0);
                this.Server = await this.Database.GetOrRegisterServerAsync(serverMock);
            }

            [Fact]
            public async Task ReturnsErrorIfNewDescriptionIsNull()
            {
                var result = await this.Servers.SetDescriptionAsync(this.Database, this.Server, null);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task ReturnsErrorIfNewDescriptionIsEmpty()
            {
                var result = await this.Servers.SetDescriptionAsync(this.Database, this.Server, string.Empty);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task ReturnsErrorIfNewDescriptionIsWhitespace()
            {
                var result = await this.Servers.SetDescriptionAsync(this.Database, this.Server, "     ");

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task ReturnsErrorIfNewDescriptionIsSameAsOldDescription()
            {
                var old = "oogabooga";
                this.Server.Description = old;

                var result = await this.Servers.SetDescriptionAsync(this.Database, this.Server, old);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task ReturnsErrorIfNewDescriptionIsTooLong()
            {
                var newDescription = new string('a', 801);
                var result = await this.Servers.SetDescriptionAsync(this.Database, this.Server, newDescription);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task CanSetDescription()
            {
                var newDescription = "oogabooga";
                var result = await this.Servers.SetDescriptionAsync(this.Database, this.Server, newDescription);

                Assert.True(result.IsSuccess);
                Assert.Equal(newDescription, this.Server.Description);
            }
        }

        public class GetJoinMessage : ServerServiceTestBase
        {
            private Server Server;

            public override async Task InitializeAsync()
            {
                var serverMock = MockHelper.CreateDiscordGuild(0);
                this.Server = await this.Database.GetOrRegisterServerAsync(serverMock);
            }

            [Fact]
            public void ReturnsErrorIfJoinMessageIsNull()
            {
                var result = this.Servers.GetJoinMessage(this.Server);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public void ReturnsErrorIfJoinMessageIsEmpty()
            {
                this.Server.JoinMessage = string.Empty;

                var result = this.Servers.GetJoinMessage(this.Server);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public void ReturnsErrorIfJoinMessageIsWhitespace()
            {
                this.Server.JoinMessage = "      ";

                var result = this.Servers.GetJoinMessage(this.Server);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.ObjectNotFound, result.Error);
            }

            [Fact]
            public void CanGetJoinMessage()
            {
                const string expected = "oogabooga";
                this.Server.JoinMessage = expected;

                var result = this.Servers.GetJoinMessage(this.Server);

                Assert.True(result.IsSuccess);
                Assert.Equal(expected, result.Entity);
            }
        }

        public class SetJoinMessageAsync : ServerServiceTestBase
        {
            private Server Server;

            public override async Task InitializeAsync()
            {
                var serverMock = MockHelper.CreateDiscordGuild(0);
                this.Server = await this.Database.GetOrRegisterServerAsync(serverMock);
            }

            [Fact]
            public async Task ReturnsErrorIfNewJoinMessageIsNull()
            {
                var result = await this.Servers.SetJoinMessageAsync(this.Database, this.Server, null);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task ReturnsErrorIfNewJoinMessageIsEmpty()
            {
                var result = await this.Servers.SetJoinMessageAsync(this.Database, this.Server, string.Empty);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task ReturnsErrorIfNewJoinMessageIsWhitespace()
            {
                var result = await this.Servers.SetJoinMessageAsync(this.Database, this.Server, "     ");

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task ReturnsErrorIfNewJoinMessageIsSameAsOldJoinMessage()
            {
                var old = "oogabooga";
                this.Server.JoinMessage = old;

                var result = await this.Servers.SetJoinMessageAsync(this.Database, this.Server, old);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task ReturnsErrorIfNewJoinMessageIsTooLong()
            {
                var newJoinMessage = new string('a', 1201);
                var result = await this.Servers.SetJoinMessageAsync(this.Database, this.Server, newJoinMessage);

                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            }

            [Fact]
            public async Task CanSetJoinMessage()
            {
                var newJoinMessage = "oogabooga";
                var result = await this.Servers.SetJoinMessageAsync(this.Database, this.Server, newJoinMessage);

                Assert.True(result.IsSuccess);
                Assert.Equal(newJoinMessage, this.Server.JoinMessage);
            }
        }

        public class SetIsNSFWAsync : ServerServiceTestBase
        {
            private Server Server;

            public override async Task InitializeAsync()
            {
                var serverMock = MockHelper.CreateDiscordGuild(0);
                this.Server = await this.Database.GetOrRegisterServerAsync(serverMock);
            }

            [Fact]
            public async Task ReturnsErrorIfValueIsSameAsCurrent()
            {
                var result = await this.Servers.SetIsNSFWAsync(this.Database, this.Server, true);
                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task CanSetValue()
            {
                var result = await this.Servers.SetIsNSFWAsync(this.Database, this.Server, false);

                Assert.True(result.IsSuccess);
                Assert.False(this.Server.IsNSFW);
            }
        }

        public class SetSendJoinMessageAsync : ServerServiceTestBase
        {
            private Server Server;

            public override async Task InitializeAsync()
            {
                var serverMock = MockHelper.CreateDiscordGuild(0);
                this.Server = await this.Database.GetOrRegisterServerAsync(serverMock);
            }

            [Fact]
            public async Task ReturnsErrorIfValueIsSameAsCurrent()
            {
                var result = await this.Servers.SetSendJoinMessageAsync(this.Database, this.Server, false);
                Assert.False(result.IsSuccess);
                Assert.Equal(CommandError.Unsuccessful, result.Error);
            }

            [Fact]
            public async Task CanSetValue()
            {
                var result = await this.Servers.SetSendJoinMessageAsync(this.Database, this.Server, true);

                Assert.True(result.IsSuccess);
                Assert.True(this.Server.SendJoinMessage);
            }
        }
    }
}
