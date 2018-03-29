//
//  Roleplay.cs
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

using System.Collections.Generic;
using System.Linq;

using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Database.Users;

using Discord;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Database.Roleplaying
{
	/// <summary>
	/// Represents a saved roleplay.
	/// </summary>
	public class Roleplay : IOwnedNamedEntity, IServerEntity
	{
		/// <inheritdoc />
		public uint ID { get; set; }

		/// <inheritdoc />
		public ulong ServerID { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the roleplay is currently active in a channel.
		/// </summary>
		public bool IsActive { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the roleplay can be viewed or replayed by anyone.
		/// </summary>
		public bool IsPublic { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the roleplay is NSFW.
		/// </summary>
		public bool IsNSFW { get; set; }

		/// <summary>
		/// Gets or sets the ID of the channel that the roleplay is active in.
		/// </summary>
		public ulong ActiveChannelID { get; set; }

		/// <inheritdoc />
		public User Owner { get; set; }

		/// <summary>
		/// Gets or sets the users that have been invited to join the roleplay.
		/// </summary>
		public List<User> InvitedUsers { get; set; }

		/// <summary>
		/// Gets or sets the users that have been kicked from the roleplay.
		/// </summary>
		public List<User> KickedUsers { get; set; }

		/// <summary>
		/// Gets or sets the participants of the roleplay.
		/// </summary>
		public List<User> Participants { get; set; }

		/// <inheritdoc />
		public string Name { get; set; }

		/// <inheritdoc />
		[NotNull]
		public string EntityTypeDisplayName => nameof(Roleplay);

		/// <summary>
		/// Gets or sets the summary of the roleplay.
		/// </summary>
		public string Summary { get; set; }

		/// <summary>
		/// Gets or sets the saved messages in the roleplay.
		/// </summary>
		public List<UserMessage> Messages { get; set; }

		/// <inheritdoc />
		public bool IsOwner(User user)
		{
			return IsOwner(user.DiscordID);
		}

		/// <inheritdoc />
		public bool IsOwner(IUser user)
		{
			return IsOwner(user.Id);
		}

		/// <inheritdoc />
		public bool IsOwner(ulong userID)
		{
			return this.Owner.DiscordID == userID;
		}

		/// <summary>
		/// Determines whether or not the given user is a participant of the roleplay.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns>true if the user is a participant; otherwise, false.</returns>
		[Pure]
		public bool IsParticipant([NotNull] User user)
		{
			return IsParticipant(user.DiscordID);
		}

		/// <summary>
		/// Determines whether or not the given user is a participant of the roleplay.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns>true if the user is a participant; otherwise, false.</returns>
		[Pure]
		public bool IsParticipant([NotNull] IUser user)
		{
			return IsParticipant(user.Id);
		}

		/// <summary>
		/// Determines whether or not the given user ID is a participant of the roleplay.
		/// </summary>
		/// <param name="userID">The ID of the user.</param>
		/// <returns>true if the user is a participant; otherwise, false.</returns>
		[Pure]
		public bool IsParticipant(ulong userID)
		{
			return this.Participants.Any(p => p.DiscordID == userID);
		}

		/// <summary>
		/// Determines whether or not the given user is invited to the roleplay.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns>true if the user is invited; otherwise, false.</returns>
		[Pure]
		public bool IsInvited([NotNull] User user)
		{
			return IsInvited(user.DiscordID);
		}

		/// <summary>
		/// Determines whether or not the given user is invited to the roleplay.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns>true if the user is invited; otherwise, false.</returns>
		[Pure]
		public bool IsInvited([NotNull] IUser user)
		{
			return IsInvited(user.Id);
		}

		/// <summary>
		/// Determines whether or not the given user ID is invited to the roleplay.
		/// </summary>
		/// <param name="userID">The ID of the user.</param>
		/// <returns>true if the user is invited; otherwise, false.</returns>
		[Pure]
		public bool IsInvited(ulong userID)
		{
			return this.InvitedUsers.Any(iu => iu.DiscordID == userID);
		}

		/// <summary>
		/// Determines whether or not the given user is kicked from the roleplay.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns>true if the user is kicked; otherwise, false.</returns>
		[Pure]
		public bool IsKicked([NotNull] User user)
		{
			return IsKicked(user.DiscordID);
		}

		/// <summary>
		/// Determines whether or not the given user is kicked from the roleplay.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns>true if the user is kicked; otherwise, false.</returns>
		[Pure]
		public bool IsKicked([NotNull] IUser user)
		{
			return IsKicked(user.Id);
		}

		/// <summary>
		/// Determines whether or not the given user ID is kicked from the roleplay.
		/// </summary>
		/// <param name="userID">The ID of the user.</param>
		/// <returns>true if the user is kicked; otherwise, false.</returns>
		[Pure]
		public bool IsKicked(ulong userID)
		{
			return this.KickedUsers.Any(ku => ku.DiscordID == userID);
		}
	}
}
