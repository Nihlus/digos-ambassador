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

using DIGOS.Ambassador.Database.Roleplaying;
using DIGOS.Ambassador.Extensions;

using Discord;
using Discord.Commands;

using iTextSharp.text;
using iTextSharp.text.pdf;
using JetBrains.Annotations;
using MoreLinq;

namespace DIGOS.Ambassador.Services.Exporters
{
    /// <summary>
    /// Exports roleplays in PDF format.
    /// </summary>
    public class PDFRoleplayExporter : RoleplayExporterBase
    {
        private const float DefaultParagraphSpacing = 8.0f;
        private static readonly Font StandardFont = new Font(Font.FontFamily.HELVETICA, 11.0f);
        private static readonly Font ItalicFont = new Font(Font.FontFamily.HELVETICA, 11.0f, Font.ITALIC);
        private static readonly Font TitleFont = new Font(Font.FontFamily.HELVETICA, 48.0f, Font.BOLD);

        /// <summary>
        /// Initializes a new instance of the <see cref="PDFRoleplayExporter"/> class.
        /// </summary>
        /// <param name="context">The context of the export operation.</param>
        public PDFRoleplayExporter(ICommandContext context)
            : base(context)
        {
        }

        /// <inheritdoc />
        public override async Task<ExportedRoleplay> ExportAsync(Roleplay roleplay)
        {
            // Create our document
            var pdfDoc = new Document();

            var filePath = Path.GetTempFileName();
            using (var of = File.Create(filePath))
            {
                using (PdfWriter.GetInstance(pdfDoc, of))
                {
                    pdfDoc.Open();

                    var owner = await this.Context.Guild.GetUserAsync((ulong)roleplay.Owner.DiscordID);

                    pdfDoc.AddAuthor(owner.Nickname);
                    pdfDoc.AddCreationDate();
                    pdfDoc.AddCreator("DIGOS Ambassador");
                    pdfDoc.AddTitle(roleplay.Name);

                    var joinedUsers = await Task.WhenAll(roleplay.JoinedUsers.Select(p => this.Context.Guild.GetUserAsync((ulong)p.User.DiscordID)));

                    pdfDoc.Add(CreateTitle(roleplay.Name));
                    pdfDoc.Add(CreateParticipantList(joinedUsers));

                    pdfDoc.NewPage();

                    var messages = roleplay.Messages.OrderBy(m => m.Timestamp).DistinctBy(m => m.Contents);
                    foreach (var message in messages)
                    {
                        pdfDoc.Add(CreateMessage(message.AuthorNickname, message.Contents));
                    }

                    pdfDoc.Close();
                }
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
        [NotNull]
        private Paragraph CreateTitle(string title)
        {
            var chunk = new Chunk(title, TitleFont);

            var para = new Paragraph(chunk)
            {
                SpacingAfter = DefaultParagraphSpacing
            };

            return para;
        }

        [NotNull]
        private Paragraph CreateParticipantList([NotNull] IEnumerable<IGuildUser> participants)
        {
            var paragraph = new Paragraph
            {
                SpacingAfter = DefaultParagraphSpacing
            };

            var participantsTitleChunk = new Chunk("Participants: \n", StandardFont);
            paragraph.Add(participantsTitleChunk);

            foreach (var participant in participants)
            {
                var content = $"{participant.Username}\n";
                var participantChunk = new Chunk(content, ItalicFont);

                paragraph.Add(participantChunk);
            }

            return paragraph;
        }

        [NotNull]
        private Paragraph CreateMessage([NotNull] string author, [NotNull] string contents)
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

        [NotNull]
        private Paragraph FormatContentString([NotNull] string contents)
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
                    chunk.SetBackground(BaseColor.LIGHT_GRAY, 4, 4, 4, 4);

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
