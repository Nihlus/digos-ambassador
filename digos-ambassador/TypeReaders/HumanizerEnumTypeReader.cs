//
//  HumanizerEnumTypeReader.cs
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
using System.Threading.Tasks;
using Discord.Commands;
using Humanizer;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.TypeReaders
{
	/// <summary>
	/// Reads enums using Humanizer's DehumanizeTo function.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	public class HumanizerEnumTypeReader<T> : TypeReader where T : struct, IComparable, IFormattable
	{
		/// <inheritdoc />
		[NotNull]
		public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
		{
			try
			{
				var result = input.DehumanizeTo<T>();
				return Task.FromResult(TypeReaderResult.FromSuccess(result));
			}
			catch (NoMatchFoundException)
			{
				var message = $"Couldn't parse \"{input}\" as an enum of type \"{typeof(T).Name}\"";
				return Task.FromResult(TypeReaderResult.FromError(CommandError.ObjectNotFound, message));
			}
		}
	}
}
