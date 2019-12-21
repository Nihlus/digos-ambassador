//
//  MockHelper.cs
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
using Discord;
using JetBrains.Annotations;
using Moq;

namespace DIGOS.Ambassador.Tests.Utility
{
    /// <summary>
    /// Helper methods for mocking objects.
    /// </summary>
    [PublicAPI]
    public static class MockHelper
    {
        /// <summary>
        /// Creates a simple mocked <see cref="IUser"/> object with the given ID.
        /// </summary>
        /// <param name="id">The ID of the object.</param>
        /// <returns>A mocked object.</returns>
        [NotNull]
        public static IUser CreateDiscordUser(ulong id) => CreateDiscordEntity<IUser>(id);

        /// <summary>
        /// Creates a simple mocked <see cref="IGuildUser"/> object with the given ID.
        /// </summary>
        /// <param name="id">The ID of the object.</param>
        /// <returns>A mocked object.</returns>
        [NotNull]
        public static IGuildUser CreateDiscordGuildUser(ulong id) => CreateDiscordEntity<IGuildUser>(id);

        /// <summary>
        /// Creates a simple mocked <see cref="IGuild"/> object with the given ID.
        /// </summary>
        /// <param name="id">The ID of the object.</param>
        /// <param name="ownerId">The ID of the guild owner.</param>
        /// <returns>A mocked object.</returns>
        [NotNull]
        public static IGuild CreateDiscordGuild(long id, long? ownerId = null)
        {
            var mock = new Mock<IGuild>();
            mock.Setup(u => u.Id).Returns((ulong)id);

            if (!(ownerId is null))
            {
                mock.Setup(u => u.OwnerId).Returns((ulong)ownerId.Value);
            }

            return mock.Object;
        }

        /// <summary>
        /// Creates a simple mocked <see cref="IRole"/> object with the given ID.
        /// </summary>
        /// <param name="id">The ID of the object.</param>
        /// <param name="guild">The guild of the role.</param>
        /// <returns>A mocked object.</returns>
        [NotNull]
        public static IRole CreateDiscordRole(int id, IGuild? guild = null)
        {
            var mock = new Mock<IRole>();
            mock.Setup(r => r.Id).Returns((ulong)id);

            if (!(guild is null))
            {
                mock.Setup(r => r.Guild).Returns(guild);
            }

            return mock.Object;
        }

        /// <summary>
        /// Creates a simple mocked <see cref="ITextChannel"/> object with the the given ID.
        /// </summary>
        /// <param name="id">The ID of the object.</param>
        /// <returns>A mocked object.</returns>
        [NotNull]
        public static ITextChannel CreateDiscordTextChannel(ulong id) => CreateDiscordEntity<ITextChannel>(id);

        /// <summary>
        /// Creates a generic mocked Discord entity with the given ID.
        /// </summary>
        /// <param name="id">The ID of the entity.</param>
        /// <param name="mockConfiguration">The configuration method for the mocked entity.</param>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <returns>A mocked object.</returns>
        public static TEntity CreateDiscordEntity<TEntity>(ulong id, Action<Mock<TEntity>> mockConfiguration = null)
            where TEntity : class, IEntity<ulong>
        {
            var mock = new Mock<TEntity>();
            mock.Setup(r => r.Id).Returns(id);

            mockConfiguration?.Invoke(mock);

            return mock.Object;
        }
    }
}
