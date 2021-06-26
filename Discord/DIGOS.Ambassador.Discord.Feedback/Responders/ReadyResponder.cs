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
using DIGOS.Ambassador.Discord.Feedback.Services;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DIGOS.Ambassador.Discord.Feedback.Responders
{
    /// <summary>
    /// Responds to the <see cref="IReady"/> event, storing connection information in a singleton service.
    /// </summary>
    public class ReadyResponder : IResponder<IReady>
    {
        private readonly IDiscordRestOAuth2API _oauth2API;
        private readonly IdentityInformationService _identityInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadyResponder"/> class.
        /// </summary>
        /// <param name="identityInformation">The identity information service.</param>
        /// <param name="oauth2API">The OAuth2 API.</param>
        public ReadyResponder(IdentityInformationService identityInformation, IDiscordRestOAuth2API oauth2API)
        {
            _identityInformation = identityInformation;
            _oauth2API = oauth2API;
        }

        /// <inheritdoc/>
        public async Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = default)
        {
            _identityInformation.ID = gatewayEvent.User.ID;

            var getApplication = await _oauth2API.GetCurrentBotApplicationInformationAsync(ct);
            if (!getApplication.IsSuccess)
            {
                return Result.FromError(getApplication);
            }

            var application = getApplication.Entity;

            if (application.Owner is null)
            {
                return new GenericError("The application's owner was not present.");
            }

            _identityInformation.ApplicationID = application.ID;
            _identityInformation.OwnerID = application.Owner.ID.Value;

            return Result.FromSuccess();
        }
    }
}
