//
//  ClearDefaultCharacterForUserAsync.cs
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
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Discord.Commands;
using Moq;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Characters
{
    public partial class CharacterServiceTests
    {
        public class ClearDefaultCharacterForUserAsync : CharacterServiceTestBase
        {
            private const string CharacterName = "Test";

            private readonly IUser _owner = MockHelper.CreateDiscordUser(0);
            private readonly IGuild _guild = MockHelper.CreateDiscordGuild(1);

            private User _user = null!;
            private Character _character = null!;

            public override async Task InitializeAsync()
            {
                _user = (await this.Users.GetOrRegisterUserAsync(_owner)).Entity;

                _character = new Character((long)_guild.Id, _user, CharacterName);

                this.Database.Characters.Update(_character);
                await this.Database.SaveChangesAsync();
            }

            [Fact]
            public async Task CanClearDefaultCharacter()
            {
                var contextMock = new Mock<ICommandContext>();
                contextMock.Setup(c => c.Message.Author.Id).Returns(_owner.Id);
                contextMock.Setup(c => c.Guild).Returns(_guild);

                var context = contextMock.Object;

                await this.Characters.SetDefaultCharacterForUserAsync
                (
                    context,
                    _character,
                    _user
                );

                var result = await this.Characters.ClearDefaultCharacterForUserAsync
                (
                    context,
                    _user
                );

                Assert.True(result.IsSuccess);

                var defaultCharacterResult = await this.Characters.GetDefaultCharacterAsync
                (
                    _user,
                    _guild
                );

                Assert.False(defaultCharacterResult.IsSuccess);
            }

            [Fact]
            public async Task ReturnsErrorIfDefaultCharacterIsNotSet()
            {
                var contextMock = new Mock<ICommandContext>();
                contextMock.Setup(c => c.Message.Author.Id).Returns(_owner.Id);
                contextMock.Setup(c => c.Guild).Returns(_guild);

                var context = contextMock.Object;

                var result = await this.Characters.ClearDefaultCharacterForUserAsync
                (
                    context,
                    _user
                );

                Assert.False(result.IsSuccess);

                var defaultCharacterResult = await this.Characters.GetDefaultCharacterAsync
                (
                    _user,
                    _guild
                );

                Assert.False(defaultCharacterResult.IsSuccess);
            }
        }
    }
}
