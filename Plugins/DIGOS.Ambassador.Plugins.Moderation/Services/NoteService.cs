//
//  NoteService.cs
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
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using Discord;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Plugins.Moderation.Services
{
    /// <summary>
    /// Acts as an interface for accessing and modifying notes.
    /// </summary>
    [PublicAPI]
    public sealed class NoteService
    {
        [NotNull] private readonly ModerationDatabaseContext _database;
        [NotNull] private readonly ServerService _servers;
        [NotNull] private readonly UserService _users;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoteService"/> class.
        /// </summary>
        /// <param name="database">The database context.</param>
        /// <param name="servers">The server service.</param>
        /// <param name="users">The user service.</param>
        public NoteService
        (
            [NotNull] ModerationDatabaseContext database,
            [NotNull] ServerService servers,
            [NotNull] UserService users
        )
        {
            _database = database;
            _servers = servers;
            _users = users;
        }

        /// <summary>
        /// Retrieves a note with the given ID from the database.
        /// </summary>
        /// <param name="server">The server the note is on.</param>
        /// <param name="noteID">The note ID.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<UserNote>> GetNoteAsync([NotNull] IGuild server, long noteID)
        {
            // The server isn't strictly required here, but it prevents leaking notes between servers.
            var note = await _database.UserNotes.FirstOrDefaultAsync
            (
                n => n.ID == noteID &&
                     n.Server.DiscordID == (long)server.Id
            );

            if (note is null)
            {
                return RetrieveEntityResult<UserNote>.FromError("There's no note with that ID in the database.");
            }

            return note;
        }

        /// <summary>
        /// Creates a note for the given user.
        /// </summary>
        /// <param name="authorUser">The author of the note.</param>
        /// <param name="guildUser">The user.</param>
        /// <param name="content">The content of the note.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<UserNote>> CreateNoteAsync
        (
            [NotNull] IUser authorUser,
            [NotNull] IGuildUser guildUser,
            [NotNull] string content
        )
        {
            var getServer = await _servers.GetOrRegisterServerAsync(guildUser.Guild);
            if (!getServer.IsSuccess)
            {
                return CreateEntityResult<UserNote>.FromError(getServer);
            }

            var server = getServer.Entity;

            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return CreateEntityResult<UserNote>.FromError(getUser);
            }

            var user = getUser.Entity;

            var getAuthor = await _users.GetOrRegisterUserAsync(authorUser);
            if (!getAuthor.IsSuccess)
            {
                return CreateEntityResult<UserNote>.FromError(getAuthor);
            }

            var author = getAuthor.Entity;

            var note = new UserNote(server, user, author, string.Empty);

            var setContent = await SetNoteContentsAsync(note, content);
            if (!setContent.IsSuccess)
            {
                return CreateEntityResult<UserNote>.FromError(setContent);
            }

            _database.UserNotes.Update(note);

            await _database.SaveChangesAsync();

            // Requery the database
            var getNote = await GetNoteAsync(guildUser.Guild, note.ID);
            if (!getNote.IsSuccess)
            {
                return CreateEntityResult<UserNote>.FromError(getNote);
            }

            return CreateEntityResult<UserNote>.FromSuccess(getNote.Entity);
        }

        /// <summary>
        /// Sets the contents of the given note.
        /// </summary>
        /// <param name="note">The note.</param>
        /// <param name="content">The content.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetNoteContentsAsync([NotNull] UserNote note, [NotNull] string content)
        {
            if (content.IsNullOrWhitespace())
            {
                return ModifyEntityResult.FromError("You must provide some content for the note.");
            }

            if (content.Length > 1024)
            {
                return ModifyEntityResult.FromError
                (
                    "The note is too long. It can be at most 1024 characters."
                );
            }

            if (note.Content == content)
            {
                return ModifyEntityResult.FromError("That's already the note's contents.");
            }

            note.Content = content;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }
    }
}
