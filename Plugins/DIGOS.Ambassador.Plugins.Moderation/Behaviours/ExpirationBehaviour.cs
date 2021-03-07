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
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using DIGOS.Ambassador.Plugins.Core.Services;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Behaviours.Bases;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Rest.Results;
using Remora.Results;
using static Remora.Discord.API.Abstractions.Results.DiscordError;

namespace DIGOS.Ambassador.Plugins.Moderation.Behaviours
{
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
            var identityService = tickServices.GetRequiredService<IdentityInformationService>();

            var warningService = tickServices.GetRequiredService<WarningService>();
            var expiredWarnings = await warningService.GetExpiredWarningsAsync(ct);
            foreach (var expiredWarning in expiredWarnings)
            {
                if (ct.IsCancellationRequested)
                {
                    return Result.FromSuccess();
                }

                // We'll use a transaction per warning to avoid timeouts
                using var warningTransaction = new TransactionScope
                (
                    TransactionScopeOption.Required,
                    this.TransactionOptions,
                    TransactionScopeAsyncFlowOption.Enabled
                );

                var deleteResult = await warningService.DeleteWarningAsync(expiredWarning, ct);
                if (!deleteResult.IsSuccess)
                {
                    this.Log.LogWarning("Failed to rescind warning: {Reason}", deleteResult.Unwrap().Message);
                    return deleteResult;
                }

                var notifyResult = await loggingService.NotifyUserWarningRemovedAsync
                (
                    expiredWarning,
                    identityService.ID
                );

                if (!notifyResult.IsSuccess)
                {
                    this.Log.LogWarning("Failed to rescind warning: {Reason}", notifyResult.Unwrap().Message);
                    return notifyResult;
                }

                warningTransaction.Complete();
            }

            var banService = tickServices.GetRequiredService<BanService>();
            var guildAPI = tickServices.GetRequiredService<IDiscordRestGuildAPI>();
            var expiredBans = await banService.GetExpiredBansAsync(ct);
            foreach (var expiredBan in expiredBans)
            {
                if (ct.IsCancellationRequested)
                {
                    return Result.FromSuccess();
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
                    ct
                );

                if (!removeBan.IsSuccess)
                {
                    if (removeBan.Unwrap() is DiscordRestResultError dre)
                    {
                        switch (dre.DiscordError.Code)
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
                                this.Log.LogWarning("Failed to rescind ban: {Reason}", removeBan.Unwrap().Message);
                                return removeBan;
                            }
                        }
                    }
                    else
                    {
                        this.Log.LogWarning("Failed to rescind ban: {Reason}", removeBan.Unwrap().Message);
                        return removeBan;
                    }
                }

                var deleteResult = await banService.DeleteBanAsync(expiredBan, ct);
                if (!deleteResult.IsSuccess)
                {
                    this.Log.LogWarning("Failed to rescind ban: {Reason}", removeBan.Unwrap().Message);
                    return deleteResult;
                }

                var notifyResult = await loggingService.NotifyUserUnbannedAsync(expiredBan, identityService.ID);
                if (!notifyResult.IsSuccess)
                {
                    this.Log.LogWarning("Failed to rescind ban: {Reason}", removeBan.Unwrap().Message);
                    return notifyResult;
                }

                banTransaction.Complete();
            }

            return Result.FromSuccess();
        }
    }
}
