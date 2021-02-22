//
//  JumboCommands.cs
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
using System.ComponentModel;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.JumboEmotes.EmojiTools;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.JumboEmotes.CommandModules
{
    /// <summary>
    /// Assorted commands that don't really fit anywhere - just for fun, testing, etc.
    /// </summary>
    [UsedImplicitly]
    public class JumboCommands : CommandGroup
    {
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly ICommandContext _context;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="JumboCommands"/> class.
        /// </summary>
        /// <param name="channelAPI">The Discord channel API.</param>
        /// <param name="context">The command context.</param>
        /// <param name="httpClient">The HTTP client.</param>
        public JumboCommands
        (
            IDiscordRestChannelAPI channelAPI,
            ICommandContext context,
            HttpClient httpClient
        )
        {
            _httpClient = httpClient;
            _channelAPI = channelAPI;
            _context = context;
        }

        /// <summary>
        /// Sends a jumbo version of the given emote to the chat, if available.
        /// </summary>
        /// <param name="emoji">The emote.</param>
        [UsedImplicitly]
        [Command("jumbo")]
        [Description("Sends a jumbo version of the given emote to the chat, if available.")]
        public async Task<IResult> JumboAsync(IEmoji emoji)
        {
            string emoteUrl;
            if (emoji.ID is not null)
            {
                emoteUrl = $"https://cdn.discordapp.com/emojis/${emoji.ID}.png";
            }
            else
            {
                if (emoji.Name is null)
                {
                    return Result.FromError(new GenericError("Looks like a bad emoji. Oops!"));
                }

                var emojiName = emoji.Name;
                if (EmojiMap.Map.TryGetValue(emoji.Name, out var mappedEmote))
                {
                    emojiName = mappedEmote;
                }

                var hexValues = new List<string>();
                for (var i = 0; i < emojiName.Length; ++i)
                {
                    var codepoint = char.ConvertToUtf32(emojiName, i);

                    // 0xFE0F is a variation marker, which explicitly requests a colourful version of the emoji, and
                    // not a monochrome text variant. Since Twemoji only provides the colourful ones, we can safely
                    // skip it.
                    if (codepoint == 0xFE0F)
                    {
                        continue;
                    }

                    var codepointHex = codepoint.ToString("x");
                    hexValues.Add(codepointHex);

                    // ConvertToUtf32() might have parsed an extra character as some characters are combinations of two
                    // 16-bit characters which start at 0x00d800 and end at 0x00dfff (Called surrogate low and surrogate
                    // high)
                    //
                    // If the character is in this span, we have already essentially parsed the next index of the string
                    // as well. Therefore we make sure to skip the next one.
                    if (char.IsSurrogate(emojiName, i))
                    {
                        ++i;
                    }
                }

                var emojiCode = string.Join("-", hexValues);
                emoteUrl = $"https://raw.githubusercontent.com/twitter/twemoji/master/assets/72x72/{emojiCode}.png";
            }

            var response = await _httpClient.GetAsync(emoteUrl, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                return Result.FromError(new GenericError("Sorry, I couldn't find that emote."));
            }

            var eb = new Embed
            {
                Colour = Color.Purple,
                Image = new EmbedImage { Url = emoteUrl }
            };

            var sendEmoji = await _channelAPI.CreateMessageAsync
            (
                _context.ChannelID,
                embed: eb,
                ct: this.CancellationToken
            );

            return sendEmoji.IsSuccess ? Result.FromSuccess() : Result.FromError(sendEmoji);
        }
    }
}
