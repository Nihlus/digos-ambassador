﻿//
//  Roleplay.cs
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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using JetBrains.Annotations;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Roleplaying.Model;

/// <summary>
/// Represents a saved roleplay.
/// </summary>
[Table("Roleplays", Schema = "RoleplayModule")]
[PublicAPI]
public class Roleplay : EFEntity, IOwnedNamedEntity, IServerEntity
{
    /// <inheritdoc />
    public virtual Server Server { get; private set; } = null!;

    /// <inheritdoc />
    public virtual User Owner { get; set; } = null!;

    /// <summary>
    /// Gets a value indicating whether or not the roleplay is currently active in a channel.
    /// </summary>
    public bool IsActive { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether or not the roleplay can be viewed or replayed by anyone.
    /// </summary>
    public bool IsPublic { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether or not the roleplay is NSFW.
    /// </summary>
    public bool IsNSFW { get; internal set; }

    /// <summary>
    /// Gets the ID of the channel that the roleplay is active in.
    /// </summary>
    public Snowflake? ActiveChannelID { get; internal set; }

    /// <summary>
    /// Gets the ID of the roleplay's dedicated channel.
    /// </summary>
    public Snowflake? DedicatedChannelID { get; internal set; }

    /// <summary>
    /// Gets the users that are participating in the roleplay in any way.
    /// </summary>
    public virtual List<RoleplayParticipant> ParticipatingUsers { get; private set; } = new();

    /// <inheritdoc />
    public string Name { get; internal set; } = null!;

    /// <summary>
    /// Gets the summary of the roleplay.
    /// </summary>
    public string? Summary { get; internal set; }

    /// <summary>
    /// Gets the saved messages in the roleplay.
    /// </summary>
    public virtual List<UserMessage> Messages { get; private set; } = new();

    /// <summary>
    /// Gets the last time the roleplay was updated.
    /// </summary>
    public DateTimeOffset? LastUpdated { get; internal set; }

    /// <summary>
    /// Gets the users that have joined the roleplay.
    /// </summary>
    [NotMapped]
    public IEnumerable<RoleplayParticipant> JoinedUsers =>
        this.ParticipatingUsers.Where(p => p.Status == ParticipantStatus.Joined);

    /// <summary>
    /// Gets the users that have been kicked from the roleplay.
    /// </summary>
    [NotMapped]
    public IEnumerable<RoleplayParticipant> KickedUsers =>
        this.ParticipatingUsers.Where(p => p.Status == ParticipantStatus.Kicked);

    /// <summary>
    /// Gets the users that have been invited to the roleplay.
    /// </summary>
    [NotMapped]
    public IEnumerable<RoleplayParticipant> InvitedUsers =>
        this.ParticipatingUsers.Where(p => p.Status == ParticipantStatus.Invited);

    /// <inheritdoc />
    [NotMapped]
    public string EntityTypeDisplayName => nameof(Roleplay);

    /// <summary>
    /// Initializes a new instance of the <see cref="Roleplay"/> class.
    /// </summary>
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized", Justification = "Required by EF Core.")]
    protected Roleplay()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Roleplay"/> class.
    /// </summary>
    /// <param name="server">The ID of the server the roleplay is created on.</param>
    /// <param name="owner">The owner of the roleplay.</param>
    /// <param name="name">The name of the roleplay.</param>
    /// <param name="summary">The summary of the roleplay.</param>
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor", Justification = "Required by EF Core.")]
    public Roleplay(Server server, User owner, string name, string summary)
    {
        this.Server = server;
        this.Owner = owner;
        this.Name = name;
        this.Summary = summary;
    }

    /// <summary>
    /// Gets the summary if it is set; otherwise, return <paramref name="defaultSummary"/>.
    /// </summary>
    /// <param name="defaultSummary">The default summary to use when one is not present.</param>
    /// <returns>The description.</returns>
    public string GetSummaryOrDefault(string defaultSummary = "No summary set.")
    {
        return this.Summary ?? defaultSummary;
    }

    /// <inheritdoc />
    public bool IsOwner(User user)
    {
        return IsOwner(user.DiscordID);
    }

    /// <inheritdoc />
    public bool IsOwner(Snowflake userID)
    {
        return this.Owner.DiscordID == userID;
    }

    /// <summary>
    /// Determines whether or not the given user is a participant of the roleplay.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>true if the user is a participant; otherwise, false.</returns>
    [Pure]
    public bool HasJoined(User user)
    {
        return HasJoined(user.DiscordID);
    }

    /// <summary>
    /// Determines whether or not the given user is a participant of the roleplay.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>true if the user is a participant; otherwise, false.</returns>
    [Pure]
    public bool HasJoined(IUser user)
    {
        return HasJoined(user.ID);
    }

    /// <summary>
    /// Determines whether or not the given user ID is a participant of the roleplay.
    /// </summary>
    /// <param name="userID">The ID of the user.</param>
    /// <returns>true if the user is a participant; otherwise, false.</returns>
    [Pure]
    public bool HasJoined(Snowflake userID)
    {
        return this.JoinedUsers.Any(p => p.User.DiscordID == userID);
    }

    /// <summary>
    /// Determines whether or not the given user is invited to the roleplay.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>true if the user is invited; otherwise, false.</returns>
    [Pure]
    public bool IsInvited(User user)
    {
        return IsInvited(user.DiscordID);
    }

    /// <summary>
    /// Determines whether or not the given user is invited to the roleplay.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>true if the user is invited; otherwise, false.</returns>
    [Pure]
    public bool IsInvited(IUser user)
    {
        return IsInvited(user.ID);
    }

    /// <summary>
    /// Determines whether or not the given user ID is invited to the roleplay.
    /// </summary>
    /// <param name="userID">The ID of the user.</param>
    /// <returns>true if the user is invited; otherwise, false.</returns>
    [Pure]
    public bool IsInvited(Snowflake userID)
    {
        return this.InvitedUsers.Any(iu => iu.User.DiscordID == userID);
    }

    /// <summary>
    /// Determines whether or not the given user is kicked from the roleplay.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>true if the user is kicked; otherwise, false.</returns>
    [Pure]
    public bool IsKicked(User user)
    {
        return IsKicked(user.DiscordID);
    }

    /// <summary>
    /// Determines whether or not the given user is kicked from the roleplay.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>true if the user is kicked; otherwise, false.</returns>
    [Pure]
    public bool IsKicked(IUser user)
    {
        return IsKicked(user.ID);
    }

    /// <summary>
    /// Determines whether or not the given user ID is kicked from the roleplay.
    /// </summary>
    /// <param name="userID">The ID of the user.</param>
    /// <returns>true if the user is kicked; otherwise, false.</returns>
    [Pure]
    public bool IsKicked(Snowflake userID)
    {
        return this.KickedUsers.Any(ku => ku.User.DiscordID == userID);
    }
}
