//
//  ReactionConditionResponder.cs
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
using System.Transactions;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Core;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Autorole.Responders
{
    /// <summary>
    /// Responds to message reactions, updating relevant autoroles.
    /// </summary>
    public class ReactionConditionResponder :
        IResponder<IMessageReactionAdd>,
        IResponder<IMessageReactionRemove>,
        IResponder<IMessageReactionRemoveAll>,
        IResponder<IMessageReactionRemoveEmoji>
    {
        private readonly AutoroleService _autoroles;
        private readonly AutoroleUpdateService _autoroleUpdates;
        private readonly IDiscordRestGuildAPI _guildAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReactionConditionResponder"/> class.
        /// </summary>
        /// <param name="autoroles">The autorole service.</param>
        /// <param name="autoroleUpdates">The autorole update service.</param>
        /// <param name="guildAPI">The guild API.</param>
        public ReactionConditionResponder
        (
            AutoroleService autoroles,
            AutoroleUpdateService autoroleUpdates,
            IDiscordRestGuildAPI guildAPI
        )
        {
            _autoroles = autoroles;
            _autoroleUpdates = autoroleUpdates;
            _guildAPI = guildAPI;
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IMessageReactionAdd gatewayEvent, CancellationToken ct = default)
        {
            if (!gatewayEvent.GuildID.HasValue)
            {
                return Result.FromSuccess();
            }

            var guild = gatewayEvent.GuildID.Value;
            var autoroles = await _autoroles.GetAutorolesAsync
            (
                guild,
                q => q
                    .Where(a => a.IsEnabled)
                    .Where
                    (
                        a => a.Conditions.Any
                        (
                            c =>
                                c is ReactionCondition &&
                                ((ReactionCondition)c).MessageID == gatewayEvent.MessageID &&
                                ((ReactionCondition)c).ChannelID == gatewayEvent.ChannelID &&
                                ((ReactionCondition)c).EmoteName == gatewayEvent.Emoji.Name
                        )
                    ),
                ct
            );

            var user = gatewayEvent.UserID;
            foreach (var autorole in autoroles)
            {
                using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                var updateAutorole = await _autoroleUpdates.UpdateAutoroleForUserAsync(autorole, guild, user, ct);
                if (!updateAutorole.IsSuccess)
                {
                    return Result.FromError(updateAutorole);
                }

                transaction.Complete();
            }

            return Result.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IMessageReactionRemove gatewayEvent, CancellationToken ct = default)
        {
            if (!gatewayEvent.GuildID.HasValue)
            {
                return Result.FromSuccess();
            }

            var guild = gatewayEvent.GuildID.Value;
            var autoroles = await _autoroles.GetAutorolesAsync
            (
                guild,
                q => q
                    .Where(a => a.IsEnabled)
                    .Where
                    (
                        a => a.Conditions.Any
                        (
                            c =>
                                c is ReactionCondition &&
                                ((ReactionCondition)c).MessageID == gatewayEvent.MessageID &&
                                ((ReactionCondition)c).ChannelID == gatewayEvent.ChannelID &&
                                ((ReactionCondition)c).EmoteName == gatewayEvent.Emoji.Name
                        )
                    ),
                ct
            );

            var user = gatewayEvent.UserID;
            foreach (var autorole in autoroles)
            {
                using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                var updateAutorole = await _autoroleUpdates.UpdateAutoroleForUserAsync(autorole, guild, user, ct);
                if (!updateAutorole.IsSuccess)
                {
                    return Result.FromError(updateAutorole);
                }

                transaction.Complete();
            }

            return Result.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IMessageReactionRemoveAll gatewayEvent, CancellationToken ct = default)
        {
            if (!gatewayEvent.GuildID.HasValue)
            {
                return Result.FromSuccess();
            }

            var guild = gatewayEvent.GuildID.Value;
            var autoroles = await _autoroles.GetAutorolesAsync
            (
                guild,
                q => q
                    .Where(a => a.IsEnabled)
                    .Where
                    (
                        a => a.Conditions.Any
                        (
                            c =>
                                c is ReactionCondition &&
                                ((ReactionCondition)c).MessageID == gatewayEvent.MessageID &&
                                ((ReactionCondition)c).ChannelID == gatewayEvent.ChannelID
                        )
                    ),
                ct
            );

            var users = new List<Snowflake>();
            Optional<Snowflake> after = default;
            while (true)
            {
                var listMembers = await _guildAPI.ListGuildMembersAsync(guild, after: after, ct: ct);
                if (!listMembers.IsSuccess)
                {
                    return Result.FromError(listMembers);
                }

                var members = listMembers.Entity;
                if (members.Count == 0)
                {
                    break;
                }

                users.AddRange(listMembers.Entity.Select(m => m.User.Value!.ID));
                after = users.Last();
            }

            foreach (var autorole in autoroles)
            {
                foreach (var user in users)
                {
                    using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                    var updateAutorole = await _autoroleUpdates.UpdateAutoroleForUserAsync(autorole, guild, user, ct);
                    if (!updateAutorole.IsSuccess)
                    {
                        return Result.FromError(updateAutorole);
                    }

                    transaction.Complete();
                }
            }

            return Result.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IMessageReactionRemoveEmoji gatewayEvent, CancellationToken ct = default)
        {
            if (!gatewayEvent.GuildID.HasValue)
            {
                return Result.FromSuccess();
            }

            var guild = gatewayEvent.GuildID.Value;
            var autoroles = await _autoroles.GetAutorolesAsync
            (
                guild,
                q => q
                    .Where(a => a.IsEnabled)
                    .Where
                    (
                        a => a.Conditions.Any
                        (
                            c =>
                                c is ReactionCondition &&
                                ((ReactionCondition)c).MessageID == gatewayEvent.MessageID &&
                                ((ReactionCondition)c).ChannelID == gatewayEvent.ChannelID &&
                                ((ReactionCondition)c).EmoteName == gatewayEvent.Emoji.Name
                        )
                    ),
                ct
            );

            var users = new List<Snowflake>();
            Optional<Snowflake> after = default;
            while (true)
            {
                var listMembers = await _guildAPI.ListGuildMembersAsync(guild, after: after, ct: ct);
                if (!listMembers.IsSuccess)
                {
                    return Result.FromError(listMembers);
                }

                var members = listMembers.Entity;
                if (members.Count == 0)
                {
                    break;
                }

                users.AddRange(listMembers.Entity.Select(m => m.User.Value!.ID));
                after = users.Last();
            }

            foreach (var autorole in autoroles)
            {
                foreach (var user in users)
                {
                    using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                    var updateAutorole = await _autoroleUpdates.UpdateAutoroleForUserAsync(autorole, guild, user, ct);
                    if (!updateAutorole.IsSuccess)
                    {
                        return Result.FromError(updateAutorole);
                    }

                    transaction.Complete();
                }
            }

            return Result.FromSuccess();
        }
    }
}
