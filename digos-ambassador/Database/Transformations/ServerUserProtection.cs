//
//  ServerUserProtection.cs
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
using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Database.Users;
using DIGOS.Ambassador.Transformations;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Database.Transformations
{
	/// <summary>
	/// Holds protection data for a specific user on a specific server.
	/// </summary>
	public class ServerUserProtection : IEFEntity
	{
		/// <inheritdoc />
		public long ID { get; set; }

		/// <summary>
		/// Gets or sets the user that owns this protection data.
		/// </summary>
		[Required]
		public User User { get; set; }

		/// <summary>
		/// Gets or sets the server that this protection data is valid on.
		/// </summary>
		[Required]
		public Server Server { get; set; }

		/// <summary>
		/// Gets or sets the active protection type on this server.
		/// </summary>
		public ProtectionType Type { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the user has opted in to transformations.
		/// </summary>
		public bool HasOptedIn { get; set; }

		/// <summary>
		/// Creates a default server-specific protection object based on the given global protection data.
		/// </summary>
		/// <param name="globalProtection">The global protection data.</param>
		/// <param name="server">The server that the protection should be valid for.</param>
		/// <returns>A server-specific protection object.</returns>
		[Pure]
		[NotNull]
		public static ServerUserProtection CreateDefault([NotNull] GlobalUserProtection globalProtection, [NotNull] Server server)
		{
			return new ServerUserProtection
			{
				User = globalProtection.User,
				Server = server,
				Type = globalProtection.DefaultType,
				HasOptedIn = globalProtection.DefaultOptIn
			};
		}
	}
}
