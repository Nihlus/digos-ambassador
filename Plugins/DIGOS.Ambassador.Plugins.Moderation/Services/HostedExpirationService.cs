//
//  HostedExpirationService.cs
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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Rest.Results;
using Remora.Results;
using static Remora.Discord.API.Abstractions.Results.DiscordError;

namespace DIGOS.Ambassador.Plugins.Moderation.Services;

/// <summary>
/// Rescinds expired warnings and bans.
/// </summary>
[UsedImplicitly]
internal sealed class HostedExpirationService : BackgroundService
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostedExpirationService"/> class.
    /// </summary>
    /// <param name="services">The services.</param>
    public HostedExpirationService(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct))
        {
            await using var scope = _services.CreateAsyncScope();

            var scopedExpirationService = scope.ServiceProvider.GetRequiredService<ScopedExpirationService>();
            await scopedExpirationService.ExecuteAsync(ct);
        }
    }

    /// <summary>
    /// Handles scoped execution of the actual service code.
    /// </summary>
    internal sealed class ScopedExpirationService
    {
        private readonly ILogger<ScopedExpirationService> _log;
        private readonly ChannelLoggingService _loggingService;
        private readonly IDiscordRestGuildAPI _guildAPI;
        private readonly IDiscordRestUserAPI _userAPI;
        private readonly WarningService _warningService;
        private readonly BanService _banService;

        private readonly TransactionOptions _transactionOptions = new()
        {
            Timeout = TransactionManager.DefaultTimeout,
            IsolationLevel = IsolationLevel.Serializable
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ScopedExpirationService"/> class.
        /// </summary>
        /// <param name="log">The logging instance for this type.</param>
        /// <param name="loggingService">The channel logging service.</param>
        /// <param name="guildAPI">The guild API.</param>
        /// <param name="userAPI">The user API.</param>
        /// <param name="warningService">The warning service.</param>
        /// <param name="banService">The ban service.</param>
        public ScopedExpirationService
        (
            ILogger<ScopedExpirationService> log,
            ChannelLoggingService loggingService,
            IDiscordRestGuildAPI guildAPI,
            IDiscordRestUserAPI userAPI,
            WarningService warningService,
            BanService banService
        )
        {
            _log = log;
            _loggingService = loggingService;
            _guildAPI = guildAPI;
            _userAPI = userAPI;
            _warningService = warningService;
            _banService = banService;
        }

        /// <summary>
        /// Executes the service's workload.
        /// </summary>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            var joinedGuilds = new List<Snowflake>();
            await foreach (var get in GetGuildsAsync(ct))
            {
                if (get.IsSuccess)
                {
                    joinedGuilds.Add(get.Entity);
                }
            }

            await RescindExpiredWarningsAsync(joinedGuilds, ct);
            await RescindExpiredBansAsync(joinedGuilds, ct);
        }

        private async Task RescindExpiredWarningsAsync(IReadOnlyList<Snowflake> joinedGuilds, CancellationToken ct)
        {
            var expiredWarnings = await _warningService.GetExpiredWarningsAsync(ct);

            foreach (var expiredWarning in expiredWarnings)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                // We'll use a transaction per warning to avoid timeouts
                using var warningTransaction = new TransactionScope
                (
                    TransactionScopeOption.Required,
                    _transactionOptions,
                    TransactionScopeAsyncFlowOption.Enabled
                );

                if (!joinedGuilds.Contains(expiredWarning.Server.DiscordID))
                {
                    var deleteOldResult = await _warningService.DeleteWarningAsync(expiredWarning, ct);
                    if (!deleteOldResult.IsSuccess)
                    {
                        // skip to the next one if we can't delete this
                        _log.LogWarning("Failed to delete warning: {Reason}", deleteOldResult.Error.Message);
                        continue;
                    }

                    warningTransaction.Complete();
                    continue;
                }

                var notifyResult = await _loggingService.NotifyUserWarningRemovedAsync
                (
                    expiredWarning,
                    null
                );

                if (!notifyResult.IsSuccess)
                {
                    // just log it, we're still fine if we can't notify the end user
                    _log.LogWarning("Failed to notify rescinded warning: {Reason}", notifyResult.Error.Message);
                }

                var deleteResult = await _warningService.DeleteWarningAsync(expiredWarning, ct);
                if (!deleteResult.IsSuccess)
                {
                    // skip to the next one if we can't rescind this
                    _log.LogWarning("Failed to rescind warning: {Reason}", deleteResult.Error.Message);
                    continue;
                }

                warningTransaction.Complete();
            }
        }

        private async Task RescindExpiredBansAsync(IReadOnlyList<Snowflake> joinedGuilds, CancellationToken ct)
        {
            var expiredBans = await _banService.GetExpiredBansAsync(ct);
            foreach (var expiredBan in expiredBans)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                // We'll use a transaction per warning to avoid timeouts
                using var banTransaction = new TransactionScope
                (
                    TransactionScopeOption.Required,
                    _transactionOptions,
                    TransactionScopeAsyncFlowOption.Enabled
                );

                if (!joinedGuilds.Contains(expiredBan.Server.DiscordID))
                {
                    var deleteOldResult = await _banService.DeleteBanAsync(expiredBan, ct);
                    if (!deleteOldResult.IsSuccess)
                    {
                        // skip to the next one if we can't delete this
                        _log.LogWarning("Failed to delete ban from database: {Reason}", deleteOldResult.Error.Message);
                        continue;
                    }

                    banTransaction.Complete();
                    continue;
                }

                var removeBan = await _guildAPI.RemoveGuildBanAsync
                (
                    expiredBan.Server.DiscordID,
                    expiredBan.User.DiscordID,
                    ct: ct
                );

                if (!removeBan.IsSuccess)
                {
                    if (removeBan.Error is RestResultError<RestError> dre)
                    {
                        switch (dre.Error.Code)
                        {
                            case MissingPermission:
                            {
                                // Don't save the results, but continue processing expirations
                                continue;
                            }
                            case UnknownBan:
                            {
                                // User is probably already unbanned, this is OK
                                break;
                            }
                            default:
                            {
                                _log.LogWarning("Failed to rescind ban: {Reason}", removeBan.Error.Message);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        _log.LogWarning("Failed to rescind ban: {Reason}", removeBan.Error.Message);
                        continue;
                    }
                }

                var notifyResult = await _loggingService.NotifyUserUnbannedAsync(expiredBan, null);
                if (!notifyResult.IsSuccess)
                {
                    // just log it, we're still fine if we can't notify the end user
                    _log.LogWarning("Failed to notify rescinded ban: {Reason}", notifyResult.Error.Message);
                }

                var deleteResult = await _banService.DeleteBanAsync(expiredBan, ct);
                if (!deleteResult.IsSuccess)
                {
                    // skip to the next one if we can't delete this
                    _log.LogWarning("Failed to delete ban from database: {Reason}", deleteResult.Error.Message);
                    continue;
                }

                banTransaction.Complete();
            }
        }

        private async IAsyncEnumerable<Result<Snowflake>> GetGuildsAsync
        (
            [EnumeratorCancellation] CancellationToken ct = default
        )
        {
            Optional<Snowflake> after = default;
            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    yield break;
                }

                var getGuilds = await _userAPI.GetCurrentUserGuildsAsync(after: after, ct: ct);
                if (!getGuilds.IsSuccess)
                {
                    yield break;
                }

                var retrievedGuilds = getGuilds.Entity;
                if (retrievedGuilds.Count == 0)
                {
                    break;
                }

                foreach (var retrievedGuild in retrievedGuilds)
                {
                    if (!retrievedGuild.ID.IsDefined(out var guildID))
                    {
                        continue;
                    }

                    yield return guildID;
                }

                after = getGuilds.Entity[^1].ID;
            }
        }
    }
}
