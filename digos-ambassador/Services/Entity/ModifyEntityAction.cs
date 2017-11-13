//
//  ModifyEntityAction.cs
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

namespace DIGOS.Ambassador.Services.Entity
{
	/// <summary>
	/// Enumerates which actions can be taken when an entity is modified.
	/// </summary>
	public enum ModifyEntityAction
	{
		/// <summary>
		/// No changes were made.
		/// </summary>
		None,

		/// <summary>
		/// A new entity was added.
		/// </summary>
		Added,

		/// <summary>
		/// An existing entity was edited.
		/// </summary>
		Edited
	}
}
