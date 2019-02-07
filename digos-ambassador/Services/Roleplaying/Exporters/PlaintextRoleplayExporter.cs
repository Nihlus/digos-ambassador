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

using DIGOS.Ambassador.Database.Roleplaying;

using Discord.Commands;
using MoreLinq;

namespace DIGOS.Ambassador.Services.Exporters
{
    /// <summary>
    /// Exports roleplays in plaintext format.
    /// </summary>
    public class PlaintextRoleplayExporter : RoleplayExporterBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlaintextRoleplayExporter"/> class.
        /// </summary>
        /// <param name="context">The command context for this export operation.</param>
        public PlaintextRoleplayExporter(ICommandContext context)
            : base(context)
        {
        }

        /// <inheritdoc />
        public override async Task<ExportedRoleplay> ExportAsync(Roleplay roleplay)
        {
            var owner = await this.Context.Guild.GetUserAsync((ulong)roleplay.Owner.DiscordID);

            var filePath = Path.GetTempFileName();
            using (var of = File.Create(filePath))
            {
                using (var ofw = new StreamWriter(of))
                {
                    await ofw.WriteLineAsync($"Roleplay name: {roleplay.Name}");
                    await ofw.WriteLineAsync($"Owner: {owner.Username}");

                    var joinedUsers = await Task.WhenAll(roleplay.JoinedUsers.Select(p => this.Context.Guild.GetUserAsync((ulong)p.User.DiscordID)));

                    await ofw.WriteLineAsync("Participants:");
                    foreach (var participant in joinedUsers)
                    {
                        await ofw.WriteLineAsync(participant.Username);
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
            }

            var resultFile = File.OpenRead(filePath);
            var exported = new ExportedRoleplay(roleplay.Name, ExportFormat.Plaintext, resultFile);
            return exported;
        }
    }
}
