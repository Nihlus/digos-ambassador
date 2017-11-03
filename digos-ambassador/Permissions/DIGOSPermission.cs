//
//  DIGOSPermission.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

namespace DIGOS.Ambassador.Permissions
{
	/// <summary>
	/// Represents different permissions specific to the ambassador.
	/// </summary>
	public enum DIGOSPermission
	{
		/// <summary>
		/// Allows the user to grant permissions. This is a dangerous permission to have.
		/// </summary>
		GrantPermission,

		/// <summary>
		/// Allows the user to set classes.
		/// </summary>
		SetClass,

		/// <summary>
		/// Allows the user to create characters.
		/// </summary>
		CreateCharacter,

		/// <summary>
		/// Allows the user to import characters.
		/// </summary>
		ImportCharacter,

		/// <summary>
		/// Allows the user to delete characters.
		/// </summary>
		DeleteCharacter,

		/// <summary>
		/// Allows the user to edit user information.
		/// </summary>
		EditUser,
	}
}
