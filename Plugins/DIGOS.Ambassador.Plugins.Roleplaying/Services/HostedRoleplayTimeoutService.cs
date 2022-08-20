//
//  HostedRoleplayTimeoutService.cs
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Services;

/// <summary>
/// Times out roleplays, stopping them if they've been inactive for more than a set time.
/// </summary>
[UsedImplicitly]
internal sealed class HostedRoleplayTimeoutService : BackgroundService
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostedRoleplayTimeoutService"/> class.
    /// </summary>
    /// <param name="services">The services.</param>
    public HostedRoleplayTimeoutService(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct))
        {
            await using var scope = _services.CreateAsyncScope();

            var scopedExpirationService = scope.ServiceProvider.GetRequiredService<ScopedRoleplayTimeoutService>();
            await scopedExpirationService.ExecuteAsync(ct);
        }
    }

    /// <summary>
    /// Handles scoped execution of the actual service code.
    /// </summary>
    internal sealed class ScopedRoleplayTimeoutService
    {
        private readonly ILogger<ScopedRoleplayTimeoutService> _log;
        private readonly FeedbackService _feedback;
        private readonly RoleplayDiscordService _roleplayService;

        private readonly TransactionOptions _transactionOptions = new()
        {
            Timeout = TransactionManager.DefaultTimeout,
            IsolationLevel = IsolationLevel.Serializable
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ScopedRoleplayTimeoutService"/> class.
        /// </summary>
        /// <param name="log">The logging instance for this type.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="roleplayService">The roleplaying service.</param>
        public ScopedRoleplayTimeoutService
        (
            ILogger<ScopedRoleplayTimeoutService> log,
            FeedbackService feedback,
            RoleplayDiscordService roleplayService
        )
        {
            _log = log;
            _feedback = feedback;
            _roleplayService = roleplayService;
        }

        /// <summary>
        /// Executes the service's workload.
        /// </summary>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            var timedOutRoleplays = await _roleplayService.QueryRoleplaysAsync
            (
                q => q
                    .Where(r => r.IsActive)
                    .Where(r => r.LastUpdated.HasValue)
                    .Where(r => DateTimeOffset.UtcNow - r.LastUpdated > TimeSpan.FromHours(72))
            );

            foreach (var roleplay in timedOutRoleplays)
            {
                ct.ThrowIfCancellationRequested();

                // We'll use a transaction per warning to avoid timeouts
                using var timeoutTransaction = new TransactionScope
                (
                    TransactionScopeOption.Required,
                    _transactionOptions,
                    TransactionScopeAsyncFlowOption.Enabled
                );

                var stopRoleplay = await _roleplayService.StopRoleplayAsync(roleplay);
                if (!stopRoleplay.IsSuccess)
                {
                    // ignore this for now
                    _log.LogWarning("Failed to stop the roleplay \"{Name}\"", roleplay.Name);
                    continue;
                }

                var notifyResult = await NotifyOwnerAsync(roleplay);
                if (!notifyResult.IsSuccess)
                {
                    // fine, continue
                    _log.LogWarning
                    (
                        "Failed to notify the owner of the roleplay \"{Name}\" that it timed out",
                        roleplay.Name
                    );
                }

                timeoutTransaction.Complete();
            }
        }

        /// <summary>
        /// Notifies the owner of the roleplay that it was stopped because it timed out.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<Result> NotifyOwnerAsync(Roleplay roleplay)
        {
            var notification = new Embed
            {
                Colour = _feedback.Theme.Secondary,
                Description = $"Due to inactivity, your roleplay \"{roleplay.Name}\" has been stopped.",
                Footer = new EmbedFooter($"You can restart it by running !rp start \"{roleplay.Name}\".")
            };

            var send = await _feedback.SendPrivateEmbedAsync(roleplay.Owner.DiscordID, notification);
            return send.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(send);
        }
    }
}
