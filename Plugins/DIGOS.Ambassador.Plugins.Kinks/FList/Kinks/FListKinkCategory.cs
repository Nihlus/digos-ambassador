//
//  FListKinkCategory.cs
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

using System.Net;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Kinks.FList.Kinks;

/// <summary>
/// Represents a JSON kink category from the F-list API.
/// </summary>
[PublicAPI]
internal class FListKinkCategory
{
    /// <summary>
    /// Gets the category name.
    /// </summary>
    public string Group { get; }

    /// <summary>
    /// Gets the kinks in the category.
    /// </summary>
    [JsonPropertyName("items")]
    public FListKink[] Kinks { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FListKinkCategory"/> class.
    /// </summary>
    /// <param name="group">The category name.</param>
    /// <param name="kinks">The retrieved kinks.</param>
    public FListKinkCategory(string group, FListKink[] kinks)
    {
        this.Group = WebUtility.HtmlDecode(group);
        this.Kinks = kinks;
    }
}
