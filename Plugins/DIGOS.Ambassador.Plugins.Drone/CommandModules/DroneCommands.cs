//
//  DroneCommands.cs
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
using System.ComponentModel;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Feedback.Results;
using DIGOS.Ambassador.Plugins.Drone.Extensions;
using DIGOS.Ambassador.Plugins.Drone.Services;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Drone.CommandModules
{
    /// <summary>
    /// Contains droning commands.
    /// </summary>
    [Description("Contains some commands to perform actions with drones.")]
    public class DroneCommands : CommandGroup
    {
        private readonly ContentService _content;
        private readonly DroneService _drone;
        private readonly UserFeedbackService _feedback;
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
            UserFeedbackService feedback,
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
        public async Task<Result<UserMessage>> DroneAsync(IGuildMember member)
        {
            if (!member.User.HasValue)
            {
                throw new InvalidOperationException();
            }

            var droneMessage = member.User.Value.ID == _context.User.ID
                ? _content.GetRandomSelfDroneMessage()
                : _content.GetRandomTurnTheTablesMessage();

            var sendMessage = await _feedback.SendConfirmationAsync
            (
                _context.ChannelID,
                _context.User.ID,
                droneMessage,
                this.CancellationToken
            );

            if (!sendMessage.IsSuccess)
            {
                return Result<UserMessage>.FromError(sendMessage);
            }

            var droneResult = await _drone.DroneUserAsync
            (
                _context.GuildID.Value,
                _context.User.ID,
                this.CancellationToken
            );

            return !droneResult.IsSuccess
                ? Result<UserMessage>.FromError(droneResult)
                : new ConfirmationMessage(_content.GetRandomConfirmationMessage());
        }
    }
}
