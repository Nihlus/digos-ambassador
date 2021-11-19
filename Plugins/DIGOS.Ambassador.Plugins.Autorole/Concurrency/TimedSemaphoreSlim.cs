//
//  TimedSemaphoreSlim.cs
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

namespace DIGOS.Ambassador.Plugins.Autorole.Concurrency;

/// <summary>
/// Represents a semaphore and a timestamp.
/// </summary>
public class TimedSemaphoreSlim
{
    /// <summary>
    /// Gets the time when the semaphore was last used. Defaults to the time it was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>
    /// Gets the semaphore.
    /// </summary>
    public SemaphoreSlim Semaphore { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimedSemaphoreSlim"/> class.
    /// </summary>
    /// <param name="initialPool">The initial number of available slots.</param>
    /// <param name="maxPool">The maximum number of available slots.</param>
    public TimedSemaphoreSlim(int initialPool, int maxPool = 0)
    {
        this.Timestamp = DateTimeOffset.UtcNow;
        this.Semaphore = new SemaphoreSlim(initialPool, maxPool);
    }

    /// <summary>
    /// Updates the timestamp of the semaphore.
    /// </summary>
    public void UpdateTimestamp()
    {
        this.Timestamp = DateTimeOffset.UtcNow;
    }
}
