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
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Autorole.Model.Conditions;

/// <summary>
/// Represents a required reaction to a message.
/// </summary>
public class ReactionCondition : AutoroleCondition
{
    /// <summary>
    /// Gets the ID of the Discord channel that the message is in.
    /// </summary>
    public Snowflake ChannelID { get; private set; }

    /// <summary>
    /// Gets the ID of the Discord message.
    /// </summary>
    public Snowflake MessageID { get; internal set; }

    /// <summary>
    /// Gets the name of the required emote.
    /// </summary>
    public string EmoteName { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactionCondition"/> class.
    /// </summary>
    [Obsolete("Required by EF Core.")]
    protected ReactionCondition()
    {
        this.EmoteName = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactionCondition"/> class.
    /// </summary>
    /// <param name="channelID">The channel ID.</param>
    /// <param name="messageID">The message ID.</param>
    /// <param name="emoteName">The name of the emote.</param>
    [UsedImplicitly]
    protected ReactionCondition(Snowflake channelID, Snowflake messageID, string emoteName)
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
    public ReactionCondition(IMessage message, IEmoji emote)
        : this
        (
            message.ChannelID,
            message.ID,
            emote.Name ?? emote.ID.ToString() ?? throw new InvalidOperationException()
        )
    {
    }

    /// <inheritdoc />
    public override string GetDescriptiveUIText()
    {
        return $"Has reacted to {this.MessageID} in <#{this.ChannelID}> " +
               $"with :{this.EmoteName}:";
    }

    /// <inheritdoc />
    public override bool HasSameConditionsAs(IAutoroleCondition autoroleCondition)
    {
        if (autoroleCondition is not ReactionCondition reactionCondition)
        {
            return false;
        }

        return this.ChannelID == reactionCondition.ChannelID &&
               this.MessageID == reactionCondition.MessageID &&
               this.EmoteName == reactionCondition.EmoteName;
    }

    /// <inheritdoc/>
    public override async Task<Result<bool>> IsConditionFulfilledForUserAsync
    (
        IServiceProvider services,
        Snowflake guildID,
        Snowflake userID,
        CancellationToken ct = default
    )
    {
        var channelAPI = services.GetRequiredService<IDiscordRestChannelAPI>();
        Optional<Snowflake> lastUser = default;
        while (true)
        {
            var getReactions = await channelAPI.GetReactionsAsync
            (
                this.ChannelID,
                this.MessageID,
                this.EmoteName,
                after: lastUser,
                ct: ct
            );

            if (!getReactions.IsSuccess)
            {
                return Result<bool>.FromError(getReactions);
            }

            var users = getReactions.Entity;
            if (users.Count == 0)
            {
                break;
            }

            if (users.Any(u => u.ID == userID))
            {
                return true;
            }

            lastUser = users[^1].ID;
        }

        return false;
    }
}
