//
//  AutoroleConfiguration.cs
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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.Bases;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using JetBrains.Annotations;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace DIGOS.Ambassador.Plugins.Autorole.Model;

/// <summary>
/// Represents an automatic role.
/// </summary>
[Table("AutoroleConfigurations", Schema = "AutoroleModule")]
public class AutoroleConfiguration : EFEntity
{
    /// <summary>
    /// Gets the server the autorole belongs to.
    /// </summary>
    public virtual Server Server { get; private set; } = null!;

    /// <summary>
    /// Gets the ID of the Discord role.
    /// </summary>
    public Snowflake DiscordRoleID { get; private set; }

    /// <summary>
    /// Gets the conditions for acquiring the role.
    /// </summary>
    public virtual List<AutoroleCondition> Conditions { get; private set; } = null!;

    /// <summary>
    /// Gets a value indicating whether the role needs external confirmation (from a moderator, for example)
    /// to be applied after all conditions are met.
    /// </summary>
    public bool RequiresConfirmation { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether the role is currently enabled; that is, whether new users meeting the
    /// conditions will have the role applied.
    /// </summary>
    public bool IsEnabled { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoroleConfiguration"/> class.
    /// </summary>
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized", Justification = "Initialized by EF Core.")]
    [UsedImplicitly]
    protected AutoroleConfiguration()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoroleConfiguration"/> class.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="discordRoleID">The ID of the discord role to assign.</param>
    protected AutoroleConfiguration(Server server, Snowflake discordRoleID)
    {
        this.Server = server;
        this.DiscordRoleID = discordRoleID;
        this.Conditions = new List<AutoroleCondition>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoroleConfiguration"/> class.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="discordRole">The Discord role.</param>
    public AutoroleConfiguration(Server server, IRole discordRole)
        : this(server, discordRole.ID)
    {
    }
}
