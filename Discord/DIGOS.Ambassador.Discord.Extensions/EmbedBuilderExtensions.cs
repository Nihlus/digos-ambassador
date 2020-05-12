//
//  EmbedBuilderExtensions.cs
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

using Discord;

namespace DIGOS.Ambassador.Discord.Extensions
{
    /// <summary>
    /// Extension methods for the <see cref="EmbedBuilder"/> class.
    /// </summary>
    public static class EmbedBuilderExtensions
    {
        /// <summary>
        /// Copies an <see cref="EmbedBuilder"/> to a new instance with the same settings.
        /// </summary>
        /// <param name="baseBuilder">The base embed builder.</param>
        /// <returns>The copied instance.</returns>
        public static EmbedBuilder CopyEmbedBuilder(this EmbedBuilder baseBuilder)
        {
            var newBuilder = new EmbedBuilder()
                .WithDescription(baseBuilder.Description)
                .WithAuthor(baseBuilder.Author)
                .WithFooter(baseBuilder.Footer)
                .WithTitle(baseBuilder.Title)
                .WithUrl(baseBuilder.Url)
                .WithImageUrl(baseBuilder.ImageUrl)
                .WithThumbnailUrl(baseBuilder.ThumbnailUrl)
                .WithFields(baseBuilder.Fields);

            if (baseBuilder.Color.HasValue)
            {
                newBuilder = newBuilder.WithColor(baseBuilder.Color.Value);
            }

            if (baseBuilder.Timestamp.HasValue)
            {
                newBuilder = newBuilder.WithTimestamp(baseBuilder.Timestamp.Value);
            }

            return newBuilder;
        }
    }
}
