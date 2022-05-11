//
//  ServerRoleplaySettings.cs
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
using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using Remora.Rest.Core;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Roleplaying.Model;

/// <summary>
/// Represents server-specific settings related to the roleplaying module.
/// </summary>
[Table("ServerSettings", Schema = "RoleplayModule")]
public class ServerRoleplaySettings : EFEntity
{
    /// <summary>
    /// Gets the server the settings are relevant for.
    /// </summary>
    public virtual Server Server { get; private set; } = null!;

    /// <summary>
    /// Gets the channel category generated roleplay channels should be created under.
    /// </summary>
    public Snowflake? DedicatedRoleplayChannelsCategory { get; internal set; }

    /// <summary>
    /// Gets the channel that archived roleplays are exported to.
    /// </summary>
    public Snowflake? ArchiveChannel { get; internal set; }

    /// <summary>
    /// Gets the default user role; that is, the role that all valid users on the server should have. Typically,
    /// this is the @everyone role, but certain servers use that as a very restricted role, and give users their
    /// default role after some condition is met. This allows those servers to override the standard @everyone role
    /// used for dynamic roleplay channels.
    /// </summary>
    public Snowflake? DefaultUserRole { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerRoleplaySettings"/> class.
    /// </summary>
    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    protected ServerRoleplaySettings()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerRoleplaySettings"/> class.
    /// </summary>
    /// <param name="server">The server that the settings are bound to.</param>
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor", Justification = "Required by EF Core.")]
    public ServerRoleplaySettings(Server server)
    {
        this.Server = server;
    }
}
