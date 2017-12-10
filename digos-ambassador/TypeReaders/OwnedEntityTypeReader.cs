//
//  OwnedEntityTypeReader.cs
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Services;

using Discord;
using Discord.Commands;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.TypeReaders
{
	/// <summary>
	/// Reads owned entities from command arguments.
	/// </summary>
	/// <typeparam name="T1">User classes.</typeparam>
	/// <typeparam name="T2">Owned entity classes.</typeparam>
	public abstract class OwnedEntityTypeReader<T1, T2> : TypeReader
		where T1 : class, IUser
		where T2 : class, IOwnedNamedEntity
	{
		/// <inheritdoc />
		public sealed override async Task<TypeReaderResult> Read(ICommandContext context, string input, IServiceProvider services)
		{
			IUser owner = null;
			string entityName;
			if (!input.IsNullOrWhitespace() && input.Any(c => c == ':'))
			{
				// We have a mentioned owner and a name. Owners may have colons in the name, so let's check from the back.
				int splitIndex = input.LastIndexOf(':');
				var rawUser = input.Substring(0, splitIndex);
				entityName = input.Substring(splitIndex + 1);

				// Try to parse the user
				var userParseResult = await ReadUserAsync(context, rawUser);
				if (!userParseResult.IsSuccess)
				{
					return TypeReaderResult.FromError(userParseResult);
				}

				var highestScore = userParseResult.Values.Max(v => v.Score);
				owner = userParseResult.Values.First(v => v.Score >= highestScore).Value as T1;
			}
			else
			{
				// We might just have a user and not a name, so let's try parsing it
				var userParseResult = await ReadUserAsync(context, input);
				if (userParseResult.IsSuccess)
				{
					entityName = null;

					var highestScore = userParseResult.Values.Max(v => v.Score);
					owner = userParseResult.Values.First(v => v.Score >= highestScore).Value as T1;
				}
				else
				{
					entityName = input;
				}
			}

			owner = owner ?? context.User;

			var retrieveEntityResult = await RetrieveEntityAsync(owner, entityName, context, services);
			if (!retrieveEntityResult.IsSuccess)
			{
				return TypeReaderResult.FromError(retrieveEntityResult);
			}

			return TypeReaderResult.FromSuccess(retrieveEntityResult.Entity);
		}

		/// <summary>
		/// Retrieves the named entity from the given user.
		/// </summary>
		/// <param name="entityOwner">The owner of the entity.</param>
		/// <param name="entityName">The name of the entity.</param>
		/// <param name="context">The context of the command.</param>
		/// <param name="services">The injected services.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		protected abstract Task<RetrieveEntityResult<T2>> RetrieveEntityAsync
		(
			[NotNull] IUser entityOwner,
			[CanBeNull] string entityName,
			[NotNull] ICommandContext context,
			[NotNull] IServiceProvider services
		);

		private async Task<TypeReaderResult> ReadUserAsync(ICommandContext context, string input)
		{
			var results = new Dictionary<ulong, TypeReaderValue>();

			IReadOnlyCollection<IUser> channelUsers =
			(
				await context.Channel.GetUsersAsync(CacheMode.CacheOnly).Flatten().ConfigureAwait(false)
			)
			.ToArray();

			IReadOnlyCollection<IGuildUser> guildUsers = ImmutableArray.Create<IGuildUser>();

			if (context.Guild != null)
			{
				guildUsers = await context.Guild.GetUsersAsync(CacheMode.CacheOnly).ConfigureAwait(false);
			}

			// By Mention (1.0)
			if (MentionUtils.TryParseUser(input, out ulong id))
			{
				if (context.Guild != null)
				{
					AddResult(results, await context.Guild.GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false) as T1, 1.00f);
				}
				else
				{
					AddResult(results, await context.Channel.GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false) as T1, 1.00f);
				}
			}

			// By Id (0.9)
			if (ulong.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out id))
			{
				if (context.Guild != null)
				{
					AddResult(results, await context.Guild.GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false) as T1, 0.90f);
				}
				else
				{
					AddResult(results, await context.Channel.GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false) as T1, 0.90f);
				}
			}

			// By Username + Discriminator (0.7-0.85)
			int index = input.LastIndexOf('#');
			if (index >= 0)
			{
				string username = input.Substring(0, index);
				if (ushort.TryParse(input.Substring(index + 1), out ushort discriminator))
				{
					var channelUser = channelUsers.FirstOrDefault(x => x.DiscriminatorValue == discriminator &&
						string.Equals(username, x.Username, StringComparison.OrdinalIgnoreCase));
					AddResult(results, channelUser as T1, channelUser?.Username == username ? 0.85f : 0.75f);

					var guildUser = guildUsers.FirstOrDefault(x => x.DiscriminatorValue == discriminator &&
						string.Equals(username, x.Username, StringComparison.OrdinalIgnoreCase));
					AddResult(results, guildUser as T1, guildUser?.Username == username ? 0.80f : 0.70f);
				}
			}

			// By Username (0.5-0.6)
			{
				foreach (var channelUser in channelUsers.Where(x => string.Equals(input, x.Username, StringComparison.OrdinalIgnoreCase)))
				{
					AddResult(results, channelUser as T1, channelUser.Username == input ? 0.65f : 0.55f);
				}

				foreach (var guildUser in guildUsers.Where(x => string.Equals(input, x.Username, StringComparison.OrdinalIgnoreCase)))
				{
					AddResult(results, guildUser as T1, guildUser.Username == input ? 0.60f : 0.50f);
				}
			}

			// By Nickname (0.5-0.6)
			{
				foreach (var channelUser in channelUsers.Where(x => string.Equals(input, (x as IGuildUser)?.Nickname, StringComparison.OrdinalIgnoreCase)))
				{
					AddResult(results, channelUser as T1, (channelUser as IGuildUser)?.Nickname == input ? 0.65f : 0.55f);
				}

				foreach (var guildUser in guildUsers.Where(x => string.Equals(input, x.Nickname, StringComparison.OrdinalIgnoreCase)))
				{
					AddResult(results, guildUser as T1, guildUser.Nickname == input ? 0.60f : 0.50f);
				}
			}

			if (results.Count > 0)
			{
				return TypeReaderResult.FromSuccess(results.Values.ToImmutableArray());
			}

			return TypeReaderResult.FromError(CommandError.ObjectNotFound, "User not found.");
		}

		private void AddResult(IDictionary<ulong, TypeReaderValue> results, T1 user, float score)
		{
			if (user != null && !results.ContainsKey(user.Id))
			{
				results.Add(user.Id, new TypeReaderValue(user, score));
			}
		}
	}
}
