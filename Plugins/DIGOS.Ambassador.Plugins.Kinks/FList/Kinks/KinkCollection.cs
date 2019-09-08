//
//  KinkCollection.cs
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

using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace DIGOS.Ambassador.Plugins.Kinks.FList.Kinks
{
    /// <summary>
    /// Represents a JSON kink collection from the F-list API, organized into categories.
    /// </summary>
    internal class KinkCollection
    {
        /// <summary>
        /// Gets or sets the error that the API returned, if any.
        /// </summary>
        [JsonProperty("error")]
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the kink categories.
        /// </summary>
        [JsonProperty("kinks")]
        public Dictionary<string, FListKinkCategory> KinkCategories { get; set; }

        /// <summary>
        /// Creates a new <see cref="KinkCollection"/> from the given JSON string.
        /// </summary>
        /// <param name="json">The serialized kink collection.</param>
        /// <returns>A deserialized kink collection.</returns>
        [Pure]
        [NotNull]
        public static KinkCollection FromJson([NotNull] string json)
        {
            var settings = new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None,
            };

            return JsonConvert.DeserializeObject<KinkCollection>(json, settings);
        }
    }
}
