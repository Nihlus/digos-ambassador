//
//  AutoroleUpdateService.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Autorole.Model;
using DIGOS.Ambassador.Plugins.Autorole.Results;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;
using static DIGOS.Ambassador.Plugins.Autorole.Results.AutoroleUpdateStatus;

namespace DIGOS.Ambassador.Plugins.Autorole.Services;

/// <summary>
/// Handles business logic for updating of autoroles.
/// </summary>
public class AutoroleUpdateService
{
    private readonly AutoroleService _autoroles;
    private readonly IDiscordRestGuildAPI _guildAPI;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoroleUpdateService"/> class.
    /// </summary>
    /// <param name="autoroles">The autorole service.</param>
    /// <param name="guildAPI">The guild API.</param>
    public AutoroleUpdateService
    (
        AutoroleService autoroles,
        IDiscordRestGuildAPI guildAPI
    )
    {
        _autoroles = autoroles;
        _guildAPI = guildAPI;
    }

    /// <summary>
    /// Applies the given autorole to the given user, if it is applicable. If the user no longer qualifies,
    /// the autorole is removed.
    /// </summary>
    /// <param name="autorole">The autorole.</param>
    /// <param name="guildID">The ID of the guild the user is on.</param>
    /// <param name="userID">The ID of the user.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result<AutoroleUpdateStatus>> UpdateAutoroleForUserAsync
    (
        AutoroleConfiguration autorole,
        Snowflake guildID,
        Snowflake userID,
        CancellationToken ct = default
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

        var getRoles = await _guildAPI.GetGuildRolesAsync(guildID, ct);
        if (!getRoles.IsSuccess)
        {
            return Result<AutoroleUpdateStatus>.FromError(getRoles);
        }

        var roles = getRoles.Entity;

        if (roles.All(r => r.ID != autorole.DiscordRoleID))
        {
            // If the role can't be found any longer, we disable it
            var disableAutoroleAsync = await _autoroles.DisableAutoroleAsync(autorole, ct);

            return !disableAutoroleAsync.IsSuccess
                ? Result<AutoroleUpdateStatus>.FromError(disableAutoroleAsync)
                : Disabled;
        }

        var getIsUserQualified = await _autoroles.IsUserQualifiedForAutoroleAsync(autorole, userID, ct);
        if (!getIsUserQualified.IsSuccess)
        {
            return Result<AutoroleUpdateStatus>.FromError(getIsUserQualified);
        }

        var isUserQualified = getIsUserQualified.Entity;

        var getMember = await _guildAPI.GetGuildMemberAsync(guildID, userID, ct);
        if (!getMember.IsSuccess)
        {
            return Result<AutoroleUpdateStatus>.FromError(getMember);
        }

        var member = getMember.Entity;

        if (!member.User.IsDefined(out var user))
        {
            return Unqualified;
        }

        if (user.IsBot.IsDefined(out var isBot) && isBot)
        {
            return Unqualified;
        }

        var userHasRole = member.Roles.Contains(autorole.DiscordRoleID);

        switch (isUserQualified)
        {
            case true when userHasRole:
            {
                return Unchanged;
            }
            case false when userHasRole:
            {
                var removeRole = await _guildAPI.RemoveGuildMemberRoleAsync
                (
                    guildID,
                    userID,
                    autorole.DiscordRoleID,
                    ct: ct
                );

                if (!removeRole.IsSuccess)
                {
                    return Result<AutoroleUpdateStatus>.FromError(removeRole);
                }

                var getConfirmation = await _autoroles.GetOrCreateAutoroleConfirmationAsync
                (
                    autorole,
                    userID,
                    ct
                );

                if (!getConfirmation.IsSuccess)
                {
                    return Removed;
                }

                // Remove any existing affirmation
                var confirmation = getConfirmation.Entity;
                var removeConfirmation = await _autoroles.RemoveAutoroleConfirmationAsync(confirmation, ct);

                return !removeConfirmation.IsSuccess
                    ? Result<AutoroleUpdateStatus>.FromError(removeConfirmation)
                    : Removed;
            }
            case false:
            {
                // At this point, the user doesn't have the role, and either is or is not qualified.
                // We consider a no-op for an unqualified user a success.
                return Unqualified;
            }
        }

        if (autorole.RequiresConfirmation)
        {
            var getConfirmation = await _autoroles.GetOrCreateAutoroleConfirmationAsync
            (
                autorole,
                userID,
                ct
            );

            if (!getConfirmation.IsSuccess)
            {
                return Result<AutoroleUpdateStatus>.FromError(getConfirmation);
            }

            var confirmation = getConfirmation.Entity;
            if (!confirmation.IsConfirmed)
            {
                // We consider a no-op for an qualified but not affirmed user a success.
                return RequiresAffirmation;
            }
        }

        var addRole = await _guildAPI.AddGuildMemberRoleAsync(guildID, userID, autorole.DiscordRoleID, ct: ct);

        return !addRole.IsSuccess
            ? Result<AutoroleUpdateStatus>.FromError(addRole)
            : Applied;
    }
}
