//
//  PortraitService.cs
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
using DIGOS.Ambassador.Core.Services;

namespace DIGOS.Ambassador.Plugins.Amby.Services
{
    /// <summary>
    /// Provides access to remote URLs with various portraits.
    /// </summary>
    public class PortraitService
    {
        private readonly ContentService _content;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortraitService"/> class.
        /// </summary>
        /// <param name="content">The content service.</param>
        public PortraitService(ContentService content)
        {
            _content = content;

            this.AmbyPortraitUri = new Uri(_content.BaseCDNUri, "portraits/amby-irbynx-3.png");
            this.BrokenAmbyUri = new Uri(_content.BaseCDNUri, "portraits/maintenance.png");
            this.BwehUri = new Uri(_content.BaseCDNUri, "portraits/bweh.png");
        }

        /// <summary>
        /// Gets the <see cref="Uri"/> pointing to a proper bweh.
        /// </summary>
        public Uri BwehUri { get; }

        /// <summary>
        /// Gets the <see cref="Uri"/> pointing to a portrait of Amby.
        /// </summary>
        public Uri AmbyPortraitUri { get; }

        /// <summary>
        /// Gets the <see cref="Uri"/> pointing to a broken Ambybot portrait.
        /// </summary>
        public Uri BrokenAmbyUri { get; }
    }
}
