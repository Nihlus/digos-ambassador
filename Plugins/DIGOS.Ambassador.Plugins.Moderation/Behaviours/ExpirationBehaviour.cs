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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Behaviours.Bases;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Rest.Results;
using Remora.Results;
using static Remora.Discord.API.Abstractions.Results.DiscordError;

namespace DIGOS.Ambassador.Plugins.Moderation.Behaviours;

/// <summary>
/// Rescinds expired warnings and bans.
/// </summary>
[UsedImplicitly]
internal sealed class ExpirationBehaviour : ContinuousBehaviour<ExpirationBehaviour>
{
    /// <inheritdoc/>
    protected override bool UseTransaction => false;

    /// <inheritdoc/>
    protected override TimeSpan TickDelay => TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpirationBehaviour"/> class.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="logger">The logging instance for this type.</param>
    public ExpirationBehaviour
    (
        IServiceProvider services,
        ILogger<ExpirationBehaviour> logger
    )
        : base(services, logger)
    {
    }

    /// <inheritdoc/>
    protected override async Task<Result> OnTickAsync(CancellationToken ct, IServiceProvider tickServices)
    {
        var loggingService = tickServices.GetRequiredService<ChannelLoggingService>();
        var guildAPI = tickServices.GetRequiredService<IDiscordRestGuildAPI>();
        var userAPI = tickServices.GetRequiredService<IDiscordRestUserAPI>();

        var joinedGuilds = new List<Snowflake>();
        await foreach (var get in GetGuildsAsync(userAPI, ct))
        {
            if (get.IsSuccess)
            {
                joinedGuilds.Add(get.Entity);
            }
        }

        var getSelf = await userAPI.GetCurrentUserAsync(ct);
        if (!getSelf.IsSuccess)
        {
            return Result.FromError(getSelf);
        }

        var self = getSelf.Entity;

        var warningService = tickServices.GetRequiredService<WarningService>();
        var expiredWarnings = await warningService.GetExpiredWarningsAsync(ct);

        foreach (var expiredWarning in expiredWarnings)
        {
            if (ct.IsCancellationRequested)
            {
                return Result.FromSuccess();
            }

            if (!joinedGuilds.Contains(expiredWarning.Server.DiscordID))
            {
                continue;
            }

            // We'll use a transaction per warning to avoid timeouts
            using var warningTransaction = new TransactionScope
            (
                TransactionScopeOption.Required,
                this.TransactionOptions,
                TransactionScopeAsyncFlowOption.Enabled
            );

            var notifyResult = await loggingService.NotifyUserWarningRemovedAsync
            (
                expiredWarning,
                self.ID
            );

            if (!notifyResult.IsSuccess)
            {
                this.Log.LogWarning("Failed to rescind warning: {Reason}", notifyResult.Error.Message);
                return notifyResult;
            }

            var deleteResult = await warningService.DeleteWarningAsync(expiredWarning, ct);
            if (!deleteResult.IsSuccess)
            {
                this.Log.LogWarning("Failed to rescind warning: {Reason}", deleteResult.Error.Message);
                return deleteResult;
            }

            warningTransaction.Complete();
        }

        var banService = tickServices.GetRequiredService<BanService>();
        var expiredBans = await banService.GetExpiredBansAsync(ct);
        foreach (var expiredBan in expiredBans)
        {
            if (ct.IsCancellationRequested)
            {
                return Result.FromSuccess();
            }

            if (!joinedGuilds.Contains(expiredBan.Server.DiscordID))
            {
                continue;
            }

            // We'll use a transaction per warning to avoid timeouts
            using var banTransaction = new TransactionScope
            (
                TransactionScopeOption.Required,
                this.TransactionOptions,
                TransactionScopeAsyncFlowOption.Enabled
            );

            var removeBan = await guildAPI.RemoveGuildBanAsync
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
                            this.Log.LogWarning("Failed to rescind ban: {Reason}", removeBan.Error.Message);
                            return removeBan;
                        }
                    }
                }
                else
                {
                    this.Log.LogWarning("Failed to rescind ban: {Reason}", removeBan.Error.Message);
                    return removeBan;
                }
            }

            var notifyResult = await loggingService.NotifyUserUnbannedAsync(expiredBan, self.ID);
            if (!notifyResult.IsSuccess)
            {
                this.Log.LogWarning("Failed to rescind ban: {Reason}", notifyResult.Error.Message);
                return notifyResult;
            }

            var deleteResult = await banService.DeleteBanAsync(expiredBan, ct);
            if (!deleteResult.IsSuccess)
            {
                this.Log.LogWarning("Failed to rescind ban: {Reason}", deleteResult.Error.Message);
                return deleteResult;
            }

            banTransaction.Complete();
        }

        return Result.FromSuccess();
    }

    private static async IAsyncEnumerable<Result<Snowflake>> GetGuildsAsync
    (
        IDiscordRestUserAPI userAPI,
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

            var getGuilds = await userAPI.GetCurrentUserGuildsAsync(after: after, ct: ct);
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
