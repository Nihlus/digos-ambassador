//
//  ReadyResponder.cs
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
using DIGOS.Ambassador.Plugins.Core.Services;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Core.Responders
{
    /// <summary>
    /// Responds to the <see cref="IReady"/> event, storing connection information in a singleton service.
    /// </summary>
    public class ReadyResponder : IResponder<IReady>
    {
        private readonly IdentityInformationService _identityInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadyResponder"/> class.
        /// </summary>
        /// <param name="identityInformation">The identity information service.</param>
        public ReadyResponder(IdentityInformationService identityInformation)
        {
            _identityInformation = identityInformation;
        }

        /// <inheritdoc/>
        public Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = default)
        {
            _identityInformation.ID = gatewayEvent.User.ID;
            return Task.FromResult(Result.FromSuccess());
        }
    }
}
