//
//  AdminCommands.cs
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
using DIGOS.Ambassador.Modules.Base;
using DIGOS.Ambassador.Services;

using Discord.Commands;

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Modules
{
    /// <summary>
    /// Administrative commands that directly affect the bot on a global scale.
    /// </summary>
    [UsedImplicitly]
    [Group("admin")]
    [Summary("Administrative commands that directly affect the bot on a global scale.")]
    public class AdminCommands : DatabaseModuleBase
    {
        private readonly UserFeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminCommands"/> class.
        /// </summary>
        /// <param name="database">A database context from the context pool.</param>
        /// <param name="feedback">The user feedback service.</param>
        public AdminCommands(AmbyDatabaseContext database, UserFeedbackService feedback)
            : base(database)
        {
            _feedback = feedback;
        }

        /// <summary>
        /// Wipes the database, resetting it to its initial state.
        /// </summary>
        [UsedImplicitly]
        [Alias("wipe-db", "reset-db")]
        [Command("wipe-db")]
        [Summary("Wipes the database, resetting it to its initial state.")]
        [RequireContext(ContextType.DM)]
        [RequireOwner]
        public async Task ResetDatabaseAsync()
        {
            await this.Database.Database.EnsureDeletedAsync();
            await this.Database.Database.MigrateAsync();

            await this.Database.SaveChangesAsync();
            await _feedback.SendConfirmationAsync(this.Context, "Database reset.");
        }
    }
}
