﻿//
//  PDFRoleplayExporter.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using Remora.Discord.API.Abstractions.Rest;
using Document = iTextSharp.text.Document;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Services.Exporters;

/// <summary>
/// Exports roleplays in PDF format.
/// </summary>
public sealed class PDFRoleplayExporter : IRoleplayExporter
{
    private const float _defaultParagraphSpacing = 8.0f;
    private static readonly Font _standardFont = new(Font.HELVETICA, 11.0f);
    private static readonly Font _italicFont = new(Font.HELVETICA, 11.0f, Font.ITALIC);
    private static readonly Font _titleFont = new(Font.HELVETICA, 48.0f, Font.BOLD);

    private readonly IDiscordRestGuildAPI _guildAPI;

    /// <summary>
    /// Initializes a new instance of the <see cref="PDFRoleplayExporter"/> class.
    /// </summary>
    /// <param name="guildAPI">The guild API.</param>
    public PDFRoleplayExporter(IDiscordRestGuildAPI guildAPI)
    {
        _guildAPI = guildAPI;
    }

    /// <inheritdoc />
    public async Task<ExportedRoleplay> ExportAsync(Roleplay roleplay)
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

            var getOwner = await _guildAPI.GetGuildMemberAsync(roleplay.Server.DiscordID, roleplay.Owner.DiscordID);
            if (!getOwner.IsSuccess)
            {
                var messageByUser = roleplay.Messages.FirstOrDefault
                (
                    m => m.Author == roleplay.Owner
                );

                if (messageByUser is not null)
                {
                    ownerNickname = messageByUser.AuthorNickname;
                }
            }
            else
            {
                var owner = getOwner.Entity;
                ownerNickname = owner.Nickname.HasValue
                    ? owner.Nickname.Value
                    : owner.User.HasValue
                        ? owner.User.Value.Username
                        : throw new InvalidOperationException();
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
                        var getParticipant = await _guildAPI.GetGuildMemberAsync
                        (
                            roleplay.Server.DiscordID,
                            p.User.DiscordID
                        );

                        if (!getParticipant.IsSuccess)
                        {
                            var messageByUser = roleplay.Messages.FirstOrDefault
                            (
                                m => m.Author == p.User
                            );

                            return messageByUser is null
                                ? $"Unknown user ({p.User.DiscordID})"
                                : messageByUser.AuthorNickname;
                        }

                        var participant = getParticipant.Entity;
                        return participant.Nickname.HasValue && participant.Nickname.Value is not null
                            ? participant.Nickname.Value
                            : participant.User.HasValue
                                ? participant.User.Value.Username
                                : throw new InvalidOperationException();
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
    private static Paragraph CreateTitle(string title)
    {
        var chunk = new Chunk(title, _titleFont);

        var para = new Paragraph(chunk)
        {
            SpacingAfter = _defaultParagraphSpacing
        };

        return para;
    }

    private static Paragraph CreateParticipantList(IEnumerable<string> participantNames)
    {
        var paragraph = new Paragraph
        {
            SpacingAfter = _defaultParagraphSpacing
        };

        var participantsTitleChunk = new Chunk("Participants: \n", _standardFont);
        paragraph.Add(participantsTitleChunk);

        foreach (var participantName in participantNames)
        {
            var content = $"{participantName}\n";
            var participantChunk = new Chunk(content, _italicFont);

            paragraph.Add(participantChunk);
        }

        return paragraph;
    }

    private static Paragraph CreateMessage(string author, string contents)
    {
        var authorChunk = new Chunk($"{author} \n", _italicFont);

        var para = new Paragraph
        {
            SpacingAfter = _defaultParagraphSpacing
        };

        para.Add(authorChunk);
        para.Add(FormatContentString(contents));

        para.SpacingAfter = 8.0f;

        return para;
    }

    private static Paragraph FormatContentString(string contents)
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
                var spacingChunk = new Chunk("\n", _standardFont);

                var chunk = new Chunk($"{splits[i]}", _italicFont);
                chunk.SetBackground(BaseColor.LightGray, 4, 4, 4, 4);

                subPara.Add(spacingChunk);
                subPara.Add(chunk);
                subPara.Add(spacingChunk);

                paragraph.Add(subPara);
            }
            else
            {
                var chunk = new Chunk(splits[i], _standardFont);
                paragraph.Add(chunk);
            }
        }

        return paragraph;
    }
}
