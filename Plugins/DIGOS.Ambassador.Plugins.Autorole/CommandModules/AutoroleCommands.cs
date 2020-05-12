//
//  AutoroleCommands.cs
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
using Discord;
using Discord.Commands;
using JetBrains.Annotations;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Autorole.CommandModules
{
    /// <summary>
    /// Commands for creating, using, and interacting with autoroles.
    /// </summary>
    [UsedImplicitly]
    [Alias("autorole", "at")]
    [Group("autorole")]
    [Summary("Commands for creating, editing, and interacting with automatic roles.")]
    public partial class AutoroleCommands : ModuleBase
    {
        /// <summary>
        /// Creates a new autorole configuration for the given Discord role.
        /// </summary>
        /// <param name="discordRole">The discord role.</param>
        [UsedImplicitly]
        [Alias("create")]
        [Command("create")]
        [Summary("Creates a new autorole configuration for the given Discord role.")]
        public async Task CreateAutoroleAsync(IRole discordRole)
        {
        }

        /// <summary>
        /// Deletes an existing autorole configuration for the given Discord role.
        /// </summary>
        /// <param name="discordRole">The discord role.</param>
        [UsedImplicitly]
        [Alias("delete")]
        [Command("delete")]
        [Summary("Deletes an existing autorole configuration for the given Discord role.")]
        public async Task DeleteAutoroleAsync(IRole discordRole)
        {
        }

        /// <summary>
        /// Enables the given autorole, allowing it to be added to users.
        /// </summary>
        /// <param name="discordRole">The discord role.</param>
        [UsedImplicitly]
        [Alias("enable")]
        [Command("enable")]
        [Summary("Enables the given autorole, allowing it to be added to users.")]
        public async Task EnableAutoroleAsync(IRole discordRole)
        {
        }

        /// <summary>
        /// Disables the given autorole, preventing it from being added to users.
        /// </summary>
        /// <param name="discordRole">The discord role.</param>
        [UsedImplicitly]
        [Alias("disable")]
        [Command("disable")]
        [Summary("Disables the given autorole, preventing it from being added to users.")]
        public async Task DisableAutoroleAsync(IRole discordRole)
        {
        }
    }
}
