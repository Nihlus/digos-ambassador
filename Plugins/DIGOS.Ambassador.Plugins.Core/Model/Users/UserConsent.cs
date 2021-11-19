//
//  UserConsent.cs
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

using System.ComponentModel.DataAnnotations.Schema;
using DIGOS.Ambassador.Core.Database.Entities;
using Remora.Discord.Core;

namespace DIGOS.Ambassador.Plugins.Core.Model.Users;

/// <summary>
/// Holds information about whether or not a user has granted consent to store user data.
/// </summary>
[Table("UserConsents", Schema = "Core")]
public class UserConsent : EFEntity
{
    /// <summary>
    /// Gets the Discord ID of the user.
    /// </summary>
    public Snowflake DiscordID { get; private set; }

    /// <summary>
    /// Gets a value indicating whether or not the user has consented.
    /// </summary>
    public bool HasConsented { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserConsent"/> class.
    /// </summary>
    /// <param name="discordID">The Discord ID of the user.</param>
    public UserConsent(Snowflake discordID)
    {
        this.DiscordID = discordID;
    }
}
