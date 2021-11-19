//
//  User.cs
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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DIGOS.Ambassador.Core.Database.Entities;
using Remora.Discord.Core;

namespace DIGOS.Ambassador.Plugins.Core.Model.Users;

/// <summary>
/// Represents globally accessible information about a user.
/// </summary>
[Table("Users", Schema = "Core")]
public class User : EFEntity
{
    /// <summary>
    /// Gets the Discord ID of the user.
    /// </summary>
    public Snowflake DiscordID { get; private set; }

    /// <summary>
    /// Gets the biography of the user. This contains useful information that the users provide themselves.
    /// </summary>
    public string Bio { get; internal set; }

    /// <summary>
    /// Gets the current timezone of the user. This is an hour offset ( + or - ) to UTC/GMT.
    /// </summary>
    public int? Timezone { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="discordID">The Discord ID of the user.</param>
    public User(Snowflake discordID)
    {
        this.DiscordID = discordID;
        this.Bio = string.Empty;
    }
}
