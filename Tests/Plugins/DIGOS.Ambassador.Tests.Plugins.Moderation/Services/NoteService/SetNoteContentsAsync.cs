//
//  SetNoteContentsAsync.cs
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

using System;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using DIGOS.Ambassador.Tests.Plugins.Moderation.Bases;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Moderation.Services.NoteService
{
    public partial class NoteService
    {
        public class SetNoteContentsAsync : NoteServiceTestBase
        {
            private readonly UserNote _note = new UserNote(new Server(0), new User(0), new User(1), string.Empty);

            [Fact]
            public async Task ReturnsUnsuccessfulIfNewContentsAreEmpty()
            {
                var result = await this.Notes.SetNoteContentsAsync(_note, string.Empty);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulIfNewContentsAreNull()
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = await this.Notes.SetNoteContentsAsync(_note, null);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulIfNewContentIsTooLong()
            {
                var result = await this.Notes.SetNoteContentsAsync(_note, new string('a', 1025));

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulIfNewContentsAreIdentical()
            {
                await this.Notes.SetNoteContentsAsync(_note, "Dummy thicc");
                var result = await this.Notes.SetNoteContentsAsync(_note, "Dummy thicc");

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulIfNewContentsAreWellFormed()
            {
                await this.Notes.SetNoteContentsAsync(_note, "Dummy thicc");
                var result = await this.Notes.SetNoteContentsAsync(_note, "Not dummy thicc");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ActuallySetsContents()
            {
                await this.Notes.SetNoteContentsAsync(_note, "Dummy thicc");

                Assert.Equal("Dummy thicc", _note.Content);
            }
        }
    }
}
