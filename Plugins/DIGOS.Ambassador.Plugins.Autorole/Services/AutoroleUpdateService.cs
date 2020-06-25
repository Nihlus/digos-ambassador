//
//  AutoroleUpdateService.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Services.TransientState;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Plugins.Autorole.Model;
using DIGOS.Ambassador.Plugins.Autorole.Results;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using static DIGOS.Ambassador.Plugins.Autorole.Results.AutoroleUpdateStatus;

namespace DIGOS.Ambassador.Plugins.Autorole.Services
{
    /// <summary>
    /// Handles business logic for updating of autoroles.
    /// </summary>
    public class AutoroleUpdateService : AbstractTransientStateService
    {
        private readonly DiscordSocketClient _discordClient;
        private readonly AutoroleService _autoroles;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoroleUpdateService"/> class.
        /// </summary>
        /// <param name="discordClient">The Discord client.</param>
        /// <param name="autoroles">The autorole service.</param>
        /// <param name="log">The logging instance.</param>
        public AutoroleUpdateService
        (
            DiscordSocketClient discordClient,
            AutoroleService autoroles,
            ILogger<AbstractTransientStateService> log
        )
            : base(log, autoroles)
        {
            _discordClient = discordClient;
            _autoroles = autoroles;
        }

        /// <summary>
        /// Checks the status of an autorole, applying it to any users which qualify. If any user no longer qualifies,
        /// the autorole is removed from that user.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <returns>
        /// A modification result which may or may not have succeeded. If any individual autorole update fails, this
        /// result will also indicate failure. That does not mean that *all* updates failed, however.
        /// </returns>
        public async IAsyncEnumerable<AutoroleUpdateResult> UpdateAutoroleAsync(AutoroleConfiguration autorole)
        {
            var guild = _discordClient.GetGuild((ulong)autorole.Server.DiscordID);
            if (guild is null)
            {
                yield break;
            }

            // Ensure we have all users available to us
            if (guild.MemberCount != guild.Users.Count)
            {
                await guild.DownloadUsersAsync();
            }

            foreach (var user in guild.Users)
            {
                yield return await UpdateAutoroleForUserAsync(autorole, user);
            }
        }

        /// <summary>
        /// Applies the given autorole to the given user, if it is applicable. If the user no longer qualifies,
        /// the autorole is removed.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="guildUser">The user.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<AutoroleUpdateResult> UpdateAutoroleForUserAsync
        (
            AutoroleConfiguration autorole, IGuildUser guildUser
        )
        {
            if (!autorole.IsEnabled)
            {
                return Disabled;
            }

            if (!autorole.Conditions.Any())
            {
                return Unconditional;
            }

            var role = guildUser.Guild.GetRole((ulong)autorole.DiscordRoleID);
            if (role is null)
            {
                return AutoroleUpdateResult.FromError("The relevant role could not be found. Deleted?");
            }

            var userHasRole = guildUser.RoleIds.Contains(role.Id);
            var getIsUserQualified = await _autoroles.IsUserQualifiedForAutoroleAsync(autorole, guildUser);
            if (!getIsUserQualified.IsSuccess)
            {
                return AutoroleUpdateResult.FromError(getIsUserQualified);
            }

            var isUserQualified = getIsUserQualified.Entity;

            if (isUserQualified && userHasRole)
            {
                return Unchanged;
            }

            if (!isUserQualified && userHasRole)
            {
                try
                {
                    await guildUser.RemoveRoleAsync(role);

                    var getConfirmation = await _autoroles.GetOrCreateAutoroleConfirmationAsync(autorole, guildUser);
                    if (!getConfirmation.IsSuccess)
                    {
                        return Removed;
                    }

                    // Remove any existing affirmation
                    var confirmation = getConfirmation.Entity;
                    var removeConfirmation = await _autoroles.RemoveAutoroleConfirmationAsync(confirmation);
                    if (!removeConfirmation.IsSuccess)
                    {
                        return AutoroleUpdateResult.FromError(removeConfirmation);
                    }

                    return Removed;
                }
                catch (HttpException hex) when (hex.WasCausedByMissingPermission())
                {
                    return AutoroleUpdateResult.FromError("No permission to change the user's role.");
                }
            }

            // At this point, the user doesn't have the role, and either is or is not qualified.
            if (!isUserQualified)
            {
                // We consider a no-op for an unqualified user a success.
                return Unqualified;
            }

            try
            {
                if (autorole.RequiresConfirmation)
                {
                    var getConfirmation = await _autoroles.GetOrCreateAutoroleConfirmationAsync(autorole, guildUser);
                    if (!getConfirmation.IsSuccess)
                    {
                        return AutoroleUpdateResult.FromError("Couldn't get a valid confirmation entry for the user.");
                    }

                    var confirmation = getConfirmation.Entity;
                    if (!confirmation.IsConfirmed)
                    {
                        // We consider a no-op for an qualified but not affirmed user a success.
                        return RequiresAffirmation;
                    }
                }

                await guildUser.AddRoleAsync(role);
            }
            catch (HttpException hex) when (hex.WasCausedByMissingPermission())
            {
                return AutoroleUpdateResult.FromError("No permission to change the user's role.");
            }

            return Applied;
        }
    }
}
