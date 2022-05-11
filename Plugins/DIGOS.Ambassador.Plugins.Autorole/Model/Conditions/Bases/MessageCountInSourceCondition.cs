//
//  MessageCountInSourceCondition.cs
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

using Remora.Rest.Core;

namespace DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.Bases;

/// <summary>
/// Represents a condition where a role would be assigned after a user posts a certain number of messages in a
/// given source location.
/// </summary>
/// <typeparam name="TActualCondition">The actual condition type.</typeparam>
public abstract class MessageCountInSourceCondition<TActualCondition> : AutoroleCondition
    where TActualCondition : MessageCountInSourceCondition<TActualCondition>
{
    /// <summary>
    /// Gets the Discord ID of the message source.
    /// </summary>
    public Snowflake SourceID { get; internal set; }

    /// <summary>
    /// Gets the required number of messages.
    /// </summary>
    public long RequiredCount { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageCountInSourceCondition{TActualCondition}"/> class.
    /// </summary>
    /// <param name="sourceID">The source ID.</param>
    /// <param name="requiredCount">The required message count.</param>
    protected MessageCountInSourceCondition(Snowflake sourceID, long requiredCount)
    {
        this.SourceID = sourceID;
        this.RequiredCount = requiredCount;
    }

    /// <inheritdoc />
    public override bool HasSameConditionsAs(IAutoroleCondition autoroleCondition)
    {
        if (autoroleCondition is not TActualCondition channelCondition)
        {
            return false;
        }

        return this.SourceID == channelCondition.SourceID &&
               this.RequiredCount == channelCondition.RequiredCount;
    }
}
