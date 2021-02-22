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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Humanizer;
using Humanizer.Bytes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;

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
        public Embed CreateMessageQuote(IMessage message, Snowflake quotingUser)
        {
            var eb = new Embed();

            // Copy the message's rich embed directly, if it exists
            var firstEmbed = message.Embeds.FirstOrDefault();
            if (firstEmbed is not null && firstEmbed.Type.HasValue && firstEmbed.Type.Value is EmbedType.Rich)
            {
                var fields = new List<IEmbedField>();
                if (firstEmbed.Fields.HasValue)
                {
                    fields.AddRange(firstEmbed.Fields.Value!);
                }

                fields.Add(CreateQuoteMarkerField(message, quotingUser));

                return eb with
                {
                    Title = firstEmbed.Title,
                    Type = firstEmbed.Type,
                    Description = firstEmbed.Description,
                    Url = firstEmbed.Url,
                    Timestamp = firstEmbed.Timestamp,
                    Colour = firstEmbed.Colour,
                    Footer = firstEmbed.Footer,
                    Image = firstEmbed.Image,
                    Thumbnail = firstEmbed.Thumbnail,
                    Video = firstEmbed.Video,
                    Provider = firstEmbed.Provider,
                    Author = firstEmbed.Author,
                    Fields = fields
                };
            }

            eb = AddAttachmentInfo(message, eb);
            eb = AddContent(message, eb);
            eb = AddOtherEmbed(message, eb);
            eb = AddActivity(message, eb);
            eb = AddMeta(message, quotingUser, eb);

            return eb;
        }

        /// <summary>
        /// Attempts to add information about attached images, if they exist.
        /// </summary>
        /// <param name="message">The quoted message.</param>
        /// <param name="embed">The embed to add the information to.</param>
        /// <returns>true if information was added; otherwise, false.</returns>
        private Embed AddAttachmentInfo(IMessage message, Embed embed)
        {
            var firstAttachment = message.Attachments.FirstOrDefault();
            if (firstAttachment is null)
            {
                return embed;
            }

            if (firstAttachment.Height is not null)
            {
                return embed with { Image = new EmbedImage(firstAttachment.Url) };
            }

            var fields = new List<IEmbedField>();
            if (embed.Fields.HasValue)
            {
                fields.AddRange(embed.Fields.Value!);
            }

            fields.Add
            (
                new EmbedField($"Attachment (Size: {new ByteSize(firstAttachment.Size)})", firstAttachment.Url)
            );

            return embed with { Fields = fields };
        }

        /// <summary>
        /// Adds information about the activity a message was involved in, if present.
        /// </summary>
        /// <param name="message">The quoted message.</param>
        /// <param name="embed">The embed to add the information to.</param>
        private Embed AddActivity(IMessage message, Embed embed)
        {
            if (!message.Activity.HasValue)
            {
                return embed;
            }

            var activity = message.Activity.Value;

            var fields = new List<IEmbedField>();
            if (embed.Fields.HasValue)
            {
                fields.AddRange(embed.Fields.Value!);
            }

            fields.Add(new EmbedField("Invite type", activity!.Type.ToString()));

            if (activity.PartyID.HasValue)
            {
                fields.Add(new EmbedField("Party ID", activity!.PartyID.Value!));
            }

            return embed with { Fields = fields };
        }

        /// <summary>
        /// Adds information about other embeds in a quoted message.
        /// </summary>
        /// <param name="message">The quoted message.</param>
        /// <param name="embed">The embed to add the information to.</param>
        private Embed AddOtherEmbed(IMessage message, Embed embed)
        {
            if (!message.Embeds.Any())
            {
                return embed;
            }

            var firstEmbed = message.Embeds.First();
            if (!firstEmbed.Type.HasValue)
            {
                return embed;
            }

            var fields = new List<IEmbedField>();
            if (embed.Fields.HasValue)
            {
                fields.AddRange(embed.Fields.Value!);
            }

            fields.Add(new EmbedField("Embed Type", message.Embeds.First().Type.Value.ToString()));

            return embed with
            {
                Fields = fields
            };
        }

        /// <summary>
        /// Adds the content of the quoted message to the embed.
        /// </summary>
        /// <param name="message">The quoted message.</param>
        /// <param name="embed">The embed to add the content to.</param>
        private Embed AddContent(IMessage message, Embed embed)
        {
            if (string.IsNullOrWhiteSpace(message.Content))
            {
                return embed;
            }

            return embed with
            {
                Url = GetJumpUrl(message),
                Description = message.Content
            };
        }

        /// <summary>
        /// Adds meta information about the quote to the embed.
        /// </summary>
        /// <param name="message">The quoted message.</param>
        /// <param name="quotingUser">The quoting user.</param>
        /// <param name="embed">The embed to add the information to.</param>
        private Embed AddMeta
        (
            IMessage message,
            Snowflake quotingUser,
            Embed embed
        )
        {
            EmbedAuthor author;
            if (message.Author.Avatar is not null)
            {
                author = new EmbedAuthor
                {
                    Name = $"<@{message.Author.ID}>",
                    Url = $"https://cdn.discordsapp.com/avatars/{message.Author.ID}/{message.Author.Avatar.Value}.png"
                };
            }
            else
            {
                author = new EmbedAuthor
                {
                    Name = $"<@{message.Author.ID}>"
                };
            }

            return embed with
            {
                Author = author,
                Footer = new EmbedFooter(GetPostedTimeInfo(message)),
                Colour = Color.FromArgb(95, 186, 125),
                Fields = new[]
                {
                    CreateQuoteMarkerField(message, quotingUser)
                }
            };
        }

        private static EmbedField CreateQuoteMarkerField(IMessage message, Snowflake quotingUser)
        {
            return new
            (
                "Quoted by",
                $"<@{quotingUser}> from **[<#{message.ChannelID}>]({GetJumpUrl(message)})**",
                true
            );
        }

        private static string GetJumpUrl(IMessage message)
        {
            var guildID = message.GuildID.HasValue ? message.GuildID.Value.ToString() : "@me";
            return $"https://discord.com/channels/{guildID}/{message.ChannelID}/{message.ID}";
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
                   $"in <#{message.ChannelID}>";
        }
    }
}
