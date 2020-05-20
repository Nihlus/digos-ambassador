//
//  DroneService.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Drone.Extensions;
using Discord;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Drone.Services
{
    /// <summary>
    /// Contains business logic for creating and generating drone characters.
    /// </summary>
    public class DroneService
    {
        private readonly Random _random;
        private readonly UserService _users;
        private readonly CharacterService _characters;
        private readonly ContentService _content;

        /// <summary>
        /// Initializes a new instance of the <see cref="DroneService"/> class.
        /// </summary>
        /// <param name="characters">The character service.</param>
        /// <param name="random">An entropy source.</param>
        /// <param name="users">The user service.</param>
        /// <param name="content">The content service.</param>
        public DroneService(CharacterService characters, Random random, UserService users, ContentService content)
        {
            _characters = characters;
            _random = random;
            _users = users;
            _content = content;
        }

        /// <summary>
        /// Drones the given user, creating a randomized sharkdrone character for them and forcing them into that form.
        /// </summary>
        /// <param name="guildUser">The user to drone.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<Character>> DroneUserAsync(IGuildUser guildUser)
        {
            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(getUser);
            }

            var user = getUser.Entity;

            var generatedIdentity = await GenerateDroneIdentityAsync(user, guildUser);
            var generatedAppearance = _content.GetRandomDroneAvatarUri();

            throw new NotImplementedException();
        }

        /// <summary>
        /// Generates a drone name.
        /// </summary>
        /// <param name="user">The database user.</param>
        /// <param name="guildUser">The Discord user.</param>
        /// <returns>The generated identity.</returns>
        private async Task<(string Name, string Nickname)> GenerateDroneIdentityAsync(User user, IGuildUser guildUser)
        {
            var characters = _characters.GetUserCharacters(user, guildUser.Guild);
            var characterNames = await characters.Select(c => c.Name).ToListAsync();

            string? characterName;
            string? displayName;
            while (true)
            {
                var serialNumber = _random.Next(0, 9999);
                var firstLetter = guildUser.Username.First();

                var generatedName = $"sharkdrone-{char.ToLowerInvariant(firstLetter)}{serialNumber}";
                if (characterNames.Contains(generatedName))
                {
                    continue;
                }

                characterName = generatedName;
                displayName = $"D-V: {char.ToUpperInvariant(firstLetter)}{serialNumber:D4}";

                break;
            }

            return (characterName, displayName);
        }

        /// <summary>
        /// Gets a random message from a selection of messages where Amby acquiesces someone's desire to drone
        /// themselves.
        /// </summary>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<string>> GetRandomSelfDroneMessageAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a random message from a selection of messages where Amby turns the tables on a would-be droner.
        /// </summary>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<string>> GetRandomTurnTheTablesMessageAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a random message from a selection of messages where a person has just been droned.
        /// </summary>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<string>> GetRandomConfirmationMessageAsync()
        {
            throw new NotImplementedException();
        }
    }
}
