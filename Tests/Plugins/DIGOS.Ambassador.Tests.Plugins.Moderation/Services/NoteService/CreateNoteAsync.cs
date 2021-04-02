//
//  CreateNoteAsync.cs
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

namespace DIGOS.Ambassador.Tests.Plugins.Moderation.Services.NoteService
{
    public partial class NoteService
    {
        public class CreateNoteAsync : NoteServiceTestBase
        {
            private readonly Snowflake _user = new Snowflake(0);
            private readonly Snowflake _guild = new Snowflake(1);

            private readonly Snowflake _author = new Snowflake(1);

            [Fact]
            private async Task ReturnsSuccessful()
            {
                var result = await this.Notes.CreateNoteAsync(_author, _user, _guild, "Dummy thicc");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            private async Task ActuallyCreatesNote()
            {
                await this.Notes.CreateNoteAsync(_author, _user, _guild, "Dummy thicc");

                Assert.NotEmpty(this.Database.UserNotes);
            }
        }
    }
}
