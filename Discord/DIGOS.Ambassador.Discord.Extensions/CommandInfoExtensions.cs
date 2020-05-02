//
//  CommandInfoExtensions.cs
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

using System.Linq;
using Discord.Commands;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Discord.Extensions
{
    /// <summary>
    /// Extensions for the <see cref="CommandInfo"/> class.
    /// </summary>
    public static class CommandInfoExtensions
    {
        /// <summary>
        /// Gets the actual name of the command, without the module prefix.
        /// </summary>
        /// <param name="this">The command.</param>
        /// <returns>The actual name.</returns>
        [Pure]
        public static string GetActualName(this CommandInfo @this)
        {
            var commandName = @this.Name;

            // HACK: override the command name if the name has capitals in it, which means it's a top-level command
            if (commandName.Any(char.IsUpper))
            {
                commandName = @this.Aliases.First();
            }

            return commandName;
        }

        /// <summary>
        /// Gets the full command used to invoke this command.
        /// </summary>
        /// <param name="this">The command.</param>
        /// <returns>The full command.</returns>
        public static string GetFullCommand(this CommandInfo @this)
        {
            var prefix = @this.Module.GetFullPrefix();

            return $"{prefix}{@this.GetActualName()}";
        }
    }
}
