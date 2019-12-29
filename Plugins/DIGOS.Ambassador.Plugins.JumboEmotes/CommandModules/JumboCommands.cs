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
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.EmojiTools;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.JumboEmotes.CommandModules
{
    /// <summary>
    /// Assorted commands that don't really fit anywhere - just for fun, testing, etc.
    /// </summary>
    [UsedImplicitly]
    [Name("jumbo")]
    [Summary("Emote jumbofying commands.")]
    public class JumboCommands : ModuleBase
    {
        private readonly UserFeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="JumboCommands"/> class.
        /// </summary>
        /// <param name="feedback">The user feedback service.</param>
        public JumboCommands(UserFeedbackService feedback)
        {
            _feedback = feedback;
        }

        /// <summary>
        /// Sends a jumbo version of the given emote to the chat, if available.
        /// </summary>
        /// <param name="emoteName">The emote.</param>
        [UsedImplicitly]
        [Command("jumbo")]
        [Summary("Sends a jumbo version of the given emote to the chat, if available.")]
        public async Task JumboAsync(string emoteName)
        {
            string emoteUrl;

            var guildEmote = this.Context.Guild?.Emotes.FirstOrDefault
            (
                e => e.Name.Equals(emoteName, StringComparison.OrdinalIgnoreCase)
            );

            if (!(guildEmote is null))
            {
                emoteUrl = guildEmote.Url;
            }
            else if (Emote.TryParse(emoteName, out var emote))
            {
                emoteUrl = emote.Url;
            }
            else
            {
                if (EmojiMap.Map.TryGetValue(emoteName, out var mappedEmote))
                {
                    emoteName = mappedEmote;
                }

                var hexValues = new List<string>();
                for (var i = 0; i < emoteName.Length; ++i)
                {
                    var codepoint = char.ConvertToUtf32(emoteName, i);

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
                    if (char.IsSurrogate(emoteName, i))
                    {
                        ++i;
                    }
                }

                var emojiCode = string.Join("-", hexValues);
                emoteUrl = $"https://raw.githubusercontent.com/twitter/twemoji/master/assets/72x72/{emojiCode}.png";
            }

            using var client = new HttpClient();
            var response = await client.GetAsync(emoteUrl, HttpCompletionOption.ResponseHeadersRead);
            if (response.IsSuccessStatusCode)
            {
                var eb = _feedback.CreateEmbedBase();
                eb.WithImageUrl(emoteUrl);

                await _feedback.SendEmbedAsync(this.Context.Channel, eb.Build());
            }
            else
            {
                await _feedback.SendWarningAsync(this.Context, "Sorry, I couldn't find that emote.");
            }
        }
    }
}
