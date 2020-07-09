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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using Discord;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Moderation.Services
{
    /// <summary>
    /// Acts as an interface for accessing and modifying notes.
    /// </summary>
    public sealed class NoteService
    {
        private readonly ModerationDatabaseContext _database;
        private readonly ServerService _servers;
        private readonly UserService _users;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoteService"/> class.
        /// </summary>
        /// <param name="database">The database context.</param>
        /// <param name="servers">The server service.</param>
        /// <param name="users">The user service.</param>
        public NoteService
        (
            ModerationDatabaseContext database,
            ServerService servers,
            UserService users
        )
        {
            _database = database;
            _servers = servers;
            _users = users;
        }

        /// <summary>
        /// Gets the notes attached to the given user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>The notes.</returns>
        public Task<IReadOnlyList<UserNote>> GetNotesAsync(IGuildUser user, CancellationToken ct = default)
        {
            return _database.UserNotes.ServersideQueryAsync
            (
                q => q.Where
                (
                    n => n.User.DiscordID == (long)user.Id && n.Server.DiscordID == (long)user.Guild.Id
                ),
                ct
            );
        }

        /// <summary>
        /// Retrieves a note with the given ID from the database.
        /// </summary>
        /// <param name="server">The server the note is on.</param>
        /// <param name="noteID">The note ID.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<UserNote>> GetNoteAsync
        (
            IGuild server,
            long noteID,
            CancellationToken ct = default
        )
        {
            // The server isn't strictly required here, but it prevents leaking notes between servers.
            var note = await _database.UserNotes.ServersideQueryAsync
            (
                q => q.FirstOrDefaultAsync
                (
                    n => n.ID == noteID && n.Server.DiscordID == (long)server.Id,
                    ct
                )
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
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<UserNote>> CreateNoteAsync
        (
            IUser authorUser,
            IGuildUser guildUser,
            string content,
            CancellationToken ct = default
        )
        {
            var getServer = await _servers.GetOrRegisterServerAsync(guildUser.Guild, ct);
            if (!getServer.IsSuccess)
            {
                return CreateEntityResult<UserNote>.FromError(getServer);
            }

            var server = getServer.Entity;

            var getUser = await _users.GetOrRegisterUserAsync(guildUser, ct);
            if (!getUser.IsSuccess)
            {
                return CreateEntityResult<UserNote>.FromError(getUser);
            }

            var user = getUser.Entity;

            var getAuthor = await _users.GetOrRegisterUserAsync(authorUser, ct);
            if (!getAuthor.IsSuccess)
            {
                return CreateEntityResult<UserNote>.FromError(getAuthor);
            }

            var author = getAuthor.Entity;

            var note = _database.CreateProxy<UserNote>(server, user, author, string.Empty);
            _database.UserNotes.Update(note);

            var setContent = await SetNoteContentsAsync(note, content, ct);
            if (!setContent.IsSuccess)
            {
                return CreateEntityResult<UserNote>.FromError(setContent);
            }

            await _database.SaveChangesAsync(ct);

            return note;
        }

        /// <summary>
        /// Sets the contents of the given note.
        /// </summary>
        /// <param name="note">The note.</param>
        /// <param name="content">The content.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetNoteContentsAsync
        (
            UserNote note,
            string content,
            CancellationToken ct = default
        )
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
            note.NotifyUpdate();

            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Deletes the given note.
        /// </summary>
        /// <param name="note">The note to delete.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A deletion result which may or may note have succeeded.</returns>
        public async Task<DeleteEntityResult> DeleteNoteAsync
        (
            UserNote note,
            CancellationToken ct = default
        )
        {
            if (!_database.UserNotes.Any(n => n.ID == note.ID))
            {
                return DeleteEntityResult.FromError
                (
                    "That note isn't in the database. This is probably an error in the bot."
                );
            }

            _database.UserNotes.Remove(note);
            await _database.SaveChangesAsync(ct);

            return DeleteEntityResult.FromSuccess();
        }
    }
}
