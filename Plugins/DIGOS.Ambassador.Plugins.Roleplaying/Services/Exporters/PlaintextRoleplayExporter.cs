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

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using Discord;
using MoreLinq.Extensions;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Services.Exporters
{
    /// <summary>
    /// Exports roleplays in plaintext format.
    /// </summary>
    internal sealed class PlaintextRoleplayExporter : RoleplayExporterBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlaintextRoleplayExporter"/> class.
        /// </summary>
        /// <param name="guild">The command context for this export operation.</param>
        public PlaintextRoleplayExporter(IGuild guild)
            : base(guild)
        {
        }

        /// <inheritdoc />
        public override async Task<ExportedRoleplay> ExportAsync(Roleplay roleplay)
        {
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
}
