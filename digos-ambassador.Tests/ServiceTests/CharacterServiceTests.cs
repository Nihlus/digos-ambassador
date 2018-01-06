//
//  CharacterServiceTests.cs
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

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Tests.Database;

using Discord;
using Discord.Commands;
using Moq;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591

namespace DIGOS.Ambassador.Tests.ServiceTests
{
	public class CharacterServiceTests
	{
		public class GetCharacters : IDisposable
		{
			private readonly GlobalInfoContext Database;
			private readonly CharacterService Characters;

			public GetCharacters()
			{
				this.Characters = new CharacterService(new CommandService(), new OwnedEntityService(), new ContentService(), null);
				this.Database = new MockedDatabase().GetDatabaseContext();
			}

			[Fact]
			public void ReturnsNoCharactersFromEmptyDatabase()
			{
				var guildMock = new Mock<IGuild>();
				guildMock.Setup(g => g.Id).Returns(0);

				var guild = guildMock.Object;
				var result = this.Characters.GetCharacters(this.Database, guild);

				Assert.Empty(result);
			}

			[Fact]
			public void ReturnsSingleCharacterFromSingleCharacterOnServer()
			{
				var guildMock = new Mock<IGuild>();
				guildMock.Setup(g => g.Id).Returns(0);

				this.Database.Characters.Add(new Character { ServerID = 0 });
				this.Database.SaveChanges();

				var guild = guildMock.Object;
				var result = this.Characters.GetCharacters(this.Database, guild);

				Assert.NotEmpty(result);
				Assert.Single(result);
			}

			[Fact]
			public void ReturnsNoCharacterFromSingleCharacterOnServerWhenRequestedServerIsDifferent()
			{
				var guildMock = new Mock<IGuild>();
				guildMock.Setup(g => g.Id).Returns(1);

				this.Database.Characters.Add(new Character { ServerID = 0 });
				this.Database.SaveChanges();

				var guild = guildMock.Object;
				var result = this.Characters.GetCharacters(this.Database, guild);

				Assert.Empty(result);
			}

			[Fact]
			public void ReturnsCorrectCharactersFromDatabase()
			{
				var guildMock = new Mock<IGuild>();
				guildMock.Setup(g => g.Id).Returns(1);

				this.Database.Characters.Add(new Character { ServerID = 0 });
				this.Database.Characters.Add(new Character { ServerID = 1 });
				this.Database.Characters.Add(new Character { ServerID = 1 });
				this.Database.SaveChanges();

				var guild = guildMock.Object;
				var result = this.Characters.GetCharacters(this.Database, guild);

				Assert.NotEmpty(result);
				Assert.Equal(2, result.Count());
			}

			public void Dispose()
			{
				this.Database?.Dispose();
			}
		}

		public class CreateCharacterAsync : IDisposable
		{
			private readonly GlobalInfoContext Database;
			private readonly CharacterService Characters;

			private readonly IGuild Guild;
			private readonly IUser User;
			private readonly ICommandContext Context;

			public CreateCharacterAsync()
			{
				this.Database = new MockedDatabase().GetDatabaseContext();
				this.Characters = new CharacterService(new CommandService(), new OwnedEntityService(), new ContentService(), null);
				this.Characters.WithPronounProvider(new TheyPronounProvider());

				var mockedUser = new Mock<IUser>();
				mockedUser.Setup(u => u.Id).Returns(0);

				this.User = mockedUser.Object;

				var mockedMessage = new Mock<IUserMessage>();
				mockedMessage.Setup(m => m.Author).Returns(this.User);

				var mockedGuild = new Mock<IGuild>();
				mockedGuild.Setup(g => g.Id).Returns(1);

				this.Guild = mockedGuild.Object;

				var mockedContext = new Mock<ICommandContext>();
				mockedContext.Setup(c => c.Message).Returns(mockedMessage.Object);
				mockedContext.Setup(c => c.Guild).Returns(this.Guild);

				this.Context = mockedContext.Object;
			}

			[Fact]
			public async Task CanCreateWithNameOnly()
			{
				var result = await this.Characters.CreateCharacterAsync(this.Database, this.Context, "Test");

				Assert.True(result.IsSuccess);
				Assert.NotEmpty(this.Database.Characters);
				Assert.Equal("Test", this.Database.Characters.First().Name);
			}

			public void Dispose()
			{
				this.Database?.Dispose();
			}
		}
	}
}
