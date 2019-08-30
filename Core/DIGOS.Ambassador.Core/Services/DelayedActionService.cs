//
//  DelayedActionService.cs
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
using System.Collections.Concurrent;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Core.Services
{
    /// <summary>
    /// Handles queueing of things to do at a later time.
    /// </summary>
    public class DelayedActionService
    {
        /// <summary>
        /// Gets the currently running timeout tasks.
        /// </summary>
        [NotNull]
        internal ConcurrentQueue<Task> RunningTimeouts { get; }

        /// <summary>
        /// Gets the task factories for the given timeouts.
        /// </summary>
        [NotNull]
        internal ConcurrentDictionary<Task, Func<Task>> ScheduledTasks { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayedActionService"/> class.
        /// </summary>
        public DelayedActionService()
        {
            this.RunningTimeouts = new ConcurrentQueue<Task>();
            this.ScheduledTasks = new ConcurrentDictionary<Task, Func<Task>>();
        }

        /// <summary>
        /// Schedules an action to be performed at an arbitrary time in the future.
        /// </summary>
        /// <param name="task">The action to perform.</param>
        /// <param name="timeout">The time to delay its execution.</param>
        public void DelayUntil(Func<Task> task, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);

            this.RunningTimeouts.Enqueue(timeoutTask);

            // Add the task and its running timeout
            while (!this.ScheduledTasks.TryAdd(timeoutTask, task))
            {
            }
        }
    }
}
