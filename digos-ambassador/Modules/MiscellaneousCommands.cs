//
//  MiscellaneousCommands.cs
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

using DIGOS.Ambassador.EmojiTools;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Services;

using Discord;
using Discord.Commands;
using Discord.Net;
using JetBrains.Annotations;
using static Discord.Commands.ContextType;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Modules
{
    /// <summary>
    /// Assorted commands that don't really fit anywhere - just for fun, testing, etc.
    /// </summary>
    [UsedImplicitly]
    [Name("miscellaneous")]
    [Summary("Assorted commands that don't really fit anywhere - just for fun, testing, etc.")]
    public class MiscellaneousCommands : ModuleBase<SocketCommandContext>
    {
        private readonly ContentService Content;

        private readonly UserFeedbackService Feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="MiscellaneousCommands"/> class.
        /// </summary>
        /// <param name="content">The content service.</param>
        /// <param name="feedback">The user feedback service.</param>
        public MiscellaneousCommands(ContentService content, UserFeedbackService feedback)
        {
            this.Content = content;
            this.Feedback = feedback;
        }

        /// <summary>
        /// Throws an exception.
        /// </summary>
        [UsedImplicitly]
        [Command("exception")]
        public Task ExceptionAsync() => throw new InvalidOperationException("Kaboom!");

        /// <summary>
        /// Instructs Amby to contact you over DM.
        /// </summary>
        [UsedImplicitly]
        [Command("contact")]
        [Summary("Instructs Amby to contact you over DM.")]
        [RequireContext(Guild)]
        public async Task ContactSelfAsync() => await ContactUserAsync(this.Context.User);

        /// <summary>
        /// Instructs Amby to contact a user over DM.
        /// </summary>
        /// <param name="discordUser">The user to contact.</param>
        [UsedImplicitly]
        [Command("contact")]
        [Summary("Instructs Amby to contact a user over DM.")]
        [RequireContext(Guild)]
        [RequireUserPermission(GuildPermission.MentionEveryone)]
        public async Task ContactUserAsync([NotNull] IUser discordUser)
        {
            if (discordUser.Id == this.Context.Client.CurrentUser.Id)
            {
                await this.Feedback.SendErrorAsync(this.Context, "That's a splendid idea - at least then, I'd get an intelligent reply.");
                return;
            }

            if (discordUser.IsBot)
            {
                await this.Feedback.SendErrorAsync(this.Context, "I could do that, but I doubt I'd get a reply.");
                return;
            }

            var eb = this.Feedback.CreateFeedbackEmbed
            (
                discordUser,
                Color.DarkPurple,
                $"Hello there, {discordUser.Mention}. I've been instructed to initiate... negotiations... with you. \nA good place to start would be the \"!help <topic>\" command."
            );

            var userDMChannel = await discordUser.GetOrCreateDMChannelAsync();
            try
            {
                await userDMChannel.SendMessageAsync(string.Empty, false, eb);
            }
            catch (HttpException hex) when (hex.WasCausedByDMsNotAccepted())
            {
                return;
            }
            finally
            {
                await userDMChannel.CloseAsync();
            }

            await this.Feedback.SendConfirmationAsync(this.Context, "User contacted.");
        }

        /// <summary>
        /// Sasses the user in a DIGOS fashion.
        /// </summary>
        [UsedImplicitly]
        [Command("sass")]
        [Summary("Sasses the user in a DIGOS fashion.")]
        public async Task SassAsync()
        {
            var isNsfwChannel = this.Context.Channel is ITextChannel textChannel && textChannel.IsNsfw;
            string sass = this.Content.GetSass(isNsfwChannel);

            await this.Feedback.SendConfirmationAsync(this.Context, sass);
        }

        /// <summary>
        /// Bweh! A silly command.
        /// </summary>
        [UsedImplicitly]
        [Command("bweh")]
        [Summary("Bweh!")]
        public async Task BwehAsync()
        {
            var eb = this.Feedback.CreateEmbedBase();
            eb.WithImageUrl(this.Content.BwehUri.ToString());

            await this.Feedback.SendEmbedAsync(this.Context.Channel, eb.Build());
        }

        /// <summary>
        /// Boops the invoking user.
        /// </summary>
        [UsedImplicitly]
        [Command("boop")]
        [Summary("Boops you.")]
        public async Task BoopAsync()
        {
            await this.Feedback.SendConfirmationAsync(this.Context, "*boop*");
        }

        /// <summary>
        /// Baps the invoking user.
        /// </summary>
        [UsedImplicitly]
        [Command("bap")]
        [Summary("Baps you.")]
        public async Task BapAsync()
        {
            await this.Feedback.SendConfirmationAsync(this.Context, "**baps**");
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

            var guildEmote = this.Context.Guild.Emotes.FirstOrDefault
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

                var emojiCode = string.Join('-', hexValues);
                emoteUrl = $"https://raw.githubusercontent.com/twitter/twemoji/gh-pages/2/72x72/{emojiCode}.png";
            }

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(emoteUrl, HttpCompletionOption.ResponseHeadersRead);
                if (response.IsSuccessStatusCode)
                {
                    var eb = this.Feedback.CreateEmbedBase();
                    eb.WithImageUrl(emoteUrl);

                    await this.Feedback.SendEmbedAsync(this.Context.Channel, eb.Build());
                }
                else
                {
                    await this.Feedback.SendWarningAsync(this.Context, "Sorry, I couldn't find that emote.");
                }
            }
        }

        /// <summary>
        /// Boops the target user.
        /// </summary>
        /// <param name="target">The target.</param>
        [UsedImplicitly]
        [Command("boop")]
        [Summary("Boops the user.")]
        public async Task BoopAsync([NotNull] IUser target)
        {
            if (target.IsMe(this.Context.Client))
            {
                await this.Feedback.SendConfirmationAsync(this.Context, "...seriously?");
                await this.Feedback.SendConfirmationAsync(this.Context, $"*boops {this.Context.User.Mention}*");

                return;
            }

            await this.Feedback.SendConfirmationAsync(this.Context, $"*boops {target.Mention}*");
        }

        /// <summary>
        /// Baps the target user.
        /// </summary>
        /// <param name="target">The target.</param>
        [UsedImplicitly]
        [Command("bap")]
        [Summary("Baps the user.")]
        public async Task BapAsync([NotNull] IUser target)
        {
            if (target.IsMe(this.Context.Client))
            {
                await this.Feedback.SendConfirmationAsync(this.Context, "...seriously?");
                await this.Feedback.SendConfirmationAsync(this.Context, $"**baps {this.Context.User.Mention}**");

                return;
            }

            await this.Feedback.SendConfirmationAsync(this.Context, $"**baps {target.Mention}**");
        }

        /// <summary>
        /// Shows some information about Amby's metaworkings.
        /// </summary>
        [UsedImplicitly]
        [Alias("info", "information", "about")]
        [Command("info")]
        [Summary("Shows some information about Amby's metaworkings.")]
        public async Task InfoAsync()
        {
            var eb = this.Feedback.CreateEmbedBase();

            eb.WithAuthor(this.Context.Client.CurrentUser);
            eb.WithTitle("The DIGOS Ambassador (\"Amby\")");
            eb.WithImageUrl(this.Content.AmbyPortraitUri.ToString());

            eb.WithDescription
            (
                "Amby is a Discord bot written in C# using the Discord.Net and EF Core frameworks. As an ambassador for " +
                "the DIGOS community, she provides a number of useful services for communities with similar interests - " +
                "namely, roleplaying, transformation, weird and wonderful sexual kinks, and much more.\n" +
                "\n" +
                "Amby is free and open source software, licensed under the AGPLv3. All of her source code can be freely " +
                "viewed and improved on Github at https://github.com/Nihlus/digos-ambassador. You are free to " +
                "run your own instance of Amby, redistribute her code, and modify it to your heart's content. If you're " +
                "not familiar with the AGPL, an excellent summary is available here: " +
                "https://choosealicense.com/licenses/agpl-3.0/.\n" +
                "\n" +
                "Any bugs you encounter should be reported on Github, following the issue template provided there. The " +
                "same holds for feature requests, for which a separate template is provided. Contributions in the form " +
                "of code, artwork, bug triaging, or quality control testing is always greatly appreciated!\n" +
                "\n" +
                "Stay sharky~\n" +
                "- Amby"
            );

            await this.Feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, eb.Build());
        }
    }
}
