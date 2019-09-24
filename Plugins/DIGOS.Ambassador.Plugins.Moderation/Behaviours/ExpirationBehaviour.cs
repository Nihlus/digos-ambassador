//
//  ExpirationBehaviour.cs
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Behaviours;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Moderation.Behaviours
{
    /// <summary>
    /// Rescinds expired warnings and bans.
    /// </summary>
    [UsedImplicitly]
    internal sealed class ExpirationBehaviour : ContinuousBehaviour
    {
        [NotNull]
        private readonly WarningService _warnings;

        [NotNull]
        private readonly BanService _bans;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpirationBehaviour"/> class.
        /// </summary>
        /// <param name="client">The Discord client.</param>
        /// <param name="warnings">The warning service.</param>
        /// <param name="bans">The ban service.</param>
        public ExpirationBehaviour
        (
            [NotNull] DiscordSocketClient client,
            [NotNull] WarningService warnings,
            [NotNull] BanService bans
        )
            : base(client)
        {
            _warnings = warnings;
            _bans = bans;
        }

        /// <inheritdoc/>
        protected override async Task OnTickAsync(CancellationToken ct)
        {
            if (this.Client.ConnectionState != ConnectionState.Connected)
            {
                // Give the client some time to start up
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                return;
            }

            var now = DateTime.UtcNow;

            foreach (var guild in this.Client.Guilds)
            {
                var warnings = _warnings.GetWarnings(guild).Where(w => w.IsTemporary);
                foreach (var warning in warnings)
                {
                    if (warning.ExpiresOn <= now)
                    {
                        await _warnings.DeleteWarningAsync(warning);
                    }
                }

                if (!guild.GetUser(this.Client.CurrentUser.Id).GuildPermissions.BanMembers)
                {
                    // No point in trying to rescind bans if the bot doesn't have ban perms
                    continue;
                }

                var bans = _bans.GetBans(guild).Where(b => b.IsTemporary);
                foreach (var ban in bans)
                {
                    if (ban.ExpiresOn <= now)
                    {
                        await _bans.DeleteBanAsync(ban);
                        await guild.RemoveBanAsync((ulong)ban.User.DiscordID);
                    }
                }
            }

            await Task.Delay(TimeSpan.FromHours(1), ct);
        }
    }
}
