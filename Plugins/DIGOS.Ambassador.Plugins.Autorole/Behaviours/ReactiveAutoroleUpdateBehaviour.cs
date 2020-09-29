//
//  ReactiveAutoroleUpdateBehaviour.cs
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
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Autorole.Model;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions;
using DIGOS.Ambassador.Plugins.Autorole.Results;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Discord.Behaviours;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Autorole.Behaviours
{
    /// <summary>
    /// A reactive behaviour that updates autoroles in response to events.
    /// </summary>
    [UsedImplicitly]
    public class ReactiveAutoroleUpdateBehaviour : ClientEventBehaviour<ReactiveAutoroleUpdateBehaviour>
    {
        private readonly UserFeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReactiveAutoroleUpdateBehaviour"/> class.
        /// </summary>
        /// <param name="client">The Discord client.</param>
        /// <param name="services">The services.</param>
        /// <param name="logger">The service's logging instance.</param>
        /// <param name="feedback">The user feedback service.</param>
        public ReactiveAutoroleUpdateBehaviour
        (
            DiscordSocketClient client,
            IServiceProvider services,
            ILogger<ReactiveAutoroleUpdateBehaviour> logger,
            UserFeedbackService feedback
        )
            : base(client, services, logger)
        {
            _feedback = feedback;
        }

        /// <inheritdoc />
        protected override async Task<OperationResult> MessageReceivedAsync(SocketMessage message)
        {
            using var eventScope = this.Services.CreateScope();
            var autoroles = eventScope.ServiceProvider.GetRequiredService<AutoroleService>();
            var autoroleUpdates = eventScope.ServiceProvider.GetRequiredService<AutoroleUpdateService>();

            if (!(message.Channel is ITextChannel textChannel))
            {
                return OperationResult.FromSuccess();
            }

            if (!(message.Author is IGuildUser guildUser))
            {
                return OperationResult.FromSuccess();
            }

            var guildAutoroles = (await autoroles.GetAutorolesAsync(textChannel.Guild)).ToList();
            if (guildAutoroles.Count == 0)
            {
                return OperationResult.FromSuccess();
            }

            var relevantAutoroles = guildAutoroles.Where
            (
                a => a.Conditions.Any
                (
                    c =>
                    {
                        if (!(c is MessageCountInChannelCondition channelCondition))
                        {
                            return c is MessageCountInGuildCondition;
                        }

                        if (message.Channel.Id == (ulong)channelCondition.SourceID)
                        {
                            return false;
                        }

                        return false;
                    }
                )
            ).ToList();

            if (relevantAutoroles.Count == 0)
            {
                return OperationResult.FromSuccess();
            }

            return await UpdateRelevantAutorolesForUserAsync
            (
                autoroles,
                autoroleUpdates,
                relevantAutoroles.ToAsyncEnumerable(),
                guildUser
            );
        }

        /// <inheritdoc/>
        protected override async Task<OperationResult> ReactionsClearedAsync
        (
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel
        )
        {
            using var eventScope = this.Services.CreateScope();
            var autoroles = eventScope.ServiceProvider.GetRequiredService<AutoroleService>();
            var autoroleUpdates = eventScope.ServiceProvider.GetRequiredService<AutoroleUpdateService>();

            var realMessage = await message.GetOrDownloadAsync();

            if (!(realMessage.Author is IGuildUser guildUser))
            {
                return OperationResult.FromSuccess();
            }

            var relevantAutoroles = GetRelevantReactionAutoroles(autoroles, message);
            return await UpdateRelevantAutorolesForUserAsync(autoroles, autoroleUpdates, relevantAutoroles, guildUser);
        }

        /// <inheritdoc/>
        protected override async Task<OperationResult> ReactionRemovedAsync
        (
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction
        )
        {
            using var eventScope = this.Services.CreateScope();
            var autoroles = eventScope.ServiceProvider.GetRequiredService<AutoroleService>();
            var autoroleUpdates = eventScope.ServiceProvider.GetRequiredService<AutoroleUpdateService>();

            var realMessage = await message.GetOrDownloadAsync();

            if (!(realMessage.Author is IGuildUser guildUser))
            {
                return OperationResult.FromSuccess();
            }

            var relevantAutoroles = GetRelevantReactionAutoroles(autoroles, message, reaction);
            return await UpdateRelevantAutorolesForUserAsync(autoroles, autoroleUpdates, relevantAutoroles, guildUser);
        }

        /// <inheritdoc />
        protected override async Task<OperationResult> ReactionAddedAsync
        (
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction
        )
        {
            using var eventScope = this.Services.CreateScope();
            var autoroles = eventScope.ServiceProvider.GetRequiredService<AutoroleService>();
            var autoroleUpdates = eventScope.ServiceProvider.GetRequiredService<AutoroleUpdateService>();

            var realMessage = await message.GetOrDownloadAsync();

            if (!(realMessage.Author is IGuildUser guildUser))
            {
                return OperationResult.FromSuccess();
            }

            var relevantAutoroles = GetRelevantReactionAutoroles(autoroles, message, reaction);
            return await UpdateRelevantAutorolesForUserAsync(autoroles, autoroleUpdates, relevantAutoroles, guildUser);
        }

        private async IAsyncEnumerable<AutoroleConfiguration> GetRelevantReactionAutoroles
        (
            AutoroleService autoroles,
            Cacheable<IUserMessage, ulong> message,
            SocketReaction? reaction = null
        )
        {
            var realMessage = await message.GetOrDownloadAsync();

            if (!(realMessage.Channel is ITextChannel textChannel))
            {
                yield break;
            }

            var guildAutoroles = (await autoroles.GetAutorolesAsync(textChannel.Guild)).ToList();
            if (guildAutoroles.Count == 0)
            {
                yield break;
            }

            foreach (var autorole in guildAutoroles)
            {
                var isRelevant = autorole.Conditions.Any
                (
                    c =>
                    {
                        if (!(c is ReactionCondition reactionCondition))
                        {
                            return false;
                        }

                        if (reactionCondition.MessageID != (long)realMessage.Id)
                        {
                            return false;
                        }

                        if (reaction is null)
                        {
                            return true;
                        }

                        if (reactionCondition.EmoteName != reaction.Emote.Name)
                        {
                            return false;
                        }

                        return true;
                    }
                );

                if (isRelevant)
                {
                    yield return autorole;
                }
            }
        }

        private async Task<OperationResult> UpdateRelevantAutorolesForUserAsync
        (
            AutoroleService autoroles,
            AutoroleUpdateService autoroleUpdates,
            IAsyncEnumerable<AutoroleConfiguration> relevantAutoroles,
            IGuildUser guildUser
        )
        {
            await foreach (var relevantAutorole in relevantAutoroles)
            {
                var updateResult = await autoroleUpdates.UpdateAutoroleForUserAsync(relevantAutorole, guildUser);
                if (!updateResult.IsSuccess)
                {
                    this.Log.LogError(updateResult.Exception, updateResult.ErrorReason);
                    continue;
                }

                switch (updateResult.Status)
                {
                    case AutoroleUpdateStatus.RequiresAffirmation:
                    {
                        await NotifyUserNeedsAffirmation(autoroles, relevantAutorole, guildUser);
                        break;
                    }
                }
            }

            return OperationResult.FromSuccess();
        }

        private async Task NotifyUserNeedsAffirmation
        (
            AutoroleService autoroles,
            AutoroleConfiguration autorole,
            IGuildUser user
        )
        {
            var getAutoroleConfirmation = await autoroles.GetOrCreateAutoroleConfirmationAsync(autorole, user);
            if (!getAutoroleConfirmation.IsSuccess)
            {
                this.Log.LogError(getAutoroleConfirmation.Exception, getAutoroleConfirmation.ErrorReason);
                return;
            }

            var autoroleConfirmation = getAutoroleConfirmation.Entity;

            if (autoroleConfirmation.HasNotificationBeenSent)
            {
                return;
            }

            var getSettings = await autoroles.GetOrCreateServerSettingsAsync(user.Guild);
            if (!getSettings.IsSuccess)
            {
                this.Log.LogError(getSettings.Exception, getSettings.ErrorReason);
                return;
            }

            var settings = getSettings.Entity;

            var notificationChannelID = settings.AffirmationRequiredNotificationChannelID;
            if (notificationChannelID is null)
            {
                return;
            }

            var notificationChannel = await user.Guild.GetTextChannelAsync((ulong)notificationChannelID.Value);
            if (notificationChannel is null)
            {
                return;
            }

            var embed = _feedback.CreateEmbedBase()
                .WithTitle("Confirmation Required")
                .WithDescription
                (
                    $"{MentionUtils.MentionUser(user.Id)} has met the requirements for the " +
                    $"{MentionUtils.MentionRole((ulong)autorole.DiscordRoleID)} role.\n" +
                    $"\n" +
                    $"Use \"!at affirm {MentionUtils.MentionRole((ulong)autorole.DiscordRoleID)} " +
                    $"{MentionUtils.MentionUser(user.Id)}\" to affirm and give the user the role."
                )
                .WithColor(Color.Green);

            try
            {
                await _feedback.SendEmbedAsync(notificationChannel, embed.Build());

                autoroleConfirmation.HasNotificationBeenSent = true;
            }
            catch (HttpException hex) when (hex.WasCausedByMissingPermission())
            {
            }
        }
    }
}
