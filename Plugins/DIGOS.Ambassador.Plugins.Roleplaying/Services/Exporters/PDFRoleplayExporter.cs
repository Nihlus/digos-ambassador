//
//  PDFRoleplayExporter.cs
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MoreLinq.Extensions;
using Remora.Discord.API.Abstractions.Objects;
using Document = iTextSharp.text.Document;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Services.Exporters
{
    /// <summary>
    /// Exports roleplays in PDF format.
    /// </summary>
    internal sealed class PDFRoleplayExporter : RoleplayExporterBase
    {
        private const float DefaultParagraphSpacing = 8.0f;
        private static readonly Font StandardFont = new Font(Font.HELVETICA, 11.0f);
        private static readonly Font ItalicFont = new Font(Font.HELVETICA, 11.0f, Font.ITALIC);
        private static readonly Font TitleFont = new Font(Font.HELVETICA, 48.0f, Font.BOLD);

        /// <summary>
        /// Initializes a new instance of the <see cref="PDFRoleplayExporter"/> class.
        /// </summary>
        /// <param name="guild">The context of the export operation.</param>
        public PDFRoleplayExporter(IGuild guild)
            : base(guild)
        {
        }

        /// <inheritdoc />
        public override async Task<ExportedRoleplay> ExportAsync(Roleplay roleplay)
        {
            // Create our document
            var pdfDoc = new Document();

            var filePath = Path.GetTempFileName();
            await using (var of = File.Create(filePath))
            {
                var writer = PdfWriter.GetInstance(pdfDoc, of);
                writer.Open();
                pdfDoc.Open();

                var ownerNickname = $"Unknown user ({roleplay.Owner.DiscordID})";

                var owner = await this.Guild.GetUserAsync((ulong)roleplay.Owner.DiscordID);
                if (!(owner is null))
                {
                    ownerNickname = owner.Nickname ?? owner.Username;
                }
                else
                {
                    var messageByUser = roleplay.Messages.FirstOrDefault
                    (
                        m => m.Author == roleplay.Owner
                    );

                    if (!(messageByUser is null))
                    {
                        ownerNickname = messageByUser.AuthorNickname;
                    }
                }

                pdfDoc.AddAuthor(ownerNickname);
                pdfDoc.AddCreationDate();
                pdfDoc.AddCreator("DIGOS Ambassador");
                pdfDoc.AddTitle(roleplay.Name);

                var joinedUsers = await Task.WhenAll
                (
                    roleplay.JoinedUsers.Select
                    (
                        async p =>
                        {
                            var guildUser = await this.Guild.GetUserAsync((ulong)p.User.DiscordID);
                            if (!(guildUser is null))
                            {
                                return guildUser.Username;
                            }

                            var messageByUser = roleplay.Messages.FirstOrDefault
                            (
                                m => m.Author == p.User
                            );

                            if (messageByUser is null)
                            {
                                return $"Unknown user ({p.User.DiscordID})";
                            }

                            return messageByUser.AuthorNickname;
                        }
                    )
                );

                pdfDoc.Add(CreateTitle(roleplay.Name));
                pdfDoc.Add(CreateParticipantList(joinedUsers));

                pdfDoc.NewPage();

                var messages = roleplay.Messages.OrderBy(m => m.Timestamp).DistinctBy(m => m.Contents);
                foreach (var message in messages)
                {
                    pdfDoc.Add(CreateMessage(message.AuthorNickname, message.Contents));
                }

                pdfDoc.Close();
                writer.Flush();
                writer.Close();
            }

            var resultFile = File.OpenRead(filePath);
            var exported = new ExportedRoleplay(roleplay.Name, ExportFormat.PDF, resultFile);
            return exported;
        }

        /// <summary>
        /// Creates a title that can be inserted into a PDF document.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <returns>The resulting paragraph.</returns>
        private Paragraph CreateTitle(string title)
        {
            var chunk = new Chunk(title, TitleFont);

            var para = new Paragraph(chunk)
            {
                SpacingAfter = DefaultParagraphSpacing
            };

            return para;
        }

        private Paragraph CreateParticipantList(IEnumerable<string> participantNames)
        {
            var paragraph = new Paragraph
            {
                SpacingAfter = DefaultParagraphSpacing
            };

            var participantsTitleChunk = new Chunk("Participants: \n", StandardFont);
            paragraph.Add(participantsTitleChunk);

            foreach (var participantName in participantNames)
            {
                var content = $"{participantName}\n";
                var participantChunk = new Chunk(content, ItalicFont);

                paragraph.Add(participantChunk);
            }

            return paragraph;
        }

        private Paragraph CreateMessage(string author, string contents)
        {
            var authorChunk = new Chunk($"{author} \n", ItalicFont);

            var para = new Paragraph
            {
                SpacingAfter = DefaultParagraphSpacing
            };

            para.Add(authorChunk);
            para.Add(FormatContentString(contents));

            para.SpacingAfter = 8.0f;

            return para;
        }

        private Paragraph FormatContentString(string contents)
        {
            var splits = contents.Split(new[] { "```" }, StringSplitOptions.None).Select(s => s.TrimStart('\n')).ToList();
            var paragraph = new Paragraph();

            for (var i = 0; i < splits.Count; ++i)
            {
                if (splits[i].IsNullOrWhitespace())
                {
                    continue;
                }

                if (i % 2 == 1)
                {
                    var subPara = new Paragraph();
                    var spacingChunk = new Chunk("\n", StandardFont);

                    var chunk = new Chunk($"{splits[i]}", ItalicFont);
                    chunk.SetBackground(BaseColor.LightGray, 4, 4, 4, 4);

                    subPara.Add(spacingChunk);
                    subPara.Add(chunk);
                    subPara.Add(spacingChunk);

                    paragraph.Add(subPara);
                }
                else
                {
                    var chunk = new Chunk(splits[i], StandardFont);
                    paragraph.Add(chunk);
                }
            }

            return paragraph;
        }
    }
}
