//
//  PaginatedEmbed.cs
//
//  Author:
//        Jarl Gullberg <jarl.gullberg@gmail.com>
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
using Discord;
using Discord.Addons.Interactive;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Pagination
{
	/// <summary>
	/// Represents a paginated gallery of embeds.
	/// </summary>
	public class PaginatedEmbed : IPager<EmbedBuilder, PaginatedEmbed>
	{
		/// <inheritdoc />
		public IList<EmbedBuilder> Pages { get; set; }

		/// <inheritdoc />
		public PaginatedAppearanceOptions Options { get; set; } = PaginatedAppearanceOptions.Default;

		/// <summary>
		/// Initializes a new instance of the <see cref="PaginatedEmbed"/> class.
		/// </summary>
		/// <param name="embeds">The embeds to paginate.</param>
		public PaginatedEmbed(IList<EmbedBuilder> embeds)
		{
			this.Pages = embeds;
		}

		/// <inheritdoc />
		[NotNull]
		public PaginatedEmbed WithPage(EmbedBuilder page)
		{
			this.Pages.Add(page);
			return this;
		}

		/// <inheritdoc />
		[NotNull]
		public PaginatedEmbed WithPages([NotNull] IEnumerable<EmbedBuilder> pages)
		{
			foreach (var page in pages)
			{
				WithPage(page);
			}

			return this;
		}

		/// <inheritdoc />
		public Embed BuildEmbed(int page)
		{
			var currentPage = this.Pages[page];

			return currentPage
			.WithFooter(f => f.Text = string.Format(this.Options.FooterFormat, page + 1, this.Pages.Count))
			.Build();
		}
	}
}
