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
using Microsoft.EntityFrameworkCore;
using Remora.Discord.Core;
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
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>The notes.</returns>
        public Task<IReadOnlyList<UserNote>> GetNotesAsync(Snowflake guildID, Snowflake userID, CancellationToken ct = default)
        {
            return _database.UserNotes.ServersideQueryAsync
            (
                q => q.Where
                (
                    n => n.User.DiscordID == userID && n.Server.DiscordID == guildID
                ),
                ct
            );
        }

        /// <summary>
        /// Retrieves a note with the given ID from the database.
        /// </summary>
        /// <param name="guildID">The ID of the guild the note is on.</param>
        /// <param name="noteID">The note ID.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<UserNote>> GetNoteAsync
        (
            Snowflake guildID,
            long noteID,
            CancellationToken ct = default
        )
        {
            // The server isn't strictly required here, but it prevents leaking notes between servers.
            var note = await _database.UserNotes.ServersideQueryAsync
            (
                q => q.FirstOrDefaultAsync
                (
                    n => n.ID == noteID && n.Server.DiscordID == guildID,
                    ct
                )
            );

            if (note is null)
            {
                return new UserError("There's no note with that ID in the database.");
            }

            return note;
        }

        /// <summary>
        /// Creates a note for the given user.
        /// </summary>
        /// <param name="authorID">The ID of the author of the warning.</param>
        /// <param name="userID">The ID of the warned user.</param>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="content">The content of the note.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<Result<UserNote>> CreateNoteAsync
        (
            Snowflake authorID,
            Snowflake userID,
            Snowflake guildID,
            string content,
            CancellationToken ct = default
        )
        {
            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result<UserNote>.FromError(getServer);
            }

            var server = getServer.Entity;

            var getUser = await _users.GetOrRegisterUserAsync(userID, ct);
            if (!getUser.IsSuccess)
            {
                return Result<UserNote>.FromError(getUser);
            }

            var user = getUser.Entity;

            var getAuthor = await _users.GetOrRegisterUserAsync(authorID, ct);
            if (!getAuthor.IsSuccess)
            {
                return Result<UserNote>.FromError(getAuthor);
            }

            var author = getAuthor.Entity;

            var note = _database.CreateProxy<UserNote>(server, user, author, string.Empty);
            _database.UserNotes.Update(note);

            var setContent = await SetNoteContentsAsync(note, content, ct);
            if (!setContent.IsSuccess)
            {
                return Result<UserNote>.FromError(setContent);
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
        public async Task<Result> SetNoteContentsAsync
        (
            UserNote note,
            string content,
            CancellationToken ct = default
        )
        {
            if (content.IsNullOrWhitespace())
            {
                return new UserError("You must provide some content for the note.");
            }

            if (content.Length > 1024)
            {
                return new UserError
                (
                    "The note is too long. It can be at most 1024 characters."
                );
            }

            if (note.Content == content)
            {
                return new UserError("That's already the note's contents.");
            }

            note.Content = content;
            note.NotifyUpdate();

            await _database.SaveChangesAsync(ct);

            return Result.FromSuccess();
        }

        /// <summary>
        /// Deletes the given note.
        /// </summary>
        /// <param name="note">The note to delete.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A deletion result which may or may note have succeeded.</returns>
        public async Task<Result> DeleteNoteAsync
        (
            UserNote note,
            CancellationToken ct = default
        )
        {
            if (!_database.UserNotes.Any(n => n.ID == note.ID))
            {
                return new UserError
                (
                    "That note isn't in the database. This is probably an error in the bot."
                );
            }

            _database.UserNotes.Remove(note);
            await _database.SaveChangesAsync(ct);

            return Result.FromSuccess();
        }
    }
}
