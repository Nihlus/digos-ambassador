//
//  UserNote.cs
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

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Moderation.Model;

/// <summary>
/// Represents a short note about a user.
/// </summary>
[Table("UserNotes", Schema = "ModerationModule")]
[PublicAPI]
public class UserNote : AuthoredUserEntity
{
    /// <summary>
    /// Gets the content of the note.
    /// </summary>
    public string Content { get; internal set; } = null!;

    /// <summary>
    /// Gets the time at which the note was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserNote"/> class.
    /// </summary>
    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized", Justification = "Initialized by EF Core.")]
    protected UserNote()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserNote"/> class.
    /// </summary>
    /// <param name="server">The server that the note was created on.</param>
    /// <param name="user">The user that the note is attached to.</param>
    /// <param name="author">The user that created the note.</param>
    /// <param name="content">The content of the note.</param>
    public UserNote
    (
        Server server,
        User user,
        User author,
        string content
    )
        : base(server, user, author)
    {
        this.Content = content;

        this.UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Notifies the entity that it has been updated, updating its timestamp.
    /// </summary>
    public void NotifyUpdate()
    {
        this.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
