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

using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Extensions.Results;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Moderation.Permissions;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using DIGOS.Ambassador.Plugins.Permissions.Preconditions;
using Discord.Commands;
using JetBrains.Annotations;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Moderation.CommandModules
{
    public partial class NoteCommands
    {
        /// <summary>
        /// Note setter commands.
        /// </summary>
        [PublicAPI]
        [Group("set")]
        public class NoteSetCommands : ModuleBase
        {
            private readonly NoteService _notes;

            private readonly UserFeedbackService _feedback;

            /// <summary>
            /// Initializes a new instance of the <see cref="NoteSetCommands"/> class.
            /// </summary>
            /// <param name="notes">The moderation service.</param>
            /// <param name="feedback">The feedback service.</param>
            public NoteSetCommands
            (
                NoteService notes,
                UserFeedbackService feedback
            )
            {
                _notes = notes;
                _feedback = feedback;
            }

            /// <summary>
            /// Sets the contents of the note.
            /// </summary>
            /// <param name="noteID">The ID of the note to delete.</param>
            /// <param name="newContents">The new contents of the note.</param>
            [Command("content")]
            [Summary("Sets the contents of the note.")]
            [RequirePermission(typeof(ManageNotes), PermissionTarget.All)]
            [RequireContext(ContextType.Guild)]
            public async Task<RuntimeResult> SetNoteContentsAsync(long noteID, string newContents)
            {
                var getNote = await _notes.GetNoteAsync(this.Context.Guild, noteID);
                if (!getNote.IsSuccess)
                {
                    return getNote.ToRuntimeResult();
                }

                var note = getNote.Entity;

                var setContents = await _notes.SetNoteContentsAsync(note, newContents);
                if (!setContents.IsSuccess)
                {
                    return setContents.ToRuntimeResult();
                }

                return RuntimeCommandResult.FromSuccess("Note contents updated.");
            }
        }
    }
}
