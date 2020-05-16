//
//  MessageCountInChannelCondition.cs
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
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.Bases;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using Discord;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace DIGOS.Ambassador.Plugins.Autorole.Model.Conditions
{
    /// <summary>
    /// Represents a condition that requires a certain number of messages in a specific channel.
    /// </summary>
    public class MessageCountInChannelCondition : MessageCountInSourceCondition<MessageCountInChannelCondition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCountInChannelCondition"/> class.
        /// </summary>
        /// <param name="sourceID">The source ID.</param>
        /// <param name="requiredCount">The required message count.</param>
        [UsedImplicitly]
        protected MessageCountInChannelCondition(long sourceID, long requiredCount)
            : base(sourceID, requiredCount)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCountInChannelCondition"/> class.
        /// </summary>
        /// <param name="textChannel">The source guild.</param>
        /// <param name="requiredCount">The required number of messages.</param>
        public MessageCountInChannelCondition(ITextChannel textChannel, long requiredCount)
            : this((long)textChannel.Id, requiredCount)
        {
        }

        /// <inheritdoc />
        public override string GetDescriptiveUIText()
        {
            return $"{this.RequiredCount} messages in {MentionUtils.MentionChannel((ulong)this.SourceID)}";
        }

        /// <inheritdoc/>
        public override async Task<bool> IsConditionFulfilledForUser(IServiceProvider services, IGuildUser discordUser)
        {
            var statistics = services.GetRequiredService<UserStatisticsService>();

            var channel = await discordUser.Guild.GetTextChannelAsync((ulong)this.SourceID);
            if (channel is null)
            {
                // TODO: Maybe we should throw here instead, or return a monad?
                return false;
            }

            var getUserStatistics = await statistics.GetOrCreateUserChannelStatisticsAsync(discordUser, channel);
            if (!getUserStatistics.IsSuccess)
            {
                // TODO: Same here...
                return false;
            }

            var userStatistics = getUserStatistics.Entity;

            return userStatistics.MessageCount.HasValue && userStatistics.MessageCount >= this.RequiredCount;
        }
    }
}
