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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Plugins.Moderation.Permissions;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using Humanizer;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Moderation.CommandModules
{
    /// <summary>
    /// Note-related commands, such as viewing or editing info about a specific note.
    /// </summary>
    [Group("note")]
    [Description("Note-related commands, such as viewing or editing info about a specific note.")]
    public partial class NoteCommands : CommandGroup
    {
        private readonly NoteService _notes;
        private readonly InteractivityService _interactivity;
        private readonly ChannelLoggingService _logging;
        private readonly ICommandContext _context;
        private readonly IDiscordRestUserAPI _userAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoteCommands"/> class.
        /// </summary>
        /// <param name="notes">The moderation service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="logging">The logging service.</param>
        /// <param name="context">The command context.</param>
        /// <param name="userAPI">The user API.</param>
        public NoteCommands
        (
            NoteService notes,
            InteractivityService interactivity,
            ChannelLoggingService logging,
            ICommandContext context,
            IDiscordRestUserAPI userAPI
        )
        {
            _notes = notes;
            _interactivity = interactivity;
            _logging = logging;
            _context = context;
            _userAPI = userAPI;
        }

        /// <summary>
        /// Lists the notes attached to the given user.
        /// </summary>
        /// <param name="user">The user.</param>
        [Command("list")]
        [Description("Lists the notes attached to the given user.")]
        [RequirePermission(typeof(ManageNotes), PermissionTarget.Other)]
        [RequireContext(ChannelContext.Guild)]
        public async Task<IResult> ListNotesAsync(IUser user)
        {
            var notes = await _notes.GetNotesAsync(_context.GuildID.Value, user.ID);

            var createPages = await PaginatedEmbedFactory.PagesFromCollectionAsync
            (
                notes,
                async note =>
                {
                    var getAuthor = await _userAPI.GetUserAsync(note.Author.DiscordID);
                    if (!getAuthor.IsSuccess)
                    {
                        return Result<Embed>.FromError(getAuthor);
                    }

                    var author = getAuthor.Entity;

                    var getAuthorAvatar = CDN.GetUserAvatarUrl(author);

                    var embedFields = new List<EmbedField>();
                    var eb = new Embed
                    {
                        Title = $"Note #{note.ID} for {user.Username}:{user.Discriminator}",
                        Colour = Color.Gold,
                        Author = new EmbedAuthor
                        {
                            Name = author.Username,
                            IconUrl = getAuthorAvatar.IsSuccess ? getAuthorAvatar.Entity.ToString() : default
                        },
                        Description = note.Content,
                        Fields = embedFields
                    };

                    embedFields.Add(new EmbedField("Created", note.CreatedAt.Humanize()));

                    if (note.CreatedAt != note.UpdatedAt)
                    {
                        embedFields.Add(new EmbedField("Last Updated", note.UpdatedAt.Humanize()));
                    }

                    return eb;
                }
            );

            if (createPages.Any(p => !p.IsSuccess))
            {
                return createPages.First(p => !p.IsSuccess);
            }

            var pages = createPages.Select(p => p.Entity!).ToList();

            await _interactivity.SendInteractiveMessageAsync
            (
                _context.ChannelID,
                (channelID, messageID, channelAPI) => new PaginatedMessage
                (
                    channelID,
                    messageID,
                    channelAPI,
                    _context.User.ID,
                    pages
                )
            );

            return Result.FromSuccess();
        }

        /// <summary>
        /// Adds a note to the given user.
        /// </summary>
        /// <param name="user">The user to add the note to.</param>
        /// <param name="content">The contents of the note.</param>
        [Command("add")]
        [Description("Adds a note to the given user.")]
        [RequirePermission(typeof(ManageNotes), PermissionTarget.All)]
        [RequireContext(ChannelContext.Guild)]
        public async Task<IResult> AddNoteAsync(IUser user, string content)
        {
            var addNote = await _notes.CreateNoteAsync(_context.User.ID, user.ID, _context.GuildID.Value, content);
            if (!addNote.IsSuccess)
            {
                return addNote;
            }

            var note = addNote.Entity;

            await _logging.NotifyUserNoteAddedAsync(note);
            return Result<string>.FromSuccess($"Note added (ID {note.ID}).");
        }

        /// <summary>
        /// Deletes the given note.
        /// </summary>
        /// <param name="noteID">The ID of the note to delete.</param>
        [Command("delete")]
        [Description("Deletes the given note.")]
        [RequirePermission(typeof(ManageNotes), PermissionTarget.All)]
        [RequireContext(ChannelContext.Guild)]
        public async Task<IResult> DeleteNoteAsync(long noteID)
        {
            var getNote = await _notes.GetNoteAsync(_context.GuildID.Value, noteID);
            if (!getNote.IsSuccess)
            {
                return getNote;
            }

            var note = getNote.Entity;

            // This has to be done before the warning is actually deleted - otherwise, the lazy loader is removed and
            // navigation properties can't be evaluated
            var notifyResult = await _logging.NotifyUserNoteRemovedAsync(note, _context.User.ID);
            if (!notifyResult.IsSuccess)
            {
                return notifyResult;
            }

            var deleteNote = await _notes.DeleteNoteAsync(note);
            if (!deleteNote.IsSuccess)
            {
                return deleteNote;
            }

            return Result<string>.FromSuccess("Note deleted.");
        }
    }
}
