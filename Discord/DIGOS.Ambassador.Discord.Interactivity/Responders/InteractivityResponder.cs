//
//  InteractivityResponder.cs
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

using System.Threading;
using System.Threading.Tasks;
using Remora.Results;

namespace DIGOS.Ambassador.Discord.Interactivity.Responders
{
    /// <summary>
    /// Acts as a base class for interactivity responders.
    /// </summary>
    public abstract class InteractivityResponder
    {
        /// <summary>
        /// Gets the interactivity service.
        /// </summary>
        protected InteractivityService Interactivity { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractivityResponder"/> class.
        /// </summary>
        /// <param name="interactivity">The interactivity service.</param>
        protected InteractivityResponder(InteractivityService interactivity)
        {
            this.Interactivity = interactivity;
        }

        /// <summary>
        /// Called by the interactivity service when a new interactive entity is created.
        /// </summary>
        /// <param name="nonce">The nonce that identifies the entity.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        public abstract Task<Result> OnCreateAsync(string nonce, CancellationToken ct = default);
    }
}
