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

using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Drone.Extensions;
using DIGOS.Ambassador.Plugins.Drone.Services;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Drone.CommandModules
{
    /// <summary>
    /// Contains droning commands.
    /// </summary>
    [Name("drone")]
    [Summary("Contains some commands to perform actions with drones.")]
    public class DroneCommands : ModuleBase
    {
        private readonly ContentService _content;
        private readonly DroneService _drone;
        private readonly UserFeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="DroneCommands"/> class.
        /// </summary>
        /// <param name="drone">The drone service.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="content">The content service.</param>
        public DroneCommands(DroneService drone, UserFeedbackService feedback, ContentService content)
        {
            _drone = drone;
            _feedback = feedback;
            _content = content;
        }

        /// <summary>
        /// Drones the target user... or does it? In reality, this is a turn-the-tables command that drones the invoker
        /// instead.
        /// </summary>
        /// <param name="user">The user to drone.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [UsedImplicitly]
        [Command("drone")]
        [RequireContext(ContextType.Guild)]
        [RequireNsfw]
        [Summary("Drones the target user.")]
        public async Task DroneAsync(IGuildUser user)
        {
            if (!(this.Context.User is IGuildUser target))
            {
                await _feedback.SendErrorAsync(this.Context, "The target user wasn't a guild user.");
                return;
            }

            var droneMessage = user == target
                ? _content.GetRandomSelfDroneMessage()
                : _content.GetRandomTurnTheTablesMessage();

            await _feedback.SendConfirmationAsync(this.Context, droneMessage);

            var droneResult = await _drone.DroneUserAsync(target);
            if (!droneResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, droneResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, _content.GetRandomConfirmationMessage());
            _drone.SaveChanges();
        }
    }
}
