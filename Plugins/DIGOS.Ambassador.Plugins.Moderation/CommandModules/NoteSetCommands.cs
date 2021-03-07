//
//  NoteSetCommands.cs
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

using System.ComponentModel;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Moderation.Permissions;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Moderation.CommandModules
{
    public partial class NoteCommands
    {
        /// <summary>
        /// Note setter commands.
        /// </summary>
        [Group("set")]
        public class NoteSetCommands : CommandGroup
        {
            private readonly NoteService _notes;
            private readonly ICommandContext _context;

            /// <summary>
            /// Initializes a new instance of the <see cref="NoteSetCommands"/> class.
            /// </summary>
            /// <param name="notes">The moderation service.</param>
            /// <param name="context">The command context.</param>
            public NoteSetCommands
            (
                NoteService notes,
                ICommandContext context
            )
            {
                _notes = notes;
                _context = context;
            }

            /// <summary>
            /// Sets the contents of the note.
            /// </summary>
            /// <param name="noteID">The ID of the note to delete.</param>
            /// <param name="newContents">The new contents of the note.</param>
            [Command("content")]
            [Description("Sets the contents of the note.")]
            [RequirePermission(typeof(ManageNotes), PermissionTarget.All)]
            [RequireContext(ChannelContext.Guild)]
            public async Task<IResult> SetNoteContentsAsync(long noteID, string newContents)
            {
                var getNote = await _notes.GetNoteAsync(_context.GuildID.Value, noteID);
                if (!getNote.IsSuccess)
                {
                    return getNote;
                }

                var note = getNote.Entity;

                var setContents = await _notes.SetNoteContentsAsync(note, newContents);
                if (!setContents.IsSuccess)
                {
                    return setContents;
                }

                return Result<string>.FromSuccess("Note contents updated.");
            }
        }
    }
}
