//
//  IWizard.cs
//
//  Author:
//        Jarl Gullberg <jarl.gullberg@gmail.com>
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
using System.Threading.Tasks;
using Discord;

namespace DIGOS.Ambassador.Wizards
{
	/// <summary>
	/// Represents an interactive wizard.
	/// </summary>
	public interface IWizard : IEmoteConsumer
	{
		/// <summary>
		/// Gets the emotes that should be active for the current page.
		/// </summary>
		/// <returns>The emotes.</returns>
		IEnumerable<IEmote> GetCurrentPageEmotes();

			/// <summary>
		/// Gets the current page in the wizard.
		/// </summary>
		/// <returns>The current page.</returns>
		Task<Embed> GetCurrentPageAsync();
	}
}
