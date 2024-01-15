//
//  DroneCommands.cs
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
using System.ComponentModel;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Plugins.Drone.Extensions;
using DIGOS.Ambassador.Plugins.Drone.Services;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Drone.CommandModules;

/// <summary>
/// Contains droning commands.
/// </summary>
[Description("Contains some commands to perform actions with drones.")]
public class DroneCommands : CommandGroup
{
    private readonly Snowflake _ambyID = new Snowflake(135347310845624320ul);
    private readonly ContentService _content;
    private readonly DroneService _drone;
    private readonly FeedbackService _feedback;
    private readonly ICommandContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="DroneCommands"/> class.
    /// </summary>
    /// <param name="drone">The drone service.</param>
    /// <param name="feedback">The feedback service.</param>
    /// <param name="content">The content service.</param>
    /// <param name="context">The command context.</param>
    public DroneCommands
    (
        DroneService drone,
        FeedbackService feedback,
        ContentService content,
        ICommandContext context
    )
    {
        _drone = drone;
        _feedback = feedback;
        _content = content;
        _context = context;
    }

    /// <summary>
    /// Drones the target user... or does it? In reality, this is a turn-the-tables command that drones the invoker
    /// instead.
    /// </summary>
    /// <param name="member">The user to drone.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [UsedImplicitly]
    [Command("drone")]
    [RequireContext(ChannelContext.Guild)]
    [Description("Drones the target user.")]
    public async Task<Result<FeedbackMessage>> DroneAsync(IGuildMember member)
    {
        if (!_context.TryGetGuildID(out var guildID))
        {
            throw new InvalidOperationException();
        }

        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        if (!member.User.IsDefined(out var user))
        {
            throw new InvalidOperationException();
        }

        var isAmbyInvoking = userID == _ambyID;
        var droneMessage = user.ID == userID
            ? _content.GetRandomSelfDroneMessage()
            : isAmbyInvoking
                ? _content.GetRandomTargetedMessage()
                : _content.GetRandomTurnTheTablesMessage();

        var sendMessage = await _feedback.SendContextualNeutralAsync
        (
            droneMessage,
            userID,
            ct: this.CancellationToken
        );

        if (!sendMessage.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(sendMessage);
        }

        var targetUser = isAmbyInvoking
            ? user.ID
            : userID;

        var droneResult = await _drone.DroneUserAsync
        (
            guildID,
            targetUser,
            this.CancellationToken
        );

        return !droneResult.IsSuccess
            ? Result<FeedbackMessage>.FromError(droneResult)
            : new FeedbackMessage(_content.GetRandomConfirmationMessage(), _feedback.Theme.Secondary);
    }
}
