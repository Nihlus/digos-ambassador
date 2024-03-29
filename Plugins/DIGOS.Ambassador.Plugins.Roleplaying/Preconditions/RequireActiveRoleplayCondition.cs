﻿//
//  RequireActiveRoleplayCondition.cs
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

using System;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using Remora.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Preconditions;

/// <summary>
/// Restricts the usage of a command to the owner of the currently active roleplay. Furthermore, it also requires a
/// roleplay to be current.
/// </summary>
public class RequireActiveRoleplayCondition : ICondition<RequireActiveRoleplayAttribute>
{
    private readonly IOperationContext _context;
    private readonly RoleplayDiscordService _roleplayService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireActiveRoleplayCondition"/> class.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="roleplayService">The roleplay service.</param>
    public RequireActiveRoleplayCondition(IOperationContext context, RoleplayDiscordService roleplayService)
    {
        _context = context;
        _roleplayService = roleplayService;
    }

    /// <inheritdoc />
    public async ValueTask<Result> CheckAsync(RequireActiveRoleplayAttribute attribute, CancellationToken ct = default)
    {
        if (!_context.TryGetChannelID(out var channelID))
        {
            throw new InvalidOperationException();
        }

        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var result = await _roleplayService.GetActiveRoleplayAsync(channelID);
        if (!result.IsSuccess)
        {
            return Result.FromError(result);
        }

        if (!attribute.RequireOwner)
        {
            return Result.FromSuccess();
        }

        var roleplay = result.Entity;
        return roleplay.Owner.DiscordID != userID
            ? new UserError("Only the roleplay owner can do that.")
            : Result.FromSuccess();
    }
}
