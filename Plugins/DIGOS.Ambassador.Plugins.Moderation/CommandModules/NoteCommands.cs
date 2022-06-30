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

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
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
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Pagination;
using Remora.Discord.Pagination.Extensions;
using Remora.Rest.Core;
using Remora.Results;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Moderation.CommandModules;

/// <summary>
/// Note-related commands, such as viewing or editing info about a specific note.
/// </summary>
[Group("note")]
[Description("Note-related commands, such as viewing or editing info about a specific note.")]
public partial class NoteCommands : CommandGroup
{
    private readonly NoteService _notes;
    private readonly ChannelLoggingService _logging;
    private readonly ICommandContext _context;
    private readonly IDiscordRestUserAPI _userAPI;
    private readonly FeedbackService _feedback;

    /// <summary>
    /// Initializes a new instance of the <see cref="NoteCommands"/> class.
    /// </summary>
    /// <param name="notes">The moderation service.</param>
    /// <param name="logging">The logging service.</param>
    /// <param name="context">The command context.</param>
    /// <param name="userAPI">The user API.</param>
    /// <param name="feedback">The feedback service.</param>
    public NoteCommands
    (
        NoteService notes,
        ChannelLoggingService logging,
        ICommandContext context,
        IDiscordRestUserAPI userAPI,
        FeedbackService feedback
    )
    {
        _notes = notes;
        _logging = logging;
        _context = context;
        _userAPI = userAPI;
        _feedback = feedback;
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
                    Author = new EmbedAuthor(author.Username)
                    {
                        IconUrl = getAuthorAvatar.IsSuccess
                            ? getAuthorAvatar.Entity.ToString()
                            : default(Optional<string>)
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

        var pages = createPages.Select(p => p.Entity).ToList();

        return (Result)await _feedback.SendContextualPaginatedMessageAsync
        (
            _context.User.ID,
            pages,
            ct: this.CancellationToken
        );
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
    public async Task<Result<FeedbackMessage>> AddNoteAsync(IUser user, string content)
    {
        var addNote = await _notes.CreateNoteAsync(_context.User.ID, user.ID, _context.GuildID.Value, content);
        if (!addNote.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(addNote);
        }

        var note = addNote.Entity;

        await _logging.NotifyUserNoteAddedAsync(note);
        return new FeedbackMessage($"Note added (ID {note.ID}).", _feedback.Theme.Secondary);
    }

    /// <summary>
    /// Deletes the given note.
    /// </summary>
    /// <param name="noteID">The ID of the note to delete.</param>
    [Command("delete")]
    [Description("Deletes the given note.")]
    [RequirePermission(typeof(ManageNotes), PermissionTarget.All)]
    [RequireContext(ChannelContext.Guild)]
    public async Task<Result<FeedbackMessage>> DeleteNoteAsync(long noteID)
    {
        var getNote = await _notes.GetNoteAsync(_context.GuildID.Value, noteID);
        if (!getNote.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(getNote);
        }

        var note = getNote.Entity;

        // This has to be done before the warning is actually deleted - otherwise, the lazy loader is removed and
        // navigation properties can't be evaluated
        var notifyResult = await _logging.NotifyUserNoteRemovedAsync(note, _context.User.ID);
        if (!notifyResult.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(notifyResult);
        }

        var deleteNote = await _notes.DeleteNoteAsync(note);
        return deleteNote.IsSuccess
            ? new FeedbackMessage("Note deleted.", _feedback.Theme.Secondary)
            : Result<FeedbackMessage>.FromError(deleteNote);
    }
}
