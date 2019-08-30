//
//  InteractivityServiceExtensions.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity.Messages;
using Discord;
using Discord.Commands;
using Discord.Net;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Discord.Interactivity
{
    /// <summary>
    /// Extensions for the <see cref="InteractivityService"/> class.
    /// </summary>
    public static class InteractivityServiceExtensions
    {
        /// <summary>
        /// Sends a paginated message to the context user's direct messaging channel, alerting them if they are
        /// not already in it.
        /// </summary>
        /// <param name="this">The interactive service.</param>
        /// <param name="context">The command context.</param>
        /// <param name="feedback">The feedback service to use.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>The message that was sent.</returns>
        public static async Task SendPrivateInteractiveMessageAsync
        (
            [NotNull] this InteractivityService @this,
            [NotNull] ICommandContext context,
            [NotNull] UserFeedbackService feedback,
            [NotNull] InteractiveMessage message
        )
        {
            var userChannel = await context.User.GetOrCreateDMChannelAsync();
            try
            {
                var eb = feedback.CreateFeedbackEmbed(context.User, Color.DarkPurple, "Loading...");
                await feedback.SendEmbedAndDeleteAsync(userChannel, eb, TimeSpan.FromSeconds(1));

                if (!(context.Channel is IDMChannel))
                {
                    await feedback.SendConfirmationAsync(context, "Please check your private messages.");
                }

                await @this.SendInteractiveMessageAsync(userChannel, message);
            }
            catch (HttpException hex)
            {
                if (hex.WasCausedByDMsNotAccepted())
                {
                    await feedback.SendWarningAsync
                    (
                        context,
                        "You don't accept DMs from non-friends on this server, so I'm unable to do that."
                    );
                }

                throw;
            }
        }

        /// <summary>
        /// Sends a paginated message to the context user's direct messaging channel, alerting them if they are
        /// not already in it, and deletes it after a certain timeout. Defaults to 5 minutes.
        /// </summary>
        /// <param name="this">The interactive service.</param>
        /// <param name="context">The command context.</param>
        /// <param name="feedback">The feedback service to use.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="timeout">The timeout after which the embed will be deleted. Defaults to 5 minutes.</param>
        /// <returns>The message that was sent.</returns>
        public static async Task SendPrivateInteractiveMessageAndDeleteAsync
        (
            [NotNull] this InteractivityService @this,
            [NotNull] ICommandContext context,
            [NotNull] UserFeedbackService feedback,
            [NotNull] InteractiveMessage message,
            [CanBeNull] TimeSpan? timeout = null
        )
        {
            timeout = timeout ?? TimeSpan.FromMinutes(5.0);

            var userChannel = await context.User.GetOrCreateDMChannelAsync();
            try
            {
                var eb = feedback.CreateFeedbackEmbed(context.User, Color.DarkPurple, "Loading...");
                await feedback.SendEmbedAndDeleteAsync(userChannel, eb, TimeSpan.FromSeconds(1));

                if (!(context.Channel is IDMChannel))
                {
                    await feedback.SendConfirmationAsync(context, "Please check your private messages.");
                }

                await @this.SendInteractiveMessageAndDeleteAsync(userChannel, message, timeout);
            }
            catch (HttpException hex)
            {
                if (hex.WasCausedByDMsNotAccepted())
                {
                    await feedback.SendWarningAsync
                    (
                        context,
                        "You don't accept DMs from non-friends on this server, so I'm unable to do that."
                    );
                }

                throw;
            }
        }
    }
}
