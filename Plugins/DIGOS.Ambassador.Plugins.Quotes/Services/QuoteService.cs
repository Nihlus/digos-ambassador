//
//  QuoteService.cs
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

using System.Linq;
using Discord;
using Humanizer;
using Humanizer.Bytes;

namespace DIGOS.Ambassador.Plugins.Quotes.Services
{
    /// <summary>
    /// Handles creation of quotes.
    /// </summary>
    public class QuoteService
    {
        /// <summary>
        /// Creates a message quote.
        /// </summary>
        /// <param name="message">The message to quote.</param>
        /// <param name="quotingUser">The user that is quoting the message.</param>
        /// <returns>The quote.</returns>
        public EmbedBuilder CreateMessageQuote(IMessage message, IMentionable quotingUser)
        {
            var eb = new EmbedBuilder();

            if (TryCopyRichEmbed(message, quotingUser, ref eb))
            {
                return eb;
            }

            if (!TryAddImageAttachmentInfo(message, ref eb))
            {
                TryAddOtherAttachmentInfo(message, ref eb);
            }

            AddContent(message, ref eb);
            AddOtherEmbed(message, ref eb);
            AddActivity(message, ref eb);
            AddMeta(message, quotingUser, ref eb);

            return eb;
        }

        /// <summary>
        /// Attempts to add information about attached images, if they exist.
        /// </summary>
        /// <param name="message">The quoted message.</param>
        /// <param name="embed">The embed to add the information to.</param>
        /// <returns>true if information was added; otherwise, false.</returns>
        private bool TryAddImageAttachmentInfo(IMessage message, ref EmbedBuilder embed)
        {
            var firstAttachment = message.Attachments.FirstOrDefault();
            if (firstAttachment is null || firstAttachment.Height is null)
            {
                return false;
            }

            embed.WithImageUrl(firstAttachment.Url);

            return true;
        }

        /// <summary>
        /// Attempts to add information about an attachment, if it exists.
        /// </summary>
        /// <param name="message">The quoted message.</param>
        /// <param name="embed">The embed to add the information to.</param>
        private void TryAddOtherAttachmentInfo(IMessage message, ref EmbedBuilder embed)
        {
            var firstAttachment = message.Attachments.FirstOrDefault();
            if (firstAttachment is null)
            {
                return;
            }

            embed.AddField($"Attachment (Size: {new ByteSize(firstAttachment.Size)})", firstAttachment.Url);
        }

        /// <summary>
        /// Attempts to copy the full rich embed from a message, if it has one.
        /// </summary>
        /// <param name="message">The quoted message.</param>
        /// <param name="executingUser">The user that quoted the message.</param>
        /// <param name="embed">The embed to replace.</param>
        /// <returns>true if a rich embed was copied; otherwise, false.</returns>
        private bool TryCopyRichEmbed
        (
            IMessage message,
            IMentionable executingUser,
            ref EmbedBuilder embed
        )
        {
            var firstEmbed = message.Embeds.FirstOrDefault();
            if (firstEmbed?.Type != EmbedType.Rich)
            {
                return false;
            }

            embed = message.Embeds
                .First()
                .ToEmbedBuilder()
                .AddField
                (
                    "Quoted by",
                    $"{executingUser.Mention} from **[#{message.Channel.Name}]({message.GetJumpUrl()})**",
                    true
                );

            if (firstEmbed.Color is null)
            {
                embed.Color = Color.DarkGrey;
            }

            return true;
        }

        /// <summary>
        /// Adds information about the activity a message was involved in, if present.
        /// </summary>
        /// <param name="message">The quoted message.</param>
        /// <param name="embed">The embed to add the information to.</param>
        private void AddActivity(IMessage message, ref EmbedBuilder embed)
        {
            if (message.Activity is null)
            {
                return;
            }

            embed
                .AddField("Invite Type", message.Activity.Type)
                .AddField("Party Id", message.Activity.PartyId);
        }

        /// <summary>
        /// Adds information about other embeds in a quoted message.
        /// </summary>
        /// <param name="message">The quoted message.</param>
        /// <param name="embed">The embed to add the information to.</param>
        private void AddOtherEmbed(IMessage message, ref EmbedBuilder embed)
        {
            if (!message.Embeds.Any())
            {
                return;
            }

            embed
                .AddField("Embed Type", message.Embeds.First().Type);
        }

        /// <summary>
        /// Adds the content of the quoted message to the embed.
        /// </summary>
        /// <param name="message">The quoted message.</param>
        /// <param name="embed">The embed to add the content to.</param>
        private void AddContent(IMessage message, ref EmbedBuilder embed)
        {
            if (string.IsNullOrWhiteSpace(message.Content))
            {
                return;
            }

            if (message.Channel is IGuildChannel guildChannel)
            {
                var messageUrl = $"https://discordapp.com/channels/" +
                                 $"{guildChannel.Guild.Id}/{guildChannel.Id}/{message.Id}";

                embed.WithUrl(messageUrl);
            }

            embed.WithDescription(message.Content);
        }

        /// <summary>
        /// Adds meta information about the quote to the embed.
        /// </summary>
        /// <param name="message">The quoted message.</param>
        /// <param name="quotingUser">The quoting user.</param>
        /// <param name="embed">The embed to add the information to.</param>
        private void AddMeta
        (
            IMessage message,
            IMentionable quotingUser,
            ref EmbedBuilder embed
        )
        {
            embed
                .WithAuthor(message.Author)
                .WithFooter(GetPostedTimeInfo(message))
                .WithColor(new Color(95, 186, 125))
                .AddField
                (
                    "Quoted by",
                    $"{quotingUser.Mention} from **[#{message.Channel.Name}]({message.GetJumpUrl()})**",
                    true
                );
        }

        /// <summary>
        /// Gets a formatted string that explains when the message was posted.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The formatted time.</returns>
        private static string GetPostedTimeInfo(IMessage message)
        {
            return $"{message.Timestamp.DateTime.ToOrdinalWords()} " +
                   $"at {message.Timestamp:HH:mm}, " +
                   $"in #{message.Channel.Name}";
        }
    }
}
