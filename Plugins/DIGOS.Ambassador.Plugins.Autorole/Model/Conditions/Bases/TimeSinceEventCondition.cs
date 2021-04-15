//
//  TimeSinceEventCondition.cs
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

namespace DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.Bases
{
    /// <summary>
    /// Represents an abstract condition requiring a set amount of time to have passed since an event.
    /// </summary>
    /// <typeparam name="TActualCondition">The actual condition.</typeparam>
    public abstract class TimeSinceEventCondition<TActualCondition> : AutoroleCondition
        where TActualCondition : TimeSinceEventCondition<TActualCondition>
    {
        /// <summary>
        /// Gets the required elapsed time.
        /// </summary>
        public TimeSpan RequiredTime { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSinceEventCondition{TActualCondition}"/> class.
        /// </summary>
        /// <param name="requiredTime">The required time.</param>
        protected TimeSinceEventCondition(TimeSpan requiredTime)
        {
            this.RequiredTime = requiredTime;
        }

        /// <inheritdoc />
        public override bool HasSameConditionsAs(IAutoroleCondition autoroleCondition)
        {
            if (autoroleCondition is not TActualCondition actualCondition)
            {
                return false;
            }

            return this.RequiredTime == actualCondition.RequiredTime;
        }
    }
}
