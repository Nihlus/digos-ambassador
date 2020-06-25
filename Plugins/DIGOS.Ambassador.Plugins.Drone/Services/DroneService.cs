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
using DIGOS.Ambassador.Core.Services.TransientState;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Plugins.Drone.Extensions;
using Discord;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Drone.Services
{
    /// <summary>
    /// Contains business logic for creating and generating drone characters.
    /// </summary>
    public class DroneService : AbstractTransientStateService
    {
        private readonly Random _random;
        private readonly CharacterDiscordService _characters;
        private readonly CharacterRoleService _characterRoles;
        private readonly ContentService _content;

        /// <summary>
        /// Initializes a new instance of the <see cref="DroneService"/> class.
        /// </summary>
        /// <param name="characters">The character service.</param>
        /// <param name="random">An entropy source.</param>
        /// <param name="content">The content service.</param>
        /// <param name="characterRoles">The role service.</param>
        public DroneService
        (
            CharacterDiscordService characters,
            Random random,
            ContentService content,
            CharacterRoleService characterRoles
        )
            : base(characters, characterRoles)
        {
            _characters = characters;
            _random = random;
            _content = content;
            _characterRoles = characterRoles;
        }

        /// <summary>
        /// Drones the given user, creating a randomized sharkdrone character for them and forcing them into that form.
        /// </summary>
        /// <param name="guildUser">The user to drone.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<Character>> DroneUserAsync(IGuildUser guildUser)
        {
            var createGeneratedIdentity = await GenerateDroneIdentityAsync(guildUser);
            if (!createGeneratedIdentity.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(createGeneratedIdentity);
            }

            var (name, nickname) = createGeneratedIdentity.Entity;
            var generatedCharacterResult = await _characters.CreateCharacterAsync
            (
                guildUser,
                name,
                _content.GetRandomDroneAvatarUri().ToString(),
                nickname,
                _content.GetRandomDroneSummary(),
                _content.GetRandomDroneDescription(),
                new FemininePronounProvider().Family
            );

            if (!generatedCharacterResult.IsSuccess)
            {
                return generatedCharacterResult;
            }

            var character = generatedCharacterResult.Entity;

            var droneRole = guildUser.Guild.Roles.FirstOrDefault
            (
                r => r.Name.Contains("Drone") || r.Name.Contains("Dronies")
            );

            if (!(droneRole is null))
            {
                var getCharacterRole = await _characterRoles.GetCharacterRoleAsync(droneRole);
                if (getCharacterRole.IsSuccess)
                {
                    var characterRole = getCharacterRole.Entity;
                    var setCharacterRole = await _characterRoles.SetCharacterRoleAsync
                    (
                        guildUser,
                        character,
                        characterRole
                    );

                    if (!setCharacterRole.IsSuccess)
                    {
                        return CreateEntityResult<Character>.FromError(setCharacterRole);
                    }
                }
            }

            var becomeCharacterResult = await _characters.MakeCharacterCurrentAsync(guildUser, character);
            if (!becomeCharacterResult.IsSuccess)
            {
                return CreateEntityResult<Character>.FromError(becomeCharacterResult);
            }

            return character;
        }

        /// <summary>
        /// Generates a drone name.
        /// </summary>
        /// <param name="guildUser">The Discord user.</param>
        /// <returns>The generated identity.</returns>
        private async Task<CreateEntityResult<(string Name, string Nickname)>> GenerateDroneIdentityAsync
        (
            IGuildUser guildUser
        )
        {
            var getCharacters = await _characters.GetUserCharactersAsync(guildUser);
            if (!getCharacters.IsSuccess)
            {
                return CreateEntityResult<(string Name, string Nickname)>.FromError(getCharacters);
            }

            var characters = getCharacters.Entity;
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
    }
}
