//
//  ContentServiceExtensions.cs
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
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Core.Services;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Drone.Extensions
{
    /// <summary>
    /// Extension methods for the <see cref="ContentService"/> class.
    /// </summary>
    [PublicAPI]
    public static class ContentServiceExtensions
    {
        /// <summary>
        /// Gets the names of the available avatars.
        /// </summary>
        private static string[] AvatarNames { get; } =
        {
            "avatar-1.0-shifted-0.png",
            "avatar-1.0-shifted-135.png",
            "avatar-1.0-shifted-180.png",
            "avatar-1.0-shifted-225.png",
            "avatar-1.0-shifted-270.png",
            "avatar-1.0-shifted-315.png",
            "avatar-1.0-shifted-360.png",
            "avatar-1.0-shifted-45.png",
            "avatar-1.0-shifted-90.png",
            "avatar-1.1-shifted-0.png",
            "avatar-1.1-shifted-135.png",
            "avatar-1.1-shifted-180.png",
            "avatar-1.1-shifted-225.png",
            "avatar-1.1-shifted-270.png",
            "avatar-1.1-shifted-315.png",
            "avatar-1.1-shifted-360.png",
            "avatar-1.1-shifted-45.png",
            "avatar-1.1-shifted-90.png",
            "avatar-2.0-shifted-0.png",
            "avatar-2.0-shifted-135.png",
            "avatar-2.0-shifted-180.png",
            "avatar-2.0-shifted-225.png",
            "avatar-2.0-shifted-270.png",
            "avatar-2.0-shifted-315.png",
            "avatar-2.0-shifted-360.png",
            "avatar-2.0-shifted-45.png",
            "avatar-2.0-shifted-90.png",
        };

        /// <summary>
        /// Gets a random avatar URI for drones.
        /// </summary>
        /// <param name="this">The content service.</param>
        /// <returns>The avatar URI.</returns>
        public static Uri GetRandomDroneAvatarUri(this ContentService @this)
        {
            return new Uri(@this.BaseCDNUri, $"plugins/drone/avatars/{AvatarNames.PickRandom()}");
        }

        /// <summary>
        /// Gets a random summary for a drone.
        /// </summary>
        /// <param name="this">The content service.</param>
        /// <returns>The summary.</returns>
        public static string GetRandomDroneSummary(this ContentService @this)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a random description for a drone.
        /// </summary>
        /// <param name="this">The content service.</param>
        /// <returns>The summary.</returns>
        public static string GetRandomDroneDescription(this ContentService @this)
        {
            throw new NotImplementedException();
        }
    }
}
