//
// ReactionCondition.cs
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
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.Bases;
using Discord;
using JetBrains.Annotations;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Autorole.Model.Conditions
{
    /// <summary>
    /// Represents a required reaction to a message.
    /// </summary>
    [PublicAPI]
    public class ReactionCondition : AutoroleCondition
    {
        /// <summary>
        /// Gets the ID of the Discord channel that the message is in.
        /// </summary>
        public long ChannelID { get; private set; }

        /// <summary>
        /// Gets the ID of the Discord message.
        /// </summary>
        public long MessageID { get; internal set; }

        /// <summary>
        /// Gets the name of the required emote.
        /// </summary>
        public string EmoteName { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReactionCondition"/> class.
        /// </summary>
        /// <param name="channelID">The channel ID.</param>
        /// <param name="messageID">The message ID.</param>
        /// <param name="emoteName">The name of the emote.</param>
        [UsedImplicitly]
        protected ReactionCondition(long channelID, long messageID, string emoteName)
        {
            this.ChannelID = channelID;
            this.MessageID = messageID;
            this.EmoteName = emoteName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReactionCondition"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="emote">The required reaction.</param>
        public ReactionCondition(IMessage message, IEmote emote)
            : this((long)message.Channel.Id, (long)message.Id, emote.Name)
        {
        }

        /// <inheritdoc />
        public override string GetDescriptiveUIText()
        {
            return $"Has reacted to {this.MessageID} in {MentionUtils.MentionChannel((ulong)this.ChannelID)} " +
                   $"with {this.EmoteName}";
        }

        /// <inheritdoc />
        public override bool HasSameConditionsAs(IAutoroleCondition autoroleCondition)
        {
            if (!(autoroleCondition is ReactionCondition reactionCondition))
            {
                return false;
            }

            return this.ChannelID == reactionCondition.ChannelID &&
                   this.MessageID == reactionCondition.MessageID &&
                   this.EmoteName == reactionCondition.EmoteName;
        }

        /// <inheritdoc/>
        public override async Task<RetrieveEntityResult<bool>> IsConditionFulfilledForUserAsync
        (
            IServiceProvider services,
            IGuildUser discordUser,
            CancellationToken ct = default
        )
        {
            var channel = await discordUser.Guild.GetTextChannelAsync((ulong)this.ChannelID);
            if (channel is null)
            {
                return RetrieveEntityResult<bool>.FromError("Failed to find the channel.");
            }

            var message = await channel.GetMessageAsync((ulong)this.MessageID);
            if (message is null)
            {
                return RetrieveEntityResult<bool>.FromError("Failed to find the message.");
            }

            var reactions = message.Reactions;
            var emojiKey = reactions.Keys.FirstOrDefault(k => k.Name == this.EmoteName);
            if (emojiKey is null)
            {
                // Nobody's reacted with this emoji
                return false;
            }

            var reactionData = reactions[emojiKey];
            var reactionUsers = message.GetReactionUsersAsync(emojiKey, reactionData.ReactionCount);
            await foreach (var userBatch in reactionUsers.WithCancellation(ct))
            {
                if (userBatch.Any(user => user.Id == discordUser.Id))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
