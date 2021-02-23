//
//  PaginatedMessage.cs
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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Interactivity.Messages;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Discord.Pagination
{
    /// <summary>
    /// A page building class for paginated galleries.
    /// </summary>
    public class PaginatedMessage : InteractiveMessage
    {
        private readonly IDiscordRestChannelAPI _channelAPI;

        /// <summary>
        /// Holds the pages in the message.
        /// </summary>
        private readonly IReadOnlyList<Embed> _pages;

        /// <summary>
        /// Holds the appearance options.
        /// </summary>
        private readonly PaginatedAppearanceOptions _appearance;

        /// <summary>
        /// Holds the ID of the source user.
        /// </summary>
        private readonly Snowflake _sourceUserID;

        /// <summary>
        /// Holds the names of the reactions, mapped to their emoji.
        /// </summary>
        private readonly IReadOnlyDictionary<string, IEmoji> _reactionNames;

        /// <summary>
        /// Holds the current page index.
        /// </summary>
        private int _currentPage;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaginatedMessage"/> class.
        /// </summary>
        /// <param name="channelID">The ID of the channel the message is in.</param>
        /// <param name="messageID">The ID of the message.</param>
        /// <param name="channelAPI">The channel API.</param>
        /// <param name="sourceUserID">The ID of the source user.</param>
        /// <param name="pages">The pages in the paginated message.</param>
        /// <param name="appearance">The appearance options.</param>
        public PaginatedMessage
        (
            Snowflake channelID,
            Snowflake messageID,
            IDiscordRestChannelAPI channelAPI,
            Snowflake sourceUserID,
            IReadOnlyList<Embed> pages,
            PaginatedAppearanceOptions appearance
        )
            : base(channelID, messageID)
        {
            _sourceUserID = sourceUserID;
            _pages = pages;
            _appearance = appearance;
            _channelAPI = channelAPI;

            _reactionNames = new Dictionary<string, IEmoji>
            {
                { GetEmojiName(_appearance.First), _appearance.First },
                { GetEmojiName(_appearance.Back), _appearance.Back },
                { GetEmojiName(_appearance.Next), _appearance.Next },
                { GetEmojiName(_appearance.Last), _appearance.Last },
                { GetEmojiName(_appearance.Close), _appearance.Close },
                { GetEmojiName(_appearance.Help), _appearance.Help }
            };
        }

        /// <inheritdoc />
        public override async Task<Result> OnReactionAddedAsync(Snowflake userID, IPartialEmoji emoji, CancellationToken ct = default)
        {
            if (userID != _sourceUserID)
            {
                // We handled it, but we won't react
                return Result.FromSuccess();
            }

            var reactionName = GetPartialEmojiName(emoji);
            if (!_reactionNames.TryGetValue(reactionName, out var knownEmoji))
            {
                // This isn't an emoji we react to
                return Result.FromSuccess();
            }

            // Special actions
            if (knownEmoji.Equals(_appearance.Close))
            {
                return await _channelAPI.DeleteMessageAsync(this.ChannelID, this.MessageID, ct);
            }

            if (knownEmoji.Equals(_appearance.Help))
            {
                var embed = new Embed { Colour = Color.Blue, Description = _appearance.HelpText };
                var sendHelp = await _channelAPI.CreateMessageAsync(this.ChannelID, embed: embed, ct: ct);
                return !sendHelp.IsSuccess
                    ? Result.FromError(sendHelp)
                    : Result.FromSuccess();
            }

            // Page movement actions
            if (knownEmoji.Equals(_appearance.First))
            {
                _currentPage = 0;
            }

            if (knownEmoji.Equals(_appearance.Back))
            {
                if (_currentPage <= 0)
                {
                    return Result.FromSuccess();
                }

                --_currentPage;
            }

            if (knownEmoji.Equals(_appearance.Next))
            {
                if (_currentPage >= _pages.Count - 1)
                {
                    return Result.FromSuccess();
                }

                ++_currentPage;
            }

            if (knownEmoji.Equals(_appearance.Last))
            {
                _currentPage = _pages.Count - 1;
            }

            return await UpdateAsync(ct);
        }

        /// <inheritdoc />
        public override Task<Result> OnReactionRemovedAsync
        (
            Snowflake userID,
            IPartialEmoji emoji,
            CancellationToken ct = default
        )
            => OnReactionAddedAsync(userID, emoji, ct);

        /// <inheritdoc />
        public override Task<Result> OnAllReactionsRemovedAsync(CancellationToken ct = default)
            => UpdateAsync(ct);

        /// <inheritdoc/>
        public override async Task<Result> UpdateAsync(CancellationToken ct = default)
        {
            var page = _pages[_currentPage] with
            {
                Footer = new EmbedFooter(string.Format(_appearance.FooterFormat, _currentPage + 1, _pages.Count))
            };

            var modifyMessage = await _channelAPI.EditMessageAsync(this.ChannelID, this.MessageID, embed: page, ct: ct);
            if (!modifyMessage.IsSuccess)
            {
                return Result.FromError(modifyMessage);
            }

            var updateButtons = await UpdateReactionButtonsAsync(ct);
            return updateButtons.IsSuccess
                ? Result.FromSuccess()
                : updateButtons;
        }

        /// <summary>
        /// Updates the displayed buttons.
        /// </summary>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        private async Task<Result> UpdateReactionButtonsAsync(CancellationToken ct = default)
        {
            var getMessage = await _channelAPI.GetChannelMessageAsync(this.ChannelID, this.MessageID, ct);
            if (!getMessage.IsSuccess)
            {
                return Result.FromError(getMessage);
            }

            var message = getMessage.Entity;
            var existingReactions = message.Reactions;

            var reactions = new[]
            {
                GetEmojiName(_appearance.First),
                GetEmojiName(_appearance.Back),
                GetEmojiName(_appearance.Next),
                GetEmojiName(_appearance.Last),
                GetEmojiName(_appearance.Close),
                GetEmojiName(_appearance.Help)
            };

            foreach (var reaction in reactions)
            {
                if (existingReactions.HasValue)
                {
                    if (existingReactions.Value!.Any(r => GetPartialEmojiName(r.Emoji) == reaction))
                    {
                        // This one is already added; skip it
                        continue;
                    }
                }

                var addReaction = await _channelAPI.CreateReactionAsync(this.ChannelID, this.MessageID, reaction, ct);
                if (!addReaction.IsSuccess)
                {
                    return addReaction;
                }
            }

            return Result.FromSuccess();
        }

        private string GetEmojiName(IEmoji emoji)
        {
            if (emoji.Name is not null)
            {
                return emoji.Name;
            }

            if (!emoji.ID.HasValue)
            {
                throw new InvalidOperationException();
            }

            return emoji.ID.Value.ToString();
        }

        private string GetPartialEmojiName(IPartialEmoji emoji)
        {
            if (emoji.Name.HasValue && emoji.Name.Value is not null)
            {
                return emoji.Name.Value;
            }

            if (!emoji.ID.HasValue)
            {
                throw new InvalidOperationException();
            }

            return emoji.ID.Value.ToString()!;
        }
    }
}
