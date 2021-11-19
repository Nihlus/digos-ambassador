//
//  PlaintextRoleplayExporter.cs
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.API.Abstractions.Rest;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Services.Exporters;

/// <summary>
/// Exports roleplays in plaintext format.
/// </summary>
internal sealed class PlaintextRoleplayExporter : RoleplayExporterBase
{
    /// <inheritdoc />
    public override async Task<ExportedRoleplay> ExportAsync(IServiceProvider services, Roleplay roleplay)
    {
        var guildAPI = services.GetRequiredService<IDiscordRestGuildAPI>();

        var ownerNickname = $"Unknown user ({roleplay.Owner.DiscordID})";

        var getOwner = await guildAPI.GetGuildMemberAsync(roleplay.Server.DiscordID, roleplay.Owner.DiscordID);
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

        var filePath = Path.GetTempFileName();
        await using (var of = File.Create(filePath))
        {
            await using var ofw = new StreamWriter(of);
            await ofw.WriteLineAsync($"Roleplay name: {roleplay.Name}");
            await ofw.WriteLineAsync($"Owner: {ownerNickname}");

            var joinedUsers = await Task.WhenAll
            (
                roleplay.JoinedUsers.Select
                (
                    async p =>
                    {
                        var getParticipant = await guildAPI.GetGuildMemberAsync
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
                        return participant.Nickname.HasValue
                            ? participant.Nickname.Value
                            : participant.User.HasValue
                                ? participant.User.Value.Username
                                : throw new InvalidOperationException();
                    }
                )
            );

            await ofw.WriteLineAsync("Participants:");
            foreach (var participant in joinedUsers)
            {
                await ofw.WriteLineAsync(participant);
            }

            await ofw.WriteLineAsync();
            await ofw.WriteLineAsync();

            var messages = roleplay.Messages.OrderBy(m => m.Timestamp).DistinctBy(m => m.Contents);
            foreach (var message in messages)
            {
                await ofw.WriteLineAsync($"{message.AuthorNickname}: \n{message.Contents}");
                await ofw.WriteLineAsync();
            }
        }

        var resultFile = File.OpenRead(filePath);
        var exported = new ExportedRoleplay(roleplay.Name, ExportFormat.Plaintext, resultFile);
        return exported;
    }
}
