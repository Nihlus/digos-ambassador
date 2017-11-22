//
//  Permission.cs
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

using System.ComponentModel;

namespace DIGOS.Ambassador.Permissions
{
	/// <summary>
	/// Represents different permissions specific to the ambassador.
	/// </summary>
	public enum Permission
	{
		/// <summary>
		/// Allows the user to grant and revoke permissions. This is a dangerous permission to have.
		/// </summary>
		[Description("Allows the user to grant and revoke permissions. This is a dangerous permission to have.")]
		ManagePermissions,

		/// <summary>
		/// Allows the user to set classes.
		/// </summary>
		[Description("Allows the user to set classes.")]
		SetClass,

		/// <summary>
		/// Allows the user to create characters.
		/// </summary>
		[Description("Allows the user to create characters.")]
		CreateCharacter,

		/// <summary>
		/// Allows the user to import characters from supported services.
		/// </summary>
		[Description("Allows the user to import characters from supported services.")]
		ImportCharacter,

		/// <summary>
		/// Allows the user to delete characters.
		/// </summary>
		[Description("Allows the user to delete characters.")]
		DeleteCharacter,

		/// <summary>
		/// Allows the user to edit characters.
		/// </summary>
		[Description("Allows the user to edit characters.")]
		EditCharacter,

		/// <summary>
		/// Allows the user to transfer ownership of characters.
		/// </summary>
		[Description("Allows the user to transfer ownership of characters.")]
		TransferCharacter,

		/// <summary>
		/// Allows the user to assume the form of a character.
		/// </summary>
		[Description("Allows the user to assume the form of a character.")]
		AssumeCharacter,

		/// <summary>
		/// Allows the user to edit user information.
		/// </summary>
		[Description("Allows the user to edit user information.")]
		EditUser,

		/// <summary>
		/// Allows the user to create roleplays.
		/// </summary>
		[Description("Allows the user to create roleplays.")]
		CreateRoleplay,

		/// <summary>
		/// Allows the user to delete roleplays.
		/// </summary>
		[Description("Allows the user to delete roleplays.")]
		DeleteRoleplay,

		/// <summary>
		/// Allows the user to join roleplays.
		/// </summary>
		[Description("Allows the user to join roleplays.")]
		JoinRoleplay,

		/// <summary>
		/// Allows the user to replay roleplays.
		/// </summary>
		[Description("Allows the user to replay roleplays.")]
		ReplayRoleplay,

		/// <summary>
		/// Allows the user to edit roleplay information.
		/// </summary>
		[Description("Allows the user to edit roleplay information.")]
		EditRoleplay,
	}
}
