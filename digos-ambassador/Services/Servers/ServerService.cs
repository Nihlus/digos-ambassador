//
//  ServerService.cs
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

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Extensions;
using Discord;
using Discord.Commands;

using JetBrains.Annotations;

namespace DIGOS.Ambassador.Services.Servers
{
    /// <summary>
    /// Handles modification of server settings.
    /// </summary>
    public class ServerService
    {
        /// <summary>
        /// Gets the description of the server.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public RetrieveEntityResult<string> GetDescription([NotNull] Server server)
        {
            if (server.Description.IsNullOrWhitespace())
            {
                return RetrieveEntityResult<string>.FromError(CommandError.ObjectNotFound, "No description set.");
            }

            return RetrieveEntityResult<string>.FromSuccess(server.Description);
        }

        /// <summary>
        /// Sets the description of the server.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="server">The server.</param>
        /// <param name="description">The new description.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetDescriptionAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] Server server,
            [NotNull] string description
        )
        {
            if (description.IsNullOrWhitespace())
            {
                return ModifyEntityResult.FromError
                (
                    CommandError.UnmetPrecondition,
                    "The description must not be empty."
                );
            }

            if (server.Description == description)
            {
                return ModifyEntityResult.FromError
                (
                    CommandError.Unsuccessful,
                    "That's already the server's description."
                );
            }

            if (description.Length > 800)
            {
                return ModifyEntityResult.FromError
                (
                    CommandError.UnmetPrecondition,
                    "The description may not be longer than 800 characters."
                );
            }

            server.Description = description;

            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets the server's join message.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public RetrieveEntityResult<string> GetJoinMessage([NotNull] Server server)
        {
            if (server.JoinMessage.IsNullOrWhitespace())
            {
                return RetrieveEntityResult<string>.FromError(CommandError.ObjectNotFound, "No join message set.");
            }

            return RetrieveEntityResult<string>.FromSuccess(server.JoinMessage);
        }

        /// <summary>
        /// Sets the server's join message.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="server">The server.</param>
        /// <param name="joinMessage">The new join message.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetJoinMessageAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] Server server,
            [NotNull] string joinMessage
        )
        {
            if (joinMessage.IsNullOrWhitespace())
            {
                return ModifyEntityResult.FromError
                (
                    CommandError.UnmetPrecondition,
                    "The join message must not be empty."
                );
            }

            if (server.JoinMessage == joinMessage)
            {
                return ModifyEntityResult.FromError
                (
                    CommandError.Unsuccessful,
                    "That's already the server's join message."
                );
            }

            if (joinMessage.Length > 1200)
            {
                return ModifyEntityResult.FromError
                (
                    CommandError.UnmetPrecondition,
                    "The join message may not be longer than 1200 characters."
                );
            }

            server.JoinMessage = joinMessage;

            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets a value indicating whether the server is NSFW.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="server">The server.</param>
        /// <param name="isNsfw">Whether or not the server is NSFW.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetIsNSFWAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] Server server,
            bool isNsfw
        )
        {
            if (server.IsNSFW == isNsfw)
            {
                return ModifyEntityResult.FromError
                (
                    CommandError.Unsuccessful,
                    $"The server is already {(isNsfw ? string.Empty : "not")} NSFW."
                );
            }

            server.IsNSFW = isNsfw;
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets a value indicating whether the server should send first-join messages.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="server">The server.</param>
        /// <param name="sendJoinMessage">Whether or not the server should send join messages.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetSendJoinMessageAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] Server server,
            bool sendJoinMessage
        )
        {
            if (server.SendJoinMessage == sendJoinMessage)
            {
                return ModifyEntityResult.FromError
                (
                    CommandError.Unsuccessful,
                    $"The server already {(sendJoinMessage ? string.Empty : "not")} sending first-join messages."
                );
            }

            server.SendJoinMessage = sendJoinMessage;
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the channel category to use for dedicated roleplay channels.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="server">The server.</param>
        /// <param name="category">The category to use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetDedicatedRoleplayChannelCategoryAsync
        (
            [NotNull] GlobalInfoContext db,
            [NotNull] Server server,
            [CanBeNull] ICategoryChannel category
        )
        {
            server.DedicatedRoleplayChannelsCategory = (long?)category?.Id;
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }
    }
}
