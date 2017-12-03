//
//  IEmoteConsumer.cs
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
	/// Interface for classes that consume emotes.
	/// </summary>
	public interface IEmoteConsumer
	{
		/// <summary>
		/// Gets the set of accepted emotes.
		/// </summary>
		IReadOnlyCollection<IEmote> AcceptedEmotes { get; }

		/// <summary>
		/// Consumes the given emote, performing some associated action.
		/// </summary>
		/// <param name="emote">The emote.</param>
		/// <returns>A task that must be awaited.</returns>
		Task<bool> ConsumeAsync(IEmote emote);
	}
}
