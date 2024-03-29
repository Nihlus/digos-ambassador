//
//  UserWarning.cs
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

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Moderation.Model.Bases;
using JetBrains.Annotations;
using Remora.Rest.Core;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Moderation.Model;

/// <summary>
/// Represents a warning attached to a user.
/// </summary>
[Table("UserWarnings", Schema = "ModerationModule")]
[PublicAPI]
public class UserWarning : AuthoredUserEntity
{
    /// <summary>
    /// Gets the reason for the warning.
    /// </summary>
    public string Reason { get; internal set; } = null!;

    /// <summary>
    /// Gets the message that caused the warning, if any.
    /// </summary>
    public Snowflake? MessageID { get; internal set; }

    /// <summary>
    /// Gets the time at which the note was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; internal set; }

    /// <summary>
    /// Gets the time at which the warning expires.
    /// </summary>
    public DateTimeOffset? ExpiresOn { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserWarning"/> class.
    /// </summary>
    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized", Justification = "Initialized by EF Core.")]
    protected UserWarning()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserWarning"/> class.
    /// </summary>
    /// <param name="server">The server that the warning was created on.</param>
    /// <param name="user">The user that the warning is attached to.</param>
    /// <param name="author">The user that created the warning.</param>
    /// <param name="reason">The reason for the warning.</param>
    /// <param name="messageID">The message that caused the warning, if any.</param>
    /// <param name="expiresOn">The time at which the ban expires.</param>
    public UserWarning
    (
        Server server,
        User user,
        User author,
        string reason,
        Snowflake? messageID = null,
        DateTimeOffset? expiresOn = null
    )
        : base(server, user, author)
    {
        this.Reason = reason;
        this.MessageID = messageID;

        this.UpdatedAt = DateTimeOffset.UtcNow;
        this.ExpiresOn = expiresOn;
    }

    /// <summary>
    /// Notifies the entity that it has been updated, updating its timestamp.
    /// </summary>
    public void NotifyUpdate()
    {
        this.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
