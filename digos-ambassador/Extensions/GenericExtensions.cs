//
//  GenericExtensions.cs
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

using System;

namespace DIGOS.Ambassador.Extensions
{
	/// <summary>
	/// Extensions targeting generic functions.
	/// </summary>
	public static class GenericExtensions
	{
		/// <summary>
		/// Executes the given <paramref name="action"/> if the given <paramref name="predicate"/> matches.
		/// </summary>
		/// <param name="value">The base value.</param>
		/// <param name="action">The action to take.</param>
		/// <param name="predicate">The predicate to verify with.</param>
		/// <typeparam name="T">The type of the value.</typeparam>
		/// <returns>0 if the action was not taken; otherwise, 1.</returns>
		public static int ExecuteBy<T>(this T value, Action action, Func<T, bool> predicate)
		{
			if (!predicate(value))
			{
				return 0;
			}

			action();
			return 1;
		}
	}
}
