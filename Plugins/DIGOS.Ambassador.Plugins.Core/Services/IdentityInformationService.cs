//
//  IdentityInformationService.cs
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

using Remora.Discord.Core;

namespace DIGOS.Ambassador.Plugins.Core.Services
{
    /// <summary>
    /// Contains identity information about the bot itself.
    /// </summary>
    public class IdentityInformationService
    {
        /// <summary>
        /// Gets the ID of the bot.
        /// </summary>
        public Snowflake ID { get; internal set; }

        /// <summary>
        /// Gets the application ID of the bot.
        /// </summary>
        public Snowflake ApplicationID { get; internal set; }

        /// <summary>
        /// Gets the ID of the bot's owner.
        /// </summary>
        public Snowflake OwnerID { get; internal set; }
    }
}
