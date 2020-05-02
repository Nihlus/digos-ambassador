//
//  OwnedEntityTypeReader.cs
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
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using Discord;
using Discord.Commands;
using MoreLinq.Extensions;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Core.TypeReaders
{
    /// <summary>
    /// Reads owned entities from command arguments.
    /// </summary>
    /// <typeparam name="TEntity">Owned entity classes.</typeparam>
    public abstract class OwnedEntityTypeReader<TEntity> : TypeReader where TEntity : class, IOwnedNamedEntity
    {
        /// <inheritdoc />
        public sealed override async Task<TypeReaderResult> ReadAsync
        (
            ICommandContext context,
            string input,
            IServiceProvider services
        )
        {
            IUser? owner = null;
            string? entityName;
            if (!input.IsNullOrWhitespace() && input.Any(c => c == ':'))
            {
                // We have a mentioned owner and a name. Owners may have colons in the name, so let's check from the back.
                var splitIndex = input.LastIndexOf(':');
                var rawUser = input.Substring(0, splitIndex);
                entityName = input.Substring(splitIndex + 1);

                // Try to parse the user
                var userParseResult = await ReadUserAsync(context, rawUser);
                if (!userParseResult.IsSuccess)
                {
                    return TypeReaderResult.FromError(CommandError.Unsuccessful, userParseResult.ErrorReason);
                }

                owner = userParseResult.Entity;
            }
            else
            {
                // We might just have a user and not a name, so let's try parsing it
                var userParseResult = await ReadUserAsync(context, input);
                if (userParseResult.IsSuccess)
                {
                    // The entity might have the same name as the user; if so, it should take priority over naming a
                    // user
                    if (!MentionUtils.TryParseUser(input, out _))
                    {
                        var retrieveEntityByNameResult = await RetrieveEntityAsync(null, input, context, services);
                        if (retrieveEntityByNameResult.IsSuccess)
                        {
                            if (retrieveEntityByNameResult.Entity.IsOwner(userParseResult.Entity))
                            {
                                return TypeReaderResult.FromSuccess(retrieveEntityByNameResult.Entity);
                            }

                            var message = "There's both an entity and a user with that name. Try specifying which" +
                                          " user you want to look up entities from.";

                            return TypeReaderResult.FromError
                            (
                                CommandError.Unsuccessful,
                                message
                            );
                        }
                    }

                    // It's definitely a user and not an entity
                    entityName = null;

                    owner = userParseResult.Entity;
                }
                else
                {
                    entityName = input;
                }
            }

            var retrieveEntityResult = await RetrieveEntityAsync(owner, entityName, context, services);
            if (!retrieveEntityResult.IsSuccess)
            {
                if (!(owner is null))
                {
                    return TypeReaderResult.FromError(CommandError.Unsuccessful, retrieveEntityResult.ErrorReason);
                }

                var retrieveUserEntityResult = await RetrieveEntityAsync(context.User, entityName, context, services);
                if (!retrieveUserEntityResult.IsSuccess)
                {
                    return TypeReaderResult.FromError(CommandError.Unsuccessful, retrieveUserEntityResult.ErrorReason);
                }

                return TypeReaderResult.FromSuccess(retrieveUserEntityResult.Entity);
            }

            return TypeReaderResult.FromSuccess(retrieveEntityResult.Entity);
        }

        /// <summary>
        /// Retrieves the named entity from the given user.
        /// </summary>
        /// <param name="entityOwner">The owner of the entity.</param>
        /// <param name="entityName">The name of the entity.</param>
        /// <param name="context">The context of the command.</param>
        /// <param name="services">The injected services.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        protected abstract Task<RetrieveEntityResult<TEntity>> RetrieveEntityAsync
        (
            IUser? entityOwner,
            string? entityName,
            ICommandContext context,
            IServiceProvider services
        );

        private RetrieveEntityResult<TUser> FindBestMatchingUserBy<TUser>
        (
            IEnumerable<TUser> users,
            Func<TUser, string> selector,
            string value
        )
            where TUser : class, IUser
        {
            var values = users.Select(u => (User: u, Value: selector(u))).ToImmutableList();

            var literalMatch = values.FirstOrDefault(uv => uv.Value == value).User;
            if (!(literalMatch is null))
            {
                return RetrieveEntityResult<TUser>.FromSuccess(literalMatch);
            }

            var partialMatch = values.FirstOrDefault
            (
                uv =>
                    string.Equals(uv.Value, value, StringComparison.OrdinalIgnoreCase)
            ).User;

            if (!(partialMatch is null))
            {
                return RetrieveEntityResult<TUser>.FromSuccess(partialMatch);
            }

            return RetrieveEntityResult<TUser>.FromError("No matching user found.");
        }

        private async Task<RetrieveEntityResult<IUser>> GetUserByIdAsync(ICommandContext context, ulong id)
        {
            IUser user;
            if (context.Guild != null)
            {
                user = await context.Guild.GetUserAsync(id, CacheMode.CacheOnly);
            }
            else
            {
                user = await context.Channel.GetUserAsync(id, CacheMode.CacheOnly);
            }

            if (user is null)
            {
                return RetrieveEntityResult<IUser>.FromError
                (
                                        "User could not be retrieved."
                );
            }

            return RetrieveEntityResult<IUser>.FromSuccess(user);
        }

        private async Task<RetrieveEntityResult<IUser>> ReadUserAsync(ICommandContext context, string input)
        {
            // By Mention
            if (!MentionUtils.TryParseUser(input, out var id))
            {
                var getUserResult = await GetUserByIdAsync(context, id);
                if (getUserResult.IsSuccess)
                {
                    return getUserResult;
                }
            }

            // By Id
            if (ulong.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out id))
            {
                var getUserResult = await GetUserByIdAsync(context, id);
                if (getUserResult.IsSuccess)
                {
                    return getUserResult;
                }
            }

            return await FindBestMatchingUserAsync(context, input);
        }

        private async Task<RetrieveEntityResult<IUser>> FindBestMatchingUserAsync
        (
            ICommandContext context,
            string input
        )
        {
            var channelUsers = (await context.Channel.GetUsersAsync(CacheMode.CacheOnly).FlattenAsync())
                .ToImmutableList();

            IReadOnlyCollection<IGuildUser> guildUsers = new List<IGuildUser>();

            if (!(context.Guild is null))
            {
                guildUsers = await context.Guild.GetUsersAsync(CacheMode.CacheOnly);
                guildUsers = guildUsers.ExceptBy(channelUsers, u => u.Id).Cast<IGuildUser>().ToImmutableList();
            }

            // By Username + Discriminator
            var index = input.LastIndexOf('#');
            if (index >= 0)
            {
                var username = input.Substring(0, index);
                if (ushort.TryParse(input.Substring(index + 1), out var discriminator))
                {
                    var bestUser = FindBestMatchingUserBy
                    (
                        channelUsers.Where(u => u.DiscriminatorValue == discriminator),
                        u => u.Username,
                        username
                    );

                    if (bestUser.IsSuccess)
                    {
                        return RetrieveEntityResult<IUser>.FromSuccess(bestUser.Entity);
                    }

                    var bestGuildUser = FindBestMatchingUserBy
                    (
                        guildUsers.Where(u => u.DiscriminatorValue == discriminator),
                        u => u.Username,
                        username
                    );

                    if (bestGuildUser.IsSuccess)
                    {
                        return RetrieveEntityResult<IUser>.FromSuccess(bestGuildUser.Entity);
                    }
                }
            }

            // By Username
            var bestUserByName = FindBestMatchingUserBy(channelUsers, u => u.Username, input);
            if (bestUserByName.IsSuccess)
            {
                return RetrieveEntityResult<IUser>.FromSuccess(bestUserByName.Entity);
            }

            var bestGuildUserByName = FindBestMatchingUserBy(guildUsers, gu => gu.Username, input);
            if (bestGuildUserByName.IsSuccess)
            {
                return RetrieveEntityResult<IUser>.FromSuccess(bestGuildUserByName.Entity);
            }

            if (!(context.Guild is null))
            {
                // By Nickname
                var bestUserByNick = FindBestMatchingUserBy(channelUsers.Cast<IGuildUser>(), u => u.Nickname, input);
                if (bestUserByNick.IsSuccess)
                {
                    return RetrieveEntityResult<IUser>.FromSuccess(bestUserByNick.Entity);
                }

                var bestGuildUserByNick = FindBestMatchingUserBy(guildUsers, gu => gu.Nickname, input);
                if (bestGuildUserByNick.IsSuccess)
                {
                    return RetrieveEntityResult<IUser>.FromSuccess(bestGuildUserByNick.Entity);
                }
            }

            return RetrieveEntityResult<IUser>.FromError("User not found.");
        }
    }
}
