//
//  GetCharacters.cs
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

using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Characters
{
    public static partial class CharacterServiceTests
    {
        public class GetCharacters : CharacterServiceTestBase
        {
            [Fact]
            public void ReturnsNoCharactersFromEmptyDatabase()
            {
                var result = this.Characters.GetCharacters(this.DefaultServer);

                Assert.Empty(result);
            }

            [Fact]
            public void ReturnsSingleCharacterFromSingleCharacter()
            {
                this.Database.Characters.Update
                (
                    new Character
                    (
                        this.DefaultOwner,
                        this.DefaultServer,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty
                    )
                );

                this.Database.SaveChanges();

                var result = this.Characters.GetCharacters(this.DefaultServer);

                Assert.NotEmpty(result);
                Assert.Single(result);
            }

            [Fact]
            public void ReturnsNoCharacterFromSingleCharacterWhenRequestedServerIsDifferent()
            {
                this.Database.Characters.Update
                (
                    new Character
                    (
                        this.DefaultOwner,
                        new Server(2),
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty
                    )
                );

                this.Database.SaveChanges();

                var result = this.Characters.GetCharacters(this.DefaultServer);

                Assert.Empty(result);
            }

            [Fact]
            public void ReturnsCorrectCharactersFromDatabase()
            {
                this.Database.Characters.Update
                (
                    new Character
                    (
                        this.DefaultOwner,
                        new Server(1),
                        "dummy",
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty
                    )
                );

                this.Database.Characters.Update
                (
                    new Character
                    (
                        this.DefaultOwner,
                        this.DefaultServer,
                        "dummy1",
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty
                    )
                );

                this.Database.Characters.Update
                (
                    new Character
                    (
                        this.DefaultOwner,
                        this.DefaultServer,
                        "dummy2",
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty
                    )
                );

                this.Database.SaveChanges();

                var result = this.Characters.GetCharacters(this.DefaultServer);

                Assert.NotEmpty(result);
                Assert.Equal(2, result.Count());
            }
        }
    }
}
