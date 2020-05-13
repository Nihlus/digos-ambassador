//
//  AutoroleAssignmentService.cs
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
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Plugins.Autorole.Model;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Autorole.Services
{
    /// <summary>
    /// Handles business logic for assignment of autoroles.
    /// </summary>
    public class AutoroleAssignmentService
    {
        private readonly DiscordSocketClient _discordClient;
        private readonly AutoroleService _autoroles;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoroleAssignmentService"/> class.
        /// </summary>
        /// <param name="discordClient">The Discord client.</param>
        /// <param name="autoroles">The autorole service.</param>
        public AutoroleAssignmentService
        (
            DiscordSocketClient discordClient,
            AutoroleService autoroles
        )
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
        public async Task<ModifyEntityResult> UpdateAutoroleAsync(AutoroleConfiguration autorole)
        {
            var guild = _discordClient.GetGuild((ulong)autorole.Server.DiscordID);
            if (guild is null)
            {
                return ModifyEntityResult.FromError
                (
                    "Failed to get a valid guild for the given autorole. The bot may no longer be in the guild."
                );
            }

            // Ensure we have all users available to us
            if (guild.MemberCount != guild.Users.Count)
            {
                await guild.DownloadUsersAsync();
            }

            var didAnyUpdateFail = false;
            foreach (var user in guild.Users)
            {
                var updateUser = await UpdateAutoroleForUserAsync(autorole, user);
                if (!updateUser.IsSuccess)
                {
                    didAnyUpdateFail = true;
                }
            }

            return didAnyUpdateFail
                ? ModifyEntityResult.FromError("One or more autorole updates failed.")
                : ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Applies the given autorole to the given user, if it is applicable. If the user no longer qualifies,
        /// the autorole is removed.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="guildUser">The user.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> UpdateAutoroleForUserAsync
        (
            AutoroleConfiguration autorole, IGuildUser guildUser
        )
        {
            var role = guildUser.Guild.GetRole((ulong)autorole.DiscordRoleID);
            if (role is null)
            {
                return ModifyEntityResult.FromError("The relevant role could not be found. Deleted?");
            }

            var userHasRole = guildUser.RoleIds.Contains(role.Id);
            var isUserQualified = await _autoroles.IsUserQualifiedForAutoroleAsync(autorole, guildUser);

            if (isUserQualified && userHasRole)
            {
                return ModifyEntityResult.FromSuccess();
            }

            if (!isUserQualified && userHasRole)
            {
                try
                {
                    await guildUser.RemoveRoleAsync(role);
                }
                catch (HttpException hex) when (hex.WasCausedByMissingPermission())
                {
                    return ModifyEntityResult.FromError("No permission to change the user's role.");
                }
            }

            // At this point, the user doesn't have the role, and either is or is not qualified.
            if (!isUserQualified)
            {
                // We consider a no-op for an unqualified user a success.
                return ModifyEntityResult.FromSuccess();
            }

            try
            {
                if (autorole.RequiresConfirmation)
                {
                    var getConfirmation = await _autoroles.GetOrCreateAutoroleConfirmationAsync(autorole, guildUser);
                    if (!getConfirmation.IsSuccess)
                    {
                        return ModifyEntityResult.FromError("Couldn't get a valid confirmation entry for the user.");
                    }

                    var confirmation = getConfirmation.Entity;
                    if (!confirmation.IsConfirmed)
                    {
                        // We consider a no-op for an qualified but not affirmed user a success.
                        return ModifyEntityResult.FromSuccess();
                    }
                }

                await guildUser.AddRoleAsync(role);
            }
            catch (HttpException hex) when (hex.WasCausedByMissingPermission())
            {
                return ModifyEntityResult.FromError("No permission to change the user's role.");
            }

            return ModifyEntityResult.FromSuccess();
        }
    }
}
