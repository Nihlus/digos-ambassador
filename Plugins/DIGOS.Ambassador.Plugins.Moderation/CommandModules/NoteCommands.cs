//
//  NoteCommands.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Plugins.Moderation.Permissions;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using DIGOS.Ambassador.Plugins.Permissions.Preconditions;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Moderation.CommandModules
{
    /// <summary>
    /// Note-related commands, such as viewing or editing info about a specific note.
    /// </summary>
    [PublicAPI]
    [Group("note")]
    [Summary("Note-related commands, such as viewing or editing info about a specific note.")]
    public partial class NoteCommands : ModuleBase
    {
        [NotNull]
        private readonly NoteService _notes;

        [NotNull]
        private readonly UserFeedbackService _feedback;

        [NotNull]
        private readonly InteractivityService _interactivity;

        [NotNull]
        private readonly ChannelLoggingService _logging;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoteCommands"/> class.
        /// </summary>
        /// <param name="notes">The moderation service.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="logging">The logging service.</param>
        public NoteCommands
        (
            [NotNull] NoteService notes,
            [NotNull] UserFeedbackService feedback,
            [NotNull] InteractivityService interactivity,
            [NotNull] ChannelLoggingService logging
        )
        {
            _notes = notes;
            _feedback = feedback;
            _interactivity = interactivity;
            _logging = logging;
        }

        /// <summary>
        /// Lists the notes attached to the given user.
        /// </summary>
        /// <param name="user">The user.</param>
        [Command("list")]
        [Summary("Lists the notes attached to the given user.")]
        [RequirePermission(typeof(ManageNotes), PermissionTarget.Other)]
        [RequireContext(ContextType.Guild)]
        public async Task ListNotesAsync([NotNull] IGuildUser user)
        {
            var notes = _notes.GetNotes(user);

            var appearance = PaginatedAppearanceOptions.Default;
            appearance.Title = "Notes";
            appearance.Color = Color.Gold;

            var paginatedEmbed = await PaginatedEmbedFactory.PagesFromCollectionAsync
            (
                _feedback,
                _interactivity,
                this.Context.User,
                notes,
                async (eb, note) =>
                {
                    eb.WithTitle($"Note #{note.ID} for {user.Username}:{user.Discriminator}");

                    var author = await this.Context.Guild.GetUserAsync((ulong)note.Author.DiscordID);
                    eb.WithAuthor(author);

                    eb.WithDescription(note.Content);

                    eb.AddField("Created", note.CreatedAt);

                    if (note.CreatedAt != note.UpdatedAt)
                    {
                        eb.AddField("Last Updated", note.UpdatedAt);
                    }
                },
                appearance: appearance
            );

            await _interactivity.SendInteractiveMessageAndDeleteAsync
            (
                this.Context.Channel,
                paginatedEmbed,
                TimeSpan.FromMinutes(5)
            );
        }

        /// <summary>
        /// Adds a note to the given user.
        /// </summary>
        /// <param name="user">The user to add the note to.</param>
        /// <param name="content">The contents of the note.</param>
        [Command("add")]
        [Summary("Adds a note to the given user.")]
        [RequirePermission(typeof(ManageNotes), PermissionTarget.All)]
        [RequireContext(ContextType.Guild)]
        public async Task AddNoteAsync([NotNull] IGuildUser user, [NotNull] string content)
        {
            var addNote = await _notes.CreateNoteAsync(this.Context.User, user, content);
            if (!addNote.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, addNote.ErrorReason);
                return;
            }

            var note = addNote.Entity;
            await _feedback.SendConfirmationAsync(this.Context, $"Note added (ID {note.ID}).");
            await _logging.NotifyUserNoteAdded(note);
        }

        /// <summary>
        /// Deletes the given note.
        /// </summary>
        /// <param name="noteID">The ID of the note to delete.</param>
        [Command("delete")]
        [Summary("Deletes the given note.")]
        [RequirePermission(typeof(ManageNotes), PermissionTarget.All)]
        [RequireContext(ContextType.Guild)]
        public async Task DeleteNoteAsync(long noteID)
        {
            var getNote = await _notes.GetNoteAsync(this.Context.Guild, noteID);
            if (!getNote.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getNote.ErrorReason);
                return;
            }

            var note = getNote.Entity;

            var deleteNote = await _notes.DeleteNoteAsync(note);
            if (!deleteNote.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, deleteNote.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "Note deleted.");

            var rescinder = await this.Context.Guild.GetUserAsync(this.Context.User.Id);
            await _logging.NotifyUserNoteRemoved(note, rescinder);
        }
    }
}
