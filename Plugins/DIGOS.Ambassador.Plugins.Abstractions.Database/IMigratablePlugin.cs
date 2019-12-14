//
//  IMigratablePlugin.cs
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
using JetBrains.Annotations;
using Remora.Plugins.Abstractions;

namespace DIGOS.Ambassador.Plugins.Abstractions.Database
{
    /// <summary>
    /// Represents the public API of a plugin supporting migrations.
    /// </summary>
    [PublicAPI]
    public interface IMigratablePlugin : IPluginDescriptor
    {
        /// <summary>
        /// Performs any migrations required by the plugin.
        /// </summary>
        /// <param name="serviceProvider">The available services.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        Task<bool> MigratePluginAsync([NotNull] IServiceProvider serviceProvider);

        /// <summary>
        /// Determines whether the database has been created.
        /// </summary>
        /// <param name="serviceProvider">The available services.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        Task<bool> IsDatabaseCreatedAsync([NotNull] IServiceProvider serviceProvider);
    }
}
